using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Security.Principal;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Arkiv.Models;
using System.Linq;
using Arkiv.Data;

namespace Arkiv.Controllers
{
    [Authorize]
    public class ArchiveController : Controller
    {
        private ISqlData sql;

        public ArchiveController(ISqlData _data)
        {
            sql = _data;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IEnumerable<ColumnNameModel> model = await sql.SelectDataAsync<ColumnNameModel>("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS", null);

            List<SelectListItem> list = new List<SelectListItem>();
            
            foreach(ColumnNameModel item in model)
            {
                list.Add(new SelectListItem { Value = item.COLUMN_NAME, Text = item.COLUMN_NAME });
            }

            //IEnumerable<ArchiveDataModel> data = await sql.SelectDataAsync<ArchiveDataModel>("SELECT * FROM arkiv", null);

            return View(new ArchiveJoinedModel() { selectListItems = list.AsEnumerable(), data = null });
        }

        [HttpPost]
        public IActionResult GetFilterPartial(string SelectedColumn)
        {
            return PartialView("FilterPartial", SelectedColumn);
        }   

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
                catch (System.Exception)
                {
                    groups.Add(t.Value);
                }

            return Json(new { isInGroup = test.IsInRole("Administrators"), User.Identity.Name,  groups});
        }

        [HttpGet]
        public IActionResult Admin()
        {
            if(User.IsInRole("Administrator"))
            {
                return View();
            }

            return Redirect("Index");
        }

    }
}