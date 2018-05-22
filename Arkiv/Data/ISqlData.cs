using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Arkiv.Data
{
    public interface ISqlData
    {
        IEnumerable<T> SelectData<T>(string query, (string, object)[] parameters);

        Task<IEnumerable<T>> SelectDataAsync<T>(string query, (string, object)[] parameters);
    }
}
