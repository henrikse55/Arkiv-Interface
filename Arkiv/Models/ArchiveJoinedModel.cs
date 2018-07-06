using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Arkiv.Models
{
    public class ArchiveJoinedModel
    {
        public IEnumerable<SelectListItem> SelectListItems { get; set; }

        public IEnumerable<ArchiveDataModel> Data { get; set; }

        public int Pages { get; internal set; }

        public bool AdminPanelAccess { get; set; }

        public Dictionary<string, string> FullColumnNames { get; set; }

        public IEnumerable<ColumnNameModel> ColumnsNotOnBlacklist { get; set; }
    }
}