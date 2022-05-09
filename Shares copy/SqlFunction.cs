using System;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using System.Collections.Generic;
using System.Reflection;

namespace TanJi;

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

        using (MySqlConnection connection = new MySqlConnection())
        {
            connection.ConnectionString = GetConnString();

            if (connection.State == ConnectionState.Open)
            {
                connection.Close();
            }

            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(strSql, connection);
                MySqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    dtData.Load(reader);
                    reader.Close();
                }
            }
            catch (MySqlException e)
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
    public DataTable GetData(string strSql, MySqlParameter[] MySqlParameters)
    {
        DataTable dtData = new DataTable();

        using (MySqlConnection connection = new MySqlConnection())
        {
            connection.ConnectionString = GetConnString();

            if (connection.State == ConnectionState.Open)
            {
                connection.Close();
            }

            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(strSql, connection);
                command.Parameters.AddRange(MySqlParameters);
                MySqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    dtData.Load(reader);
                    reader.Close();
                }
                command.Parameters.Clear();
            }
            catch (MySqlException e)
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
    /// 傳入要使用的insert/update/delete的sql語法，回傳影響行數
    /// </summary>
    public int ExecuteSql(string strSql, MySqlParameter[] MySqlParameters)
    {
        int intAffectRow = -1;
        using (MySqlConnection connection = new MySqlConnection())
        {
            connection.ConnectionString = GetConnString();

            if (connection.State == ConnectionState.Open)
            {
                connection.Close();
            }
            connection.Open();
            MySqlTransaction transaction = connection.BeginTransaction();

            try
            {
                MySqlCommand command = new MySqlCommand(strSql, connection, transaction);
                command.Parameters.AddRange(MySqlParameters);
                intAffectRow = command.ExecuteNonQuery();
                transaction.Commit();
            }
            catch (MySqlException e)
            {
                transaction.Rollback();
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
    public int GetId(string strSql, MySqlParameter[] MySqlParameters)
    {
        int id = 0;
        using (MySqlConnection connection = new MySqlConnection())
        {
            connection.ConnectionString = GetConnString();

            if (connection.State == ConnectionState.Open)
            {
                connection.Close();
            }

            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(strSql, connection);
                command.Parameters.AddRange(MySqlParameters);
                MySqlDataReader reader = command.ExecuteReader();
                reader.Read();
                id = Int32.Parse(reader[0].ToString());
            }
            catch (MySqlException e)
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
