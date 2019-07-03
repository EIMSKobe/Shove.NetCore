using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Data.SqlClient;

namespace Shove.Database
{
    /// <summary>
    /// Shove 的专用的 MS SQLServer 访问组件类
    /// </summary>
    public class MSSQL : DatabaseAccess
    {
        /// <summary>
        /// 获取包含 Owner 的完整对象名称，如：T_Users -> [dbo].[T_Users]，Owner 在 .Config 中配置
        /// </summary>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public static string GetObjectFullName(string objectName)
        {
            if (objectName.IndexOf(".", StringComparison.Ordinal) >= 0)
            {
                return objectName;
            }

            if (!objectName.StartsWith("[", StringComparison.Ordinal) || !objectName.EndsWith("]", StringComparison.Ordinal))
            {
                objectName = "[" + objectName + "]";
            }

            string SQLServer_owner = AppConfigurtaionServices.GetAppSettingsString("SQLServer_owner");
            if (SQLServer_owner == null)
            {
                SQLServer_owner = "";
            }
            else
            {
                SQLServer_owner = SQLServer_owner.Trim();
            }

            if (SQLServer_owner.Trim() == "")
            {
                SQLServer_owner = "dbo";
            }

            if (!SQLServer_owner.StartsWith("[", StringComparison.Ordinal) || !SQLServer_owner.EndsWith("]", StringComparison.Ordinal))
            {
                SQLServer_owner = "[" + SQLServer_owner + "]";
            }

            return SQLServer_owner + "." + objectName;
        }

