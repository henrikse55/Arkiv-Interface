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

namespace Arkiv.Controllers
{
    [Authorize]
    public class ArchiveController : Controller
    {
        #region Constructor
        private ISqlData sql;

        public ArchiveController(ISqlData _data)
        {
            sql = _data;
        }
        #endregion

        #region Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IEnumerable<ColumnNameModel> model = await sql.SelectDataAsync<ColumnNameModel>("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS Where TABLE_NAME = 'arkiv'");

            List<SelectListItem> list = new List<SelectListItem>();
            
            foreach(ColumnNameModel item in model)
            {
                list.Add(new SelectListItem { Value = item.COLUMN_NAME, Text = item.COLUMN_NAME });
            }

            IEnumerable<ArchiveDataModel> data = await sql.SelectDataAsync<ArchiveDataModel>("SELECT TOP 50 * FROM arkiv");

            return View(new ArchiveJoinedModel() { selectListItems = list.AsEnumerable(), data = data });
        }

        [HttpPost]
        public async Task<IActionResult> GetTable(FilterModel[] Filters)
        {
            IEnumerable<ColumnNameModel> model = await sql.SelectDataAsync<ColumnNameModel>("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS Where TABLE_NAME = 'arkiv'");

            List<SelectListItem> list = new List<SelectListItem>();

            //Add every column from the database to list
            foreach (ColumnNameModel item in model)
            {
                list.Add(new SelectListItem { Value = item.COLUMN_NAME, Text = item.COLUMN_NAME });
            }
                
            string WhereClause = "WHERE ";
            List<(string, object)> ParamList = new List<(string, object)>();

            //Make sure Filters is not empty
            if (Filters.Count() > 0)
            {
                foreach(FilterModel Filter in Filters)
                {
                    //Determine which type, the current filter is
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
            }
            else
            {
                WhereClause = string.Empty; //If Filters is empty, make WhereClause empty
            }

            IEnumerable<ArchiveDataModel> data = await sql.SelectDataAsync<ArchiveDataModel>("SELECT * FROM arkiv " + WhereClause, ParamList.ToArray());

            //If there is no results, return the json object, "No Match"
            if (data.Count() == 0)
            {
                return Json("No Match");
            }

            return PartialView("TablePartial",new ArchiveJoinedModel() { selectListItems = list.AsEnumerable(), data = data });
        }

        [HttpPost]
        public IActionResult GetFilterPartial(string SelectedColumn)
        {
            return PartialView("FilterPartial", SelectedColumn);
        }
        #endregion

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

            return Json(new { isInGroup = test.IsInRole("Administrators"), User.Identity.Name,  groups});
        }
        #endregion

        #region Admin
        [HttpGet]
        public IActionResult Admin()
        {
            if(User.IsInRole("Administrators") || User.Identity.Name.Contains("henr054a"))
            {
                return View();
            }

            return Redirect("Index");
        }
        #endregion
    }
}