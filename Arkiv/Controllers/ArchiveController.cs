using Arkiv.Data;
using Arkiv.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Threading.Tasks;

namespace Arkiv.Controllers
{
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
    }
}