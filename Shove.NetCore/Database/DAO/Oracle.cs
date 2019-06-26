using System;
using System.Data;
using System.Data.OracleClient;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shove.Database
{
    /// <summary>
    /// Shove 的专用的 Oracle 访问组件类
    /// </summary>
    public class Oracle : DatabaseAccess
    {
        /// <summary>
        /// 参数列表中是否包含了 OracleType.Cursor 类型
        /// </summary>
        /// <returns></returns>
        private static bool isContainOracleTypeCursor(OracleCommand Cmd)
        {
            foreach (OracleParameter p in Cmd.Parameters)
            {
                if (p.OracleType == OracleType.Cursor)
                {
                    return true;
                }
            }

            return false;
        }

        #region BuildConnectString

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="ServerName"></param>
        /// <returns></returns>
        public static string BuildConnectString(string ServerName)
        {
            return string.Format("Data Source={0};Integrated Security=yes", ServerName);
        }

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="ServerName"></param>
        /// <param name="UserId"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        public static string BuildConnectString(string ServerName, string UserId, string Password)
        {
            return string.Format("Data Source={0};User Id={1};Password={2};Integrated Security=no", ServerName, UserId, Password);
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
            public OracleType Type;
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
            public Parameter(string name, OracleType type, int size, ParameterDirection direction, object value)
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
                ParametersName.Add(Name.StartsWith(":") ? Name.Substring(1) : Name);
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

        private static void AddParameter(ref OracleCommand Cmd, params Parameter[] Params)
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

                OracleParameter param = new OracleParameter();
                param.ParameterName = Params[i].Name.StartsWith(":") ? Params[i].Name.Substring(1) : Params[i].Name;
                param.OracleType = Params[i].Type;

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

        private static void AddOutputParameter(OracleCommand Cmd, ref OutputParameter Outputs)
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
                OracleParameter param = Cmd.Parameters[i];

                if ((param.Direction != ParameterDirection.InputOutput) &&
                    (param.Direction != ParameterDirection.Output))
                {
                    continue;
                }

                Outputs.Add(param.ParameterName, param.Value);
            }
        }

        private static OracleParameter GetReturnParameter(OracleCommand Cmd)
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
                OracleParameter param = Cmd.Parameters[i];

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
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static long ExecuteNonQuery(string CommandText, ref OutputParameter Outputs, params Parameter[] Params)
        {
            return ExecuteNonQuery(GetConnectionStringFromConfig(), CommandText, ref Outputs, Params);
        }

        /// <summary>
        /// 执行数据库命令
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="CommandText"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static long ExecuteNonQuery(string ConnectionString, string CommandText, ref OutputParameter Outputs, params Parameter[] Params)
        {
            OracleConnection conn = CreateDataConnection<OracleConnection>(ConnectionString);

            if (conn == null)
            {
                return -1001;
            }

            long Result = ExecuteNonQuery(conn, CommandText, ref Outputs, Params);

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
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static long ExecuteNonQuery(OracleConnection conn, string CommandText, ref OutputParameter Outputs, params Parameter[] Params)
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

            OracleCommand Cmd = new OracleCommand(CommandText, conn);
            AddParameter(ref Cmd, Params);

            OracleTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                return -1001;
            }

            Cmd.Transaction = trans;
            long Result = -1002;

            try
            {
                Result = Cmd.ExecuteNonQuery();
                trans.Commit();
            }
            catch
            {
                try
                {
                    trans.Rollback();
                }
                catch { }

                Result = -1002;
            }

            if (!InitOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            if (Result >= 0)
            {
                // 填写返回参数
                AddOutputParameter(Cmd, ref Outputs);
            }

            return Result;
        }

        /// <summary>
        /// 执行数据库命令(不用事务)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="CommandText"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static long ExecuteNonQueryNoTranscation(OracleConnection conn, string CommandText, ref OutputParameter Outputs, params Parameter[] Params)
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

            OracleCommand Cmd = new OracleCommand(CommandText, conn);
            AddParameter(ref Cmd, Params);

            long Result = -1002;

            try
            {
                Result = Cmd.ExecuteNonQuery();
            }
            catch
            {
                Result = -1002;
            }

            if (!InitOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            if (Result >= 0)
            {
                // 填写返回参数
                AddOutputParameter(Cmd, ref Outputs);
            }

            return Result;
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
            OracleConnection conn = CreateDataConnection<OracleConnection>(ConnectionString);

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
        /// 打开数据集
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static DataTable Select(OracleConnection conn, string CommandText, params Parameter[] Params)
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

            OracleDataAdapter da = new OracleDataAdapter("", conn);
            OracleCommand Cmd = new OracleCommand(CommandText, conn);

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
            OracleConnection conn = CreateDataConnection<OracleConnection>(ConnectionString);

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
        public static object ExecuteScalar(OracleConnection conn, string CommandText, params Parameter[] Params)
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

            OracleCommand Cmd = new OracleCommand(CommandText, conn);
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
        /// <param name="ReturnDbType"></param>
        /// <param name="ReturnSize"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(string FunctionName, OracleType ReturnDbType, int ReturnSize, ref OutputParameter Outputs, params Parameter[] Params)
        {
            return ExecuteFunction(GetConnectionStringFromConfig(), FunctionName, ReturnDbType, ReturnSize, ref Outputs, Params);
        }

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="FunctionName"></param>
        /// <param name="ReturnDbType"></param>
        /// <param name="ReturnSize"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(string ConnectionString, string FunctionName, OracleType ReturnDbType, int ReturnSize, ref OutputParameter Outputs, params Parameter[] Params)
        {
            OracleConnection conn = CreateDataConnection<OracleConnection>(ConnectionString);

            if (conn == null)
            {
                return null;
            }

            object obj = ExecuteFunction(conn, FunctionName, ReturnDbType, ReturnSize, ref Outputs, Params);

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
        /// <param name="ReturnDbType"></param>
        /// <param name="ReturnSize"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(OracleConnection conn, string FunctionName, OracleType ReturnDbType, int ReturnSize, ref OutputParameter Outputs, params Parameter[] Params)
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

            string ObjectNamePreFix = System.Configuration.ConfigurationManager.AppSettings["ObjectNamePreFix_Function"];

            OracleCommand Cmd = new OracleCommand((string.IsNullOrEmpty(ObjectNamePreFix) ? "" : ObjectNamePreFix) + FunctionName, conn);
            Cmd.CommandType = CommandType.StoredProcedure;
            AddParameter(ref Cmd, Params);

            Cmd.Parameters.Add("ReturnValue", ReturnDbType, ReturnSize).Direction = ParameterDirection.ReturnValue;

            if (isContainOracleTypeCursor(Cmd))
            {
                throw new Exception("Shove.Database.Oracle.ExecuteFunction 方法的参数、返回参数都不能包含 OracleType.Cursor 类型，包含了此类型时，必须通过 Shove.Database.Oracle.ExecuteFunctionWithQuery 方法执行。");
            }

            OracleTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                return null;
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

            if (Result)
            {
                // 填写返回参数
                AddOutputParameter(Cmd, ref Outputs);
            }

            return Result ? Cmd.Parameters["ReturnValue"].Value : null;
        }

        #endregion

        #region ExecuteFunctionWithQuery

        /// <summary>
        /// 执行函数，返回结果集
        /// </summary>
        /// <param name="FunctionName"></param>
        /// <param name="ReturnDbType"></param>
        /// <param name="ReturnSize"></param>
        /// <param name="ReturnDataSet"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteFunctionWithQuery(string FunctionName, OracleType ReturnDbType, int ReturnSize, ref DataSet ReturnDataSet, ref OutputParameter Outputs, params Parameter[] Params)
        {
            return ExecuteFunctionWithQuery(GetConnectionStringFromConfig(), FunctionName, ReturnDbType, ReturnSize, ref ReturnDataSet, ref Outputs, Params);
        }

        /// <summary>
        /// 执行函数，返回结果集
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="FunctionName"></param>
        /// <param name="ReturnDbType"></param>
        /// <param name="ReturnSize"></param>
        /// <param name="ReturnDataSet"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteFunctionWithQuery(string ConnectionString, string FunctionName, OracleType ReturnDbType, int ReturnSize, ref DataSet ReturnDataSet, ref OutputParameter Outputs, params Parameter[] Params)
        {
            OracleConnection conn = CreateDataConnection<OracleConnection>(ConnectionString);

            if (conn == null)
            {
                return null;
            }

            object obj = ExecuteFunctionWithQuery(conn, FunctionName, ReturnDbType, ReturnSize, ref ReturnDataSet, ref Outputs, Params);

            try
            {
                conn.Close();
            }
            catch { }

            return obj;
        }

        /// <summary>
        /// 执行函数，返回结果集
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="FunctionName"></param>
        /// <param name="ReturnDbType"></param>
        /// <param name="ReturnSize"></param>
        /// <param name="ReturnDataSet"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteFunctionWithQuery(OracleConnection conn, string FunctionName, OracleType ReturnDbType, int ReturnSize, ref DataSet ReturnDataSet, ref OutputParameter Outputs, params Parameter[] Params)
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

            string ObjectNamePreFix = System.Configuration.ConfigurationManager.AppSettings["ObjectNamePreFix_Function"];

            OracleDataAdapter da = new OracleDataAdapter((string.IsNullOrEmpty(ObjectNamePreFix) ? "" : ObjectNamePreFix) + FunctionName, conn);
            OracleCommand Cmd = da.SelectCommand;
            Cmd.CommandType = CommandType.StoredProcedure;
            AddParameter(ref Cmd, Params);

            Cmd.Parameters.Add("ReturnValue", ReturnDbType, ReturnSize).Direction = ParameterDirection.ReturnValue;

            OracleTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                return null;
            }

            if (ReturnDataSet == null)
            {
                ReturnDataSet = new DataSet();
            }

            Cmd.Transaction = trans;
            bool Result = false;

            try
            {
                da.Fill(ReturnDataSet);
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

            if (Result)
            {
                // 填写返回参数
                AddOutputParameter(Cmd, ref Outputs);
            }

            return Result ? Cmd.Parameters["ReturnValue"].Value : null;
        }

        #endregion

        #region ExecuteProcedure

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="ProcedureName"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteProcedure(string ProcedureName, ref OutputParameter Outputs, params Parameter[] Params)
        {
            return ExecuteProcedure(GetConnectionStringFromConfig(), ProcedureName, ref Outputs, Params);
        }

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="ProcedureName"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteProcedure(string ConnectionString, string ProcedureName, ref OutputParameter Outputs, params Parameter[] Params)
        {
            OracleConnection conn = CreateDataConnection<OracleConnection>(ConnectionString);

            if (conn == null)
            {
                return -1001;
            }

            int Result = ExecuteProcedure(conn, ProcedureName, ref Outputs, Params);

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
        /// <param name="ProcedureName"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteProcedure(OracleConnection conn, string ProcedureName, ref OutputParameter Outputs, params Parameter[] Params)
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
                    return -1002;
                }
            }

            string ObjectNamePreFix = System.Configuration.ConfigurationManager.AppSettings["ObjectNamePreFix_Procedure"];

            OracleCommand Cmd = new OracleCommand((string.IsNullOrEmpty(ObjectNamePreFix) ? "" : ObjectNamePreFix) + ProcedureName, conn);
            Cmd.CommandType = CommandType.StoredProcedure;
            AddParameter(ref Cmd, Params);

            if (isContainOracleTypeCursor(Cmd))
            {
                throw new Exception("Shove.Database.Oracle.ExecuteProcedure 方法的参数不能包含 OracleType.Cursor 类型，包含了此类型时，必须通过 Shove.Database.Oracle.ExecuteProcedureWithQuery 方法执行。");
            }

            OracleTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                return -1003;
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

            if (Result)
            {
                // 填写返回参数
                AddOutputParameter(Cmd, ref Outputs);
            }

            return Result ? 0 : -1004;
        }

        #endregion

        #region ExecuteProcedureWithQuery

        /// <summary>
        /// 执行存储过程，返回结果集
        /// </summary>
        /// <param name="ProcedureName"></param>
        /// <param name="ReturnDataSet"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteProcedureWithQuery(string ProcedureName, ref DataSet ReturnDataSet, ref OutputParameter Outputs, params Parameter[] Params)
        {
            return ExecuteProcedureWithQuery(GetConnectionStringFromConfig(), ProcedureName, ref ReturnDataSet, ref Outputs, Params);
        }

        /// <summary>
        /// 执行存储过程，返回结果集
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="ProcedureName"></param>
        /// <param name="ReturnDataSet"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteProcedureWithQuery(string ConnectionString, string ProcedureName, ref DataSet ReturnDataSet, ref OutputParameter Outputs, params Parameter[] Params)
        {
            OracleConnection conn = CreateDataConnection<OracleConnection>(ConnectionString);

            if (conn == null)
            {
                return -1001;
            }

            int Result = ExecuteProcedureWithQuery(conn, ProcedureName, ref ReturnDataSet, ref Outputs, Params);

            try
            {
                conn.Close();
            }
            catch { }

            return Result;
        }

        /// <summary>
        /// 执行存储过程，返回结果集
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="ProcedureName"></param>
        /// <param name="ReturnDataSet"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteProcedureWithQuery(OracleConnection conn, string ProcedureName, ref DataSet ReturnDataSet, ref OutputParameter Outputs, params Parameter[] Params)
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
                    return -1002;
                }
            }

            string ObjectNamePreFix = System.Configuration.ConfigurationManager.AppSettings["ObjectNamePreFix_Procedure"];

            OracleDataAdapter da = new OracleDataAdapter((string.IsNullOrEmpty(ObjectNamePreFix) ? "" : ObjectNamePreFix) + ProcedureName, conn);
            OracleCommand Cmd = da.SelectCommand;
            Cmd.CommandType = CommandType.StoredProcedure;
            AddParameter(ref Cmd, Params);

            OracleTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                return -1003;
            }

            if (ReturnDataSet == null)
            {
                ReturnDataSet = new DataSet();
            }

            Cmd.Transaction = trans;
            bool Result = false;

            try
            {
                da.Fill(ReturnDataSet);
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

            if (Result)
            {
                // 填写返回参数
                AddOutputParameter(Cmd, ref Outputs);
            }

            return Result ? 0 : -1004;
        }

        #endregion

        #region BackupDatabase

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
            OracleConnection conn = CreateDataConnection<OracleConnection>(ConnectionString);

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
        public static byte[] BackupDataToZipStream(OracleConnection conn)
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

            DataTable dt = Select(conn, "select table_name from user_tables order by table_name");

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
                string TableName = dr["table_name"].ToString();

                DataTable Table = Select(conn, "select * from " + TableName);

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
            OracleConnection conn = CreateDataConnection<OracleConnection>(ConnectionString);

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
        public static int RestoreDataFromZipStream(OracleConnection conn, byte[] DataBuffer)
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
            OracleTransaction trans;
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
                    OracleCommand Cmd = new OracleCommand("delete from " + dt.TableName, conn);
                    Cmd.Transaction = trans;
                    Cmd.ExecuteNonQuery();

                    OracleDataAdapter da = new OracleDataAdapter("select * from " + dt.TableName, conn);
                    System.Data.DataTable dtUpdate = new System.Data.DataTable();
                    da.Fill(dtUpdate);

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        dtUpdate.ImportRow(dt.Rows[i]);
                    }

                    OracleCommandBuilder cb = new OracleCommandBuilder(da);
                    try
                    {
                        da.SelectCommand.Transaction = trans;
                        da.UpdateCommand.Transaction = trans;
                        da.DeleteCommand.Transaction = trans;
                        da.InsertCommand.Transaction = trans;
                    }
                    catch { }

                    da.Update(dtUpdate);
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

        #region ShoveDAL 工具通过一下四个类生成表、视图的访问的持久化代码

        /// <summary>
        /// 表的字段类，ShoveDAL.30 工具使用
        /// </summary>
        public class Field
        {
            private string _SequenceName;
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
            public OracleType DbType;
            /// <summary>
            /// 
            /// </summary>
            public string SequenceName
            {
                get
                {
                    return _SequenceName;
                }
                set
                {
                    if (_Value != null)
                    {
                        throw new Exception("the member “" + Name + "” is has value, can't set to sequence.");
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        throw new Exception("the member “" + Name + "” SequenceName can't is EnptyOrNull.");
                    }

                    _SequenceName = value;

                    if (Parent != null)
                    {
                        ((TableBase)Parent).Fields.Add(this);
                    }
                }
            }

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
                    if (!string.IsNullOrEmpty(_SequenceName))
                    {
                        throw new Exception("the member “" + Name + "” is a sequence column, can't set value.");
                    }

                    _Value = value;

                    if (Parent != null)
                    {
                        ((TableBase)Parent).Fields.Add(this);
                    }
                }
            }

            /// <summary>
            /// 构造
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="name"></param>
            /// <param name="canonicalidentifiername"></param>
            /// <param name="dbtype"></param>
            public Field(object parent, string name, string canonicalidentifiername, OracleType dbtype)
            {
                Parent = parent;
                Name = name;
                CanonicalIdentifierName = canonicalidentifiername;
                DbType = dbtype;
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

            #region Open

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

                return Select("select " + (FieldList == "" ? "*" : FieldList) + " from " + TableName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
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

                return Select(ConnectionString, "select " + (FieldList == "" ? "*" : FieldList) + " from " + TableName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            /// <summary>
            /// 打开表
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <returns></returns>
            public DataTable Open(OracleConnection conn, string FieldList, string Condition, string Order)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                return Select(conn, "select " + (FieldList == "" ? "*" : FieldList) + " from " + TableName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            #endregion

            #region GetCount

            /// <summary>
            /// 获取表记录数
            /// </summary>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar("select count(*) from " + TableName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

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

                object Result = ExecuteScalar(ConnectionString, "select count(*) from " + TableName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

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
            public long GetCount(OracleConnection conn, string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar(conn, "select count(*) from " + TableName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
            }

            #endregion

            #region Insert

            /// <summary>
            /// 增加记录
            /// </summary>
            /// <returns>小于0表示失败，0表示成功，无自增值，大于0表示自增值(赋值时赋值的第一个自增字段)</returns>
            public long Insert()
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                string FirstSequenceName = "";

                string InsertFieldsList = "";
                string InsertValuesList = "";
                IList<Parameter> Parameters = new List<Parameter>();

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        InsertFieldsList += ", ";
                        InsertValuesList += ", ";
                    }

                    InsertFieldsList += Fields[i].Name;

                    if (!string.IsNullOrEmpty(Fields[i].SequenceName))
                    {
                        InsertValuesList += Fields[i].SequenceName + ".nextval";

                        if (string.IsNullOrEmpty(FirstSequenceName))
                        {
                            FirstSequenceName = Fields[i].SequenceName;
                        }
                    }
                    else
                    {
                        InsertValuesList += ":" + Fields[i].CanonicalIdentifierName;
                        Parameters.Add(new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value));
                    }
                }

                string CommandText = "begin insert into " + TableName + " (" + InsertFieldsList + ") values (" + InsertValuesList + "); select " + (string.IsNullOrEmpty(FirstSequenceName) ? "0" : FirstSequenceName + ".currval") + " into :ShoveOracleReturnSequenceId from dual;end;";

                Parameters.Add(new Parameter("ShoveOracleReturnSequenceId", OracleType.Int32, 0, ParameterDirection.Output, 0));
                Parameter[] t_Parameters = new Parameter[Parameters.Count];
                Parameters.CopyTo(t_Parameters, 0);

                OutputParameter Outputs = new OutputParameter();
                long Result = ExecuteNonQuery(CommandText, ref Outputs, t_Parameters);

                Fields.Clear();

                return (Result < 0) ? Result : System.Convert.ToInt64(Outputs["ShoveOracleReturnSequenceId"]);
            }

            /// <summary>
            /// 增加记录
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <returns>小于0表示失败，0表示成功，无自增值，大于0表示自增值(赋值时赋值的第一个自增字段)</returns>
            public long Insert(string ConnectionString)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                string FirstSequenceName = "";

                string InsertFieldsList = "";
                string InsertValuesList = "";
                IList<Parameter> Parameters = new List<Parameter>();

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        InsertFieldsList += ", ";
                        InsertValuesList += ", ";
                    }

                    InsertFieldsList += Fields[i].Name;

                    if (!string.IsNullOrEmpty(Fields[i].SequenceName))
                    {
                        InsertValuesList += Fields[i].SequenceName + ".nextval";

                        if (string.IsNullOrEmpty(FirstSequenceName))
                        {
                            FirstSequenceName = Fields[i].SequenceName;
                        }
                    }
                    else
                    {
                        InsertValuesList += ":" + Fields[i].CanonicalIdentifierName;
                        Parameters.Add(new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value));
                    }
                }

                string CommandText = "begin insert into " + TableName + " (" + InsertFieldsList + ") values (" + InsertValuesList + "); select " + (string.IsNullOrEmpty(FirstSequenceName) ? "0" : FirstSequenceName + ".currval") + " into :ShoveOracleReturnSequenceId from dual;end;";

                Parameters.Add(new Parameter("ShoveOracleReturnSequenceId", OracleType.Int32, 0, ParameterDirection.Output, 0));
                Parameter[] t_Parameters = new Parameter[Parameters.Count];
                Parameters.CopyTo(t_Parameters, 0);

                OutputParameter Outputs = new OutputParameter();
                long Result = ExecuteNonQuery(ConnectionString, CommandText, ref Outputs, t_Parameters);

                Fields.Clear();

                return (Result < 0) ? Result : System.Convert.ToInt64(Outputs["ShoveOracleReturnSequenceId"]);
            }

            /// <summary>
            /// 增加记录
            /// </summary>
            /// <param name="conn"></param>
            /// <returns>小于0表示失败，0表示成功，无自增值，大于0表示自增值(赋值时赋值的第一个自增字段)</returns>
            public long Insert(OracleConnection conn)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                string FirstSequenceName = "";

                string InsertFieldsList = "";
                string InsertValuesList = "";
                IList<Parameter> Parameters = new List<Parameter>();

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        InsertFieldsList += ", ";
                        InsertValuesList += ", ";
                    }

                    InsertFieldsList += Fields[i].Name;

                    if (!string.IsNullOrEmpty(Fields[i].SequenceName))
                    {
                        InsertValuesList += Fields[i].SequenceName + ".nextval";

                        if (string.IsNullOrEmpty(FirstSequenceName))
                        {
                            FirstSequenceName = Fields[i].SequenceName;
                        }
                    }
                    else
                    {
                        InsertValuesList += ":" + Fields[i].CanonicalIdentifierName;
                        Parameters.Add(new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value));
                    }
                }

                string CommandText = "begin insert into " + TableName + " (" + InsertFieldsList + ") values (" + InsertValuesList + "); select " + (string.IsNullOrEmpty(FirstSequenceName) ? "0" : FirstSequenceName + ".currval") + " into :ShoveOracleReturnSequenceId from dual;end;";

                Parameters.Add(new Parameter("ShoveOracleReturnSequenceId", OracleType.Int32, 0, ParameterDirection.Output, 0));
                Parameter[] t_Parameters = new Parameter[Parameters.Count];
                Parameters.CopyTo(t_Parameters, 0);

                OutputParameter Outputs = new OutputParameter();
                long Result = ExecuteNonQuery(conn, CommandText, ref Outputs, t_Parameters);

                Fields.Clear();

                return (Result < 0) ? Result : System.Convert.ToInt64(Outputs["ShoveOracleReturnSequenceId"]);
            }

            #endregion

            #region Delete

            /// <summary>
            /// 删除表记录
            /// </summary>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Delete(string Condition)
            {
                Condition = Condition.Trim();

                OutputParameter Outputs = new OutputParameter();
                long Result = ExecuteNonQuery("delete from " + TableName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)), ref Outputs);

                Fields.Clear();

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

                OutputParameter Outputs = new OutputParameter();
                long Result = ExecuteNonQuery(ConnectionString, "delete from " + TableName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)), ref Outputs);

                Fields.Clear();

                return Result;
            }

            /// <summary>
            /// 删除表记录
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Delete(OracleConnection conn, string Condition)
            {
                Condition = Condition.Trim();

                OutputParameter Outputs = new OutputParameter();
                long Result = ExecuteNonQuery(conn, "delete from " + TableName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)), ref Outputs);

                Fields.Clear();

                return Result;
            }

            #endregion

            #region Update

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

                string CommandText = "update " + TableName + " set ";
                IList<Parameter> Parameters = new List<Parameter>();

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        CommandText += ", ";
                    }

                    CommandText += Fields[i].Name + " = ";

                    if (!string.IsNullOrEmpty(Fields[i].SequenceName))
                    {
                        CommandText += Fields[i].SequenceName + ".nextval";
                    }
                    else
                    {
                        CommandText += ":" + Fields[i].CanonicalIdentifierName;
                        Parameters.Add(new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value));
                    }
                }

                if (!string.IsNullOrEmpty(Condition))
                {
                    CommandText += " where " + FilteSqlInfusionForCondition(Condition);
                }

                Parameter[] t_Parameters = new Parameter[Parameters.Count];
                Parameters.CopyTo(t_Parameters, 0);

                OutputParameter Outputs = new OutputParameter();
                long Result = ExecuteNonQuery(CommandText, ref Outputs, t_Parameters);

                Fields.Clear();

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

                string CommandText = "update " + TableName + " set ";
                IList<Parameter> Parameters = new List<Parameter>();

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        CommandText += ", ";
                    }

                    CommandText += Fields[i].Name + " = ";

                    if (!string.IsNullOrEmpty(Fields[i].SequenceName))
                    {
                        CommandText += Fields[i].SequenceName + ".nextval";
                    }
                    else
                    {
                        CommandText += ":" + Fields[i].CanonicalIdentifierName;
                        Parameters.Add(new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value));
                    }
                }

                if (!string.IsNullOrEmpty(Condition))
                {
                    CommandText += " where " + FilteSqlInfusionForCondition(Condition);
                }

                Parameter[] t_Parameters = new Parameter[Parameters.Count];
                Parameters.CopyTo(t_Parameters, 0);

                OutputParameter Outputs = new OutputParameter();
                long Result = ExecuteNonQuery(ConnectionString, CommandText, ref Outputs, t_Parameters);

                Fields.Clear();

                return Result;
            }

            /// <summary>
            /// 更新表
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Update(OracleConnection conn, string Condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                Condition = Condition.Trim();

                string CommandText = "update " + TableName + " set ";
                IList<Parameter> Parameters = new List<Parameter>();

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        CommandText += ", ";
                    }

                    CommandText += Fields[i].Name + " = ";

                    if (!string.IsNullOrEmpty(Fields[i].SequenceName))
                    {
                        CommandText += Fields[i].SequenceName + ".nextval";
                    }
                    else
                    {
                        CommandText += ":" + Fields[i].CanonicalIdentifierName;
                        Parameters.Add(new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value));
                    }
                }

                if (!string.IsNullOrEmpty(Condition))
                {
                    CommandText += " where " + FilteSqlInfusionForCondition(Condition);
                }

                Parameter[] t_Parameters = new Parameter[Parameters.Count];
                Parameters.CopyTo(t_Parameters, 0);

                OutputParameter Outputs = new OutputParameter();
                long Result = ExecuteNonQuery(conn, CommandText, ref Outputs, t_Parameters);

                Fields.Clear();

                return Result;
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
            string ObjectNamePreFix
            {
                get
                {
                    string str = System.Configuration.ConfigurationManager.AppSettings["ObjectNamePreFix_View"];
                    return (string.IsNullOrEmpty(str) ? "" : str);
                }
            }

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

                return Select("select " + (FieldList == "" ? "*" : FieldList) + " from " + ObjectNamePreFix + ViewName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
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

                return Select(ConnectionString, "select " + (FieldList == "" ? "*" : FieldList) + " from " + ObjectNamePreFix + ViewName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            /// <summary>
            /// 打开视图
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <returns></returns>
            public DataTable Open(OracleConnection conn, string FieldList, string Condition, string Order)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                return Select(conn, "select " + (FieldList == "" ? "*" : FieldList) + " from " + ObjectNamePreFix + ViewName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            /// <summary>
            /// 获取视图记录数
            /// </summary>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar("select count(*) from " + ObjectNamePreFix + ViewName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

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

                object Result = ExecuteScalar(ConnectionString, "select count(*) from " + ObjectNamePreFix + ViewName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

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
            public long GetCount(OracleConnection conn, string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar(conn, "select count(*) from " + ObjectNamePreFix + ViewName + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

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
