﻿using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Arkiv.Models
{
    public class ArchiveJoinedModel
    {
        public IEnumerable<SelectListItem> selectListItems { get; set; }

        public IEnumerable<ArchiveDataModel> data { get; set; }

        public int pages { get; internal set; }

        public bool adminPanelAccess { get; set; }

        public Dictionary<string, string> FullColumnNames { get; set; }

        public IEnumerable<ColumnNameModel> ColumnsNotOnBlacklist { get; set; }
    }
}