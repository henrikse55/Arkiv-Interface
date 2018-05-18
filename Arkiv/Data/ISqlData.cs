using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arkiv.Data
{
    public interface ISqlData
    {
        IEnumerable<object> Execute(string query,string[] columns, params (string, object)[] values);

    }
}
