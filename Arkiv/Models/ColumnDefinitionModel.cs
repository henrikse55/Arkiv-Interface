using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arkiv.Models
{
    public class ColumnDefinitionModel
    {
        public int Id { get; set; }
        public string ShortName { get; set; }
        public string FullName { get; set; }
        public int LanguageId { get; set; }
    }
}
