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
        /// <param name="ObjectName"></param>
        /// <returns></returns>
        public static string GetObjectFullName(string ObjectName)
        {
            if (ObjectName.IndexOf(".", StringComparison.Ordinal) >= 0)
            {
                return ObjectName;
            }

            if (!ObjectName.StartsWith("[", StringComparison.Ordinal) || !ObjectName.EndsWith("]", StringComparison.Ordinal))
            {
                ObjectName = "[" + ObjectName + "]";
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

            return SQLServer_owner + "." + ObjectName;
        }

        #region BuildConnectString

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="SQLServerName"></param>
        /// <param name="SQLDatabaseName"></param>
        /// <returns></returns>
        public static string BuildConnectString(string SQLServerName, string SQLDatabaseName)
        {
            return string.Format("data source=\"{0}\";persist security info=False;initial catalog=\"{1}\"", SQLServerName, SQLDatabaseName);
        }

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="SQLServerName"></param>
        /// <param name="SQLDatabaseName"></param>
        /// <param name="SQLUID"></param>
        /// <param name="SQLPassword"></param>
        /// <returns></returns>
        public static string BuildConnectString(string SQLServerName, string SQLDatabaseName, string SQLUID, string SQLPassword)
        {
            return string.Format("PWD={0};UID={1};data source=\"{2}\";persist security info=False;initial catalog=\"{3}\"", SQLPassword, SQLUID, SQLServerName, SQLDatabaseName);
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
            private IList<string> ParametersName;
            private IList<object> ParametersValue;

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
                ParametersName.Add(Name.StartsWith("@", StringComparison.Ordinal) ? Name.Substring(1) : Name);
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
            /// <param name="Index"></param>
            /// <returns></returns>
            public object this[int Index]
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

                    if (Index > ParametersValue.Count - 1)
                    {
                        return null;
                    }

                    return ParametersValue[Index];
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="Name"></param>
            /// <returns></returns>
            public object this[string Name]
            {
                get
                {
                    if (ParametersValue == null)
                    {
                        return null;
                    }

                    for (int i = 0; i < ParametersName.Count; i++)
                    {
                        if (ParametersName[i] == Name)
                        {
                            return ParametersValue[i];
                        }
                    }

                    return null;
                }
            }
        }

        private static void AddParameter(ref SqlCommand Cmd, params Parameter[] Params)
        {
            if ((Params == null) || (Cmd == null))
            {
                return;
            }

            for (int i = 0; i < Params.Length; i++)
            {
                if (Params[i] == null)
                {
                    continue;
                }

                SqlParameter param = new SqlParameter();
                param.ParameterName = Params[i].Name.StartsWith("@", StringComparison.Ordinal) ? Params[i].Name : ("@" + Params[i].Name);
                param.SqlDbType = Params[i].Type;

                if (Params[i].Size > 0)
                {
                    param.Size = Params[i].Size;
                }

                param.Direction = Params[i].Direction;

                if (((Params[i].Direction == ParameterDirection.InputOutput) ||
                    (Params[i].Direction == ParameterDirection.Input)) &&
                    (Params[i].Value != null))
                {
                    param.Value = Params[i].Value;
                }

                Cmd.Parameters.Add(param);
            }
        }

        private static void AddOutputParameter(SqlCommand Cmd, ref OutputParameter Outputs)
        {
            if (Cmd == null)
            {
                return;
            }

            if (Cmd.Parameters.Count == 0)
            {
                return;
            }

            for (int i = 0; i < Cmd.Parameters.Count; i++)
            {
                SqlParameter param = Cmd.Parameters[i];

                if ((param.Direction != ParameterDirection.InputOutput) &&
                    (param.Direction != ParameterDirection.Output))
                {
                    continue;
                }

                Outputs.Add(param.ParameterName, param.Value);
            }
        }

        private static SqlParameter GetReturnParameter(SqlCommand Cmd)
        {
            if (Cmd == null)
            {
                return null;
            }

            if (Cmd.Parameters.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < Cmd.Parameters.Count; i++)
            {
                SqlParameter param = Cmd.Parameters[i];

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
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string CommandText, params Parameter[] Params)
        {
            return ExecuteNonQuery(GetConnectionStringFromConfig(), CommandText, Params);
        }

        /// <summary>
        /// 执行数据库命令
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string ConnectionString, string CommandText, params Parameter[] Params)
        {
            SqlConnection conn = CreateDataConnection<SqlConnection>(ConnectionString);

            if (conn == null)
            {
                return -1001;
            }

            int Result = ExecuteNonQuery(conn, CommandText, Params);

            try
            {
                conn.Close();
            }
            catch { }

            return Result;
        }

        /// <summary>
        /// 执行数据库命令
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(SqlConnection conn, string CommandText, params Parameter[] Params)
        {
            if (conn == null)
            {
                return -1001;
            }

            bool InitOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                InitOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return -1001;
                }
            }

            SqlCommand Cmd = new SqlCommand(CommandText, conn);
            AddParameter(ref Cmd, Params);

            SqlTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                return -1001;
            }

            Cmd.Transaction = trans;
            bool Result = false;

            try
            {
                Cmd.ExecuteNonQuery();
                trans.Commit();

                Result = true;
            }
            catch
            {
                try
                {
                    trans.Rollback();
                }
                catch { }

                Result = false;
            }

            if (!InitOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return Result ? 0 : -1002;
        }

        /// <summary>
        /// 执行数据库命令(不用事务)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteNonQueryNoTranscation(SqlConnection conn, string CommandText, params Parameter[] Params)
        {
            if (conn == null)
            {
                return -1001;
            }

            bool InitOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                InitOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return -1001;
                }
            }

            SqlCommand Cmd = new SqlCommand(CommandText, conn);
            AddParameter(ref Cmd, Params);

            bool Result = false;

            try
            {
                Cmd.ExecuteNonQuery();

                Result = true;
            }
            catch
            {
                Result = false;
            }

            if (!InitOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return Result ? 0 : -1002;
        }

        #endregion

        #region Select

        /// <summary>
        /// 打开数据集
        /// </summary>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static DataTable Select(string CommandText, params Parameter[] Params)
        {
            return Select(GetConnectionStringFromConfig(), CommandText, Params);
        }

        /// <summary>
        /// 打开数据集
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static DataTable Select(string ConnectionString, string CommandText, params Parameter[] Params)
        {
            SqlConnection conn = CreateDataConnection<SqlConnection>(ConnectionString);

            if (conn == null)
            {
                return null;
            }

            DataTable dt = Select(conn, CommandText, Params);

            try
            {
                conn.Close();
            }
            catch { }

            return dt;
        }

        /// <summary>
        /// 蚩??菁?
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static DataTable Select(SqlConnection conn, string CommandText, params Parameter[] Params)
        {
            if (conn == null)
            {
                return null;
            }

            bool InitOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                InitOpenState = false;

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
            SqlCommand Cmd = new SqlCommand(CommandText, conn);

            AddParameter(ref Cmd, Params);
            da.SelectCommand = Cmd;

            DataTable dt = new DataTable();
            bool Result = false;

            try
            {
                da.Fill(dt);

                Result = true;
            }
            catch
            {
                Result = false;
            }

            if (!InitOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return Result ? dt : null;
        }

        #endregion

        #region ExecuteScalar

        /// <summary>
        /// 读取第一行第一列
        /// </summary>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteScalar(string CommandText, params Parameter[] Params)
        {
            return ExecuteScalar(GetConnectionStringFromConfig(), CommandText, Params);
        }

        /// <summary>
        /// 读取第一行第一列
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteScalar(string ConnectionString, string CommandText, params Parameter[] Params)
        {
            SqlConnection conn = CreateDataConnection<SqlConnection>(ConnectionString);

            if (conn == null)
            {
                return null;
            }

            object obj = ExecuteScalar(conn, CommandText, Params);

            try
            {
                conn.Close();
            }
            catch { }

            return obj;
        }

        /// <summary>
        /// 读取第一行第一列
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteScalar(SqlConnection conn, string CommandText, params Parameter[] Params)
        {
            if (conn == null)
            {
                return null;
            }

            bool InitOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                InitOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return null;
                }
            }

            SqlCommand Cmd = new SqlCommand(CommandText, conn);
            AddParameter(ref Cmd, Params);

            object Result = null;

            try
            {
                Result = Cmd.ExecuteScalar();
            }
            catch
            {
                Result = null;
            }

            if (!InitOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return Result;
        }

        #endregion

        #region ExecuteFunction

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <param name="FunctionName"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(string FunctionName, params Parameter[] Params)
        {
            return ExecuteFunction(GetConnectionStringFromConfig(), FunctionName, Params);
        }

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="FunctionName"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(string ConnectionString, string FunctionName, params Parameter[] Params)
        {
            SqlConnection conn = CreateDataConnection<SqlConnection>(ConnectionString);

            if (conn == null)
            {
                return null;
            }

            object obj = ExecuteFunction(conn, FunctionName, Params);

            try
            {
                conn.Close();
            }
            catch { }

            return obj;
        }

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="FunctionName"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(SqlConnection conn, string FunctionName, params Parameter[] Params)
        {
            if (conn == null)
            {
                return null;
            }

            bool InitOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                InitOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return null;
                }
            }

            string CommandText = "select " + GetObjectFullName(FunctionName) + "(";

            if (Params != null)
            {
                for (int i = 0; i < Params.Length; i++)
                {
                    if (Params[i] != null)
                    {
                        bool isChar = false;
                        bool isNChar = false;

                        if ((Params[i].Type == SqlDbType.Char) || (Params[i].Type == SqlDbType.DateTime) || (Params[i].Type == SqlDbType.SmallDateTime) ||
                            (Params[i].Type == SqlDbType.Text) || (Params[i].Type == SqlDbType.UniqueIdentifier) || (Params[i].Type == SqlDbType.VarChar))
                        {
                            isChar = true;
                        }

                        if ((Params[i].Type == SqlDbType.NChar) || (Params[i].Type == SqlDbType.NText) || (Params[i].Type == SqlDbType.NVarChar))
                        {
                            isNChar = true;
                        }

                        if (!CommandText.EndsWith("("))
                        {
                            CommandText += ", ";
                        }

                        if (isChar)
                        {
                            CommandText += "\'";
                        }

                        if (isNChar)
                        {
                            CommandText += "N\'";
                        }

                        CommandText += Params[i].Value.ToString();

                        if (isChar || isNChar)
                        {
                            CommandText += "\'";
                        }
                    }
                }

                CommandText += ")";
            }


            object Result = ExecuteScalar(conn, CommandText);

            if (!InitOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return Result;
        }

        #endregion

        #region ExecuteStoredProcedureNonQuery

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="StoredProcedureName"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureNonQuery(string StoredProcedureName, ref OutputParameter Outputs, params Parameter[] Params)
        {
            return ExecuteStoredProcedureNonQuery(GetConnectionStringFromConfig(), StoredProcedureName, ref Outputs, Params);
        }

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="StoredProcedureName"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureNonQuery(string ConnectionString, string StoredProcedureName, ref OutputParameter Outputs, params Parameter[] Params)
        {
            SqlConnection conn = CreateDataConnection<SqlConnection>(ConnectionString);

            if (conn == null)
            {
                return -1001;
            }

            int Result = ExecuteStoredProcedureNonQuery(conn, StoredProcedureName, ref Outputs, Params);

            try
            {
                conn.Close();
            }
            catch { }

            return Result;
        }

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="StoredProcedureName"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureNonQuery(SqlConnection conn, string StoredProcedureName, ref OutputParameter Outputs, params Parameter[] Params)
        {
            if (conn == null)
            {
                return -1001;
            }

            bool InitOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                InitOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return -1001;
                }
            }

            SqlCommand Cmd = new SqlCommand(GetObjectFullName(StoredProcedureName), conn);
            Cmd.CommandType = CommandType.StoredProcedure;

            AddParameter(ref Cmd, Params);

            // 增加返回值参数
            SqlParameter ReturnValue = new SqlParameter("@Shove_Database_MSSQL_ExecuteStoredProcedureNonQuery_Rtn", SqlDbType.Int);
            ReturnValue.Direction = ParameterDirection.ReturnValue;
            Cmd.Parameters.Add(ReturnValue);

            SqlTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                return -1001;
            }

            Cmd.Transaction = trans;
            bool Result = false;

            try
            {
                Cmd.ExecuteNonQuery();
                trans.Commit();

                Result = true;
            }
            catch
            {
                try
                {
                    trans.Rollback();
                }
                catch { }

                Result = false;
            }

            if (!InitOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            if (!Result)
            {
                return -1002;
            }

            // 填写返回参数
            AddOutputParameter(Cmd, ref Outputs);

            // 获取过程的返回值
            ReturnValue = GetReturnParameter(Cmd);

            if (ReturnValue != null)
            {
                return (int)ReturnValue.Value;
            }

            return 0;
        }

        #endregion

        #region ExecuteStoredProcedureWithQuery

        /// <summary>
        /// 执行存储过程(不带返回记录集)
        /// </summary>
        /// <param name="StoredProcedureName"></param>
        /// <param name="ds"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureWithQuery(string StoredProcedureName, ref DataSet ds, ref OutputParameter Outputs, params Parameter[] Params)
        {
            return ExecuteStoredProcedureWithQuery(GetConnectionStringFromConfig(), StoredProcedureName, ref ds, ref Outputs, Params);
        }

        /// <summary>
        /// 执行存储过程(不带返回记录集)
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="StoredProcedureName"></param>
        /// <param name="ds"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureWithQuery(string ConnectionString, string StoredProcedureName, ref DataSet ds, ref OutputParameter Outputs, params Parameter[] Params)
        {
            SqlConnection conn = CreateDataConnection<SqlConnection>(ConnectionString);

            if (conn == null)
            {
                return -1001;
            }

            int Result = ExecuteStoredProcedureWithQuery(conn, StoredProcedureName, ref ds, ref Outputs, Params);

            try
            {
                conn.Close();
            }
            catch { }

            return Result;
        }

        /// <summary>
        /// 执行存储过程(不带返回记录集)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="StoredProcedureName"></param>
        /// <param name="ds"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureWithQuery(SqlConnection conn, string StoredProcedureName, ref DataSet ds, ref OutputParameter Outputs, params Parameter[] Params)
        {
            if (conn == null)
            {
                return -1001;
            }

            bool InitOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                InitOpenState = false;

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
            SqlCommand Cmd = new SqlCommand(GetObjectFullName(StoredProcedureName), conn);

            Cmd.CommandType = CommandType.StoredProcedure;
            Cmd.Parameters.Clear();
            AddParameter(ref Cmd, Params);

            // 增加返回值参数
            SqlParameter ReturnValue = new SqlParameter("@Shove_Database_MSSQL_ExecuteStoredProcedureWithQuery_Rtn", SqlDbType.Int);
            ReturnValue.Direction = ParameterDirection.ReturnValue;
            Cmd.Parameters.Add(ReturnValue);

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
                return -1001;
            }

            Cmd.Transaction = trans;
            da.SelectCommand = Cmd;

            bool Result = false;

            try
            {
                da.Fill(ds);
                trans.Commit();

                Result = true;
            }
            catch
            {
                try
                {
                    trans.Rollback();
                }
                catch { }

                Result = false;
            }

            if (!InitOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            if (!Result)
            {
                return -1002;
            }

            //填写返回参数
            AddOutputParameter(Cmd, ref Outputs);

            // 获取过程的返回值
            ReturnValue = GetReturnParameter(Cmd);

            if (ReturnValue != null)
            {
                return (int)ReturnValue.Value;
            } 
            
            return 0;
        }

        #endregion

        #region BackupDatabase

        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="BackupFileName">包含绝对路径的文件名，注意：此路径是相对于数据库所在的服务器而言的</param>
        /// <param name="BreakLog"></param>
        /// <param name="Shrink"></param>
        /// <returns></returns>
        public static int BackupDatabase(string BackupFileName, bool BreakLog, bool Shrink)
        {
            return BackupDatabase(GetConnectionStringFromConfig(), BackupFileName, BreakLog, Shrink);
        }

        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="BackupFileName">包含绝对路径的文件名，注意：此路径是相对于数据库所在的服务器而言的</param>
        /// <param name="BreakLog"></param>
        /// <param name="Shrink"></param>
        /// <returns></returns>
        public static int BackupDatabase(string ConnectionString, string BackupFileName, bool BreakLog, bool Shrink)
        {
            SqlConnection conn = CreateDataConnection<SqlConnection>(ConnectionString);

            if (conn == null)
            {
                return -1001;
            }

            int Result = BackupDatabase(conn, BackupFileName, BreakLog, Shrink);

            try
            {
                conn.Close();
            }
            catch { }

            return Result;
        }

        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="BackupFileName">包含绝对路径的文件名，注意：此路径是相对于数据库所在的服务器而言的</param>
        /// <param name="BreakLog"></param>
        /// <param name="Shrink"></param>
        /// <returns></returns>
        public static int BackupDatabase(SqlConnection conn, string BackupFileName, bool BreakLog, bool Shrink)
        {
            if (conn == null)
            {
                return -1001;
            }

            bool InitOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                InitOpenState = false;

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

            if (!DatabaseName.StartsWith("["))
            {
                DatabaseName = "[" + DatabaseName + "]";
            }

            if (ExecuteNonQueryNoTranscation(conn, "use master") < 0)
            {
                if (!InitOpenState)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                }

                return -1002;
            }

            if (BreakLog)
            {
                if (ExecuteNonQueryNoTranscation(conn, "backup log " + DatabaseName + " with no_log") < 0)
                {
                    if (!InitOpenState)
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

            if (Shrink)
            {
                if (ExecuteNonQueryNoTranscation(conn, "DBCC SHRINKDATABASE (" + DatabaseName + ", 0)") < 0)
                {
                    if (!InitOpenState)
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

            if (ExecuteNonQueryNoTranscation(conn, "Backup database " + DatabaseName + " to disk='" + BackupFileName + "' with INIT") < 0)
            {
                if (!InitOpenState)
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
                if (!InitOpenState)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                }

                return -1006;
            }

            if (!IO.File.Compress(BackupFileName))
            {
                if (!InitOpenState)
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
        /// <param name="ConnectionString"></param>
        /// <returns></returns>
        public static byte[] BackupDataToZipStream(string ConnectionString)
        {
            SqlConnection conn = CreateDataConnection<SqlConnection>(ConnectionString);

            if (conn == null)
            {
                return null;
            }

            byte[] Result = BackupDataToZipStream(conn);

            try
            {
                conn.Close();
            }
            catch { }

            return Result;
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

            bool InitOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                InitOpenState = false;

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
                if (!InitOpenState)
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

                DataTable Table = Select(conn, "select * from [" + TableName + "]");

                if (Table == null)
                {
                    if (!InitOpenState)
                    {
                        try
                        {
                            conn.Close();
                        }
                        catch { }
                    }

                    return null;
                }

                Table.TableName = TableName;
                ds.Tables.Add(Table);
            }

            if (!InitOpenState)
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

            return String.Compress(sb.ToString());
        }

        /// <summary>
        /// 恢复数据库(从二进制压缩流中提取表数据库的 XML 进行恢复)
        /// </summary>
        /// <param name="DataBuffer"></param>
        /// <returns></returns>
        public static int RestoreDataFromZipStream(byte[] DataBuffer)
        {
            return RestoreDataFromZipStream(GetConnectionStringFromConfig(), DataBuffer);
        }

        /// <summary>
        /// 恢复数据库(从二进制压缩流中提取表数据库的 XML 进行恢复)
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="DataBuffer"></param>
        /// <returns></returns>
        public static int RestoreDataFromZipStream(string ConnectionString, byte[] DataBuffer)
        {
            SqlConnection conn = CreateDataConnection<SqlConnection>(ConnectionString);

            if (conn == null)
            {
                return -1001;
            }

            int Result = RestoreDataFromZipStream(conn, DataBuffer);

            try
            {
                conn.Close();
            }
            catch { }

            return Result;
        }

        /// <summary>
        /// 恢复数据库(从二进制压缩流中提取表数据库的 XML 进行恢复)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="DataBuffer"></param>
        /// <returns></returns>
        public static int RestoreDataFromZipStream(SqlConnection conn, byte[] DataBuffer)
        {
            if (conn == null)
            {
                return -1001;
            }

            bool InitOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                InitOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return -1001;
                }
            }

            string XmlData = String.Decompress(DataBuffer);
            if (string.IsNullOrEmpty(XmlData))
            {
                if (!InitOpenState)
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
                if (!InitOpenState)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                }

                return -1003;
            }

            if ((ds == null) || (ds.Tables.Count < 1))
            {
                if (!InitOpenState)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                }

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
                return -1001;
            }

            bool Result = false;

            try
            {
                foreach (DataTable dt in ds.Tables)
                {
                    SqlCommand Cmd = new SqlCommand("truncate table [" + dt.TableName + "]", conn);
                    Cmd.Transaction = trans;
                    Cmd.ExecuteNonQuery();
                    //Cmd.CommandText = "SET IDENTITY_INSERT [" + dt.TableName + "] ON";
                    //Cmd.ExecuteNonQuery();

                    SqlBulkCopy sqlbulkcopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, trans);
                    sqlbulkcopy.DestinationTableName = dt.TableName;

                    sqlbulkcopy.WriteToServer(dt);
                }

                trans.Commit();
                Result = true;
            }
            catch//(Exception ee)
            {
                trans.Rollback();
                Result = false;

                //System.Web.HttpContext.Current.Response.Write(ee.Message);
            }

            if (!InitOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            return Result ? 0 : -1005;
        }

        #endregion

        #region Execute SQL Script

        /// <summary>
        /// 执行 SQL 脚本
        /// </summary>
        /// <param name="Script"></param>
        /// <returns></returns>
        public static bool ExecuteSQLScript(string Script)
        {
            Script = Script.Trim();

            if (Script == "")
            {
                return true;
            }

            return ExecuteSQLScript(GetConnectionStringFromConfig(), Script);
        }

        /// <summary>
        /// 执行 SQL 脚本
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="Script"></param>
        /// <returns></returns>
        public static bool ExecuteSQLScript(string ConnectionString, string Script)
        {
            Script = Script.Trim();

            if (Script == "")
            {
                return true;
            }

            SqlConnection conn = CreateDataConnection<SqlConnection>(ConnectionString);

            if (conn == null)
            {
                return false;
            }

            bool Result = ExecuteSQLScript(conn, Script);

            try
            {
                conn.Close();
            }
            catch { }

            return Result;
        }

        /// <summary>
        /// 执行 SQL 脚本
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="Script"></param>
        /// <returns></returns>
        public static bool ExecuteSQLScript(SqlConnection conn, string Script)
        {
            Script = Script.Trim();

            if (Script == "")
            {
                return true;
            }

            Regex regex = new Regex(@"/[*][\S\s\t\r\n\v\f]*?[*]/", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Script = regex.Replace(Script, "");

            regex = new Regex(@"--[^\n]*?[\n]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Script = regex.Replace(Script, "\r\n");

            Script = Script.Trim();

            if (Script == "")
            {
                return false;
            }

            Script += "\r\n";

            regex = new Regex(@"[\n]GO[\r\t\v\s]*?[\n]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            string[] Scripts = regex.Split(Script);

            if ((Scripts == null) || (Scripts.Length == 0))
            {
                return false;
            }

            if (conn == null)
            {
                return false;
            }

            bool InitOpenState = true;

            if (conn.State != ConnectionState.Open)
            {
                InitOpenState = false;

                try
                {
                    conn.Open();
                }
                catch
                {
                    return false;
                }
            }

            SqlCommand Cmd = new SqlCommand("", conn);

            Cmd.CommandType = CommandType.Text;
            Cmd.Parameters.Clear();

            SqlTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                return false;
            }

            Cmd.Transaction = trans;

            foreach (string str in Scripts)
            {
                string strCmd = str.Trim();

                if (strCmd == "")
                {
                    continue;
                }

                Cmd.CommandText = strCmd;

                try
                {
                    Cmd.ExecuteNonQuery();
                }
                catch
                {
                    try
                    {
                        trans.Rollback();
                    }
                    catch { }

                    if (!InitOpenState)
                    {
                        try
                        {
                            conn.Close();
                        }
                        catch { }
                    }

                    return false;
                }
            }

            trans.Commit();

            if (!InitOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

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
            /// <param name="canonicalidentifiername"></param>
            /// <param name="dbtype"></param>
            /// <param name="_readonly"></param>
            public Field(object parent, string name, string canonicalidentifiername, SqlDbType dbtype, bool _readonly)
            {
                Parent = parent;
                Name = name;
                CanonicalIdentifierName = canonicalidentifiername;
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
            /// <param name="Index"></param>
            /// <returns></returns>
            public Field this[int Index]
            {
                get
                {
                    if ((Count < 1) || (Index < 0) || (Index > Count))
                    {
                        return null;
                    }

                    return fields[Index];
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
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <returns></returns>
            public DataTable Open(string FieldList, string Condition, string Order)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                return Select("select " + (FieldList == "" ? "*" : FieldList) + " from " + GetObjectFullName(TableName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            /// <summary>
            /// 打开表
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <returns></returns>
            public DataTable Open(string ConnectionString, string FieldList, string Condition, string Order)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                return Select(ConnectionString, "select " + (FieldList == "" ? "*" : FieldList) + " from " + GetObjectFullName(TableName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            /// <summary>
            /// 打开表
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <returns></returns>
            public DataTable Open(SqlConnection conn, string FieldList, string Condition, string Order)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                return Select(conn, "select " + (FieldList == "" ? "*" : FieldList) + " from " + GetObjectFullName(TableName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            /// <summary>
            /// 获取表记录数
            /// </summary>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar("select count(*) from " + GetObjectFullName(TableName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
            }

            /// <summary>
            /// 获取表记录数
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(string ConnectionString, string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar(ConnectionString, "select count(*) from " + GetObjectFullName(TableName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
            }

            /// <summary>
            /// 获取表记录数
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(SqlConnection conn, string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar(conn, "select count(*) from " + GetObjectFullName(TableName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
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

                string CommandText = "insert into " + GetObjectFullName(TableName) + " (" + InsertFieldsList + ") values (" + InsertValuesList + "); select isnull(cast(scope_identity() as bigint), -99999999)";

                object objResult = ExecuteScalar(CommandText, Parameters);

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long Result = (long)objResult;

                if (Result == -99999999)
                {
                    return 0;
                }

                return Result;
            }

            /// <summary>
            /// 增加记录
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <returns>小于0表示失败，0表示成功，无自增值，大于0表示自增值</returns>
            public long Insert(string ConnectionString)
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

                string CommandText = "insert into " + GetObjectFullName(TableName) + " (" + InsertFieldsList + ") values (" + InsertValuesList + "); select isnull(cast(scope_identity() as bigint), -99999999)";

                object objResult = ExecuteScalar(ConnectionString, CommandText, Parameters);

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long Result = (long)objResult;

                if (Result == -99999999)
                {
                    return 0;
                }

                return Result;
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

                string CommandText = "insert into " + GetObjectFullName(TableName) + " (" + InsertFieldsList + ") values (" + InsertValuesList + "); select isnull(cast(scope_identity() as bigint), -99999999)";

                object objResult = ExecuteScalar(conn, CommandText, Parameters);

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long Result = (long)objResult;

                if (Result == -99999999)
                {
                    return 0;
                }

                return Result;
            }

            /// <summary>
            /// 删除表记录
            /// </summary>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Delete(string Condition)
            {
                Condition = Condition.Trim();

                object objResult = ExecuteScalar("delete from " + GetObjectFullName(TableName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + "; select isnull(cast(rowcount_big() as bigint), -99999999)");

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long Result = (long)objResult;

                if (Result == -99999999)
                {
                    return 0;
                }

                return Result;
            }

            /// <summary>
            /// 删除表记录
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Delete(string ConnectionString, string Condition)
            {
                Condition = Condition.Trim();

                object objResult = ExecuteScalar(ConnectionString, "delete from " + GetObjectFullName(TableName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + "; select isnull(cast(rowcount_big() as bigint), -99999999)");

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long Result = (long)objResult;

                if (Result == -99999999)
                {
                    return 0;
                }

                return Result;
            }

            /// <summary>
            /// 删除表记录
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Delete(SqlConnection conn, string Condition)
            {
                Condition = Condition.Trim();

                object objResult = ExecuteScalar(conn, "delete from " + GetObjectFullName(TableName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + "; select isnull(cast(rowcount_big() as bigint), -99999999)");

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long Result = (long)objResult;

                if (Result == -99999999)
                {
                    return 0;
                }

                return Result;
            }
            
            /// <summary>
            /// 更新表
            /// </summary>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Update(string Condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                Condition = Condition.Trim();

                string CommandText = "update " + GetObjectFullName(TableName) + " set ";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        CommandText += ", ";
                    }

                    CommandText += "[" + Fields[i].Name + "] = @" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                if (!string.IsNullOrEmpty(Condition))
                {
                    CommandText += " where " + FilteSqlInfusionForCondition(Condition);
                }

                CommandText += "; select isnull(cast(rowcount_big() as bigint), -99999999)";

                object objResult = ExecuteScalar(CommandText, Parameters);

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long Result = (long)objResult;

                if (Result == -99999999)
                {
                    return 0;
                }

                return Result;
            }

            /// <summary>
            /// 更新表
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Update(string ConnectionString, string Condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                Condition = Condition.Trim();

                string CommandText = "update " + GetObjectFullName(TableName) + " set ";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        CommandText += ", ";
                    }

                    CommandText += "[" + Fields[i].Name + "] = @" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                if (!string.IsNullOrEmpty(Condition))
                {
                    CommandText += " where " + FilteSqlInfusionForCondition(Condition);
                }

                CommandText += "; select isnull(cast(rowcount_big() as bigint), -99999999)";

                object objResult = ExecuteScalar(ConnectionString, CommandText, Parameters);

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long Result = (long)objResult;

                if (Result == -99999999)
                {
                    return 0;
                }

                return Result;
            }

            /// <summary>
            /// 更新表
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Update(SqlConnection conn, string Condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                Condition = Condition.Trim();

                string CommandText = "update " + GetObjectFullName(TableName) + " set ";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        CommandText += ", ";
                    }

                    CommandText += "[" + Fields[i].Name + "] = @" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                if (!string.IsNullOrEmpty(Condition))
                {
                    CommandText += " where " + FilteSqlInfusionForCondition(Condition);
                }

                CommandText += "; select isnull(cast(rowcount_big() as bigint), -99999999)";

                object objResult = ExecuteScalar(conn, CommandText, Parameters);

                if (objResult == null)
                {
                    return -102;
                }

                Fields.Clear();

                long Result = (long)objResult;

                if (Result == -99999999)
                {
                    return 0;
                }

                return Result;
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
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <returns></returns>
            public DataTable Open(string FieldList, string Condition, string Order)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                return Select("select " + (FieldList == "" ? "*" : FieldList) + " from " + GetObjectFullName(ViewName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            /// <summary>
            /// 打开视图
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <returns></returns>
            public DataTable Open(string ConnectionString, string FieldList, string Condition, string Order)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                return Select(ConnectionString, "select " + (FieldList == "" ? "*" : FieldList) + " from " + GetObjectFullName(ViewName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            /// <summary>
            /// 打开视图
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <returns></returns>
            public DataTable Open(SqlConnection conn, string FieldList, string Condition, string Order)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                return Select(conn, "select " + (FieldList == "" ? "*" : FieldList) + " from " + GetObjectFullName(ViewName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            /// <summary>
            /// 获取视图记录数
            /// </summary>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar("select count(*) from " + GetObjectFullName(ViewName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
            }

            /// <summary>
            /// 获取视图记录数
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(string ConnectionString, string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar(ConnectionString, "select count(*) from " + GetObjectFullName(ViewName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
            }

            /// <summary>
            /// 获取视图记录数
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(SqlConnection conn, string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar(conn, "select count(*) from " + GetObjectFullName(ViewName) + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
            }
        }

        #endregion
    }
}
