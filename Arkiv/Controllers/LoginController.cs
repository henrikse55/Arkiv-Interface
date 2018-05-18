using Microsoft.AspNetCore.Mvc;

namespace Arkiv.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
    }
}