using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arkiv.Models
{
    public class ActivityLogModel
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public DateTime Time { get; set; }
        public string User { get; set; }
        public string Paramters { get; set; }
    }
}
