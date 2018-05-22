using Arkiv.Data;
using Arkiv.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data;
using System.Security.Principal;
using System.Threading.Tasks;

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

        public IActionResult Index()
        {
            return View();
        }

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
    }
}