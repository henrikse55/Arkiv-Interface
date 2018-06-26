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
            IEnumerable<ColumnNameModel> ColumnNames = await sql.SelectDataAsync<ColumnNameModel>("SELECT COLUMNS.COLUMN_NAME FROM ColumnBlacklist INNER JOIN INFORMATION_SCHEMA.COLUMNS ON [Column] != COLUMNS.COLUMN_NAME Where TABLE_NAME = 'arkiv'");

            List<SelectListItem> ColumnNamesSelectList = new List<SelectListItem>();
            
            foreach(ColumnNameModel column in ColumnNames)
            {
                ColumnNamesSelectList.Add(new SelectListItem { Value = column.COLUMN_NAME, Text = column.COLUMN_NAME });
            }

            //This section checks if the current user has access to the admin panel
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
            if (Filters != null && Filters.Count() == 0)
            {
                string parameters = JsonConvert.SerializeObject(new { Filters, OrderData });
                sql.LogAsync("Filter Applied", DateTime.Now, User.Identity.Name, parameters);
            }

            string ColumnNamesQuery = @"SELECT COLUMNS.COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'arkiv' AND COLUMN_NAME NOT IN (
                                        	SELECT [Column] FROM ColumnBlacklist
                                        )";

            IEnumerable<ColumnNameModel> ColumnNames = await sql.SelectDataAsync<ColumnNameModel>(ColumnNamesQuery);

            List<SelectListItem> ColumnNamesSelectList = ColumnNames.Select(x => new SelectListItem() { Value = x.COLUMN_NAME, Text = x.COLUMN_NAME}).ToList();

            ////Add every column from the database to list
            //foreach (ColumnNameModel column in ColumnNames)
            //{
            //    ColumnNamesSelectList.Add(new SelectListItem { Value = column.COLUMN_NAME, Text = column.COLUMN_NAME });
            //}

            StringBuilder WhereClause = new StringBuilder("WHERE (");

            #region Active Directory account cheching
            bool ClearWhereFlag = false;

            IEnumerable<ActiveModel> AdModels = await sql.SelectDataAsync<ActiveModel>("SELECT * FROM active");

            IEnumerable<string> groups = GetUserGroups();

            IEnumerable<string> SortedModels = (from model in AdModels where groups.Any(g => g == model.Group) select model.DEVI);

            List<(string, object)> ParamList = new List<(string, object)>();

            if (SortedModels.Count() > 0)
            {
                for (int i = 0; i < SortedModels.Count(); i++)
                {
                    if (i < SortedModels.Count() && i > 0)
                    {
                        WhereClause.Append(" OR ");
                    }
                    WhereClause.Append("DEVI = @DEVI" + i);
                    ParamList.Add(("@DEVI" + i, SortedModels.ElementAt(i)));

                }

                WhereClause.Append(")");
            }
            else
            {
                ClearWhereFlag = true;
            }
            #endregion

            if (Filters.Count() > 0)
            {
                if (SortedModels.Count() != 0)
                    WhereClause.Append(" AND ");

                object locker = new object();
                Filters.AsParallel().ForAll(Filter =>
                {
                    switch (Filter.Type)
                    {
                        case "Single":
                            //Check if the current item is the last item
                            lock (locker)
                            {
                                WhereClause.Append(Filter.Name + " like " + "@" + Filter.Value.One.Replace(" ", string.Empty) + (!Filter.Equals(Filters.Last()) ? " AND " : ""));
                            }

                            ParamList.Add(("@" + Filter.Value.One.Replace(" ", string.Empty), "%" + Filter.Value.One + "%")); 
                            break;
                        case "Range":
                            //Check if the current item is the last item
                            lock (locker)
                            {
                                WhereClause.Append(Filter.Name + " BETWEEN " + "@" + Filter.Value.One.Replace(" ", string.Empty) + " AND " + "@" + Filter.Value.Two.Replace(" ", string.Empty) + (!Filter.Equals(Filters.Last()) ? " AND " : ""));
                            }

                            ParamList.AddRange(new(string, object)[]
                            {
                                ("@" + Filter.Value.One.Replace(" ", string.Empty), Filter.Value.One),
                                ("@" + Filter.Value.Two.Replace(" ", string.Empty), Filter.Value.Two)
                            }); 
                            break;
                    }
                });

            } else if(ClearWhereFlag)
            {
                    WhereClause.Clear(); //If there is no Filters, remove the WHERE keyword from WhereClause
            }

            string OrderByClause = "";

            if(OrderData.Order != null)
            {
                if (OrderData.Order.Equals("Descending"))
                {
                    OrderByClause += " ORDER BY " + OrderData.Column + " DESC OFFSET {off} ROWS FETCH NEXT 50 ROWS ONLY ";
                }
                else if(OrderData.Order.Equals("Ascending"))
                {
                    OrderByClause += " ORDER BY " + OrderData.Column + " ASC OFFSET {off} ROWS FETCH NEXT 50 ROWS ONLY ";
                }
            }
            else
            {
                OrderByClause += " ORDER BY [Id] ASC OFFSET {off} ROWS FETCH NEXT 50 ROWS ONLY ";
            }

            if (pages == 0)
                pages = 1;

            int max = pages * 50;
            int min = (max - 50 >= 0 ? max-50 : 0);

            string ColumnString = string.Empty;
            IEnumerable<ColumnNameModel> ColumnNamesForQuery = await sql.SelectDataAsync<ColumnNameModel>("SELECT COLUMNS.COLUMN_NAME FROM ColumnBlacklist INNER JOIN INFORMATION_SCHEMA.COLUMNS ON [Column] != COLUMNS.COLUMN_NAME Where TABLE_NAME = 'arkiv'");

            string a = ColumnNamesForQuery.Last().COLUMN_NAME;

            ColumnNamesForQuery.ToList().ForEach(column => {
                ColumnString += "[" + column.COLUMN_NAME + "]" + (column.COLUMN_NAME.Equals(ColumnNamesForQuery.Last().COLUMN_NAME) ? " " : ", ");

            });

            string DataQueryV2 = @"SELECT {ColumnString}
                                   FROM arkiv {where} {order}"
                                   .Replace("{ColumnString}", ColumnString)
                                   .Replace("{where}", WhereClause.ToString())
                                   .Replace("{order}", OrderByClause).Replace("{off}", (pages * 50).ToString());

            string countQuery = "SELECT COUNT(Id) as cn FROM arkiv " + WhereClause.ToString();
            int pageCount = (int)(await sql.GetDataRawAsync(countQuery, ParamList.ToArray())).Rows[0][0];


            IEnumerable<ArchiveDataModel> data = await sql.SelectDataAsync<ArchiveDataModel>(DataQueryV2, ParamList.ToArray());

            IEnumerable<ColumnDefinitionModel> ColumnDefinitions = await sql.SelectDataAsync<ColumnDefinitionModel>("SELECT * FROM ColumnDefinition");

            KeyValuePair<string, string>[] ColumnsforDictionary = new KeyValuePair<string, string>[ColumnNamesSelectList.Count()];

            ColumnNamesSelectList.Select((item, index) => (item, index)).AsParallel().ForAll(x =>
            {
                string value = x.item.Value;
                ColumnsforDictionary[x.index] = new KeyValuePair<string, string>(value, ColumnDefinitions.Any(def => def.ShortName.Equals(value)) ? ColumnDefinitions.Where(def => def.ShortName.Equals(value)).First().FullName : value);
            });

            //If there is no results, return the json object, "No Match"
            if (data.Count() == 0)
            {
                return Json("No Match");
            }

            return PartialView("TablePartial",new ArchiveJoinedModel() { selectListItems = ColumnNamesSelectList.AsEnumerable(), data = data, pages = pageCount, FullColumnNames = new Dictionary<string, string>(ColumnsforDictionary), ColumnsNotOnBlacklist = ColumnNamesForQuery });
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