        #region BuildConnectString

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="server"></param>
        /// <param name="database"></param>
        /// <returns></returns>
        public static string BuildConnectString(string server, string database)
        {
            return string.Format("data source=\"{0}\";persist security info=False;initial catalog=\"{1}\"", server, database);
        }

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="server"></param>
        /// <param name="database"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string BuildConnectString(string server, string database, string user, string password)
        {
            return string.Format("PWD={0};UID={1};data source=\"{2}\";persist security info=False;initial catalog=\"{3}\"", password, user, server, database);
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
            public SqlDbType Type;
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
            public Parameter(string name, SqlDbType type, int size, ParameterDirection direction, object value)
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
            /// <param name="name"></param>
            /// <param name="value"></param>
            public void Add(string name, object value)
            {
                ParametersName.Add(name.StartsWith("@", StringComparison.Ordinal) ? name.Substring(1) : name);
                ParametersValue.Add(value);
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

        private static void AddParameter(ref SqlCommand cmd, params Parameter[] _params)
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

                SqlParameter param = new SqlParameter
                {
                    ParameterName = _params[i].Name.StartsWith("@", StringComparison.Ordinal) ? _params[i].Name : ("@" + _params[i].Name),
                    SqlDbType = _params[i].Type
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

        private static void AddOutputParameter(SqlCommand cmd, ref OutputParameter outputs)
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
                SqlParameter param = cmd.Parameters[i];

                if ((param.Direction != ParameterDirection.InputOutput) &&
                    (param.Direction != ParameterDirection.Output))
                {
                    continue;
                }

                outputs.Add(param.ParameterName, param.Value);
            }
        }

        private static SqlParameter GetReturnParameter(SqlCommand cmd)
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
                SqlParameter param = cmd.Parameters[i];

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
            SqlConnection conn = CreateDataConnection<SqlConnection>(connectionString);

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
        public static int ExecuteNonQuery(SqlConnection conn, string commandText, params Parameter[] _params)
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

            SqlCommand cmd = new SqlCommand(commandText, conn);
            AddParameter(ref cmd, _params);

            SqlTransaction trans;
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
        public static int ExecuteNonQueryNoTranscation(SqlConnection conn, string commandText, params Parameter[] _params)
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

            SqlCommand cmd = new SqlCommand(commandText, conn);
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
            SqlConnection conn = CreateDataConnection<SqlConnection>(connectionString);

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
        /// 蚩??菁?
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static DataTable Select(SqlConnection conn, string commandText, params Parameter[] _params)
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

            SqlDataAdapter da = new SqlDataAdapter("", conn);
            SqlCommand cmd = new SqlCommand(commandText, conn);

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
            SqlConnection conn = CreateDataConnection<SqlConnection>(connectionString);

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
        public static object ExecuteScalar(SqlConnection conn, string commandText, params Parameter[] _params)
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

            SqlCommand cmd = new SqlCommand(commandText, conn);
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
            SqlConnection conn = CreateDataConnection<SqlConnection>(connectionString);

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
        public static object ExecuteFunction(SqlConnection conn, string functionName, params Parameter[] _params)
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

            string commandText = "select " + GetObjectFullName(functionName) + "(";

            if (_params != null)
            {
                for (int i = 0; i < _params.Length; i++)
                {
                    if (_params[i] != null)
                    {
                        bool isChar = false;
                        bool isNChar = false;

                        if ((_params[i].Type == SqlDbType.Char) || (_params[i].Type == SqlDbType.DateTime) || (_params[i].Type == SqlDbType.SmallDateTime) ||
                            (_params[i].Type == SqlDbType.Text) || (_params[i].Type == SqlDbType.UniqueIdentifier) || (_params[i].Type == SqlDbType.VarChar))
                        {
                            isChar = true;
                        }

                        isNChar |= ((_params[i].Type == SqlDbType.NChar) || (_params[i].Type == SqlDbType.NText) || (_params[i].Type == SqlDbType.NVarChar));

                        if (!commandText.EndsWith("(", StringComparison.Ordinal))
                        {
                            commandText += ", ";
                        }

                        if (isChar)
                        {
                            commandText += "\'";
                        }

                        if (isNChar)
                        {
                            commandText += "N\'";
                        }

                        commandText += _params[i].Value.ToString();

                        if (isChar || isNChar)
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
            SqlConnection conn = CreateDataConnection<SqlConnection>(connectionString);

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
        public static int ExecuteStoredProcedureNonQuery(SqlConnection conn, string storedProcedureName, ref OutputParameter outputs, params Parameter[] _params)
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

            SqlCommand cmd = new SqlCommand(GetObjectFullName(storedProcedureName), conn);
            cmd.CommandType = CommandType.StoredProcedure;

            AddParameter(ref cmd, _params);

            // 增加返回值参数
            SqlParameter returnValue = new SqlParameter("@Shove_Database_MSSQL_ExecuteStoredProcedureNonQuery_Rtn", SqlDbType.Int)
            {
                Direction = ParameterDirection.ReturnValue
            };
            cmd.Parameters.Add(returnValue);

            SqlTransaction trans;
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

            if (!result)
            {
                return -1002;
            }

            // 填写返回参数
            AddOutputParameter(cmd, ref outputs);

            // 获取过程的返回值
            returnValue = GetReturnParameter(cmd);

            if (returnValue != null)
            {
                return (int)returnValue.Value;
            }

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
            SqlConnection conn = CreateDataConnection<SqlConnection>(connectionString);

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
        public static int ExecuteStoredProcedureWithQuery(SqlConnection conn, string storedProcedureName, ref DataSet ds, ref OutputParameter outputs, params Parameter[] _params)
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

            SqlDataAdapter da = new SqlDataAdapter("", conn);
            SqlCommand cmd = new SqlCommand(GetObjectFullName(storedProcedureName), conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Clear();
            AddParameter(ref cmd, _params);

            // 增加返回值参数
            SqlParameter returnValue = new SqlParameter("@Shove_Database_MSSQL_ExecuteStoredProcedureWithQuery_Rtn", SqlDbType.Int)
            {
                Direction = ParameterDirection.ReturnValue
            };
            cmd.Parameters.Add(returnValue);

            if (ds == null)
            {
                ds = new DataSet();
            }

            SqlTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                cmd.Dispose();
                da.Dispose();
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
            finally
            {
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

            if (!result)
            {
                return -1002;
            }

            //填写返回参数
            AddOutputParameter(cmd, ref outputs);
            // 获取过程的返回值
            returnValue = GetReturnParameter(cmd);

            cmd.Dispose();

            if (returnValue != null)
            {
                return (int)returnValue.Value;
            } 
            
            return 0;
        }

        #endregion

        #region BackupDatabase

        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="backupFileName">包含绝对路径的文件名，注意：此路径是相对于数据库所在的服务器而言的</param>
        /// <param name="breakLog"></param>
        /// <param name="shrink"></param>
        /// <returns></returns>
        public static int BackupDatabase(string backupFileName, bool breakLog, bool shrink)
        {
            return BackupDatabase(GetConnectionStringFromConfig(), backupFileName, breakLog, shrink);
        }

        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="backupFileName">包含绝对路径的文件名，注意：此路径是相对于数据库所在的服务器而言的</param>
        /// <param name="breakLog"></param>
        /// <param name="shrink"></param>
        /// <returns></returns>
        public static int BackupDatabase(string connectionString, string backupFileName, bool breakLog, bool shrink)
        {
            SqlConnection conn = CreateDataConnection<SqlConnection>(connectionString);

            if (conn == null)
            {
                return -1001;
            }

            int result = BackupDatabase(conn, backupFileName, breakLog, shrink);

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
        /// 备份数据库
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="backupFileName">包含绝对路径的文件名，注意：此路径是相对于数据库所在的服务器而言的</param>
        /// <param name="breakLog"></param>
        /// <param name="shrink"></param>
        /// <returns></returns>
        public static int BackupDatabase(SqlConnection conn, string backupFileName, bool breakLog, bool shrink)
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

            string DatabaseName = conn.Database;

            if (!DatabaseName.StartsWith("[", StringComparison.Ordinal))
            {
                DatabaseName = "[" + DatabaseName + "]";
            }

            if (ExecuteNonQueryNoTranscation(conn, "use master") < 0)
            {
                if (!initOpenState)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                }

                return -1002;
            }

            if (breakLog)
            {
                if (ExecuteNonQueryNoTranscation(conn, "backup log " + DatabaseName + " with no_log") < 0)
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
            }

            if (shrink)
            {
                if (ExecuteNonQueryNoTranscation(conn, "DBCC SHRINKDATABASE (" + DatabaseName + ", 0)") < 0)
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
            }

            if (ExecuteNonQueryNoTranscation(conn, "Backup database " + DatabaseName + " to disk='" + backupFileName + "' with INIT") < 0)
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

            if (ExecuteNonQueryNoTranscation(conn, "use " + DatabaseName) < 0)
            {
                if (!initOpenState)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                }

                return -1006;
            }

            if (!IO.File.Compress(backupFileName))
            {
                if (!initOpenState)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                }

                return -1007;
            }

            return 0;
        }

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
            SqlConnection conn = CreateDataConnection<SqlConnection>(connectionString);

            if (conn == null)
            {
                return null;
            }

            byte[] result = BackupDataToZipStream(conn);

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
        /// 备份数据库(表数据库 XML 描述，压缩为二进制流)
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static byte[] BackupDataToZipStream(SqlConnection conn)
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

            DataTable dt = Select(conn, "Select * from sysobjects where OBJECTPROPERTY(id, N'IsUserTable') = 1 and OBJECTPROPERTY(id,N'IsMSShipped')=0 and [name] <> 'sysdiagrams'");

            if (dt == null)
            {
                if (!initOpenState)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                }

                return null;
            }

            DataSet ds = new DataSet(conn.Database);

            foreach (DataRow dr in dt.Rows)
            {
                string TableName = dr["name"].ToString();

                using (DataTable Table = Select(conn, "select * from [" + TableName + "]"))
                {
                    if (Table == null)
                    {
                        if (!initOpenState)
                        {
                            try
                            {
                                conn.Close();
                            }
                            catch { }
                        }

                        dt.Dispose();
                        return null;
                    }

                    Table.TableName = TableName;
                    ds.Tables.Add(Table);
                }
            }

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            ds.WriteXml(sw, XmlWriteMode.WriteSchema);
            ds.Dispose();
            dt.Dispose();

            return String.Compress(sb.ToString());
        }

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
            SqlConnection conn = CreateDataConnection<SqlConnection>(connectionString);

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
            finally
            {
                conn.Dispose();
            }

