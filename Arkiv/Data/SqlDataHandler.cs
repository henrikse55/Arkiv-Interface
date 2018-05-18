using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Arkiv.Data
{
    public class SqlDataHandler : ISqlData
    {
        private string _connection {get; set;}
        private SqlConnection _conn;

        public SqlDataHandler(string connection)
        {
            _connection = connection;
            _conn = new SqlConnection(_connection);
        }

        public IEnumerable<object> Execute(string query, string[] columns, params (string, object)[] values)
        {
            using (SqlTransaction trans = _conn.BeginTransaction())
            {
                _conn.Open();
                using (SqlCommand command = new SqlCommand(query, _conn, trans))
                {
                    if (columns != null)
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        List<object> items = new List<object>();
                        while (reader.Read())
                        {
                            var item = (from x in columns
                                        select new
                                        {
                                            column = x,
                                            value = reader[x]
                                        });

                            items.Add(item);
                        }

                        trans.Commit();
                        return items;
                    }
                    else
                    {
                        command.ExecuteNonQuery();
                        return null;
                    }
                }
            }
        }
    }
}
