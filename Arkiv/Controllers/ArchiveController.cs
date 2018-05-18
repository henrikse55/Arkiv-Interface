using Microsoft.AspNetCore.Mvc;

namespace Arkiv.Controllers
{
    public class ArchiveController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}