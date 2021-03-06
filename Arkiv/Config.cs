﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arkiv
{
    public class Config
    {
        public string Connection { get; set; }
        public IEnumerable<string> AdminUsers { get; set; }
        public IEnumerable<string> AdminGroups { get; set; }
        public bool ActivityLogging { get; set; }
        public string PdfPath { get; internal set; }
    }
}
