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
        private static bool IsContainOracleTypeCursor(OracleCommand cmd)
        {
            foreach (OracleParameter p in cmd.Parameters)
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
        /// <param name="server"></param>
        /// <returns></returns>
        public static string BuildConnectString(string server)
        {
            return string.Format("Data Source={0};Integrated Security=yes", server);
        }

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="server"></param>
        /// <param name="uid"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string BuildConnectString(string server, string uid, string password)
        {
            return string.Format("Data Source={0};User Id={1};Password={2};Integrated Security=no", server, uid, password);
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
                ParametersName.Add(Name.StartsWith(":", StringComparison.Ordinal) ? Name.Substring(1) : Name);
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

        private static void AddParameter(ref OracleCommand cmd, params Parameter[] _params)
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

                OracleParameter param = new OracleParameter();
                param.ParameterName = _params[i].Name.StartsWith(":", StringComparison.Ordinal) ? _params[i].Name.Substring(1) : _params[i].Name;
                param.OracleType = _params[i].Type;

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

        private static void AddOutputParameter(OracleCommand cmd, ref OutputParameter outputs)
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
                OracleParameter param = cmd.Parameters[i];

                if ((param.Direction != ParameterDirection.InputOutput) &&
                    (param.Direction != ParameterDirection.Output))
                {
                    continue;
                }

                outputs.Add(param.ParameterName, param.Value);
            }
        }

        private static OracleParameter GetReturnParameter(OracleCommand cmd)
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
                OracleParameter param = cmd.Parameters[i];

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
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static long ExecuteNonQuery(string commandText, ref OutputParameter outputs, params Parameter[] _params)
        {
            return ExecuteNonQuery(GetConnectionStringFromConfig(), commandText, ref outputs, _params);
        }

        /// <summary>
        /// 执行数据库命令
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="commandText"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static long ExecuteNonQuery(string connectionString, string commandText, ref OutputParameter outputs, params Parameter[] _params)
        {
            OracleConnection conn = CreateDataConnection<OracleConnection>(connectionString);

            if (conn == null)
            {
                return -1001;
            }

            long result = ExecuteNonQuery(conn, commandText, ref outputs, _params);

            try
            {
                conn.Close();
            }
            catch { }

            return result;
        }

        /// <summary>
        /// 执行数据库命令
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static long ExecuteNonQuery(OracleConnection conn, string commandText, ref OutputParameter outputs, params Parameter[] _params)
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

            OracleCommand cmd = new OracleCommand(commandText, conn);
            AddParameter(ref cmd, _params);

            OracleTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                return -1001;
            }

            cmd.Transaction = trans;
            long result = -1002;

            try
            {
                result = cmd.ExecuteNonQuery();
                trans.Commit();
            }
            catch
            {
                try
                {
                    trans.Rollback();
                }
                catch { }

                result = -1002;
            }

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            if (result >= 0)
            {
                // 填写返回参数
                AddOutputParameter(cmd, ref outputs);
            }

            return result;
        }

        /// <summary>
        /// 执行数据库命令(不用事务)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static long ExecuteNonQueryNoTranscation(OracleConnection conn, string commandText, ref OutputParameter outputs, params Parameter[] _params)
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

            OracleCommand cmd = new OracleCommand(commandText, conn);
            AddParameter(ref cmd, _params);

            long result = -1002;

            try
            {
                result = cmd.ExecuteNonQuery();
            }
            catch
            {
                result = -1002;
            }

            if (!initOpenState)
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }

            if (result >= 0)
            {
                // 填写返回参数
                AddOutputParameter(cmd, ref outputs);
            }

            return result;
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
            OracleConnection conn = CreateDataConnection<OracleConnection>(connectionString);

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

            return dt;
        }

        /// <summary>
        /// 打开数据集
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static DataTable Select(OracleConnection conn, string commandText, params Parameter[] _params)
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

            OracleDataAdapter da = new OracleDataAdapter("", conn);
            OracleCommand cmd = new OracleCommand(commandText, conn);

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
            OracleConnection conn = CreateDataConnection<OracleConnection>(connectionString);

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

            return obj;
        }

        /// <summary>
        /// 读取第一行第一列
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteScalar(OracleConnection conn, string commandText, params Parameter[] _params)
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

            OracleCommand cmd = new OracleCommand(commandText, conn);
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
        /// <param name="returnDbType"></param>
        /// <param name="returnSize"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(string functionName, OracleType returnDbType, int returnSize, ref OutputParameter outputs, params Parameter[] _params)
        {
            return ExecuteFunction(GetConnectionStringFromConfig(), functionName, returnDbType, returnSize, ref outputs, _params);
        }

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="functionName"></param>
        /// <param name="returnDbType"></param>
        /// <param name="returnSize"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(string connectionString, string functionName, OracleType returnDbType, int returnSize, ref OutputParameter outputs, params Parameter[] _params)
        {
            OracleConnection conn = CreateDataConnection<OracleConnection>(connectionString);

            if (conn == null)
            {
                return null;
            }

            object obj = ExecuteFunction(conn, functionName, returnDbType, returnSize, ref outputs, _params);

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
        /// <param name="functionName"></param>
        /// <param name="returnDbType"></param>
        /// <param name="returnSize"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(OracleConnection conn, string functionName, OracleType returnDbType, int returnSize, ref OutputParameter outputs, params Parameter[] _params)
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

            string objectNamePreFix = System.Configuration.ConfigurationManager.AppSettings["ObjectNamePreFix_Function"];

            OracleCommand cmd = new OracleCommand((string.IsNullOrEmpty(objectNamePreFix) ? "" : objectNamePreFix) + functionName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            AddParameter(ref cmd, _params);

            cmd.Parameters.Add("ReturnValue", returnDbType, returnSize).Direction = ParameterDirection.ReturnValue;

            if (IsContainOracleTypeCursor(cmd))
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

            if (result)
            {
                // 填写返回参数
                AddOutputParameter(cmd, ref outputs);
            }

            return result ? cmd.Parameters["ReturnValue"].Value : null;
        }

        #endregion

        #region ExecuteFunctionWithQuery

        /// <summary>
        /// 执行函数，返回结果集
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="returnDbType"></param>
        /// <param name="returnSize"></param>
        /// <param name="returnDataSet"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteFunctionWithQuery(string functionName, OracleType returnDbType, int returnSize, ref DataSet returnDataSet, ref OutputParameter outputs, params Parameter[] _params)
        {
            return ExecuteFunctionWithQuery(GetConnectionStringFromConfig(), functionName, returnDbType, returnSize, ref returnDataSet, ref outputs, _params);
        }

        /// <summary>
        /// 执行函数，返回结果集
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="functionName"></param>
        /// <param name="returnDbType"></param>
        /// <param name="returnSize"></param>
        /// <param name="returnDataSet"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteFunctionWithQuery(string connectionString, string functionName, OracleType returnDbType, int returnSize, ref DataSet returnDataSet, ref OutputParameter outputs, params Parameter[] _params)
        {
            OracleConnection conn = CreateDataConnection<OracleConnection>(connectionString);

            if (conn == null)
            {
                return null;
            }

            object obj = ExecuteFunctionWithQuery(conn, functionName, returnDbType, returnSize, ref returnDataSet, ref outputs, _params);

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
        /// <param name="functionName"></param>
        /// <param name="returnDbType"></param>
        /// <param name="returnSize"></param>
        /// <param name="returnDataSet"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static object ExecuteFunctionWithQuery(OracleConnection conn, string functionName, OracleType returnDbType, int returnSize, ref DataSet returnDataSet, ref OutputParameter outputs, params Parameter[] _params)
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

            string objectNamePreFix = System.Configuration.ConfigurationManager.AppSettings["ObjectNamePreFix_Function"];

            OracleDataAdapter da = new OracleDataAdapter((string.IsNullOrEmpty(objectNamePreFix) ? "" : objectNamePreFix) + functionName, conn);
            OracleCommand cmd = da.SelectCommand;
            cmd.CommandType = CommandType.StoredProcedure;
            AddParameter(ref cmd, _params);

            cmd.Parameters.Add("ReturnValue", returnDbType, returnSize).Direction = ParameterDirection.ReturnValue;

            OracleTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                return null;
            }

            if (returnDataSet == null)
            {
                returnDataSet = new DataSet();
            }

            cmd.Transaction = trans;
            bool result;

            try
            {
                da.Fill(returnDataSet);
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

            if (result)
            {
                // 填写返回参数
                AddOutputParameter(cmd, ref outputs);
            }

            return result ? cmd.Parameters["ReturnValue"].Value : null;
        }

        #endregion

        #region ExecuteProcedure

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteProcedure(string procedureName, ref OutputParameter outputs, params Parameter[] _params)
        {
            return ExecuteProcedure(GetConnectionStringFromConfig(), procedureName, ref outputs, _params);
        }

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="procedureName"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteProcedure(string connectionString, string procedureName, ref OutputParameter outputs, params Parameter[] _params)
        {
            OracleConnection conn = CreateDataConnection<OracleConnection>(connectionString);

            if (conn == null)
            {
                return -1001;
            }

            int result = ExecuteProcedure(conn, procedureName, ref outputs, _params);

            try
            {
                conn.Close();
            }
            catch { }

            return result;
        }

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="procedureName"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteProcedure(OracleConnection conn, string procedureName, ref OutputParameter outputs, params Parameter[] _params)
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

            string objectNamePreFix = System.Configuration.ConfigurationManager.AppSettings["ObjectNamePreFix_Procedure"];

            OracleCommand cmd = new OracleCommand((string.IsNullOrEmpty(objectNamePreFix) ? "" : objectNamePreFix) + procedureName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            AddParameter(ref cmd, _params);

            if (IsContainOracleTypeCursor(cmd))
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

            if (result)
            {
                // 填写返回参数
                AddOutputParameter(cmd, ref outputs);
            }

            return result ? 0 : -1004;
        }

        #endregion

        #region ExecuteProcedureWithQuery

        /// <summary>
        /// 执行存储过程，返回结果集
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="returnDataSet"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteProcedureWithQuery(string procedureName, ref DataSet returnDataSet, ref OutputParameter outputs, params Parameter[] _params)
        {
            return ExecuteProcedureWithQuery(GetConnectionStringFromConfig(), procedureName, ref returnDataSet, ref outputs, _params);
        }

        /// <summary>
        /// 执行存储过程，返回结果集
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="procedureName"></param>
        /// <param name="returnDataSet"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteProcedureWithQuery(string connectionString, string procedureName, ref DataSet returnDataSet, ref OutputParameter outputs, params Parameter[] _params)
        {
            OracleConnection conn = CreateDataConnection<OracleConnection>(connectionString);

            if (conn == null)
            {
                return -1001;
            }

            int result = ExecuteProcedureWithQuery(conn, procedureName, ref returnDataSet, ref outputs, _params);

            try
            {
                conn.Close();
            }
            catch { }

            return result;
        }

        /// <summary>
        /// 执行存储过程，返回结果集
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="procedureName"></param>
        /// <param name="returnDataSet"></param>
        /// <param name="outputs"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteProcedureWithQuery(OracleConnection conn, string procedureName, ref DataSet returnDataSet, ref OutputParameter outputs, params Parameter[] _params)
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

            string objectNamePreFix = System.Configuration.ConfigurationManager.AppSettings["ObjectNamePreFix_Procedure"];

            OracleDataAdapter da = new OracleDataAdapter((string.IsNullOrEmpty(objectNamePreFix) ? "" : objectNamePreFix) + procedureName, conn);
            OracleCommand cmd = da.SelectCommand;
            cmd.CommandType = CommandType.StoredProcedure;
            AddParameter(ref cmd, _params);

            OracleTransaction trans;
            try
            {
                trans = conn.BeginTransaction();
            }
            catch
            {
                return -1003;
            }

            if (returnDataSet == null)
            {
                returnDataSet = new DataSet();
            }

            cmd.Transaction = trans;
            bool result;

            try
            {
                da.Fill(returnDataSet);
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

            if (result)
            {
                // 填写返回参数
                AddOutputParameter(cmd, ref outputs);
            }

            return result ? 0 : -1004;
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
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static byte[] BackupDataToZipStream(string connectionString)
        {
            OracleConnection conn = CreateDataConnection<OracleConnection>(connectionString);

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

            return result;
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

            DataTable dt = Select(conn, "select table_name from user_tables order by table_name");

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
                string TableName = dr["table_name"].ToString();

                DataTable Table = Select(conn, "select * from " + TableName);

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

                    return null;
                }

                Table.TableName = TableName;
                ds.Tables.Add(Table);
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
            OracleConnection conn = CreateDataConnection<OracleConnection>(connectionString);

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
        public static int RestoreDataFromZipStream(OracleConnection conn, byte[] dataBuffer)
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

            bool result;

            try
            {
                foreach (DataTable dt in ds.Tables)
                {
                    OracleCommand cmd = new OracleCommand("delete from " + dt.TableName, conn);
                    cmd.Transaction = trans;
                    cmd.ExecuteNonQuery();

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
                result = true;
            }
            catch//(Exception ee)
            {
                trans.Rollback();
                result = false;

                //System.Web.HttpContext.Current.Response.Write(ee.Message);
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
            /// <param name="canonicalIdentifierName"></param>
            /// <param name="dbtype"></param>
            public Field(object parent, string name, string canonicalIdentifierName, OracleType dbtype)
            {
                Parent = parent;
                Name = name;
                CanonicalIdentifierName = canonicalIdentifierName;
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

            #region Open

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

                return Select("select " + (fieldList == "" ? "*" : fieldList) + " from " + TableName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
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

                return Select(connectionString, "select " + (fieldList == "" ? "*" : fieldList) + " from " + TableName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
            }

            /// <summary>
            /// 打开表
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <returns></returns>
            public DataTable Open(OracleConnection conn, string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select(conn, "select " + (fieldList == "" ? "*" : fieldList) + " from " + TableName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
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

                object result = ExecuteScalar("select count(*) from " + TableName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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

                object result = ExecuteScalar(connectionString, "select count(*) from " + TableName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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
            public long GetCount(OracleConnection conn, string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar(conn, "select count(*) from " + TableName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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

                string commandText = "begin insert into " + TableName + " (" + InsertFieldsList + ") values (" + InsertValuesList + "); select " + (string.IsNullOrEmpty(FirstSequenceName) ? "0" : FirstSequenceName + ".currval") + " into :ShoveOracleReturnSequenceId from dual;end;";

                Parameters.Add(new Parameter("ShoveOracleReturnSequenceId", OracleType.Int32, 0, ParameterDirection.Output, 0));
                Parameter[] t_Parameters = new Parameter[Parameters.Count];
                Parameters.CopyTo(t_Parameters, 0);

                OutputParameter outputs = new OutputParameter();
                long result = ExecuteNonQuery(commandText, ref outputs, t_Parameters);

                Fields.Clear();

                return (result < 0) ? result : System.Convert.ToInt64(outputs["ShoveOracleReturnSequenceId"]);
            }

            /// <summary>
            /// 增加记录
            /// </summary>
            /// <param name="connectionString"></param>
            /// <returns>小于0表示失败，0表示成功，无自增值，大于0表示自增值(赋值时赋值的第一个自增字段)</returns>
            public long Insert(string connectionString)
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

                string commandText = "begin insert into " + TableName + " (" + InsertFieldsList + ") values (" + InsertValuesList + "); select " + (string.IsNullOrEmpty(FirstSequenceName) ? "0" : FirstSequenceName + ".currval") + " into :ShoveOracleReturnSequenceId from dual;end;";

                Parameters.Add(new Parameter("ShoveOracleReturnSequenceId", OracleType.Int32, 0, ParameterDirection.Output, 0));
                Parameter[] t_Parameters = new Parameter[Parameters.Count];
                Parameters.CopyTo(t_Parameters, 0);

                OutputParameter outputs = new OutputParameter();
                long result = ExecuteNonQuery(connectionString, commandText, ref outputs, t_Parameters);

                Fields.Clear();

                return (result < 0) ? result : System.Convert.ToInt64(outputs["ShoveOracleReturnSequenceId"]);
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

                string commandText = "begin insert into " + TableName + " (" + InsertFieldsList + ") values (" + InsertValuesList + "); select " + (string.IsNullOrEmpty(FirstSequenceName) ? "0" : FirstSequenceName + ".currval") + " into :ShoveOracleReturnSequenceId from dual;end;";

                Parameters.Add(new Parameter("ShoveOracleReturnSequenceId", OracleType.Int32, 0, ParameterDirection.Output, 0));
                Parameter[] t_Parameters = new Parameter[Parameters.Count];
                Parameters.CopyTo(t_Parameters, 0);

                OutputParameter outputs = new OutputParameter();
                long result = ExecuteNonQuery(conn, commandText, ref outputs, t_Parameters);

                Fields.Clear();

                return (result < 0) ? result : System.Convert.ToInt64(outputs["ShoveOracleReturnSequenceId"]);
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

                OutputParameter outputs = new OutputParameter();
                long result = ExecuteNonQuery("delete from " + TableName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)), ref outputs);

                Fields.Clear();

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

                OutputParameter outputs = new OutputParameter();
                long result = ExecuteNonQuery(connectionString, "delete from " + TableName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)), ref outputs);

                Fields.Clear();

                return result;
            }

            /// <summary>
            /// 删除表记录
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long Delete(OracleConnection conn, string condition)
            {
                condition = condition.Trim();

                OutputParameter outputs = new OutputParameter();
                long result = ExecuteNonQuery(conn, "delete from " + TableName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)), ref outputs);

                Fields.Clear();

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

                string commandText = "update " + TableName + " set ";
                IList<Parameter> Parameters = new List<Parameter>();

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        commandText += ", ";
                    }

                    commandText += Fields[i].Name + " = ";

                    if (!string.IsNullOrEmpty(Fields[i].SequenceName))
                    {
                        commandText += Fields[i].SequenceName + ".nextval";
                    }
                    else
                    {
                        commandText += ":" + Fields[i].CanonicalIdentifierName;
                        Parameters.Add(new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value));
                    }
                }

                if (!string.IsNullOrEmpty(condition))
                {
                    commandText += " where " + FilteSqlInfusionForCondition(condition);
                }

                Parameter[] t_Parameters = new Parameter[Parameters.Count];
                Parameters.CopyTo(t_Parameters, 0);

                OutputParameter outputs = new OutputParameter();
                long result = ExecuteNonQuery(commandText, ref outputs, t_Parameters);

                Fields.Clear();

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

                string commandText = "update " + TableName + " set ";
                IList<Parameter> Parameters = new List<Parameter>();

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        commandText += ", ";
                    }

                    commandText += Fields[i].Name + " = ";

                    if (!string.IsNullOrEmpty(Fields[i].SequenceName))
                    {
                        commandText += Fields[i].SequenceName + ".nextval";
                    }
                    else
                    {
                        commandText += ":" + Fields[i].CanonicalIdentifierName;
                        Parameters.Add(new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value));
                    }
                }

                if (!string.IsNullOrEmpty(condition))
                {
                    commandText += " where " + FilteSqlInfusionForCondition(condition);
                }

                Parameter[] t_Parameters = new Parameter[Parameters.Count];
                Parameters.CopyTo(t_Parameters, 0);

                OutputParameter outputs = new OutputParameter();
                long result = ExecuteNonQuery(connectionString, commandText, ref outputs, t_Parameters);

                Fields.Clear();

                return result;
            }

            /// <summary>
            /// 更新表
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long Update(OracleConnection conn, string condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                condition = condition.Trim();

                string commandText = "update " + TableName + " set ";
                IList<Parameter> Parameters = new List<Parameter>();

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        commandText += ", ";
                    }

                    commandText += Fields[i].Name + " = ";

                    if (!string.IsNullOrEmpty(Fields[i].SequenceName))
                    {
                        commandText += Fields[i].SequenceName + ".nextval";
                    }
                    else
                    {
                        commandText += ":" + Fields[i].CanonicalIdentifierName;
                        Parameters.Add(new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value));
                    }
                }

                if (!string.IsNullOrEmpty(condition))
                {
                    commandText += " where " + FilteSqlInfusionForCondition(condition);
                }

                Parameter[] t_Parameters = new Parameter[Parameters.Count];
                Parameters.CopyTo(t_Parameters, 0);

                OutputParameter outputs = new OutputParameter();
                long result = ExecuteNonQuery(conn, commandText, ref outputs, t_Parameters);

                Fields.Clear();

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
            string objectNamePreFix
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
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <returns></returns>
            public DataTable Open(string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select("select " + (fieldList == "" ? "*" : fieldList) + " from " + objectNamePreFix + ViewName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
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

                return Select(connectionString, "select " + (fieldList == "" ? "*" : fieldList) + " from " + objectNamePreFix + ViewName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
            }

            /// <summary>
            /// 打开视图
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <returns></returns>
            public DataTable Open(OracleConnection conn, string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select(conn, "select " + (fieldList == "" ? "*" : fieldList) + " from " + objectNamePreFix + ViewName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
            }

            /// <summary>
            /// 获取视图记录数
            /// </summary>
            /// <param name="condition"></param>
            /// <returns></returns>
            public long GetCount(string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar("select count(*) from " + objectNamePreFix + ViewName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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

                object result = ExecuteScalar(connectionString, "select count(*) from " + objectNamePreFix + ViewName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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
            public long GetCount(OracleConnection conn, string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar(conn, "select count(*) from " + objectNamePreFix + ViewName + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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
