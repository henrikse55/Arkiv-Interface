using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Arkiv.Data
{
    public interface ISqlData
    {
        /// <summary>
        /// Execute a query for selection of data from the db
        /// </summary>
        /// <typeparam name="T">The model which should be filled</typeparam>
        /// <param name="query">The query which should be executed</param>
        /// <param name="parameters">the parameter and it's corresponding value</param>
        /// <remarks>As a web page it's best to do this action async</remarks>
        /// <see  cref="SelectDataAsync{T}(string, (string, object)[])"/>
        /// <returns>A set of all rows found in the db</returns>
        IEnumerable<T> SelectData<T>(string query, (string, object)[] parameters = null);

        /// <summary>
        /// Execute a query for selection of data from the db
        /// </summary>
        /// <typeparam name="T">The model which should be filled</typeparam>
        /// <param name="query">The query which should be executed</param>
        /// <param name="parameters">the parameter and it's corresponding value</param>
        /// <returns>A set of all rows found in the db</returns>
        Task<IEnumerable<T>> SelectDataAsync<T>(string query, (string, object)[] parameters = null);

        /// <summary>
        /// Executes the given query with it's paramters attached
        /// </summary>
        /// <param name="query">the query to execute</param>
        /// <param name="parameters">the parameters to attach</param>
        /// <remarks>as a web page it's best to this action async</remarks>
        /// <see cref="ExecuteAsync(string, (string, object)[])"/>
        /// <returns>number of effected rows</returns>
        int Execute(string query, (string, object)[] parameters = null);

        /// <summary>
        /// Executes the given query with it's paramters attached
        /// </summary>
        /// <param name="query">the query to execute</param>
        /// <param name="parameters">the parameters to attach</param>
        /// <returns>number of effected rows</returns>
        Task<int> ExecuteAsync(string query, (string, object)[] parameters = null);

        /// <summary>
        /// Executes the given query with optional paramters and gives the result back raw
        /// </summary>
        /// <param name="query">The query</param>
        /// <param name="parameters">the optional paramters</param>
        /// <seealso cref="GetDataRawAsync(string, (string, object)[])"/>
        /// <returns>the data which was returned in a datatable</returns>
        DataTable GetDataRaw(string query, (string, object)[] parameters = null);


        /// <summary>
        /// Executes the given query with optional paramters and gives the result back raw
        /// </summary>
        /// <param name="query">The query</param>
        /// <param name="parameters">the optional paramters</param>
        /// <seealso cref="GetDataRawAsync(string, (string, object)[])"/>
        /// <returns>the data which was returned in a datatable</returns>
        DataTable GetDataRawAsync(string query, (string, object)[] parameters = null);

        /// <summary>
        /// Writes a log entry in the activity table
        /// </summary>
        /// <param name="action">What the user did</param>
        /// <param name="time">when the user did it</param>
        /// <param name="user">What user did it</param>
        /// <param name="parameters">any additional data</param>
        /// <remarks>as a web page it's best to do this action async</remarks>
        /// <see cref="LogAsync(string, DateTime, string, string)"/>
        void Log(string action, DateTime time, string user, string parameters);

        /// <summary>
        /// Writes a log entry in the activity table
        /// </summary>
        /// <param name="action">What the user did</param>
        /// <param name="time">when the user did it</param>
        /// <param name="user">What user did it</param>
        /// <param name="parameters">any additional data</param>
        void LogAsync(string action, DateTime time, string user, string parameters);
    }
}
