using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Security.Principal;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Arkiv.Data.Filter;
using Arkiv.Models;
using System.Linq;
using Arkiv.Data;
using System.IO;
using System;

namespace Arkiv.Controllers
{
    [Authorize]
    public class ArchiveController : Controller
    {
        #region Constructor
        private ISqlData sql;
        private Config Configuration;

        public ArchiveController(ISqlData _data, Config _config)
        {
            sql = _data;
            Configuration = _config;
        }
        #endregion

        #region Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IEnumerable<ColumnNameModel> ColumnNames = await sql.SelectDataAsync<ColumnNameModel>("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS Where TABLE_NAME = 'arkiv'");

            List<SelectListItem> ColumnNamesSelectList = new List<SelectListItem>();
            
            foreach(ColumnNameModel column in ColumnNames)
            {
                ColumnNamesSelectList.Add(new SelectListItem { Value = column.COLUMN_NAME, Text = column.COLUMN_NAME });
            }

            //This section checks if the current user has access to the admin panel
            bool AdminPanelAccess = false;

            Configuration.AdminGroups.ToList().ForEach(group => {
                if (User.IsInRole(group)) AdminPanelAccess = true;
            });

            Configuration.AdminUsers.ToList().ForEach(user => {
                if (User.Identity.Name.Contains(user)) AdminPanelAccess = true;
            });

            return View(new ArchiveJoinedModel() { selectListItems = ColumnNamesSelectList.AsEnumerable(), data = null, adminPanelAccess = AdminPanelAccess });
        }

        [HttpPost]
        [Route("/Archive/GetTable")]
        public async Task<IActionResult> GetTable(FilterModel[] Filters, OrderDataModel OrderData, int pages)
        {
            IEnumerable<ColumnNameModel> ColumnNames = await sql.SelectDataAsync<ColumnNameModel>("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS Where TABLE_NAME = 'arkiv'");

            List<SelectListItem> ColumnNamesSelectList = new List<SelectListItem>();

            //Add every column from the database to list
            foreach (ColumnNameModel column in ColumnNames)
            {
                ColumnNamesSelectList.Add(new SelectListItem { Value = column.COLUMN_NAME, Text = column.COLUMN_NAME });
            }

            #region Active Directory account cheching
            IEnumerable<ActiveModel> models = await sql.SelectDataAsync<ActiveModel>("SELECT * FROM active");

            WindowsIdentity identity = WindowsIdentity.GetCurrent();

            IEnumerable<string> groups = identity.Groups.Select(group =>
            {
                try
                {
                    return group.Translate(typeof(NTAccount)).Value;
                }
                catch (Exception) { }
                return null;
            });

            IEnumerable<string> SortedModels = (from model in models where groups.Any(g => g == model.Group.Replace("\\\\", "\\")) select model.DEVI);

            string WhereClause = "WHERE ";
            List<(string, object)> ParamList = new List<(string, object)>();

            if (SortedModels.Count(x => x.Contains('*')) == 0)
            {
                for (int i = 0; i < SortedModels.Count(); i++)
                {
                    WhereClause += "DEVI = @DEVI" + i;
                    ParamList.Add(("@DEVI" + i, SortedModels.ElementAt(i)));
                } 
            }
            #endregion

            if (Filters.Count() > 0)
            {
                    if (SortedModels.Count() != 0)
                        WhereClause += " AND ";

                    foreach(FilterModel Filter in Filters)
                    {
                        switch(Filter.Type)
                        {
                            case "Single":
                                //Check if the current item is the last item
                                if(Filter.Equals(Filters[Filters.Length - 1]))
                                {
                                    WhereClause += Filter.Name + " = " + "@" + Filter.Value.One.Replace(" ", string.Empty) + "";
                                    ParamList.Add(("@" + Filter.Value.One.Replace(" ", string.Empty), Filter.Value.One));
                                }
                                else
                                {
                                    WhereClause += Filter.Name + " = " + "@" + Filter.Value.One.Replace(" ", string.Empty) + " AND ";
                                    ParamList.Add(("@" + Filter.Value.One.Replace(" ", string.Empty), Filter.Value.One));
                                }
                                break;
                            case "Range":
                                //Check if the current item is the last item
                                if (Filter.Equals(Filters[Filters.Length - 1]))
                                {
                                    WhereClause += Filter.Name + " BETWEEN " + "@" + Filter.Value.One.Replace(" ", string.Empty) + " AND " + "@" + Filter.Value.Two.Replace(" ", string.Empty) + "";
                                    ParamList.AddRange(new(string, object)[]
                                    {
                                        ("@" + Filter.Value.One.Replace(" ", string.Empty), Filter.Value.One),
                                        ("@" + Filter.Value.Two.Replace(" ", string.Empty), Filter.Value.Two)
                                    });
                                }
                                else
                                {
                                    WhereClause += Filter.Name + " BETWEEN " + "@" + Filter.Value.One.Replace(" ", string.Empty) + " AND " + "@" + Filter.Value.Two.Replace(" ", string.Empty) + " AND ";
                                    ParamList.AddRange(new(string, object)[]
                                    {
                                        ("@" + Filter.Value.One.Replace(" ", string.Empty), Filter.Value.One),
                                        ("@" + Filter.Value.Two.Replace(" ", string.Empty), Filter.Value.Two)
                                    });
                                }
                                break;
                        }
                    }
            } else
            {
                WhereClause = ""; //If there is no Filters, remove the WHERE keyword from WhereClause
            }

            string OrderByClause = "";

            if(OrderData.Order != null)
            {
                if(OrderData.Order.Equals("Descending"))
                {
                    OrderByClause += " ORDER BY " + OrderData.Column + " DESC ";
                }
            }

            if (pages == 0)
                pages = 1;

            int max = pages * 50;
            int min = (max - 50 >= 0 ? max-50 : 0);

            string query = @"SELECT 
                           [Id], [STAMP], [FILTYP], [MODE],
                           [DOCTYP], [DOCDATE], [DOCNO], [ORNO],
                           [IVDT], [IVNO], [CUNO], [CUNM],
                           [PYNO], [ORDT], [DUDT], [DLDT],
                           [YREF], [CUOR], [CUDT], [OREF],
                           [DLIX], [ADID], [ADNM], [ORTO],
                           [CUCD], [CONO], [FACI], [DEVI],
                           [WHLO], [PRTDATE], [MVXPRT], [TOMAIL],
                           [CCMAIL], [SUNM], [PUON], [PUDT], [PATH]
                           FROM(
                               SELECT *, ROW_NUMBER() OVER(ORDER BY Id) AS RowNumber
                           
                               FROM arkiv {where}
                           ) as t
                           WHERE t.RowNumber between {min} and {max} {order}"
                            .Replace("{min}", min.ToString())
                            .Replace("{max}", max.ToString())
                            .Replace("{where}", WhereClause)
                            .Replace("{order}", OrderByClause);

            string countQuery = "SELECT COUNT(Id) as cn FROM arkiv " +WhereClause;

            int pageCount = (int)(await sql.GetDataRawAsync(countQuery, ParamList.ToArray())).Rows[0][0];
            IEnumerable<ArchiveDataModel> data = await sql.SelectDataAsync<ArchiveDataModel>(query, ParamList.ToArray());

            //If there is no results, return the json object, "No Match"
            if (data.Count() == 0)
            {
                return Json("No Match");
            }

            return PartialView("TablePartial",new ArchiveJoinedModel() { selectListItems = ColumnNamesSelectList.AsEnumerable(), data = data, pages = pageCount });
        }

