using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Arkiv.Data.Filter;
using Arkiv.Models;
using System.Linq;
using Arkiv.Data;
using System.IO;
using System;
using System.DirectoryServices.AccountManagement;
using System.Text;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

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
            //
            //Get all columns that are not blacklisted, and put their names into 'ColumnNameSelectList'
            //
            IEnumerable<ColumnNameModel> ColumnNames = await sql.SelectDataAsync<ColumnNameModel>("SELECT COLUMNS.COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'arkiv' AND COLUMNS.COLUMN_NAME NOT IN (SELECT ColumnBlacklist.[Column] FROM ColumnBlacklist) ");
            List<SelectListItem> ColumnNamesSelectList = ColumnNames.Select(x => new SelectListItem() { Value = x.COLUMN_NAME, Text = x.COLUMN_NAME }).ToList();

            //
            //Checks if the current user has access to the admin panel
            //
            bool AdminPanelAccess = false;

            Configuration.AdminGroups.ToList().ForEach(group => {
                if (IsInGroup(group)) AdminPanelAccess = true;
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
            //
            //Create log entry if a filter has been applied
            //
            if (Filters != null)
            {
                string parameters = JsonConvert.SerializeObject(new { Filters, OrderData });
                sql.LogAsync("Filter Applied", DateTime.Now, User.Identity.Name, parameters);
            }

            //
            //Get all columns that are not blacklisted, and put their names into 'ColumnNameSelectList'
            //
            IEnumerable<ColumnNameModel> ColumnNames = await sql.SelectDataAsync<ColumnNameModel>("SELECT COLUMNS.COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'arkiv' AND COLUMNS.COLUMN_NAME NOT IN (SELECT ColumnBlacklist.[Column] FROM ColumnBlacklist) ");
            List<SelectListItem> ColumnNamesSelectList = ColumnNames.Select(x => new SelectListItem() { Value = x.COLUMN_NAME, Text = x.COLUMN_NAME}).ToList();

            //
            //Gets all the DEVI values from the groups that the current user is a member of, and applies logic to SQL query accordingly
            //
            #region Active Directory account checking
            bool ClearWhereFlag = false;

            IEnumerable<ActiveModel> ActiveDirectoryGroupModel = await sql.SelectDataAsync<ActiveModel>("SELECT * FROM active");

            IEnumerable<string> Groups = GetUserGroups();

            IEnumerable<string> SortedGroups = (from model in ActiveDirectoryGroupModel where Groups.Any(g => g == model.Group) select model.DEVI);

            string WhereClause = "WHERE (";
            List<(string, object)> ParamList = new List<(string, object)>();

            if (SortedGroups.Count() > 0)
            {
                for (int i = 0; i < SortedGroups.Count(); i++)
                {
                    if (i < SortedGroups.Count() && i > 0)
                    {
                        WhereClause += " OR ";
                    }

                    WhereClause += "DEVI = @DEVI" + i;
                    ParamList.Add(("@DEVI" + i, SortedGroups.ElementAt(i)));
                }

                WhereClause += ")";
            }
            else
            {
                ClearWhereFlag = true;
            }
            #endregion

            //
            //Constructs the WhereClause
            //
            #region WhereClause
            if (Filters.Count() > 0)
            {
                if (SortedGroups.Count() != 0)
                    WhereClause += " AND ";

                object locker = new object();
                Filters.AsParallel().ForAll(Filter =>
                {
                    switch (Filter.Type)
                    {
                        case "Single":
                            //Check if the current item is the last item
                            lock (locker)
                            {
                                WhereClause += Filter.Name + " like " + "@" + Filter.Value.One.Replace(" ", string.Empty) + (!Filter.Equals(Filters.Last()) ? " AND " : "");
                            }

                            ParamList.Add(("@" + Filter.Value.One.Replace(" ", string.Empty), "%" + Filter.Value.One + "%")); 
                            break;
                        case "Range":
                            //Check if the current item is the last item
                            lock (locker)
                            {
                                WhereClause += Filter.Name + " BETWEEN " + "@" + Filter.Value.One.Replace(" ", string.Empty) + " AND " + "@" + Filter.Value.Two.Replace(" ", string.Empty) + (!Filter.Equals(Filters.Last()) ? " AND " : "");
                            }

                            ParamList.AddRange(new(string, object)[]
                            {
                                ("@" + Filter.Value.One.Replace(" ", string.Empty), Filter.Value.One),
                                ("@" + Filter.Value.Two.Replace(" ", string.Empty), Filter.Value.Two)
                            }); 
                            break;
                    }
                });

            }
            else if(ClearWhereFlag)
            {
                    WhereClause = ""; //If there is no Filters, remove the WHERE keyword from WhereClause
            }
            #endregion

            //
            //Constructs the OrderByClause
            //
            #region OrderByClause
            string OrderByClause = "";

            if(OrderData.Order != null)
            {
                if (OrderData.Order.Equals("Descending"))
                {
                    OrderByClause += " ORDER BY " + OrderData.Column + " DESC ";
                }
                else if(OrderData.Order.Equals("Ascending"))
                {
                    OrderByClause += " ORDER BY " + OrderData.Column + " ASC";
                }
            }
            #endregion

            //
            //Paging math
            //
            #region Paging
            if (pages == 0)
                pages = 1;

            int max = pages * 50;
            int min = (max - 50 >= 0 ? max-50 : 0);

            string countQuery = "SELECT COUNT(Id) as cn FROM arkiv " + WhereClause;

            int pageCount = (int)(await sql.GetDataRawAsync(countQuery, ParamList.ToArray())).Rows[0][0];
            #endregion

            //
            //Create a string containing all the names of the columns that are not blacklisted
            //
            #region ColumnString
            string ColumnString = string.Empty;
            IEnumerable<ColumnNameModel> ColumnNamesForQuery = await sql.SelectDataAsync<ColumnNameModel>("SELECT COLUMNS.COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'arkiv' AND COLUMNS.COLUMN_NAME NOT IN (SELECT ColumnBlacklist.[Column] FROM ColumnBlacklist) ");

            ColumnNamesForQuery.ToList().ForEach(column => {
                ColumnString += "[" + column.COLUMN_NAME + "]" + (column.COLUMN_NAME.Equals(ColumnNamesForQuery.ElementAt(ColumnNamesForQuery.Count() - 1).COLUMN_NAME) ? " " : ", ");

            });
            #endregion

            string query = @"SELECT 
                           {ColumnString}
                           FROM(
                               SELECT *, ROW_NUMBER() OVER(ORDER BY Id) AS RowNumber
                               FROM arkiv {where}
                           ) as t
                           WHERE t.RowNumber between {min} and {max} {order}"
                            .Replace("{min}", min.ToString())
                            .Replace("{max}", max.ToString())
                            .Replace("{where}", WhereClause)
                            .Replace("{order}", OrderByClause)
                            .Replace("{ColumnString}", ColumnString);

            IEnumerable<ArchiveDataModel> data = await sql.SelectDataAsync<ArchiveDataModel>(query, ParamList.ToArray());

            //
            //Add each available column to a Dictionary where the TKey is the Short form of the column name and the TValue is the full form
            //
            #region ColumnDictionary
            IEnumerable<ColumnDefinitionModel> ColumnDefinitions = await sql.SelectDataAsync<ColumnDefinitionModel>("SELECT * FROM ColumnDefinition");
            Dictionary<string, string> ColumnDictionary = new Dictionary<string, string>();

            ColumnNamesSelectList.ForEach(item =>
            {
                string value = item.Value;
                ColumnDictionary.Add(value, ColumnDefinitions.Any(x => x.ShortName.Equals(value)) ? ColumnDefinitions.Where(x => x.ShortName.Equals(value)).First().FullName : value);
            });
            #endregion

            //If there is no results, return the json object, "No Match"
            if (data.Count() == 0)
            {
                return Json("No Match");
            }

            return PartialView("TablePartial",new ArchiveJoinedModel() { selectListItems = ColumnNamesSelectList.AsEnumerable(), data = data, pages = pageCount, FullColumnNames = ColumnDictionary, ColumnsNotOnBlacklist = ColumnNamesForQuery });
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
                if(IsInGroup(group))
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

        #region AD Intergration
        private IEnumerable<string> GetUserGroups()
        {
            PrincipalContext context = new PrincipalContext(ContextType.Domain);
            try
            {
                UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, User.Identity.Name.Split("\\")[1]);

                if (userPrincipal != null)
                {
                    PrincipalSearchResult<Principal> groups = userPrincipal.GetAuthorizationGroups();
                    var names = groups.AsParallel().Where(x => x is GroupPrincipal).Select(x => x.Name ?? "");

                    return names;
                }
                return null;
            }catch(COMException)
            {
                return new List<string>();
            }
        }

        private bool IsInGroup(string user) => GetUserGroups().Any(x => x.Contains(user));
        #endregion

        #region Test
        [HttpGet]
        [Route("test/{group?}")]
        public IActionResult Test(string group)
        {
            if(group == null)
                group = "Administrators";

            PrincipalContext context = new PrincipalContext(ContextType.Domain);
            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, User.Identity.Name.Split("\\")[1]);
            
            if(userPrincipal != null)
            {
                PrincipalSearchResult<Principal> groups = userPrincipal.GetAuthorizationGroups();
                var names = groups.AsParallel().Where(x => x is GroupPrincipal).Select(x => x.Name ?? "");

                return Json(new { isInGroup = names.Any(x => x.Contains(group)), value = group, User.Identity.Name, names });
            }

            return Json("ERROR FUCK OFF");
        }
        #endregion  
    }
}