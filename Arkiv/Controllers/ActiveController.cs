using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arkiv.Data;
using Arkiv.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Arkiv.Controllers
{
    [Produces("application/json")]
    [Route("api/active")]
    public class ActiveController : Controller
    {
        private ISqlData sql { get; set; }
        private readonly ILogger logger;

        public ActiveController(ISqlData _sql, ILogger<ActiveController> _logger)
        {
            sql = _sql;
            logger = _logger;
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> List()
        {
            logger.LogInformation("User: {0} requested admin data", User.Identity.Name);

            IEnumerable<ActiveModel> models = await sql.SelectDataAsync<ActiveModel>("SELECT * FROM active");

            IEnumerable<ActivityLogModel> logs = await sql.SelectDataAsync<ActivityLogModel>("SELECT * FROM activity");

            return Json(new {groups = models, logs });
        }

        [HttpPost]
        [Route("deleteADE")]
        public async Task<IActionResult> DeleteADE(int id)
        {
            int result = await sql.ExecuteAsync("DELETE FROM active WHERE Id=@id", new (string, object)[] { ("@id", id) });

            return Json(new { success = result > 0});
        }

        [HttpPost]
        [Route("addEntry")]
        public async Task<IActionResult> Add(ActiveModel model)
        {
            await sql.ExecuteAsync("INSERT INTO active VALUES (@g, @v)", new(string, object)[] { ("@g", model.Group), ("@v", model.DEVI) });
            IEnumerable<ActiveModel> models = await sql.SelectDataAsync<ActiveModel>("SELECT * FROM active WHERE [Group]=@g AND DEVI=@d", new(string, object)[] { ("@g", model.Group), ("@d", model.DEVI) });
            
            if (models.Count() > 0)
                return Json(models.First());
            else
                return Json(null);
        }
    }
}