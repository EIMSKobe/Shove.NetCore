using System;
using MySql.Data.MySqlClient;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Shove.Database
{
    /// <summary>
    /// Shove 的专用的 MySQL 访问组件类
    /// </summary>
    public class MySQL : DatabaseAccess
    {
        #region BuildConnectString

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="password"></param>
        /// <param name="database"></param>
        /// <returns></returns>
        public static string BuildConnectString(string uid, string password, string database)
        {
            return string.Format("server=localhost; user id={0}; password={1}; database={2};", uid, password, database);
        }

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="server"></param>
        /// <param name="uid"></param>
        /// <param name="password"></param>
        /// <param name="database"></param>
        /// <returns></returns>
        public static string BuildConnectString(string server, string uid, string password, string database)
        {
            return string.Format("server={0}; user id={1}; password={2}; database={3};", server, uid, password, database);
        }

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="server"></param>
        /// <param name="uid"></param>
        /// <param name="password"></param>
        /// <param name="database"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static string BuildConnectString(string server, string uid, string password, string database, string port)
        {
            return string.Format("server={0}; user id={1}; password={2}; database={3}; port={4}", server, uid, password, database, port);
        }

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="server"></param>
        /// <param name="uid"></param>
        /// <param name="password"></param>
        /// <param name="database"></param>
        /// <param name="port"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static string BuildConnectString(string server, string uid, string password, string database, string port, string charset)
        {
            return string.Format("server={0}; user id={1}; password={2}; database={3}; port={4}; charset={5}", server, uid, password, database, port, charset);
        }

        #endregion

        #region Parameter

        /// <summary>
        /// 参数
        /// </summary>
        public class Parameter
        {
            /// <summary>
            /// 
            /// </summary>
            public string Name;
            /// <summary>
            /// 
            /// </summary>
            public MySqlDbType Type;
            /// <summary>
            /// 
            /// </summary>
            public int Size;
            /// <summary>
            /// 
            /// </summary>
            public ParameterDirection Direction;
            /// <summary>
            /// 
            /// </summary>
            public object Value;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name"></param>
            /// <param name="type"></param>
            /// <param name="size"></param>
            /// <param name="direction"></param>
            /// <param name="value"></param>
            public Parameter(string name, MySqlDbType type, int size, ParameterDirection direction, object value)
            {
                Name = name;
                Type = type;
                Size = size;
                Direction = direction;
                Value = value;
            }
        }

        /// <summary>
        /// 输入参数
        /// </summary>
        public class OutputParameter
        {
            private readonly IList<string> ParametersName;
            private readonly IList<object> ParametersValue;

            /// <summary>
            /// 
            /// </summary>
            public OutputParameter()
            {
                ParametersName = new List<string>();
                ParametersValue = new List<object>();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="Name"></param>
            /// <param name="Value"></param>
            public void Add(string Name, object Value)
            {
                ParametersName.Add(Name.StartsWith("?", StringComparison.Ordinal) ? Name.Substring(1) : Name);
                ParametersValue.Add(Value);
            }

            /// <summary>
            /// 
            /// </summary>
            public void Clear()
            {
                ParametersName.Clear();
                ParametersValue.Clear();
            }

            /// <summary>
            /// 
            /// </summary>
            public int Count
            {
                get
                {
                    if (ParametersName == null)
                    {
                        return 0;
                    }

                    return ParametersName.Count;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            public object this[int index]
            {
                get
                {
                    if (ParametersValue == null)
                    {
                        return null;
                    }

                    if (ParametersValue.Count == 0)
                    {
                        return null;
                    }

                    if (index > ParametersValue.Count - 1)
                    {
                        return null;
                    }

                    return ParametersValue[index];
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public object this[string name]
            {
                get
                {
                    if (ParametersValue == null)
                    {
                        return null;
                    }

                    for (int i = 0; i < ParametersName.Count; i++)
                    {
                        if (ParametersName[i] == name)
                        {
                            return ParametersValue[i];
                        }
                    }

                    return null;
                }
            }
        }

        private static void AddParameter(ref MySqlCommand cmd, params Parameter[] _params)
        {
            if ((_params == null) || (cmd == null))
            {
                return;
            }

            for (int i = 0; i < _params.Length; i++)
            {
                if (_params[i] == null)
                {
                    continue;
                }

                MySqlParameter param = new MySqlParameter
                {
                    ParameterName = _params[i].Name.StartsWith("?", StringComparison.Ordinal) ? _params[i].Name : ("?" + _params[i].Name),
                    MySqlDbType = _params[i].Type
                };

                if (_params[i].Size > 0)
                {
                    param.Size = _params[i].Size;
                }

                param.Direction = _params[i].Direction;

                if (((_params[i].Direction == ParameterDirection.InputOutput) ||
                    (_params[i].Direction == ParameterDirection.Input)) &&
                    (_params[i].Value != null))
                {
                    param.Value = _params[i].Value;
                }

                cmd.Parameters.Add(param);
            }
        }

        private static void AddOutputParameter(MySqlCommand cmd, ref OutputParameter outputs)
        {
            if (cmd == null)
            {
                return;
            }

            if (cmd.Parameters.Count == 0)
            {
                return;
            }

            for (int i = 0; i < cmd.Parameters.Count; i++)
            {
                MySqlParameter param = cmd.Parameters[i];

                if ((param.Direction != ParameterDirection.InputOutput) &&
                    (param.Direction != ParameterDirection.Output))
                {
                    continue;
                }

                outputs.Add(param.ParameterName, param.Value);
            }
        }

        private static MySqlParameter GetReturnParameter(MySqlCommand cmd)
        {
            if (cmd == null)
            {
                return null;
            }

            if (cmd.Parameters.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < cmd.Parameters.Count; i++)
            {
                MySqlParameter param = cmd.Parameters[i];

                if (param.Direction == ParameterDirection.ReturnValue)
                {
                    return param;
                }
            }

            return null;
        }

        #endregion

        #region ExecuteNonQuery

        /// <summary>
        /// 执行数据库命令
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string commandText, params Parameter[] _params)
        {
            return ExecuteNonQuery(GetConnectionStringFromConfig(), commandText, _params);
        }

        /// <summary>
        /// 执行数据库命令
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string connectionString, string commandText, params Parameter[] _params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(connectionString);

            if (conn == null)
            {
                return -1001;
            }

            int result = ExecuteNonQuery(conn, commandText, _params);

            try
            {
                conn.Close();
            }
            catch { }
            finally
            {
                conn.Dispose();
            }

            return result;
        }

        /// <summary>
        /// 执行数据库命令
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(MySqlConnection conn, string commandText, params Parameter[] _params)
        {
            if (conn == null)
            {
                return -1001;
            }

            bool initOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                initOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return -1001;
                }
            }

            //string sss = @"SET FOREIGN_KEY_CHECKS = 0;";

            MySqlCommand cmd = new MySqlCommand(commandText, conn);
            AddParameter(ref cmd, _params);

            MySqlTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                cmd.Dispose();
                return -1001;
            }

            cmd.Transaction = trans;
            bool result;

            try
            {
                cmd.CommandTimeout = 5000000;
                cmd.ExecuteNonQuery();
                trans.Commit();
                cmd.Dispose();

                result = true;
            }
            catch
            {
                try
                {
                    trans.Rollback();
                }
                catch { }

                result = false;
            }
            finally
            {
                cmd.Dispose();
            }

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return result ? 0 : -1002;
        }

        /// <summary>
        /// 执行数据库命令(不用事务)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteNonQueryNoTranscation(MySqlConnection conn, string commandText, params Parameter[] _params)
        {
            if (conn == null)
            {
                return -1001;
            }

            bool initOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                initOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return -1001;
                }
            }

            MySqlCommand cmd = new MySqlCommand(commandText, conn);
            AddParameter(ref cmd, _params);

            bool result;

            try
            {
                cmd.ExecuteNonQuery();

                result = true;
            }
            catch
            {
                result = false;
            }

            cmd.Dispose();

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return result ? 0 : -1002;
        }

        #endregion

        #region Select

        /// <summary>
        /// 打开数据集
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static DataTable Select(string commandText, params Parameter[] _params)
        {
            return Select(GetConnectionStringFromConfig(), commandText, _params);
        }

        /// <summary>
        /// 打开数据集
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static DataTable Select(string connectionString, string commandText, params Parameter[] _params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(connectionString);

            if (conn == null)
            {
                return null;
            }

            DataTable dt = Select(conn, commandText, _params);

            try
            {
                conn.Close();
            }
            catch { }
            finally
            {
                conn.Dispose();
            }

            return dt;
        }

        /// <summary>
        /// 打开数据集
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static DataTable Select(MySqlConnection conn, string commandText, params Parameter[] _params)
        {
            if (conn == null)
            {
                return null;
            }

            bool initOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                initOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return null;
                }
            }

            MySqlDataAdapter da = new MySqlDataAdapter("", conn);
            MySqlCommand cmd = new MySqlCommand(commandText, conn);

            AddParameter(ref cmd, _params);
            da.SelectCommand = cmd;

            DataTable dt = new DataTable();
            bool result;

            try
            {
                da.Fill(dt);

                result = true;
            }
            catch
            {
                result = false;
            }
            finally
            {
                cmd.Dispose();
                da.Dispose();
            }

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return result ? dt : null;
        }

        #region Loong Add

        /// <summary>
        /// 打开数据集
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static DataTable Select(string commandText, int commandTimeout, params Parameter[] _params)
        {
            return Select(GetConnectionStringFromConfig(), commandText, commandTimeout, _params);
        }

        /// <summary>
        /// 打开数据集
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="commandText"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static DataTable Select(string connectionString, string commandText, int commandTimeout, params Parameter[] _params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(connectionString);

            if (conn == null)
            {
                return null;
            }

            DataTable dt = Select(conn, commandText, commandTimeout, _params);

            try
            {
                conn.Close();
            }
            catch { }
            finally
            {
                conn.Dispose();
            }

            return dt;
        }

        /// <summary>
        /// 打开数据?
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static DataTable Select(MySqlConnection conn, string commandText, int commandTimeout, params Parameter[] _params)
        {
            if (conn == null)
            {
                return null;
            }

            bool initOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                initOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return null;
                }
            }

            MySqlDataAdapter da = new MySqlDataAdapter("", conn);
            MySqlCommand cmd = new MySqlCommand(commandText, conn)
            {
                CommandTimeout = commandTimeout
            };

            AddParameter(ref cmd, _params);
            da.SelectCommand = cmd;

            DataTable dt = new DataTable();
            bool result;

            try
            {
                da.Fill(dt);

                result = true;
            }
            catch
            {
                result = false;
            }
            finally
            {
                cmd.Dispose();
                da.Dispose();
            }

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return result ? dt : null;
        }

        #endregion

        #endregion

        #region ExecuteScalar

        /// <summary>
        /// 读取第一行第一列
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteScalar(string commandText, params Parameter[] _params)
        {
            return ExecuteScalar(GetConnectionStringFromConfig(), commandText, _params);
        }

        /// <summary>
        /// 读取第一行第一列
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteScalar(string connectionString, string commandText, params Parameter[] _params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(connectionString);

            if (conn == null)
            {
                return null;
            }

            object obj = ExecuteScalar(conn, commandText, _params);

            try
            {
                conn.Close();
            }
            catch { }
            finally
            {
                conn.Dispose();
            }

            return obj;
        }

        /// <summary>
        /// 读取第一行第一列
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteScalar(MySqlConnection conn, string commandText, params Parameter[] _params)
        {
            if (conn == null)
            {
                return null;
            }

            bool initOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                initOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return null;
                }
            }

            MySqlCommand cmd = new MySqlCommand(commandText, conn);
            AddParameter(ref cmd, _params);

            object result;

            try
            {
                result = cmd.ExecuteScalar();
            }
            catch
            {
                result = null;
            }
            finally
            {
                cmd.Dispose();
            }

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return result;
        }

        #endregion

        #region ExecuteFunction

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(string functionName, params Parameter[] _params)
        {
            return ExecuteFunction(GetConnectionStringFromConfig(), functionName, _params);
        }

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="functionName"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(string connectionString, string functionName, params Parameter[] _params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(connectionString);

            if (conn == null)
            {
                return null;
            }

            object obj = ExecuteFunction(conn, functionName, _params);

            try
            {
                conn.Close();
            }
            catch { }
            finally
            {
                conn.Dispose();
            }

            return obj;
        }

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="functionName"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(MySqlConnection conn, string functionName, params Parameter[] _params)
        {
            if (conn == null)
            {
                return null;
            }

            bool initOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                initOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return null;
                }
            }

            string commandText = "select " + functionName + "(";

            if (_params != null)
            {
                for (int i = 0; i < _params.Length; i++)
                {
                    if (_params[i] != null)
                    {
                        bool isChar = false;

                        if ((_params[i].Type == MySqlDbType.Date) || (_params[i].Type == MySqlDbType.DateTime) ||
                            (_params[i].Type == MySqlDbType.Guid) || (_params[i].Type == MySqlDbType.LongText) || (_params[i].Type == MySqlDbType.MediumText) ||
                            (_params[i].Type == MySqlDbType.Newdate) || (_params[i].Type == MySqlDbType.String) || (_params[i].Type == MySqlDbType.Text) ||
                            (_params[i].Type == MySqlDbType.Time) || (_params[i].Type == MySqlDbType.Timestamp) || (_params[i].Type == MySqlDbType.TinyText) ||
                            (_params[i].Type == MySqlDbType.VarChar) || (_params[i].Type == MySqlDbType.VarString))
                        {
                            isChar = true;
                        }

                        if (!commandText.EndsWith("(", StringComparison.Ordinal))
                        {
                            commandText += ", ";
                        }

                        if (isChar)
                        {
                            commandText += "\'";
                        }

                        commandText += _params[i].Value.ToString();

                        if (isChar)
                        {
                            commandText += "\'";
                        }
                    }
                }

                commandText += ")";
            }


            object result = ExecuteScalar(conn, commandText);

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return result;
        }

        #endregion

        #region ExecuteStoredProcedureNonQuery

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="storedProcedureName"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureNonQuery(string storedProcedureName, ref OutputParameter outputs, params Parameter[] _params)
        {
            return ExecuteStoredProcedureNonQuery(GetConnectionStringFromConfig(), storedProcedureName, ref outputs, _params);
        }

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="storedProcedureName"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureNonQuery(string connectionString, string storedProcedureName, ref OutputParameter outputs, params Parameter[] _params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(connectionString);

            if (conn == null)
            {
                return -1001;
            }

            int result = ExecuteStoredProcedureNonQuery(conn, storedProcedureName, ref outputs, _params);

            try
            {
                conn.Close();
            }
            catch { }
            finally
            {
                conn.Dispose();
            }

            return result;
        }

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="storedProcedureName"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureNonQuery(MySqlConnection conn, string storedProcedureName, ref OutputParameter outputs, params Parameter[] _params)
        {
            if (conn == null)
            {
                return -1001;
            }

            bool initOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                initOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return -1001;
                }
            }

            MySqlCommand cmd = new MySqlCommand(storedProcedureName, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            AddParameter(ref cmd, _params);

            // 增加返回值参数
            //MySqlParameter returnValue = new MySqlParameter("?Shove_Database_MySQL_ExecuteStoredProcedureNonQuery_Rtn", SqlDbType.Int);
            //returnValue.Direction = ParameterDirection.ReturnValue;
            //cmd.Parameters.Add(returnValue);

            MySqlTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                cmd.Dispose();
                return -1001;
            }

            cmd.Transaction = trans;
            bool result;

            try
            {
                cmd.ExecuteNonQuery();
                trans.Commit();

                result = true;
            }
            catch
            {
                try
                {
                    trans.Rollback();
                }
                catch { }

                result = false;
            }

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            if (!result)
            {
                cmd.Dispose();
                return -1002;
            }

            // 填写返回参数
            AddOutputParameter(cmd, ref outputs);
            cmd.Dispose();

            // 获取过程的返刂冗           //ReturnValue = GetReturnParameter(cmd);

            //if (returnValue != null)
            //{
            //    return (int)returnValue.Value;
            //}

            return 0;
        }

        #endregion

        #region ExecuteStoredProcedureWithQuery

        /// <summary>
        /// 执行存储过程(不带返回记录集)
        /// </summary>
        /// <param name="storedProcedureName"></param>
        /// <param name="ds"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureWithQuery(string storedProcedureName, ref DataSet ds, ref OutputParameter outputs, params Parameter[] _params)
        {
            return ExecuteStoredProcedureWithQuery(GetConnectionStringFromConfig(), storedProcedureName, ref ds, ref outputs, _params);
        }

        /// <summary>
        /// 执行存储过程(不带返回记录集)
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="storedProcedureName"></param>
        /// <param name="ds"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureWithQuery(string connectionString, string storedProcedureName, ref DataSet ds, ref OutputParameter outputs, params Parameter[] _params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(connectionString);

            if (conn == null)
            {
                return -1001;
            }

            int result = ExecuteStoredProcedureWithQuery(conn, storedProcedureName, ref ds, ref outputs, _params);

            try
            {
                conn.Close();
            }
            catch { }
            finally
            {
                conn.Dispose();
            }

            return result;
        }

        /// <summary>
        /// 执行存储过程(不带返回记录集)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="storedProcedureName"></param>
        /// <param name="ds"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureWithQuery(MySqlConnection conn, string storedProcedureName, ref DataSet ds, ref OutputParameter outputs, params Parameter[] _params)
        {
            if (conn == null)
            {
                return -1001;
            }

            bool initOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                initOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return -1001;
                }
            }

            MySqlDataAdapter da = new MySqlDataAdapter("", conn);
            MySqlCommand cmd = new MySqlCommand(storedProcedureName, conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Clear();
            AddParameter(ref cmd, _params);

            // 增加返回值参数
            //MySqlParameter returnValue = new MySqlParameter("?Shove_Database_MSSQL_ExecuteStoredProcedureWithQuery_Rtn", SqlDbType.Int);
            //returnValue.Direction = ParameterDirection.ReturnValue;
            //cmd.Parameters.Add(returnValue);

            if (ds == null)
            {
                ds = new DataSet();
            }

            MySqlTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                cmd.Dispose();
                return -1001;
            }

            cmd.Transaction = trans;
            da.SelectCommand = cmd;

            bool result;

            try
            {
                da.Fill(ds);
                trans.Commit();

                result = true;
            }
            catch
            {
                try
                {
                    trans.Rollback();
                }
                catch { }

                result = false;
            }

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            if (!result)
            {
                return -1002;
            }

            //填写返回参数
            AddOutputParameter(cmd, ref outputs);
            cmd.Dispose();
            ds.Dispose();

            // 获取过程的返回值
            //returnValue = GetReturnParameter(cmd);

            //if (returnValue != null)
            //{
            //    return (int)returnValue.Value;
            //}

            return 0;
        }

        #endregion

        #region BackupDatabase, 2015.6.12 Loong 修改了机制

        /// <summary>
        /// 备份数据库(表数据库 XML 描述，压缩为二进制流)
        /// </summary>
        /// <returns></returns>
        public static byte[] BackupDataToZipStream()
        {
            return BackupDataToZipStream(GetConnectionStringFromConfig());
        }

        /// <summary>
        /// 备份数据库(表数据库 XML 描述，压缩为二进制流)
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static byte[] BackupDataToZipStream(string connectionString)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(connectionString);

            if (conn == null)
            {
                return null;
            }

            byte[] buffer = BackupDataToZipStream(conn);

            try
            {
                conn.Close();
            }
            catch { }
            finally
            {
                conn.Dispose();
            }

            return buffer;
        }

        /// <summary>
        /// 备份数据库(表数据库 XML 描述，压缩为二进制流)
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static byte[] BackupDataToZipStream(MySqlConnection conn)
        {
            DataSet ds = BackupDataToDataSet(conn);

            if (ds == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            ds.WriteXml(sw, XmlWriteMode.WriteSchema);
            ds.Dispose();
            sw.Dispose();

            return String.Compress(sb.ToString());
        }

        #region Tools

        /// <summary>
        /// 备份数据库(表数据库 XML 描述，压缩为二进制流)
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        private static DataSet BackupDataToDataSet(MySqlConnection conn)
        {
            if (conn == null)
            {
                return null;
            }

            bool initOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                initOpenState = false;

                try
                { conn.Open(); }
                catch { return null; }
            }

            long sIndex = 1;
            string sql = string.Empty;
            string strTemp = string.Empty;
            string databaserName = string.Empty;	//	数据库名称            
            string dbDefiner = string.Empty;        // 数据库 函数、存储过程创建用户
            string[] dbDefinerAry = null;
            int commandTimeout = 60 * 10;           // 数据库执行超时时间

            DataSet ds = new DataSet();
            DataTable dtTemp = null;
            DataTable dtTableTemp = null;

            // 
            DataTable dtTableModels = DataTableTemplate();
            DataTable dtTableDatas = DataTableTemplate();
            DataTable dtFunctions = DataTableTemplate();
            DataTable dtProcedures = DataTableTemplate();
            DataTable dtEvents = DataTableTemplate();
            DataTable dtTriggers = DataTableTemplate();
            DataTable dtViews = DataTableTemplate();

            #region TableRows   (斫峁、绳荑

            dtTableModels.TableName = "TableModels";
            dtTableDatas.TableName = "TableDatas";

            sql = "SHOW FULL TABLES WHERE Table_type = 'BASE TABLE';";
            dtTemp = Select(conn, sql, 600);

            if (dtTemp == null)
            {
                if (!initOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            DataTable dtColInfo = null;

            foreach (DataRow dr in dtTemp.Rows)
            {
                #region Table Models

                // 重置外键约束
                if (sIndex == 1)
                {
                    dtTableModels.Rows.Add(new object[] { sIndex, "SET FOREIGN_KEY_CHECKS=0;" });
                    sIndex++;
                }

                string tableName = dr[0].ToString();

                sql = string.Format("SHOW CREATE TABLE `{0}`;", tableName);

                dtTableTemp = Select(conn, sql, commandTimeout);

                if (dtTableTemp == null)
                {
                    if (!initOpenState) { try { conn.Close(); } catch { } }
                    return null;
                }

                strTemp = string.Format("DROP TABLE IF EXISTS `{0}`; {1};", tableName, RemoveAutoIncrement(dtTableTemp.Rows[0][1].ToString()));
                // ============================================================
                dtTableModels.Rows.Add(new object[] { sIndex, strTemp });

                #endregion Table Models End

                #region Table Data

                sql = string.Format("SELECT * FROM `{0}`;", tableName);

                DataTable dtData = Select(conn, sql, commandTimeout);

                if (dtData == null)
                {
                    if (!initOpenState) { try { conn.Close(); } catch { } }
                    return null;
                }

                // no data
                if (dtData.Rows.Count < 1)
                {
                    sIndex++;
                    continue;
                }

                sql = string.Format("SHOW FULL COLUMNS FROM `{0}`;", tableName);

                dtColInfo = Select(conn, sql, commandTimeout);

                if (dtColInfo == null)
                {
                    if (!initOpenState) { try { conn.Close(); } catch { } }
                    return null;
                }

                #region 组合插入语句语法二： insert into 表名 values(值..); insert into ...

                StringBuilder sbSqlTemp = new StringBuilder();
                string cValue = string.Empty;

                sbSqlTemp.AppendLine("LOCK TABLES `" + tableName + "` WRITE;");

                for (int j = 0, dIndex = 1; j < dtData.Rows.Count; j++, dIndex++)
                {
                    sbSqlTemp.AppendLine(j == 0 ? string.Format("INSERT INTO `{0}` VALUES ", tableName) : ",");
                    sbSqlTemp.Append("(");

                    for (int k = 0; k < dtData.Columns.Count; k++)
                    {
                        if (k > 0)
                        {
                            sbSqlTemp.AppendFormat(",");
                        }

                        sbSqlTemp.Append(ConvertToSqlFormat(dtData.Rows[j][k], true, true, dtData.Columns[k], dtColInfo.Rows[k]));
                    }

                    sbSqlTemp.Append(")");
                }

                sbSqlTemp.AppendLine(";");
                sbSqlTemp.AppendLine("UNLOCK TABLES;");

                // ================================================================================
                dtTableDatas.Rows.Add(new object[] { sIndex, sbSqlTemp.ToString() });

                #endregion

                #endregion Table Data End

                sIndex++;
            }

            #endregion

            #region Functions   (函数)

            sIndex = 1;
            dtFunctions.TableName = "Functions";

            // Get Database Name
            sql = "SELECT DATABASE();";
            dtTemp = Select(conn, sql, commandTimeout);

            if (dtTemp == null)
            {
                if (!initOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            databaserName = dtTemp.Rows[0][0].ToString();

            // Show Functions
            sql = string.Format("SHOW FUNCTION STATUS WHERE UPPER(TRIM(Db))= UPPER(TRIM('{0}'));", databaserName);

            dtTableTemp = Select(conn, sql);

            if (dtTableTemp == null)
            {
                if (!initOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            foreach (DataRow dr in dtTableTemp.Rows)
            {
                // Functions Name
                sql = string.Format("SHOW CREATE FUNCTION `{0}`;", dr["Name"]);
                dtTemp = Select(conn, sql);

                if (dtTemp == null || dtTemp.Rows.Count < 1)
                {
                    if (!initOpenState) { try { conn.Close(); } catch { } }
                    return null;
                }

                // 获取 函数 SQL 脚本
                strTemp = SqlFormat(dtTemp.Rows[0][2].ToString());

                try
                {
                    // 防止不同用户进行数据库还原导致的错误
                    dbDefinerAry = dr["Definer"].ToString().Split('@');
                    dbDefiner = string.Format(" DEFINER=`{0}`@`{1}`", dbDefinerAry[0], dbDefinerAry[1]);

                    strTemp = strTemp.Replace(dbDefiner, string.Empty);
                }
                // [loong] add start
                catch { strTemp = EraseDefiner(strTemp); }
                // [loong] add end

                // **** DELIMITER;; 做法不理解
                //strTemp = string.Format("DROP FUNCTION IF EXISTS `{0}`; DELIMITER ;; {1} DELIMITER ;;", dr["Name"], strTemp);
                strTemp = string.Format("DROP FUNCTION IF EXISTS `{0}`; {1};", dr["Name"], strTemp);

                // ===============================================================================================
                dtFunctions.Rows.Add(new object[] { sIndex, strTemp });

                sIndex++;
            }

            #endregion

            #region Procedures  (存储过程)

            sIndex = 1;
            dtProcedures.TableName = "Procedures";

            // Show Procedures
            sql = string.Format("SHOW PROCEDURE STATUS WHERE UPPER(TRIM(Db))= UPPER(TRIM('{0}'));", databaserName);
            dtTemp = Select(conn, sql);

            if (dtTemp == null)
            {
                if (!initOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            foreach (DataRow dr in dtTemp.Rows)
            {
                sql = string.Format("SHOW CREATE PROCEDURE `{0}`;", dr["Name"]);
                dtTableTemp = Select(conn, sql);

                if (dtTableTemp == null || dtTableTemp.Rows.Count < 1)
                {
                    if (!initOpenState) { try { conn.Close(); } catch { } }
                    return null;
                }

                // Get Sql Script
                strTemp = SqlFormat(dtTableTemp.Rows[0]["Create Procedure"].ToString());

                try
                {
                    // 防止不同用户进行数据库还原导致的错误
                    dbDefinerAry = dr["Definer"].ToString().Split('@');
                    dbDefiner = string.Format(" DEFINER=`{0}`@`{1}`", dbDefinerAry[0], dbDefinerAry[1]);

                    strTemp = strTemp.Replace(dbDefiner, string.Empty);
                }
                // [loong] add start
                catch { strTemp = EraseDefiner(strTemp); }
                // [loong] add end

                strTemp = string.Format("DROP PROCEDURE IF EXISTS `{0}`; {1};", dr["Name"], strTemp);

                // ===============================================================================================
                dtProcedures.Rows.Add(new object[] { sIndex, strTemp });

                sIndex++;
            }

            #endregion

            #region Events  (事件)

            sIndex = 1;
            dtEvents.TableName = "Events";

            // Show Events
            sql = string.Format("SHOW EVENTS WHERE UPPER(TRIM(Db)) = UPPER(TRIM('{0}'));", databaserName);
            dtTemp = Select(conn, sql);

            if (dtTemp == null)
            {
                if (!initOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            foreach (DataRow dr in dtTemp.Rows)
            {
                // Event Name
                sql = string.Format("SHOW CREATE EVENT `{0}`;", dr["Name"]);
                dtTableTemp = Select(conn, sql);

                if (dtTableTemp == null || dtTableTemp.Rows.Count < 1)
                {
                    if (!initOpenState) { try { conn.Close(); } catch { } }
                    continue;
                }

                // Get Event Sql Script
                strTemp = SqlFormat(dtTableTemp.Rows[0]["Create Event"].ToString());

                try
                {
                    // 防止不同用户进行数据库还原导致的错误
                    dbDefinerAry = dr["Definer"].ToString().Split('@');
                    dbDefiner = string.Format(" DEFINER=`{0}`@`{1}`", dbDefinerAry[0], dbDefinerAry[1]);

                    strTemp = strTemp.Replace(dbDefiner, string.Empty);
                }
                // [loong] add start
                catch { strTemp = EraseDefiner(strTemp); }
                // [loong] add end

                strTemp = string.Format("DROP EVENT IF EXISTS `{0}`; {1};", dr["Name"], strTemp);

                // ===============================================================================================
                dtEvents.Rows.Add(new object[] { sIndex, strTemp });

                sIndex++;
            }

            #endregion

            #region Triggers    (触发器)

            sIndex = 1;
            dtTriggers.TableName = "Triggers";

            // Show Triggers
            sql = "SHOW TRIGGERS;";
            dtTemp = Select(conn, sql);

            if (dtTemp == null)
            {
                if (!initOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            foreach (DataRow dr in dtTemp.Rows)
            {
                // Trigger Name
                sql = string.Format("SHOW CREATE TRIGGER `{0}`;", dr["Trigger"]);
                dtTableTemp = Select(conn, sql);

                if (dtTableTemp == null || dtTableTemp.Rows.Count < 1)
                {
                    if (!initOpenState) { try { conn.Close(); } catch { } }
                    continue;
                }

                // Get Trigger Sql Script
                strTemp = SqlFormat(dtTableTemp.Rows[0]["SQL Original Statement"].ToString());

                try
                {
                    // 防止不同用户进行数据库还原导致的错误
                    dbDefinerAry = dr["Definer"].ToString().Split('@');
                    dbDefiner = string.Format(" DEFINER=`{0}`@`{1}`", dbDefinerAry[0], dbDefinerAry[1]);

                    strTemp = strTemp.Replace(dbDefiner, string.Empty);
                }
                // [loong] add start
                catch { strTemp = EraseDefiner(strTemp); }
                // [loong] add end    

                strTemp = string.Format("DROP TRIGGER /*!50030 IF EXISTS */ `{0}`; {1};", dr["Trigger"], strTemp);

                // ===============================================================================================
                dtTriggers.Rows.Add(new object[] { sIndex, strTemp });

                sIndex++;
            }

            #endregion

            #region Views   (视图)

            sIndex = 1;
            dtViews.TableName = "Views";

            // Show Events
            sql = string.Format("SHOW FULL TABLES FROM `{0}` WHERE Table_type = 'VIEW';", databaserName);
            dtTemp = Select(conn, sql);

            if (dtTemp == null)
            {
                if (!initOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            foreach (DataRow dr in dtTemp.Rows)
            {
                // View Name
                sql = string.Format("SHOW CREATE VIEW `{0}`;", dr[0]);
                dtTableTemp = Select(conn, sql);

                if (dtTableTemp == null || dtTableTemp.Rows.Count < 1)
                {
                    if (!initOpenState) { try { conn.Close(); } catch { } }
                    continue;
                }

                // Get View Sql Script
                strTemp = SqlFormat(dtTableTemp.Rows[0]["Create View"].ToString());

                try
                {
                    // 防止不同用户进行数据库还原导致的错误  * 如果这里存在多个用户创建的不同视图就会有遗漏的
                    //dbDefinerAry = dr["Definer"].ToString().Split('@');
                    dbDefiner = string.Format(" DEFINER=`{0}`@`{1}`", dbDefinerAry[0], dbDefinerAry[1]);
                    strTemp = strTemp.Replace(dbDefiner, string.Empty);
                }
                // [loong] add start
                catch { strTemp = EraseDefiner(strTemp); }
                // [loong] add end

                strTemp = string.Format("DROP VIEW IF EXISTS `{0}`; {1};", dr[0], strTemp);

                // ===============================================================================================
                dtViews.Rows.Add(new object[] { sIndex, strTemp });

                sIndex++;
            }

            #endregion

            if (conn != null && conn.State == ConnectionState.Open)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            ds.Tables.Add(dtTableModels);
            ds.Tables.Add(dtTableDatas);
            ds.Tables.Add(dtFunctions);
            ds.Tables.Add(dtProcedures);
            ds.Tables.Add(dtEvents);
            ds.Tables.Add(dtTriggers);
            ds.Tables.Add(dtViews);

            return ds;
        }

        #region MySQL Data Format

        static System.Globalization.NumberFormatInfo _numberFormatInfo = new System.Globalization.NumberFormatInfo()
        {
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = string.Empty
        };

        static System.Globalization.DateTimeFormatInfo _dateFormatInfo = new System.Globalization.DateTimeFormatInfo()
        {
            DateSeparator = "-",
            TimeSeparator = ":"
        };

        #endregion

        static string RemoveAutoIncrement(string sql)
        {
            string a = "AUTO_INCREMENT=";

            if (sql.Contains(a))
            {
                int i = sql.LastIndexOf(a, StringComparison.Ordinal);

                int b = i + a.Length;

                string d = "";

                int count = 0;

                while (char.IsDigit(sql[b + count]))
                {
                    char cc = sql[b + count];

                    d = d + cc;

                    count = count + 1;
                }

                sql = sql.Replace(a + d, string.Empty);
            }

            return sql;
        }

        static string EscapeStringSequence(string data)
        {
            var builder = new StringBuilder();
            foreach (var ch in data)
            {
                switch (ch)
                {
                    case '\\': // Backslash
                        builder.AppendFormat("\\\\");
                        break;
                    case '\r': // Carriage return
                        builder.AppendFormat("\\r");
                        break;
                    case '\n': // New Line
                        builder.AppendFormat("\\n");
                        break;
                    case '\b': // Backspace
                        builder.AppendFormat("\\b");
                        break;
                    case '\t': // Horizontal tab
                        builder.AppendFormat("\\t");
                        break;
                    case '\"': // Double quotation mark
                        builder.AppendFormat("\\\"");
                        break;
                    case '\'': // Single quotation mark
                        builder.AppendFormat("''");
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }

        static string ConvertByteArrayToHexString(byte[] ba)
        {
            if (ba == null || ba.Length == 0)
                return "";
            // Method 1 (slower)
            //return "0x"+ BitConverter.ToString(bytes).Replace("-", string.Empty);

            // Method 2 (faster)
            char[] c = new char[ba.Length * 2 + 2];
            byte b;

            c[0] = '0'; c[1] = 'x';
            for (int y = 0, x = 2; y < ba.Length; ++y, ++x)
            {
                b = ((byte)(ba[y] >> 4));
                c[x] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = ((byte)(ba[y] & 0xF));
                c[++x] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }

            return new string(c);
        }

        /// <summary>
        /// MySQL 字段类型处理
        /// </summary>
        /// <param name="ob"></param>
        /// <param name="wrapStringWithSingleQuote"></param>
        /// <param name="escapeStringSequence"></param>
        /// <param name="dc"></param>
        /// <param name="dr"></param>
        /// <returns></returns>
        static string ConvertToSqlFormat(object ob, bool wrapStringWithSingleQuote, bool escapeStringSequence, DataColumn dc, DataRow dr)
        {
            #region 处理 MySQL 中 DateTime 类型字段长度

            int _timeFractionLength = 0;
            string _mysqlDataType = dr["Type"] + "";

            if (dc.DataType == typeof(DateTime))
            {
                if (_mysqlDataType.Length > 8)
                {
                    try
                    {
                        string _fractionLength = "";
                        foreach (var __dL in _mysqlDataType)
                        {
                            if (Char.IsNumber(__dL))
                                _fractionLength += System.Convert.ToString(__dL);
                        }

                        if (_fractionLength.Length > 0)
                        {
                            _timeFractionLength = 0;
                            int.TryParse(_fractionLength, out _timeFractionLength);
                        }
                    }
                    catch { }
                }
            }

            #endregion

            StringBuilder sb = new StringBuilder();

            if (ob == null || ob is System.DBNull)
            {
                sb.AppendFormat("NULL");
            }
            else if (ob is System.String)
            {
                string str = (string)ob;

                if (escapeStringSequence)
                    str = EscapeStringSequence(str);

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.Append(str);

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is System.Boolean)
            {
                sb.AppendFormat(System.Convert.ToInt32(ob).ToString());
            }
            else if (ob is System.Byte[])
            {
                if (((byte[])ob).Length == 0)
                {
                    if (wrapStringWithSingleQuote)
                        return "''";
                    else
                        return "";
                }
                else
                {
                    sb.AppendFormat(ConvertByteArrayToHexString((byte[])ob));
                }
            }
            else if (ob is short)
            {
                sb.AppendFormat(((short)ob).ToString(_numberFormatInfo));
            }
            else if (ob is int)
            {
                sb.AppendFormat(((int)ob).ToString(_numberFormatInfo));
            }
            else if (ob is long)
            {
                sb.AppendFormat(((long)ob).ToString(_numberFormatInfo));
            }
            else if (ob is ushort)
            {
                sb.AppendFormat(((ushort)ob).ToString(_numberFormatInfo));
            }
            else if (ob is uint)
            {
                sb.AppendFormat(((uint)ob).ToString(_numberFormatInfo));
            }
            else if (ob is ulong)
            {
                sb.AppendFormat(((ulong)ob).ToString(_numberFormatInfo));
            }
            else if (ob is double)
            {
                sb.AppendFormat(((double)ob).ToString(_numberFormatInfo));
            }
            else if (ob is decimal)
            {
                sb.AppendFormat(((decimal)ob).ToString(_numberFormatInfo));
            }
            else if (ob is float)
            {
                sb.AppendFormat(((float)ob).ToString(_numberFormatInfo));
            }
            else if (ob is byte)
            {
                sb.AppendFormat(((byte)ob).ToString(_numberFormatInfo));
            }
            else if (ob is sbyte)
            {
                sb.AppendFormat(((sbyte)ob).ToString(_numberFormatInfo));
            }
            else if (ob is TimeSpan)
            {
                TimeSpan ts = (TimeSpan)ob;

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.AppendFormat(ts.Hours.ToString().PadLeft(2, '0'));
                sb.AppendFormat(":");
                sb.AppendFormat(ts.Minutes.ToString().PadLeft(2, '0'));
                sb.AppendFormat(":");
                sb.AppendFormat(ts.Seconds.ToString().PadLeft(2, '0'));

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is System.DateTime)
            {
                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.AppendFormat(((DateTime)ob).ToString("yyyy-MM-dd HH:mm:ss", _dateFormatInfo));

                if (_timeFractionLength > 0)
                {
                    sb.Append(".");

                    // 支持 Millisecond 微秒
                    string _microsecond = "0";

                    try
                    {
                        _microsecond = ((DateTime)ob).ToString("ffff", _dateFormatInfo);
                    }
                    catch { }

                    if (_microsecond.Length < _timeFractionLength)
                    {
                        _microsecond = _microsecond.PadLeft(_timeFractionLength, '0');
                    }
                    else if (_microsecond.Length > _timeFractionLength)
                    {
                        _microsecond = _microsecond.Substring(0, _timeFractionLength);
                    }

                    sb.Append(_microsecond.ToString().PadLeft(_timeFractionLength, '0'));
                }

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is MySql.Data.Types.MySqlDateTime)
            {
                MySql.Data.Types.MySqlDateTime mdt = (MySql.Data.Types.MySqlDateTime)ob;

                if (mdt.IsNull)
                {
                    sb.AppendFormat("NULL");
                }
                else
                {
                    if (mdt.IsValidDateTime)
                    {
                        DateTime dtime = mdt.Value;

                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");

                        if (_mysqlDataType == "datetime")
                            sb.AppendFormat(dtime.ToString("yyyy-MM-dd HH:mm:ss", _dateFormatInfo));
                        else if (_mysqlDataType == "date")
                            sb.AppendFormat(dtime.ToString("yyyy-MM-dd", _dateFormatInfo));
                        else if (_mysqlDataType == "time")
                            sb.AppendFormat(dtime.ToString("HH:mm:ss", _dateFormatInfo));
                        else
                            sb.AppendFormat(dtime.ToString("yyyy-MM-dd HH:mm:ss", _dateFormatInfo));

                        // 支持 Microsecond 微秒
                        if (_timeFractionLength > 0)
                        {
                            sb.Append(".");
                            sb.Append((dtime.ToString("ffff", _dateFormatInfo).PadLeft(_timeFractionLength, '0')));
                        }

                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");
                    }
                    else
                    {
                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");

                        if (_mysqlDataType == "datetime")
                            sb.AppendFormat("0000-00-00 00:00:00");
                        else if (_mysqlDataType == "date")
                            sb.AppendFormat("0000-00-00");
                        else if (_mysqlDataType == "time")
                            sb.AppendFormat("00:00:00");
                        else
                            sb.AppendFormat("0000-00-00 00:00:00");

                        if (_timeFractionLength > 0)
                        {
                            sb.Append(".".PadRight(_timeFractionLength, '0'));
                        }

                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");
                    }
                }
            }
            else if (ob is System.Guid)
            {
                if (_mysqlDataType == "binary(16)")
                {
                    sb.Append(ConvertByteArrayToHexString(((Guid)ob).ToByteArray()));
                }
                else if (_mysqlDataType == "char(36)")
                {
                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("'");

                    sb.Append(ob);

                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("'");
                }
                else
                {
                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("'");

                    sb.Append(ob);

                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("'");
                }
            }
            else
            {
                throw new Exception("Unhandled data type. Current processing data type: " + ob.GetType().ToString() + ". Please report this bug with this message to the development team.");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 构建数据模版
        /// </summary>
        /// <returns></returns>
        static DataTable DataTableTemplate()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Id", System.Type.GetType("System.Int64"));
            dt.Columns.Add("SQL", System.Type.GetType("System.String"));
            return dt;
        }

        /// <summary>
        /// SQL 字符串格式化处理（整理\r，\n）
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        static string SqlFormat(string sql)
        {
            return sql.Replace(Environment.NewLine, "^~~~~~~^").Replace("\r", "^~~~~~~^").Replace("\n", "^~~~~~~^").Replace("^~~~~~~^", Environment.NewLine);
        }

        // [loong] add start
        /// <summary>
        /// 清除数据库用户信息 
        /// (解决用户名不一致导致的错误)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string EraseDefiner(string input)
        {
            StringBuilder sb = new StringBuilder();
            string definer = " DEFINER=";
            int dIndex = input.ToUpper().IndexOf(definer, StringComparison.Ordinal);

            sb.AppendFormat(definer);

            bool pointAliasReached = false;
            bool point3rdQuoteReached = false;

            for (int i = dIndex + definer.Length; i < input.Length; i++)
            {
                if (!pointAliasReached)
                {
                    if (input[i] == '@')
                        pointAliasReached = true;

                    sb.Append(input[i]);
                    continue;
                }

                if (!point3rdQuoteReached)
                {
                    if (input[i] == '`')
                        point3rdQuoteReached = true;

                    sb.Append(input[i]);
                    continue;
                }

                if (input[i] != '`')
                {
                    sb.Append(input[i]);
                    continue;
                }
                else
                {
                    sb.Append(input[i]);
                    break;
                }
            }

            return input.Replace(sb.ToString(), string.Empty);
        }
        // [loong] add end
        #endregion

        #endregion

        #region RestoreDatabase, 2015.6.12 Loong 修改了机制

        /// <summary>
        /// 恢复数据库(从二进制压缩流中提取表数据库的 XML 进行恢复)
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <returns></returns>
        public static int RestoreDataFromZipStream(byte[] dataBuffer)
        {
            return RestoreDataFromZipStream(GetConnectionStringFromConfig(), dataBuffer);
        }

        /// <summary>
        /// 恢复数据库(从二进制压缩流中提取表数据库的 XML 进行恢复)
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="dataBuffer"></param>
        /// <returns></returns>
        public static int RestoreDataFromZipStream(string connectionString, byte[] dataBuffer)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(connectionString);

            if (conn == null)
            {
                return -1001;
            }

            int result = RestoreDataFromZipStream(conn, dataBuffer);

            try
            {
                conn.Close();
            }
            catch { }

            return result;
        }

        /// <summary>
        /// 恢复数据库(从二进制压缩流中提取表数据库的 XML 进行恢复)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="dataBuffer"></param>
        /// <returns></returns>
        public static int RestoreDataFromZipStream(MySqlConnection conn, byte[] dataBuffer)
        {
            if (conn == null)
            {
                return -1001;
            }

            bool initOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                initOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return -1002;
                }
            }

            string XmlData = String.Decompress(dataBuffer);

            if (string.IsNullOrEmpty(XmlData))
            {
                if (!initOpenState)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                }

                return -1003;
            }

            StringReader sr = new StringReader(XmlData);
            DataSet ds = new DataSet();

            try
            {
                ds.ReadXml(sr);
            }
            catch
            {
                if (!initOpenState)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                }

                return -1004;
            }

            if ((ds == null) || (ds.Tables.Count < 1))
            {
                if (!initOpenState)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                }

                return -1005;
            }

            // [loong] update add
            StringBuilder sbSQL = new StringBuilder();

            foreach (DataTable dt in ds.Tables)
            {
                if (dt.TableName.ToLower().Trim().IndexOf("views", StringComparison.Ordinal) > -1)
                {
                    continue;
                }

                if (dt == null) { continue; }

                foreach (DataRow dr in dt.Rows)
                {
                    sbSQL.AppendLine(dr["SQL"].ToString());
                }
            }

            MySqlTransaction trans = null;
            MySqlCommand cmd = new MySqlCommand();

            cmd.CommandTimeout = 600 * 2; // (20分钟)
            cmd.Connection = conn;

            trans = conn.BeginTransaction();
            cmd.Transaction = trans;
            cmd.CommandText = sbSQL.ToString();

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                trans.Rollback();

                throw new Exception("ExecuteNoQuery Error", ex);
            }

            // Views Start

            long sIndex = 0;
            int errorTryCount = 15;  // 重试次数
            DataTable dtTemp = null;
            DataTable dtViews = ds.Tables["Views"];
            long execResult = -1;

            while (dtViews.Rows.Count > 0 && errorTryCount > 0)
            {
                dtTemp = new DataTable();
                dtTemp = dtViews.Clone();

                foreach (DataRow dr in dtViews.Rows)
                {
                    sIndex = Convert.StrToLong(dr["Id"].ToString(), -1);

                    cmd.CommandText = dr["SQL"].ToString();

                    try
                    {
                        execResult = cmd.ExecuteNonQuery();

                        if (execResult < 0)
                        {
                            dtTemp.ImportRow(dr);
                        }
                    }
                    catch
                    {
                        dtTemp.ImportRow(dr);
                    }
                }

                dtViews = dtTemp;
                errorTryCount--;
            }

            if (errorTryCount < 1)
            {
                trans.Rollback();
                return -1010;
            }

            // Views End

            trans.Commit();

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            // [loong] update end
            return 0;
        }

        #endregion

        #region ShoveDAL 工具通过一下四个类生成表、视图的访问的持久化代码

        /// <summary>
        /// 表的字段类，ShoveDAL.30 工具使用
        /// </summary>
        public class Field
        {
            private object _Value;

            /// <summary>
            /// 
            /// </summary>
            public object Parent;
            /// <summary>
            /// 
            /// </summary>
            public string Name;
            /// <summary>
            /// 
            /// </summary>
            public string CanonicalIdentifierName;
            /// <summary>
            /// 
            /// </summary>
            public MySqlDbType DbType;
            /// <summary>
            /// 
            /// </summary>
            public bool ReadOnly;
            /// <summary>
            /// 
            /// </summary>
            public object Value
            {
                get
                {
                    return _Value;
                }
                set
                {
                    if (ReadOnly)
                    {
                        throw new Exception("the member “" + Name + "” is ReadOnly.");
                    }

                    _Value = value;

                    if (Parent != null)
                    {
                        ((TableBase)Parent).Fields.Add(this);
                    }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="name"></param>
            /// <param name="canonicalIdentifierName"></param>
            /// <param name="dbtype"></param>
            /// <param name="_readonly"></param>
            public Field(object parent, string name, string canonicalIdentifierName, MySqlDbType dbtype, bool _readonly)
            {
                Parent = parent;
                Name = name;
                CanonicalIdentifierName = canonicalIdentifierName;
                DbType = dbtype;
                ReadOnly = _readonly;
            }
        }

        /// <summary>
        /// 表的修改的字段集合，ShoveDAL.30 工具使用
        /// </summary>
        public class FieldCollection
        {
            private IList<Field> fields = new List<Field>();

            /// <summary>
            /// 
            /// </summary>
            public int Count
            {
                get
                {
                    return fields.Count;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="field"></param>
            public void Add(Field field)
            {
                fields.Add(field);
            }

            /// <summary>
            /// 
            /// </summary>
            public void Clear()
            {
                fields.Clear();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            public Field this[int index]
            {
                get
                {
                    if ((Count < 1) || (index < 0) || (index > Count))
                    {
                        return null;
                    }

                    return fields[index];
                }
            }
        }

        /// <summary>
        /// 表的基类，ShoveDAL.30 工具使用
        /// </summary>
        public class TableBase
        {
            /// <summary>
            /// 
            /// </summary>
            public string TableName = "";
            /// <summary>
            /// 
            /// </summary>
            public FieldCollection Fields = new FieldCollection();

            #region Open

            /// <summary>
            /// 打开表
            /// </summary>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <param name="limitStart">从第几条开始，小于 0 表示不限 </param>
            /// <param name="limitCount">检索多少条记录，小于 1 表示不限</param>
            /// <returns></returns>
            public DataTable Open(string fieldList, string condition, string order, long limitStart, long limitCount)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                string Limit = "";

                if (limitStart >= 0)
                {
                    Limit = " limit " + limitStart.ToString();

                    if (limitCount >= 1)
                    {
                        Limit += ", " + limitCount.ToString();
                    }
                }

                return Select("select " + (fieldList == "" ? "*" : fieldList) + " from `" + TableName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)) + Limit);
            }

            /// <summary>
            /// 打开表
            /// </summary>
            /// <param name="connectionString"></param>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <param name="limitStart">从第几条开始，小于 0 表示不限 </param>
            /// <param name="limitCount">检索多少条记录，小于 1 表示不限</param>
            /// <returns></returns>
            public DataTable Open(string connectionString, string fieldList, string condition, string order, long limitStart, long limitCount)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                string Limit = "";

                if (limitStart >= 0)
                {
                    Limit = " limit " + limitStart.ToString();

                    if (limitCount >= 1)
                    {
                        Limit += ", " + limitCount.ToString();
                    }
                }

                return Select(connectionString, "select " + (fieldList == "" ? "*" : fieldList) + " from `" + TableName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)) + Limit);
            }

            /// <summary>
            /// 打开表
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <param name="limitStart">从第几条开始，小于 0 表示不限 </param>
            /// <param name="limitCount">检索多少条记录，小于 1 表示不限</param>
            /// <returns></returns>
            public DataTable Open(MySqlConnection conn, string fieldList, string condition, string order, long limitStart, long limitCount)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                string Limit = "";

                if (limitStart >= 0)
                {
                    Limit = " limit " + limitStart.ToString();

                    if (limitCount >= 1)
                    {
                        Limit += ", " + limitCount.ToString();
                    }
                }

                return Select(conn, "select " + (fieldList == "" ? "*" : fieldList) + " from `" + TableName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)) + Limit);
            }

            #endregion

            #region GetCount

            /// <summary>
            /// 获取表记录数
            /// </summary>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long GetCount(string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar("select count(*) from `" + TableName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

                if (result == null)
                {
                    return 0;
                }

                return long.Parse(result.ToString());
            }

            /// <summary>
            /// 获取表记录数
            /// </summary>
            /// <param name="connectionString"></param>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long GetCount(string connectionString, string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar(connectionString, "select count(*) from `" + TableName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

                if (result == null)
                {
                    return 0;
                }

                return long.Parse(result.ToString());
            }

            /// <summary>
            /// 获取表记录数
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long GetCount(MySqlConnection conn, string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar(conn, "select count(*) from `" + TableName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

                if (result == null)
                {
                    return 0;
                }

                return long.Parse(result.ToString());
            }

            #endregion

            #region Insert

            /// <summary>
            /// 增加记录
            /// </summary>
            /// <returns>小于0表示失败，0表示成功，无自增值，大于0表示自增值</returns>
            public long Insert()
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                string InsertFieldsList = "";
                string InsertValuesList = "";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        InsertFieldsList += ", ";
                        InsertValuesList += ", ";
                    }

                    InsertFieldsList += "`" + Fields[i].Name + "`";
                    InsertValuesList += "?" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                string commandText = "insert into `" + TableName + "` (" + InsertFieldsList + ") values (" + InsertValuesList + "); select ifnull(LAST_INSERT_ID(), -99999999)";

                object objResult = ExecuteScalar(commandText, Parameters);

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long result = (long)objResult;

                if (result == -99999999)
                {
                    return 0;
                }

                return result;
            }

            /// <summary>
            /// 增加记录
            /// </summary>
            /// <param name="connectionString"></param>
            /// <returns>小于0表示О埽?示晒Γ?拮栽鲋担?笥?示自增值</returns>
            public long Insert(string connectionString)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                string InsertFieldsList = "";
                string InsertValuesList = "";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        InsertFieldsList += ", ";
                        InsertValuesList += ", ";
                    }

                    InsertFieldsList += "`" + Fields[i].Name + "`";
                    InsertValuesList += "?" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                string commandText = "insert into `" + TableName + "` (" + InsertFieldsList + ") values (" + InsertValuesList + "); select ifnull(LAST_INSERT_ID(), -99999999)";

                object objResult = ExecuteScalar(connectionString, commandText, Parameters);

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long result = (long)objResult;

                if (result == -99999999)
                {
                    return 0;
                }

                return result;
            }

            /// <summary>
            /// 增加记录
            /// </summary>
            /// <param name="conn"></param>
            /// <returns>小于0表示失败，0表示成功，无自增值，大于0表示自增值</returns>
            public long Insert(MySqlConnection conn)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                string InsertFieldsList = "";
                string InsertValuesList = "";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        InsertFieldsList += ", ";
                        InsertValuesList += ", ";
                    }

                    InsertFieldsList += "`" + Fields[i].Name + "`";
                    InsertValuesList += "?" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                string commandText = "insert into `" + TableName + "` (" + InsertFieldsList + ") values (" + InsertValuesList + "); select ifnull(LAST_INSERT_ID(), -99999999)";

                object objResult = ExecuteScalar(conn, commandText, Parameters);

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long result = (long)objResult;

                if (result == -99999999)
                {
                    return 0;
                }

                return result;
            }

            #endregion

            #region Delete

            /// <summary>
            /// 删除表记录
            /// </summary>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long Delete(string condition)
            {
                condition = condition.Trim();

                object objResult = ExecuteScalar("delete from `" + TableName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + "; select ifnull(ROW_COUNT(), -99999999)");

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long result = (long)objResult;

                if (result == -99999999)
                {
                    return 0;
                }

                return result;
            }

            /// <summary>
            /// 删除表记录
            /// </summary>
            /// <param name="connectionString"></param>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long Delete(string connectionString, string condition)
            {
                condition = condition.Trim();

                object objResult = ExecuteScalar(connectionString, "delete from `" + TableName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + "; select ifnull(ROW_COUNT(), -99999999)");

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long result = (long)objResult;

                if (result == -99999999)
                {
                    return 0;
                }

                return result;
            }

            /// <summary>
            /// 删除表记录
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long Delete(MySqlConnection conn, string condition)
            {
                condition = condition.Trim();

                object objResult = ExecuteScalar(conn, "delete from `" + TableName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + "; select ifnull(ROW_COUNT(), -99999999)");

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long result = (long)objResult;

                if (result == -99999999)
                {
                    return 0;
                }

                return result;
            }

            #endregion

            #region Update

            /// <summary>
            /// 更新表
            /// </summary>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long Update(string condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                condition = condition.Trim();

                string commandText = "update `" + TableName + "` set ";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        commandText += ", ";
                    }

                    commandText += "`" + Fields[i].Name + "` = ?" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                if (!string.IsNullOrEmpty(condition))
                {
                    commandText += " where " + FilteSqlInfusionForCondition(condition);
                }

                commandText += "; select ifnull(ROW_COUNT(), -99999999)";

                object objResult = ExecuteScalar(commandText, Parameters);

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long result = (long)objResult;

                if (result == -99999999)
                {
                    return 0;
                }

                return result;
            }

            /// <summary>
            /// 更新表
            /// </summary>
            /// <param name="connectionString"></param>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long Update(string connectionString, string condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                condition = condition.Trim();

                string commandText = "update `" + TableName + "` set ";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        commandText += ", ";
                    }

                    commandText += "`" + Fields[i].Name + "` = ?" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                if (!string.IsNullOrEmpty(condition))
                {
                    commandText += " where " + FilteSqlInfusionForCondition(condition);
                }

                commandText += "; select ifnull(ROW_COUNT(), -99999999)";

                object objResult = ExecuteScalar(connectionString, commandText, Parameters);

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long result = (long)objResult;

                if (result == -99999999)
                {
                    return 0;
                }

                return result;
            }

            /// <summary>
            /// 更新表
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long Update(MySqlConnection conn, string condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                condition = condition.Trim();

                string commandText = "update `" + TableName + "` set ";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        commandText += ", ";
                    }

                    commandText += "`" + Fields[i].Name + "` = ?" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                if (!string.IsNullOrEmpty(condition))
                {
                    commandText += " where " + FilteSqlInfusionForCondition(condition);
                }

                commandText += "; select ifnull(ROW_COUNT(), -99999999)";

                object objResult = ExecuteScalar(conn, commandText, Parameters);

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long result = (long)objResult;

                if (result == -99999999)
                {
                    return 0;
                }

                return result;
            }

            #endregion
        }

        /// <summary>
        /// 视图的基类，ShoveDAL.30 工具使用
        /// </summary>
        public class ViewBase
        {
            /// <summary>
            /// 视图名称
            /// </summary>
            public string ViewName = "";

            #region Open

            /// <summary>
            /// 打开视图
            /// </summary>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <param name="limitStart">从第几条开始，小于 0 表示不限 </param>
            /// <param name="limitCount">检索多少条记录，小于 1 表示不限</param>
            /// <returns></returns>
            public DataTable Open(string fieldList, string condition, string order, long limitStart, long limitCount)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                string Limit = "";

                if (limitStart >= 0)
                {
                    Limit = " limit " + limitStart.ToString();

                    if (limitCount >= 1)
                    {
                        Limit += ", " + limitCount.ToString();
                    }
                }

                return Select("select " + (fieldList == "" ? "*" : fieldList) + " from `" + ViewName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)) + Limit);
            }

            /// <summary>
            /// 打开视图
            /// </summary>
            /// <param name="connectionString"></param>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <param name="limitStart">从第几条开始，小于 0 表示不限 </param>
            /// <param name="limitCount">检索多少条记录，小于 1 表示不限</param>
            /// <returns></returns>
            public DataTable Open(string connectionString, string fieldList, string condition, string order, long limitStart, long limitCount)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                string Limit = "";

                if (limitStart >= 0)
                {
                    Limit = " limit " + limitStart.ToString();

                    if (limitCount >= 1)
                    {
                        Limit += ", " + limitCount.ToString();
                    }
                }

                return Select(connectionString, "select " + (fieldList == "" ? "*" : fieldList) + " from `" + ViewName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)) + Limit);
            }

            /// <summary>
            /// 打开视图
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <param name="limitStart">从第几条开始，小于 0 表示不限 </param>
            /// <param name="limitCount">检索多少条记录，小于 1 表示不限</param>
            /// <returns></returns>
            public DataTable Open(MySqlConnection conn, string fieldList, string condition, string order, long limitStart, long limitCount)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                string Limit = "";

                if (limitStart >= 0)
                {
                    Limit = " limit " + limitStart.ToString();

                    if (limitCount >= 1)
                    {
                        Limit += ", " + limitCount.ToString();
                    }
                }

                return Select(conn, "select " + (fieldList == "" ? "*" : fieldList) + " from `" + ViewName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)) + Limit);
            }

            #endregion

            #region GetCount

            /// <summary>
            /// 获取视图记录数
            /// </summary>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long GetCount(string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar("select count(*) from `" + ViewName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

                if (result == null)
                {
                    return 0;
                }

                return long.Parse(result.ToString());
            }

            /// <summary>
            /// 获取视图记录数
            /// </summary>
            /// <param name="connectionString"></param>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long GetCount(string connectionString, string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar(connectionString, "select count(*) from `" + ViewName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

                if (result == null)
                {
                    return 0;
                }

                return long.Parse(result.ToString());
            }

            /// <summary>
            /// 获取视图记录数
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long GetCount(MySqlConnection conn, string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar(conn, "select count(*) from `" + ViewName + "`" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

                if (result == null)
                {
                    return 0;
                }

                return long.Parse(result.ToString());
            }

            #endregion
        }

        #endregion
    }
}
