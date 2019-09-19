using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Models
{
    public class SqlHelper
    {
        //database connection string 
        private static string s_ConnectionString = "";
        public static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(s_ConnectionString))
                {
                    string conStr = ConfigurationManager.ConnectionStrings["DbConnectionStr"].ConnectionString;
                    int pos = conStr.IndexOf(";pwd=");
                    string txt1 = conStr.Substring(0, pos);
                    string txt2 = conStr.Substring(pos + 5);
                    conStr = txt1 + ";pwd=" + Common.Security.Decrypt(txt2);
                    string conStr_lower = conStr.ToLower();
                    string wUser = "Trusted_Connection";
                    if (conStr_lower.IndexOf(wUser.ToLower()) >= 0)
                    {
                        s_ConnectionString = "error";
                    }
                    else
                    {
                        s_ConnectionString = conStr;
                    }
                }
                return s_ConnectionString;
            }
        }
        // Hashtable to store cached parameters
        private static Hashtable parmCache = Hashtable.Synchronized(new Hashtable());

        private static object ReadValue(SqlDataReader reader, string fName, TypeCode typeCode)
        {
            object value = null;
            int fOrder = -1;
            try
            {
                fOrder = reader.GetOrdinal(fName);
            }
            catch (Exception ex)
            {
            }
            value = ReadValue(reader, fOrder, typeCode);
            return value;
        }
        private static object ReadValue(SqlDataReader reader, int fOrder, TypeCode typeCode)
        {
            object value = null;
            if (fOrder < 0)
            {
            }
            else if (reader.IsDBNull(fOrder))
            {
            }
            else if (typeCode == TypeCode.String)
            {
                value = reader.GetString(fOrder);
            }
            else if (typeCode == TypeCode.Int16)
            {
                value = reader.GetInt16(fOrder);
            }
            else if (typeCode == TypeCode.Int32)
            {
                value = reader.GetInt32(fOrder);
            }
            else if (typeCode == TypeCode.Int64)
            {
                value = reader.GetInt64(fOrder);
            }
            else if (typeCode == TypeCode.DateTime)
            {
                value = reader.GetDateTime(fOrder);
            }
            else if (typeCode == TypeCode.Boolean)
            {
                value = reader.GetBoolean(fOrder);
            }
            else if (typeCode == TypeCode.Decimal)
            {
                value = reader.GetDecimal(fOrder);
            }
            else if (typeCode == TypeCode.Double)
            {
                value = reader.GetDouble(fOrder);
            }
            else if (typeCode == TypeCode.Byte)
            {
                value = reader.GetByte(fOrder);
            }
            else
            {
                string str = typeCode.ToString();
                int i = 0;
            }
            return value;
        }
        public static T DoQueryFirstField<T>(string sql, T defValue, params SqlParameter[] commandParameters)
        {
            T result = defValue;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                SqlDataReader reader = ExecuteReader(connection, CommandType.Text, sql, commandParameters);

                if (reader.Read())
                {
                    Type type = result.GetType();

                    TypeCode typeCode = Type.GetTypeCode(type);
                    object value = ReadValue(reader, 0, typeCode);
                    if (value != null)
                    {
                        result = (T)value;
                    }
                }
                reader.Dispose();
            }
            return result;
        }
        public static List<T> DoQuery<T>(string sql, params SqlParameter[] commandParameters)
        {
            List<T> list = new List<T>();
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                SqlDataReader reader = ExecuteReader(connection, CommandType.Text, sql, commandParameters);

                while (reader.Read())
                {
                    T obj = Activator.CreateInstance<T>();
                    Type type = obj.GetType();
                    foreach (FieldInfo fi in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                    {
                        string fName = fi.Name;
                        TypeCode typeCode = Type.GetTypeCode(fi.FieldType);
                        object value = ReadValue(reader, fName, typeCode);
                        if (value != null)
                        {
                            fi.SetValue(obj, value);
                        }
                    }
                    foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    {
                        string fName = pi.Name;
                        TypeCode typeCode = Type.GetTypeCode(pi.PropertyType);
                        object value = ReadValue(reader, fName, typeCode);
                        if (value != null)
                        {
                            pi.SetValue(obj, value, null);
                        }
                    }

                    list.Add(obj);
                }
                reader.Dispose();
            }
            return list;
        }
        /// <summary>
        ///Execute SQL statement. Returns the number of Effected rows.
        /// </summary>
        /// <param name="cmdType">command type：TEXT , PROCEDURE</param>
        /// <param name="cmdText">str that need to be executed</param>
        /// <param name="commandParameters">query parameter</param>
        /// <returns>the numbers of effected rows</returns>
        public static int ExecuteNonQuery(CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            return ExecuteNonQuery(ConnectionString, cmdType, cmdText, commandParameters);
        }
        /// <summary>
        /// Execute sql statement,returns the numbers of effected rows（default is the TEXT command type)
        /// </summary>
        /// <param name="cmdText">sql statement</param>
        /// <param name="commandParameters">query parameter</param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string cmdText, params SqlParameter[] commandParameters)
        {
            return ExecuteNonQuery(ConnectionString, CommandType.Text, cmdText, commandParameters);
        }
        /// <summary>
        /// execute sql statement, returns the numbers of effected rows.
        /// </summary>
        /// <param name="connectionString">database connection string</param>
        /// <param name="cmdType">command type</param>
        /// <param name="cmdText">command text</param>
        /// <param name="commandParameters">command parameters</param>
        /// <returns>the numbers of effected rows</returns>
        public static int ExecuteNonQuery(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
        }
        /// <summary>
        /// execute sql statement, returns the numbers of effected rows.
        /// </summary>
        /// <param name="connection">connection</param>
        /// <param name="cmdType">command type</param>
        /// <param name="cmdText">command text</param>
        /// <param name="commandParameters">command parameter</param>
        /// <returns>the numbers of effected rows</returns>
        public static int ExecuteNonQuery(SqlConnection connection, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
            int val = cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            return val;
        }

        /// <summary>
        /// execute sql statement, returns the numbers of effected rows. it needs sql transaction
        /// </summary>
        /// <param name="trans">sqlTransaction</param>
        /// <param name="cmdType">command type</param>
        /// <param name="cmdText">command text</param>
        /// <param name="commandParameters">command parameter</param>
        /// <returns>the numbers of effected rows</returns>
        public static int ExecuteNonQuery(SqlTransaction trans, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, commandParameters);
            int val = cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            return val;
        }

        /// <summary>
        /// execute sql statement, returns the data sets.
        /// </summary>
        /// <param name="cmdType">command type</param>
        /// <param name="cmdText">command text</param>
        /// <param name="commandParameters">command parameter</param>
        /// <returns>SqlDataReader</returns>
        public static SqlDataReader ExecuteReader(CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            return ExecuteReader(ConnectionString, cmdType, cmdText, commandParameters);
        }

        public static SqlDataReader ExecuteReader(string cmdText, params SqlParameter[] commandParameters)
        {
            return ExecuteReader(ConnectionString, CommandType.Text, cmdText, commandParameters);
        }

        /// <summary>
        ///  execute sql statement, returns the data sets.
        /// </summary>
        /// <param name="connectionString">connection string</param>
        /// <param name="cmdType">command type</param>
        /// <param name="cmdText">command text</param>
        /// <param name="commandParameters">command parameter</param>
        /// <returns>SqlDataReader</returns>
        public static SqlDataReader ExecuteReader(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
                SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return rdr;
            }

        }

        /// <summary>
        /// execute sql statement, returns the data sets.
        /// </summary>
        /// <param name="connection">connection string</param>
        /// <param name="cmdType">command type</param>
        /// <param name="cmdText">command text</param>
        /// <param name="commandParameters">command parameter</param>
        /// <returns>SqlDataReader</returns>
        public static SqlDataReader ExecuteReader(SqlConnection connection, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
            SqlDataReader rdr = cmd.ExecuteReader();
            cmd.Parameters.Clear();
            return rdr;
        }

        /// <summary>
        /// execute sql statements, returns the data of first column of first row.
        /// </summary>
        /// <param name="cmdType">command type</param>
        /// <param name="cmdText">command text</param>
        /// <param name="commandParameters">command parameter</param>
        /// <returns>the data of first column of first row will convert into the corresponding type by Convert.To{Type}</returns>
        public static object ExecuteScalar(CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            return ExecuteScalar(ConnectionString, cmdType, cmdText, commandParameters);
        }

        public static object ExecuteScalar(string cmdText, params SqlParameter[] commandParameters)
        {
            return ExecuteScalar(ConnectionString, CommandType.Text, cmdText, commandParameters);
        }

        /// <summary>
        /// execute sql statements, returns the data of first column of first row.
        /// </summary>
        /// <param name="connectionString">connection string</param>
        /// <param name="cmdType">command type</param>
        /// <param name="cmdText">command text</param>
        /// <param name="commandParameters">command parameter</param>
        /// <returns>the data of first column of first row will convert into the corresponding type by Convert.To{Type}</returns>
        public static object ExecuteScalar(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return val;
            }
        }

        /// <summary>
        /// execute sql statements, returns the data of first column of first row.
        /// </summary>
        /// <param name="connection">SqlConnection</param>
        /// <param name="cmdType">command type</param>
        /// <param name="cmdText">command text</param>
        /// <param name="commandParameters">command parameter</param>
        /// <returns>the data of first column of first row will convert into the corresponding type by Convert.To{Type}</returns>
        public static object ExecuteScalar(SqlConnection connection, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
            object val = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return val;
        }

        /// <summary>
        /// execute sql statements, returns the data of first column of first row.
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="cmdType"></param>
        /// <param name="cmdText"></param>
        /// <param name="commandParameters"></param>
        /// <param name="commandParameters"></param>
        /// <returns>the data of first column of first row will convert into the corresponding type by Convert.To{Type}</returns>
        public static object ExecuteScalar(SqlTransaction trans, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, commandParameters);
            object val = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return val;
        }

        /// <summary>
        /// execute sql statement, returns a DataTable
        /// </summary>
        /// <param name="cmdType"></param>
        /// <param name="cmdText"></param>
        /// <param name="commandParameters"></param>
        /// <returns>DataTable</returns>
        public static DataTable ExecuteTable(CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            return ExecuteTable(ConnectionString, cmdType, cmdText, commandParameters);
        }
        public static DataTable ExecuteTable(string cmdText, params SqlParameter[] commandParameters)
        {
            return ExecuteTable(ConnectionString, CommandType.Text, cmdText, commandParameters);
        }
        /// <summary>
        ///   execute sql statement, returns a DataTable
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="cmdType"></param>
        /// <param name="cmdText"></param>
        /// <param name="commandParameters"></param>
        /// <returns>DataTable</returns>
        public static DataTable ExecuteTable(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
                SqlDataAdapter ap = new SqlDataAdapter();
                ap.SelectCommand = cmd;
                DataSet st = new DataSet();
                ap.Fill(st, "Result");
                cmd.Parameters.Clear();
                return st.Tables["Result"];
            }
        }

        /// <summary>
        ///  execute sql statement, returns a DataTable
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cmdType"></param>
        /// <param name="cmdText"></param>
        /// <param name="commandParameters"></param>
        /// <returns>DataTable</returns>
        public static DataTable ExecuteTable(SqlConnection connection, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
            SqlDataAdapter ap = new SqlDataAdapter();
            ap.SelectCommand = cmd;
            DataSet st = new DataSet();
            ap.Fill(st, "Result");
            cmd.Parameters.Clear();
            return st.Tables["Result"];
        }
        /// <summary>
        /// add the parameter array into the cached HashTable.
        /// </summary>
        /// <param name="cacheKey">the key in cached hashtable</param>
        /// <param name="commandParameters"></param>
        public static void CacheParameters(string cacheKey, params SqlParameter[] commandParameters)
        {
            parmCache[cacheKey] = commandParameters;
        }

        #region get the parameter array of in cached Hashtable by copy method
        /// <summary>
        /// get the parameter array of in cached Hashtable by copy method
        /// </summary>
        /// <param name="cacheKey">the key in the cached hashtable</param>
        /// <returns>parameter array</returns>
        public static SqlParameter[] GetCachedParameters(string cacheKey)
        {
            SqlParameter[] cachedParms = (SqlParameter[])parmCache[cacheKey];
            if (cachedParms == null)
                return null;

            SqlParameter[] clonedParms = new SqlParameter[cachedParms.Length];
            for (int i = 0, j = cachedParms.Length; i < j; i++)
                clonedParms[i] = (SqlParameter)((ICloneable)cachedParms[i]).Clone();

            return clonedParms;
        }
        #endregion

        #region organize the relationship
        /// <summary>
        /// organize the relationship(including parameter setting)
        /// </summary>
        /// <param name="cmd">SqlCommand</param>
        /// <param name="conn">SqlConnection</param>
        /// <param name="trans">SqlTransaction</param>
        /// <param name="cmdType">Command Type</param>
        /// <param name="cmdText">Command Text</param>
        /// <param name="cmdParms">command Parameter</param>
        private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
            cmd.Connection = conn;
            cmd.CommandTimeout = 180;
            cmd.CommandText = cmdText;
            if (trans != null)
                cmd.Transaction = trans;
            cmd.CommandType = cmdType;
            if (cmdParms != null)
            {
                String str = "";
                foreach (SqlParameter parm in cmdParms)
                {
                    str += parm.Value + ",";
                    cmd.Parameters.Add(parm);
                }

            }
        }
        #endregion

    }
}