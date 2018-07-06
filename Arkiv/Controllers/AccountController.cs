using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Principal;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Arkiv.Models;
using System.Linq;
using Arkiv.Data;
using System;

namespace Arkiv.Controllers
{
    [Produces("application/json")]
    [Route("api/account")]
    public class AccountController : Controller
    {
        private ISqlData sql { get; set; }
        private readonly ILogger logger;

        public AccountController(ISqlData sql, ILogger<AccountController> logger)
        {
            this.sql = sql;
            this.logger = logger;
        }

        [HttpGet]
        [Route("access")]
        public async Task<IActionResult> Access()
        {
            IEnumerable<ActiveModel> models = await sql.SelectDataAsync<ActiveModel>("SELECT * FROM active").ConfigureAwait(false);

            WindowsIdentity identity = WindowsIdentity.GetCurrent();

            IEnumerable<string> groups = identity.Groups.Select(group =>
            {
                try
                {
                    return group.Translate(typeof(NTAccount)).Value;
                }
                catch (Exception) { }
                return null;
            });

            var sorted = (from model in models where groups.Any(y => y == model.Group.Replace("\\\\", "\\", StringComparison.Ordinal)) select model.DEVI);

            return Json(sorted);
        }
    }
}