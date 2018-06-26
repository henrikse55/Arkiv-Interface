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

            return Json(new { groups = models });
        }

        [HttpGet]
        [Route("logs")]
        public async Task<IActionResult> Logs(int offset)
        {
            string activityQuery = @"SELECT * FROM activity 
                                     ORDER BY [Time] DESC OFFSET {off} ROWS FETCH NEXT 25 ROWS ONLY"
                                     .Replace("{off}", offset.ToString());

            string countQuery = "SELECT COUNT(Id) as cn FROM activity";

            int pageCount = (int)(await sql.GetDataRawAsync(countQuery)).Rows[0][0];

            IEnumerable<ActivityLogModel> logs = await sql.SelectDataAsync<ActivityLogModel>(activityQuery);

            return Json(new { logs, count = pageCount });
        }

        [HttpGet]
        [Route("blacklist")]
        public async Task<IActionResult> Blacklist(int offset)
        {
            string blacklistQuery = @"SELECT * FROM ColumnBlacklist
                                     ORDER BY Id ASC OFFSET {off} ROWS FETCH NEXT 25 ROWS ONLY"
                         .Replace("{off}", offset.ToString());

            string countQuery = "SELECT COUNT(Id) as cn FROM ColumnBlacklist";

            int pageCount = (int)(await sql.GetDataRawAsync(countQuery)).Rows[0][0];
            IEnumerable<BlackListModel> blacklist = await sql.SelectDataAsync<BlackListModel>(blacklistQuery);

            return Json(new { blacklist, count = pageCount });
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

        [HttpPost]
        [Route("addBlacklist")]
        public async Task<IActionResult> AddBlacklist(string blacklist)
        {
            int result = await sql.ExecuteAsync("INSERT INTO ColumnBlacklist ([Column]) VALUES (@col)", new(string, object)[] { ("@col", blacklist) });
            IEnumerable<BlackListModel> model = await sql.SelectDataAsync<BlackListModel>("SELECT * FROM ColumnBlacklist WHERE [Column]=@b", new(string, object)[] { ("@b", blacklist) });
            if(model.Count() > 0)
            {
                return Json(model.First());
            }
            return Json(result);
        }

        [HttpPost]
        [Route("deleteBlacklist")]
        public async Task<IActionResult> DeleteBlacklist(string blacklist)
        {
            int result = await sql.ExecuteAsync("DELETE FROM ColumnBlacklist WHERE [Column]=(@col)", new(string, object)[] { ("@col", blacklist) });
            if(result > 0)
            {
                return Json(true);
            }
            return Json(false);
        }
    }
}