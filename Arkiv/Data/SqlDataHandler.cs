using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public SqlDataHandler(string connection)
        {
            _connection = connection;
        }
        
        public IEnumerable<T> SelectData<T>(string query, (string, object)[] parameters = null)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    using (SqlCommand select = new SqlCommand(query, conn, trans))
                    {
                        #region Prepare parameters
                        if (parameters != null)
                            foreach ((string, object) t in parameters)
                                select.Parameters.AddWithValue(t.Item1, t.Item2);
                        #endregion

                        #region Load Data
                        Type type = typeof(T);
                        string[] properties = GetProps(type);
                        SqlDataReader reader = select.ExecuteReader();

                        while (reader.Read())
                        {
                            T instance = (T)Activator.CreateInstance(type);
                            foreach (string prop in properties)
                            {
                                int index = reader.GetOrdinal(prop);
                                Type dataType = reader.GetFieldType(index);
                                TypeConverter converter = TypeDescriptor.GetConverter(dataType);

                                if (converter.IsValid(reader[prop]))
                                {
                                    type.GetProperty(prop).SetValue(instance, converter.ConvertTo(reader[prop], type.GetProperty(prop).PropertyType));
                                }
                                else
                                {
                                    type.GetProperty(prop).SetValue(instance, reader[prop]);
                                }
                            }

                            yield return instance;
                        }
                        #endregion

                        reader.Close();
                    }
                }
            }
        }

        public async Task<IEnumerable<T>> SelectDataAsync<T>(string query, (string, object)[] parameters = null)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    using (SqlCommand select = new SqlCommand(query, conn, trans))
                    {
                        #region Prepare parameters
                        if (parameters != null)
                            foreach ((string, object) t in parameters)
                                select.Parameters.AddWithValue(t.Item1, t.Item2);
                        #endregion

                        #region Load Data
                        Type type = typeof(T);
                        string[] properties = GetProps(type);
                        SqlDataReader reader = await select.ExecuteReaderAsync().ConfigureAwait(false);

                        List<T> items = new List<T>();
                        while (reader.Read())
                        {
                            T instance = (T)Activator.CreateInstance(type);
                            foreach (string prop in properties)
                            {
                                try
                                {
                                    int index = reader.GetOrdinal(prop);
                                    Type dataType = reader.GetFieldType(index);
                                    TypeConverter converter = TypeDescriptor.GetConverter(dataType);
                                    if (converter.IsValid(reader[prop]))
                                    {
                                        try
                                        {
                                            if (!await reader.IsDBNullAsync(index).ConfigureAwait(false))
                                                type.GetProperty(prop).SetValue(instance, converter.ConvertTo(reader[prop], type.GetProperty(prop).PropertyType));
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                    else
                                    {
                                        if (!await reader.IsDBNullAsync(index).ConfigureAwait(false))
                                        {
                                            type.GetProperty(prop).SetValue(instance, reader[prop]);
                                        }
                                    }
                                }
                                catch (Exception)
                                {

                                }
                            }
                            items.Add(instance);
                        }
                        #endregion

                        reader.Close();
                        trans.Commit();

                        return items;
                    }
                }
            }
        }

        public int Execute(string query, (string, object)[] paramters = null)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand(query, conn, trans))
                    {
                        if (paramters != null)
                            foreach ((string, object) parameter in paramters)
                                command.Parameters.AddWithValue(parameter.Item1, parameter.Item2);

                        int rows = command.ExecuteNonQuery();
                        trans.Commit();

                        return rows;
                    }
                }
            }
        }

        public async Task<int> ExecuteAsync(string query, (string, object)[] parameters = null)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand(query, conn, trans))
                    {
                        if (parameters != null)
                            foreach ((string, object) parameter in parameters)
                                command.Parameters.AddWithValue(parameter.Item1, parameter.Item2);

                        int rows = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        trans.Commit();

                        return rows;
                    }
                }
            }
        }

        public void Log(string action, DateTime time, string user, string paramters)
        {
            Execute("INSERT INTO Activity VALUES (@action, @time, @user, @parameters)"
                ,new (string, object)[] 
                {
                    ("@action", action),
                    ("@time", time),
                    ("@user", user),
                    ("@parameters", paramters)
                });
        }

        public async void LogAsync(string action, DateTime time, string user, string paramters)
        {
            await ExecuteAsync("INSERT INTO activity VALUES (@action, @time, @user, @parameters)"
            , new(string, object)[]
            {
                            ("@action", action),
                            ("@time", time),
                            ("@user", user),
                            ("@parameters", paramters)
            }).ConfigureAwait(false);
        }

        public DataTable GetDataRaw(string query, (string, object)[] parameters = null)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand(query, conn, trans))
                    {
                        if (parameters != null)
                            foreach ((string, object) parameter in parameters)
                                command.Parameters.AddWithValue(parameter.Item1, parameter.Item2);

                        SqlDataReader reader = command.ExecuteReader();
                        DataTable table = new DataTable();
                        table.Load(reader);
                        return table;
                    }
                }

            }
        }

        public async Task<DataTable> GetDataRawAsync(string query, (string, object)[] parameters = null)
        {
            using (SqlConnection conn = new SqlConnection(_connection))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand(query, conn, trans))
                    {
                        if (parameters != null)
                            foreach ((string, object) parameter in parameters)
                                command.Parameters.AddWithValue(parameter.Item1, parameter.Item2);

                        SqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                        DataTable table = new DataTable();
                        table.Load(reader);
                        return table;
                    }
                }

            }
        }

        #region Utility
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
        #endregion
    }
}
