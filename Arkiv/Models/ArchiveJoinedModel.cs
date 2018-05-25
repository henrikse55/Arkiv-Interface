using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Arkiv.Models
{
    public class ArchiveJoinedModel
    {
        public IEnumerable<SelectListItem> selectListItems { get; set; }

        public IEnumerable<ArchiveDataModel> data { get; set; }
    }
}