        [HttpPost]
        public IActionResult GetFilterPartial(string SelectedColumn)
        {
            return PartialView("FilterPartial", SelectedColumn);
        }
        #endregion

        #region PDF
        [HttpGet()]
        [Route("/pdf/{doc}")]
        public async Task<IActionResult> GetPdf(string doc)
        {
            MemoryStream PdfDocumentStream = await GetFileStream(doc);

            if (PdfDocumentStream != null)
            {
                sql.LogAsync("PDF Load", DateTime.Now, User.Identity.Name, doc);
                return new FileStreamResult(PdfDocumentStream, "application/pdf");
            }
            else
            {
                sql.LogAsync("PDF Load Failed", DateTime.Now, User.Identity.Name, doc);
                return NotFound();
            }
        }

        [HttpGet()]
        [Route("/download/{doc}")]
        public async Task<IActionResult> Download(string doc)
        {
            MemoryStream PdfDocumentStream = await GetFileStream(doc);

            if (PdfDocumentStream != null)
            {
                sql.LogAsync("PDF Download", DateTime.Now, User.Identity.Name, doc);
                return new FileStreamResult(PdfDocumentStream, "application/octet-stream");
            }
            else
            {
                sql.LogAsync("PDF Download Failed", DateTime.Now, User.Identity.Name, doc);
                return NotFound();
            }
        }

        private async Task<MemoryStream> GetFileStream(string document)
        {
            IEnumerable<string> PdfDirectory = Directory.EnumerateFiles(Configuration.PdfPath);
            IEnumerable<string> PdfDocumentPath = PdfDirectory.Where(dir => dir.Contains(document)).Select(doc => doc);

            if (PdfDocumentPath.Count() > 0)
            {
                using (FileStream filestream = new FileStream(PdfDocumentPath.First(), FileMode.Open, FileAccess.Read, FileShare.Inheritable))
                {
                    MemoryStream PdfDocumentStream = new MemoryStream();
                    await filestream.CopyToAsync(PdfDocumentStream);
                    PdfDocumentStream.Position = 0;

                    return PdfDocumentStream;
                }
            }

            return null;
        }
        #endregion


        #region Admin
        [HttpGet]
        public IActionResult Admin()
        {
            foreach(string group in Configuration.AdminGroups)
            {
                if(User.IsInRole(group))
                {
                    return View();
                }
            }

            foreach(string user in Configuration.AdminUsers)
            {
                if(User.Identity.Name.Contains(user))
                {
                    return View();
                }
            }

            return Redirect("Index");
        }
        #endregion

#if DEBUG
        #region Test
        [HttpGet]
        public IActionResult Test()
        {
            var test = new WindowsPrincipal(WindowsIdentity.GetCurrent());

            List<string> groups = new List<string>();

            foreach (var t in WindowsIdentity.GetCurrent().Groups)
                try
                {
                    groups.Add(t.Translate(typeof(NTAccount)).Value);
                }
                catch (Exception)
                {
                    groups.Add(t.Value);
                }

            return Json(new { isInGroup = test.IsInRole("Administrators"), User.Identity.Name, groups });
        }
        #endregion  
#endif
    }
}