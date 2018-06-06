using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Arkiv.Data;
using Arkiv.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Arkiv.Controllers
{
    [Produces("application/json")]
    [Route("api/account")]
    public class AccountController : Controller
    {
        private ISqlData sql { get; set; }
        private readonly ILogger logger;

        public AccountController(ISqlData _sql, ILogger<AccountController> _logger)
        {
            sql = _sql;
            logger = _logger;
        }


        [HttpGet]
        [Route("access")]
        public async Task<IActionResult> Access()
        {
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

            return Json(sorted);
        }
    }
}