            return result;
        }

        /// <summary>
        /// 恢复数据库(从二进制压缩流中提取表数据库的 XML 进行恢复)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="dataBuffer"></param>
        /// <returns></returns>
        public static int RestoreDataFromZipStream(SqlConnection conn, byte[] dataBuffer)
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

                return -1002;
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

                ds.Dispose();
                sr.Dispose();

                return -1003;
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

                ds.Dispose();
                sr.Dispose();

                return -1004;
            }

            // 开始恢复数据
            SqlTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                ds.Dispose();
                sr.Dispose();

                return -1001;
            }

            bool result;

            try
            {
                foreach (DataTable dt in ds.Tables)
                {
                    SqlCommand cmd = new SqlCommand("truncate table [" + dt.TableName + "]", conn);
                    cmd.Transaction = trans;
                    cmd.ExecuteNonQuery();
                    //cmd.CommandText = "SET IDENTITY_INSERT [" + dt.TableName + "] ON";
                    //cmd.ExecuteNonQuery();
                    cmd.Dispose();

                    using (SqlBulkCopy sqlbulkcopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, trans)
                    {
                        DestinationTableName = dt.TableName
                    })
                    {
                        sqlbulkcopy.WriteToServer(dt);
                    }
                }

                trans.Commit();
                result = true;
            }
            catch//(Exception ee)
            {
                trans.Rollback();
                result = false;

                //System.Web.HttpContext.Current.Response.Write(ee.Message);
            }
            finally
            {
                ds.Dispose();
                sr.Dispose();
            }

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return result ? 0 : -1005;
        }

        #endregion

        #region Execute SQL Script

        /// <summary>
        /// 执行 SQL 脚本
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public static bool ExecuteSQLScript(string script)
        {
            script = script.Trim();

            if (script == "")
            {
                return true;
            }

            return ExecuteSQLScript(GetConnectionStringFromConfig(), script);
        }

        /// <summary>
        /// 执行 SQL 脚本
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static bool ExecuteSQLScript(string connectionString, string script)
        {
            script = script.Trim();

            if (script == "")
            {
                return true;
            }

            SqlConnection conn = CreateDataConnection<SqlConnection>(connectionString);

            if (conn == null)
            {
                return false;
            }

            bool result = ExecuteSQLScript(conn, script);

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
        /// 执行 SQL 脚本
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static bool ExecuteSQLScript(SqlConnection conn, string script)
        {
            script = script.Trim();

            if (script == "")
            {
                return true;
            }

            Regex regex = new Regex(@"/[*][\S\s\t\r\n\v\f]*?[*]/", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            script = regex.Replace(script, "");

            regex = new Regex(@"--[^\n]*?[\n]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            script = regex.Replace(script, "\r\n");

            script = script.Trim();

            if (script == "")
            {
                return false;
            }

            script += "\r\n";

            regex = new Regex(@"[\n]GO[\r\t\v\s]*?[\n]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            string[] scripts = regex.Split(script);

            if ((scripts == null) || (scripts.Length == 0))
            {
                return false;
            }

            if (conn == null)
            {
                return false;
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
                    return false;
                }
            }

            SqlCommand cmd = new SqlCommand("", conn)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.Clear();

            SqlTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                cmd.Dispose();
                return false;
            }

            cmd.Transaction = trans;

            foreach (string str in scripts)
            {
                string strCmd = str.Trim();

                if (strCmd == "")
                {
                    continue;
                }

                cmd.CommandText = strCmd;

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    try
                    {
                        trans.Rollback();
                    }
                    catch { }

                    if (!initOpenState)
                    {
                        try
                        {
                            conn.Close();
                        }
                        catch { }
                    }

                    cmd.Dispose();
                    return false;
                }
            }

            trans.Commit();

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            cmd.Dispose();

            return true;
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
            public SqlDbType DbType;
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
            public Field(object parent, string name, string canonicalIdentifierName, SqlDbType dbtype, bool _readonly)
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
            /// 表名
            /// </summary>
            public string TableName = "";
            /// <summary>
            /// 字段集合
            /// </summary>
            public FieldCollection Fields = new FieldCollection();

            /// <summary>
            /// 打开表
            /// </summary>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <returns></returns>
            public DataTable Open(string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select("select " + (fieldList == "" ? "*" : fieldList) + " from " + GetObjectFullName(TableName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
            }

            /// <summary>
            /// 打开表
            /// </summary>
            /// <param name="connectionString"></param>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <returns></returns>
            public DataTable Open(string connectionString, string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select(connectionString, "select " + (fieldList == "" ? "*" : fieldList) + " from " + GetObjectFullName(TableName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
            }

            /// <summary>
            /// 打开表
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <returns></returns>
            public DataTable Open(SqlConnection conn, string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select(conn, "select " + (fieldList == "" ? "*" : fieldList) + " from " + GetObjectFullName(TableName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
            }

            /// <summary>
            /// 获取表记录数
            /// </summary>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long GetCount(string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar("select count(*) from " + GetObjectFullName(TableName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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

                object result = ExecuteScalar(connectionString, "select count(*) from " + GetObjectFullName(TableName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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
            public long GetCount(SqlConnection conn, string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar(conn, "select count(*) from " + GetObjectFullName(TableName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

                if (result == null)
                {
                    return 0;
                }

                return long.Parse(result.ToString());
            }

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

                    InsertFieldsList += "[" + Fields[i].Name + "]";
                    InsertValuesList += "@" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                string commandText = "insert into " + GetObjectFullName(TableName) + " (" + InsertFieldsList + ") values (" + InsertValuesList + "); select isnull(cast(scope_identity() as bigint), -99999999)";

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
            /// <returns>小于0表示失败，0表示成功，无自增值，大于0表示自增值</returns>
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

                    InsertFieldsList += "[" + Fields[i].Name + "]";
                    InsertValuesList += "@" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                string commandText = "insert into " + GetObjectFullName(TableName) + " (" + InsertFieldsList + ") values (" + InsertValuesList + "); select isnull(cast(scope_identity() as bigint), -99999999)";

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
            public long Insert(SqlConnection conn)
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

                    InsertFieldsList += "[" + Fields[i].Name + "]";
                    InsertValuesList += "@" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                string commandText = "insert into " + GetObjectFullName(TableName) + " (" + InsertFieldsList + ") values (" + InsertValuesList + "); select isnull(cast(scope_identity() as bigint), -99999999)";

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

            /// <summary>
            /// 删除表记录
            /// </summary>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long Delete(string condition)
            {
                condition = condition.Trim();

                object objResult = ExecuteScalar("delete from " + GetObjectFullName(TableName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + "; select isnull(cast(rowcount_big() as bigint), -99999999)");

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

                object objResult = ExecuteScalar(connectionString, "delete from " + GetObjectFullName(TableName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + "; select isnull(cast(rowcount_big() as bigint), -99999999)");

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
            public long Delete(SqlConnection conn, string condition)
            {
                condition = condition.Trim();

                object objResult = ExecuteScalar(conn, "delete from " + GetObjectFullName(TableName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + "; select isnull(cast(rowcount_big() as bigint), -99999999)");

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
            /// <param name="condition"></param>
            /// <returns></returns>
            public long Update(string condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                condition = condition.Trim();

                string commandText = "update " + GetObjectFullName(TableName) + " set ";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        commandText += ", ";
                    }

                    commandText += "[" + Fields[i].Name + "] = @" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                if (!string.IsNullOrEmpty(condition))
                {
                    commandText += " where " + FilteSqlInfusionForCondition(condition);
                }

                commandText += "; select isnull(cast(rowcount_big() as bigint), -99999999)";

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

                string commandText = "update " + GetObjectFullName(TableName) + " set ";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        commandText += ", ";
                    }

                    commandText += "[" + Fields[i].Name + "] = @" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                if (!string.IsNullOrEmpty(condition))
                {
                    commandText += " where " + FilteSqlInfusionForCondition(condition);
                }

                commandText += "; select isnull(cast(rowcount_big() as bigint), -99999999)";

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
            public long Update(SqlConnection conn, string condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                condition = condition.Trim();

                string commandText = "update " + GetObjectFullName(TableName) + " set ";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        commandText += ", ";
                    }

                    commandText += "[" + Fields[i].Name + "] = @" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                if (!string.IsNullOrEmpty(condition))
                {
                    commandText += " where " + FilteSqlInfusionForCondition(condition);
                }

                commandText += "; select isnull(cast(rowcount_big() as bigint), -99999999)";

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

            /// <summary>
            /// 打开视图
            /// </summary>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <returns></returns>
            public DataTable Open(string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select("select " + (fieldList == "" ? "*" : fieldList) + " from " + GetObjectFullName(ViewName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
            }

            /// <summary>
            /// 打开视图
            /// </summary>
            /// <param name="connectionString"></param>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <returns></returns>
            public DataTable Open(string connectionString, string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select(connectionString, "select " + (fieldList == "" ? "*" : fieldList) + " from " + GetObjectFullName(ViewName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
            }

            /// <summary>
            /// 打开视图
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <returns></returns>
            public DataTable Open(SqlConnection conn, string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select(conn, "select " + (fieldList == "" ? "*" : fieldList) + " from " + GetObjectFullName(ViewName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
            }

            /// <summary>
            /// 获取视图记录数
            /// </summary>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long GetCount(string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar("select count(*) from " + GetObjectFullName(ViewName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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

                object result = ExecuteScalar(connectionString, "select count(*) from " + GetObjectFullName(ViewName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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
            public long GetCount(SqlConnection conn, string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar(conn, "select count(*) from " + GetObjectFullName(ViewName) + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

                if (result == null)
                {
                    return 0;
                }

                return long.Parse(result.ToString());
            }
        }

        #endregion
    }
}
