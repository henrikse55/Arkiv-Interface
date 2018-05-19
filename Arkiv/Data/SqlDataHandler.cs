using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Arkiv.Data
{
    public class SqlDataHandler : ISqlData
    {
        private string _connection {get; set;}
        private SqlConnection Connection { get; set; }


        public SqlDataHandler(string connection)
        {
            _connection = connection;
            Connection = new SqlConnection(_connection);
        }

        /// <summary>
        /// Execute a query for selection of data from the db
        /// </summary>
        /// <typeparam name="T">The model which should be filled</typeparam>
        /// <param name="query">The query which should be executed</param>
        /// <param name="parameters">the parameter and it's corresponding value</param>
        /// <returns>A set of all rows found in the db</returns>
        public IEnumerable<T> SelectData<T>(string query, (string, object)[] parameters)
        {
            Connection.Open();
            using (SqlTransaction trans = Connection.BeginTransaction())
            {
                using (SqlCommand select = new SqlCommand(query, Connection, trans))
                {
                    Type type = typeof(T);
                    string[] properties = GetProps(type);
                    SqlDataReader reader = select.ExecuteReader();

                    T instance = (T)Activator.CreateInstance(type);
                    while(reader.Read())
                    {
                        foreach (string prop in properties)
                        {
                            type.GetProperty(prop).SetValue(instance, reader[prop]);
                        }

                        yield return instance;
                    }

                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Execute a query for selection of data from the db
        /// </summary>
        /// <typeparam name="T">The model which should be filled</typeparam>
        /// <param name="query">The query which should be executed</param>
        /// <param name="parameters">the parameter and it's corresponding value</param>
        /// <returns>A set of all rows found in the db</returns>
        public async Task<IEnumerable<T>> SelectDataAsync<T>(string query, (string, object)[] parameters)
        {
            await Connection.OpenAsync();
            using (SqlTransaction trans = Connection.BeginTransaction())
            {
                using (SqlCommand select = new SqlCommand(query, Connection, trans))
                {
                    Type type = typeof(T);
                    string[] properties = GetProps(type);
                    SqlDataReader reader = await select.ExecuteReaderAsync();

                    T instance = (T)Activator.CreateInstance(type);
                    List<T> items = new List<T>();
                    while (reader.Read())
                    {
                        foreach (string prop in properties)
                        {
                            type.GetProperty(prop).SetValue(instance, reader[prop]);
                        }

                        items.Add(instance);
                    }

                    reader.Close();

                    return items;
                }
            }
        }


        /// <summary>
        /// Get's all the Properties, ignores attribute properties
        /// </summary>
        /// <param name="item">the item to get the Properties from</param>
        /// <returns>an string array with all names</returns>
        private static string[] GetProps(Type item)
        {
            PropertyInfo[] info = item.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return (from x in info select x.Name).ToArray();
        }
    }
}
