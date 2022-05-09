using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Reflection;

namespace SMTW_Management
{
    public class SqlFunction
    {
        private readonly IConfiguration Configuration;

        public SqlFunction(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private string GetConnString()
        {
            return Configuration.GetValue<string>("ConnectionStrings:DBConnectionString")
                + "Max Pool Size=" + Configuration.GetValue<string>("ConnectionStrings:MaxPool") + ";"
                + "Min Pool Size=" + Configuration.GetValue<string>("ConnectionStrings:MinPool") + ";"
                + "Connect Timeout=" + Configuration.GetValue<string>("ConnectionStrings:Conn_Timeout") + ";"
                + "Connection Lifetime=" + Configuration.GetValue<string>("ConnectionStrings:Conn_Lifetime") + ";";
        }

        /// <summary>
        /// 傳入要使用的select的sql語法，select結果存入DataTable，回傳DataTable
        /// </summary>
        public DataTable GetData(string strSql)
        {
            DataTable dtData = new DataTable();

            using (SqlConnection connection = new SqlConnection())
            {
                connection.ConnectionString = GetConnString();

                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }

                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(strSql, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        dtData.Load(reader);
                        reader.Close();
                    }
                }
                catch (SqlException e)
                {
                    throw e;
                }
                finally
                {
                    connection.Close();
                }

                return dtData;
            }
        }

        /// <summary>
        /// 傳入要使用的select的sql語法，select結果存入DataTable，回傳DataTable
        /// </summary>
        public DataTable GetData(string strSql, SqlParameter[] sqlParameters)
        {
            DataTable dtData = new DataTable();

            using (SqlConnection connection = new SqlConnection())
            {
                connection.ConnectionString = GetConnString();

                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }

                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(strSql, connection);
                    command.Parameters.AddRange(sqlParameters);
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        dtData.Load(reader);
                        reader.Close();
                    }
                }
                catch (SqlException e)
                {
                    throw e;
                }
                finally
                {
                    connection.Close();
                }

                return dtData;
            }
        }

        /// <summary>
        /// 傳入要執行的sql語法，回傳影響行數
        /// </summary>
        public int ExecuteSql(string strSql)
        {
            int intAffectRow = -1;
            using (SqlConnection connection = new SqlConnection())
            {
                connection.ConnectionString = GetConnString();

                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }

                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(strSql, connection);
                    intAffectRow = command.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    throw e;
                }
                finally
                {
                    connection.Close();
                }

                return intAffectRow;
            }
        }

        /// <summary>
        /// 傳入要使用的insert/update/delete的sql語法，回傳影響行數
        /// </summary>
        public int ExecuteSql(string strSql, SqlParameter[] sqlParameters)
        {
            int intAffectRow = -1;
            using (SqlConnection connection = new SqlConnection())
            {
                connection.ConnectionString = GetConnString();

                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }

                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(strSql, connection);
                    command.Parameters.AddRange(sqlParameters);
                    intAffectRow = command.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    throw e;
                }
                finally
                {
                    connection.Close();
                }

                return intAffectRow;
            }
        }

        /// <summary>
        /// 傳入sql語法，取得insert的資料之流水號
        /// </summary>
        public int GetId(string strSql, SqlParameter[] sqlParameters)
        {
            int id = 0;
            using (SqlConnection connection = new SqlConnection())
            {
                connection.ConnectionString = GetConnString();

                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }

                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(strSql, connection);
                    command.Parameters.AddRange(sqlParameters);
                    SqlDataReader reader = command.ExecuteReader();
                    reader.Read();
                    id = Int32.Parse(reader[0].ToString());
                }
                catch (SqlException e)
                {
                    throw e;
                }
                finally
                {
                    connection.Close();
                }

                return id;
            }
        }


        /// <summary>
        /// 傳入DataTable格式，回傳List
        /// </summary>
        public List<T> DataTableToList<T>(DataTable dtData) where T : new()
        {
            List<T> list = new List<T>();
            T t = new T();
            PropertyInfo[] properties = t.GetType().GetProperties();

            foreach (DataRow row in dtData.Rows)
            {
                t = new T();
                foreach (PropertyInfo property in properties)
                {
                    if (dtData.Columns.Contains(property.Name))
                    {
                        if (row[property.Name] != DBNull.Value)
                        {
                            object value = ChangeType(row[property.Name], property.PropertyType);
                            property.SetValue(t, value, null);
                        }
                    }
                }
                list.Add(t);
            }

            return list;
        }

        public static object ChangeType(object value, Type conversion)
        {
            var t = conversion;

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return Convert.ChangeType(value, t);
        }

    }
}