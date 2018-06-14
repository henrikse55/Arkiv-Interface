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
using System;
using System.IO;

namespace Arkiv.Controllers
{
    [Authorize]
    public class ArchiveController : Controller
    {
        #region Constructor
        private ISqlData sql;
        private Config config;

        public ArchiveController(ISqlData _data, Config _config)
        {
            sql = _data;
            config = _config;
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

            config.AdminGroups.ToList().ForEach(group => {
                if (User.IsInRole(group)) AdminPanelAccess = true;
            });

            config.AdminUsers.ToList().ForEach(user => {
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
            var groups = identity.Groups.Select(x =>
            {
                try
                {
                    return x.Translate(typeof(NTAccount)).Value;
                }
                catch (Exception e) { }
                return null;
            });
            var sorted = (from x in models where groups.Any(y => y == x.Group.Replace("\\\\", "\\")) select x.DEVI);

            string WhereClause = "WHERE ";
            List<(string, object)> ParamList = new List<(string, object)>();

            if (sorted.Count(x => x.Contains('*')) == 0)
            {
                for (int i = 0; i < sorted.Count(); i++)
                {
                    WhereClause += "DEVI = @DEVI" + i;
                    ParamList.Add(("@DEVI" + i, sorted.ElementAt(i)));
                } 
            }
            #endregion

            if (Filters.Count() > 0)
            {
                    if (sorted.Count() != 0)
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

            string OrderClause = "";

            if(OrderData.Order != null)
            {
                if(OrderData.Order.Equals("Descending"))
                {
                    OrderClause += " ORDER BY " + OrderData.Column + " DESC ";
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
                           WHERE t.RowNumber between {min} and {max}".Replace("{min}", min.ToString()).Replace("{max}", max.ToString()).Replace("{where}", WhereClause);

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
            MemoryStream item = await GetFileStream(doc);
            if (item != null)
            {
                sql.LogAsync("PDF Load", DateTime.Now, User.Identity.Name, doc);
                return new FileStreamResult(item, "application/pdf");
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
            MemoryStream item = await GetFileStream(doc);
            if (item != null)
            {
                sql.LogAsync("PDF Download", DateTime.Now, User.Identity.Name, doc);
                return new FileStreamResult(item, "application/octet-stream");
            }
            else
            {
                sql.LogAsync("PDF Download Failed", DateTime.Now, User.Identity.Name, doc);
                return NotFound();
            }
        }

        private async Task<MemoryStream> GetFileStream(string file)
        {
            var dir = Directory.EnumerateFiles(config.PdfPath);
            var item = dir.Where(x => x.Contains(file)).Select(x => x);
            if (item.Count() > 0)
            {
                using (var filestream = new FileStream(item.First(), FileMode.Open, FileAccess.Read, FileShare.Inheritable))
                {
                    MemoryStream s = new MemoryStream();
                    await filestream.CopyToAsync(s);
                    s.Position = 0;
                    return s;
                }
            }
            else
            {
                return null;
            }
        }

        #endregion


        #region Admin
        [HttpGet]
        public IActionResult Admin()
        {
            foreach(string group in config.AdminGroups)
            {
                if(User.IsInRole(group))
                {
                    return View();
                }
            }

            foreach(string user in config.AdminUsers)
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