using System;
using MySql.Data.MySqlClient;
using System.Collections;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Shove.Database
{
    /// <summary>
    /// Shove ��ר�õ� MySQL ���������
    /// </summary>
    public class MySQL : DatabaseAccess
    {
        #region BuildConnectString

        /// <summary>
        /// �������Ӵ�
        /// </summary>
        /// <param name="UID"></param>
        /// <param name="Password"></param>
        /// <param name="DatabaseName"></param>
        /// <returns></returns>
        public static string BuildConnectString(string UID, string Password, string DatabaseName)
        {
            return string.Format("server=localhost; user id={0}; password={1}; database={2};", UID, Password, DatabaseName);
        }

        /// <summary>
        /// �������Ӵ�
        /// </summary>
        /// <param name="ServerName"></param>
        /// <param name="UID"></param>
        /// <param name="Password"></param>
        /// <param name="DatabaseName"></param>
        /// <returns></returns>
        public static string BuildConnectString(string ServerName, string UID, string Password, string DatabaseName)
        {
            return string.Format("server={0}; user id={1}; password={2}; database={3};", ServerName, UID, Password, DatabaseName);
        }

        /// <summary>
        /// �������Ӵ�
        /// </summary>
        /// <param name="ServerName"></param>
        /// <param name="UID"></param>
        /// <param name="Password"></param>
        /// <param name="DatabaseName"></param>
        /// <param name="Port"></param>
        /// <returns></returns>
        public static string BuildConnectString(string ServerName, string UID, string Password, string DatabaseName, string Port)
        {
            return string.Format("server={0}; user id={1}; password={2}; database={3}; port={4}", ServerName, UID, Password, DatabaseName, Port);
        }

        /// <summary>
        /// �������Ӵ�
        /// </summary>
        /// <param name="ServerName"></param>
        /// <param name="UID"></param>
        /// <param name="Password"></param>
        /// <param name="DatabaseName"></param>
        /// <param name="Port"></param>
        /// <param name="Charset"></param>
        /// <returns></returns>
        public static string BuildConnectString(string ServerName, string UID, string Password, string DatabaseName, string Port, string Charset)
        {
            return string.Format("server={0}; user id={1}; password={2}; database={3}; port={4}; charset={5}", ServerName, UID, Password, DatabaseName, Port, Charset);
        }

        #endregion

        #region Parameter

        /// <summary>
        /// ����
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
        /// �������
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
                ParametersName.Add(Name.StartsWith("?") ? Name.Substring(1) : Name);
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

        private static void AddParameter(ref MySqlCommand Cmd, params Parameter[] Params)
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

                MySqlParameter param = new MySqlParameter();
                param.ParameterName = Params[i].Name.StartsWith("?") ? Params[i].Name : ("?" + Params[i].Name);
                param.MySqlDbType = Params[i].Type;

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

        private static void AddOutputParameter(MySqlCommand Cmd, ref OutputParameter Outputs)
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
                MySqlParameter param = Cmd.Parameters[i];

                if ((param.Direction != ParameterDirection.InputOutput) &&
                    (param.Direction != ParameterDirection.Output))
                {
                    continue;
                }

                Outputs.Add(param.ParameterName, param.Value);
            }
        }

        private static MySqlParameter GetReturnParameter(MySqlCommand Cmd)
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
                MySqlParameter param = Cmd.Parameters[i];

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
        /// ִ�����ݿ�����
        /// </summary>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string CommandText, params Parameter[] Params)
        {
            return ExecuteNonQuery(GetConnectionStringFromConfig(), CommandText, Params);
        }

        /// <summary>
        /// ִ�����ݿ�����
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string ConnectionString, string CommandText, params Parameter[] Params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(ConnectionString);

            if (conn == null)
            {
                return -1001;
            }

            int Result = ExecuteNonQuery(conn, CommandText, Params);

            try
            {
                try
                {
                    conn.Close();
                }
                catch { }
            }
            catch { }

            return Result;
        }

        /// <summary>
        /// ִ�����ݿ�����
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(MySqlConnection conn, string CommandText, params Parameter[] Params)
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

            //string sss = @"SET FOREIGN_KEY_CHECKS = 0;";

            MySqlCommand Cmd = new MySqlCommand(CommandText, conn);
            AddParameter(ref Cmd, Params);

            MySqlTransaction trans;
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
                Cmd.CommandTimeout = 5000000;
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
        /// ִ�����ݿ�����(��������)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteNonQueryNoTranscation(MySqlConnection conn, string CommandText, params Parameter[] Params)
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

            MySqlCommand Cmd = new MySqlCommand(CommandText, conn);
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
        /// �����ݼ�
        /// </summary>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static DataTable Select(string CommandText, params Parameter[] Params)
        {
            return Select(GetConnectionStringFromConfig(), CommandText, Params);
        }

        /// <summary>
        /// �����ݼ�
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static DataTable Select(string ConnectionString, string CommandText, params Parameter[] Params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(ConnectionString);

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
        /// �����ݼ�
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static DataTable Select(MySqlConnection conn, string CommandText, params Parameter[] Params)
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

            MySqlDataAdapter da = new MySqlDataAdapter("", conn);
            MySqlCommand Cmd = new MySqlCommand(CommandText, conn);

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

        #region Loong Add

        /// <summary>
        /// �����ݼ�
        /// </summary>
        /// <param name="CommandText"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static DataTable Select(string CommandText, int commandTimeout, params Parameter[] Params)
        {
            return Select(GetConnectionStringFromConfig(), CommandText, commandTimeout, Params);
        }

        /// <summary>
        /// �����ݼ�
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="CommandText"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static DataTable Select(string ConnectionString, string CommandText, int commandTimeout, params Parameter[] Params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(ConnectionString);

            if (conn == null)
            {
                return null;
            }

            DataTable dt = Select(conn, CommandText, commandTimeout, Params);

            try
            {
                conn.Close();
            }
            catch { }

            return dt;
        }

        /// <summary>
        /// ������?
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="CommandText"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static DataTable Select(MySqlConnection conn, string CommandText, int commandTimeout, params Parameter[] Params)
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

            MySqlDataAdapter da = new MySqlDataAdapter("", conn);
            MySqlCommand Cmd = new MySqlCommand(CommandText, conn);
            Cmd.CommandTimeout = commandTimeout;

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

        #endregion

        #region ExecuteScalar

        /// <summary>
        /// ��ȡ��һ�е�һ��
        /// </summary>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteScalar(string CommandText, params Parameter[] Params)
        {
            return ExecuteScalar(GetConnectionStringFromConfig(), CommandText, Params);
        }

        /// <summary>
        /// ��ȡ��һ�е�һ��
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteScalar(string ConnectionString, string CommandText, params Parameter[] Params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(ConnectionString);

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
        /// ��ȡ��һ�е�һ��
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="CommandText"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteScalar(MySqlConnection conn, string CommandText, params Parameter[] Params)
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

            MySqlCommand Cmd = new MySqlCommand(CommandText, conn);
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
        /// ִ�к���
        /// </summary>
        /// <param name="FunctionName"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(string FunctionName, params Parameter[] Params)
        {
            return ExecuteFunction(GetConnectionStringFromConfig(), FunctionName, Params);
        }

        /// <summary>
        /// ִ�к���
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="FunctionName"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(string ConnectionString, string FunctionName, params Parameter[] Params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(ConnectionString);

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
        /// ִ�к���
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="FunctionName"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static object ExecuteFunction(MySqlConnection conn, string FunctionName, params Parameter[] Params)
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

            string CommandText = "select " + FunctionName + "(";

            if (Params != null)
            {
                for (int i = 0; i < Params.Length; i++)
                {
                    if (Params[i] != null)
                    {
                        bool isChar = false;

                        if ((Params[i].Type == MySqlDbType.Date) || (Params[i].Type == MySqlDbType.DateTime) ||
                            (Params[i].Type == MySqlDbType.Guid) || (Params[i].Type == MySqlDbType.LongText) || (Params[i].Type == MySqlDbType.MediumText) ||
                            (Params[i].Type == MySqlDbType.Newdate) || (Params[i].Type == MySqlDbType.String) || (Params[i].Type == MySqlDbType.Text) ||
                            (Params[i].Type == MySqlDbType.Time) || (Params[i].Type == MySqlDbType.Timestamp) || (Params[i].Type == MySqlDbType.TinyText) ||
                            (Params[i].Type == MySqlDbType.VarChar) || (Params[i].Type == MySqlDbType.VarString))
                        {
                            isChar = true;
                        }

                        if (!CommandText.EndsWith("("))
                        {
                            CommandText += ", ";
                        }

                        if (isChar)
                        {
                            CommandText += "\'";
                        }

                        CommandText += Params[i].Value.ToString();

                        if (isChar)
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
        /// ִ�д洢����
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
        /// ִ�д洢����
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="StoredProcedureName"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureNonQuery(string ConnectionString, string StoredProcedureName, ref OutputParameter Outputs, params Parameter[] Params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(ConnectionString);

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
        /// ִ�д洢����
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="StoredProcedureName"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureNonQuery(MySqlConnection conn, string StoredProcedureName, ref OutputParameter Outputs, params Parameter[] Params)
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

            MySqlCommand Cmd = new MySqlCommand(StoredProcedureName, conn);
            Cmd.CommandType = CommandType.StoredProcedure;

            AddParameter(ref Cmd, Params);

            // ���ӷ���ֵ����
            //MySqlParameter ReturnValue = new MySqlParameter("?Shove_Database_MySQL_ExecuteStoredProcedureNonQuery_Rtn", SqlDbType.Int);
            //ReturnValue.Direction = ParameterDirection.ReturnValue;
            //Cmd.Parameters.Add(ReturnValue);

            MySqlTransaction trans;
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

            // ��д���ز���
            AddOutputParameter(Cmd, ref Outputs);

            // ��ȡ���̵ķ�����           //ReturnValue = GetReturnParameter(Cmd);

            //if (ReturnValue != null)
            //{
            //    return (int)ReturnValue.Value;
            //}

            return 0;
        }

        #endregion

        #region ExecuteStoredProcedureWithQuery

        /// <summary>
        /// ִ�д洢����(�������ؼ�¼��)
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
        /// ִ�д洢����(�������ؼ�¼��)
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="StoredProcedureName"></param>
        /// <param name="ds"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureWithQuery(string ConnectionString, string StoredProcedureName, ref DataSet ds, ref OutputParameter Outputs, params Parameter[] Params)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(ConnectionString);

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
        /// ִ�д洢����(�������ؼ�¼��)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="StoredProcedureName"></param>
        /// <param name="ds"></param>
        /// <param name="Outputs"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public static int ExecuteStoredProcedureWithQuery(MySqlConnection conn, string StoredProcedureName, ref DataSet ds, ref OutputParameter Outputs, params Parameter[] Params)
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

            MySqlDataAdapter da = new MySqlDataAdapter("", conn);
            MySqlCommand Cmd = new MySqlCommand(StoredProcedureName, conn);

            Cmd.CommandType = CommandType.StoredProcedure;
            Cmd.Parameters.Clear();
            AddParameter(ref Cmd, Params);

            // ���ӷ���ֵ����
            //MySqlParameter ReturnValue = new MySqlParameter("?Shove_Database_MSSQL_ExecuteStoredProcedureWithQuery_Rtn", SqlDbType.Int);
            //ReturnValue.Direction = ParameterDirection.ReturnValue;
            //Cmd.Parameters.Add(ReturnValue);

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

            //��д���ز���
            AddOutputParameter(Cmd, ref Outputs);

            // ��ȡ���̵ķ���ֵ
            //ReturnValue = GetReturnParameter(Cmd);

            //if (ReturnValue != null)
            //{
            //    return (int)ReturnValue.Value;
            //}

            return 0;
        }

        #endregion

        #region BackupDatabase, 2015.6.12 Loong �޸��˻���

        /// <summary>
        /// �������ݿ�(�����ݿ� XML ������ѹ��Ϊ��������)
        /// </summary>
        /// <returns></returns>
        public static byte[] BackupDataToZipStream()
        {
            return BackupDataToZipStream(GetConnectionStringFromConfig());
        }

        /// <summary>
        /// �������ݿ�(�����ݿ� XML ������ѹ��Ϊ��������)
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <returns></returns>
        public static byte[] BackupDataToZipStream(string ConnectionString)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(ConnectionString);

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

            return buffer;
        }

        /// <summary>
        /// �������ݿ�(�����ݿ� XML ������ѹ��Ϊ��������)
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

            return String.Compress(sb.ToString());
        }

        #region Tools

        /// <summary>
        /// �������ݿ�(�����ݿ� XML ������ѹ��Ϊ��������)
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        private static DataSet BackupDataToDataSet(MySqlConnection conn)
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
                { conn.Open(); }
                catch { return null; }
            }

            long sIndex = 1;
            string sql = string.Empty;
            string strTemp = string.Empty;
            string databaserName = string.Empty;	//	���ݿ�����            
            string dbDefiner = string.Empty;        // ���ݿ� �������洢���̴����û�
            string[] dbDefinerAry = null;
            int commandTimeout = 60 * 10;           // ���ݿ�ִ�г�ʱʱ��

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

            #region TableRows   (�ṡ�����

            dtTableModels.TableName = "TableModels";
            dtTableDatas.TableName = "TableDatas";

            sql = "SHOW FULL TABLES WHERE Table_type = 'BASE TABLE';";
            dtTemp = Select(conn, sql, 600);

            if (dtTemp == null)
            {
                if (!InitOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            DataTable dtColInfo = null;

            foreach (DataRow dr in dtTemp.Rows)
            {
                #region Table Models

                // �������Լ��
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
                    if (!InitOpenState) { try { conn.Close(); } catch { } }
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
                    if (!InitOpenState) { try { conn.Close(); } catch { } }
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
                    if (!InitOpenState) { try { conn.Close(); } catch { } }
                    return null;
                }

                #region ��ϲ�������﷨���� insert into ���� values(ֵ..); insert into ...

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

            #region Functions   (����)

            sIndex = 1;
            dtFunctions.TableName = "Functions";

            // Get Database Name
            sql = "SELECT DATABASE();";
            dtTemp = Select(conn, sql, commandTimeout);

            if (dtTemp == null)
            {
                if (!InitOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            databaserName = dtTemp.Rows[0][0].ToString();

            // Show Functions
            sql = string.Format("SHOW FUNCTION STATUS WHERE UPPER(TRIM(Db))= UPPER(TRIM('{0}'));", databaserName);

            dtTableTemp = Select(conn, sql);

            if (dtTableTemp == null)
            {
                if (!InitOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            foreach (DataRow dr in dtTableTemp.Rows)
            {
                // Functions Name
                sql = string.Format("SHOW CREATE FUNCTION `{0}`;", dr["Name"]);
                dtTemp = Select(conn, sql);

                if (dtTemp == null || dtTemp.Rows.Count < 1)
                {
                    if (!InitOpenState) { try { conn.Close(); } catch { } }
                    return null;
                }

                // ��ȡ ���� SQL �ű�
                strTemp = SqlFormat(dtTemp.Rows[0][2].ToString());

                try
                {
                    // ��ֹ��ͬ�û��������ݿ⻹ԭ���µĴ���
                    dbDefinerAry = dr["Definer"].ToString().Split('@');
                    dbDefiner = string.Format(" DEFINER=`{0}`@`{1}`", dbDefinerAry[0], dbDefinerAry[1]);

                    strTemp = strTemp.Replace(dbDefiner, string.Empty);
                }
                // [loong] add start
                catch { strTemp = EraseDefiner(strTemp); }
                // [loong] add end

                // **** DELIMITER;; ���������
                //strTemp = string.Format("DROP FUNCTION IF EXISTS `{0}`; DELIMITER ;; {1} DELIMITER ;;", dr["Name"], strTemp);
                strTemp = string.Format("DROP FUNCTION IF EXISTS `{0}`; {1};", dr["Name"], strTemp);

                // ===============================================================================================
                dtFunctions.Rows.Add(new object[] { sIndex, strTemp });

                sIndex++;
            }

            #endregion

            #region Procedures  (�洢����)

            sIndex = 1;
            dtProcedures.TableName = "Procedures";

            // Show Procedures
            sql = string.Format("SHOW PROCEDURE STATUS WHERE UPPER(TRIM(Db))= UPPER(TRIM('{0}'));", databaserName);
            dtTemp = Select(conn, sql);

            if (dtTemp == null)
            {
                if (!InitOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            foreach (DataRow dr in dtTemp.Rows)
            {
                sql = string.Format("SHOW CREATE PROCEDURE `{0}`;", dr["Name"]);
                dtTableTemp = Select(conn, sql);

                if (dtTableTemp == null || dtTableTemp.Rows.Count < 1)
                {
                    if (!InitOpenState) { try { conn.Close(); } catch { } }
                    return null;
                }

                // Get Sql Script
                strTemp = SqlFormat(dtTableTemp.Rows[0]["Create Procedure"].ToString());

                try
                {
                    // ��ֹ��ͬ�û��������ݿ⻹ԭ���µĴ���
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

            #region Events  (�¼�)

            sIndex = 1;
            dtEvents.TableName = "Events";

            // Show Events
            sql = string.Format("SHOW EVENTS WHERE UPPER(TRIM(Db)) = UPPER(TRIM('{0}'));", databaserName);
            dtTemp = Select(conn, sql);

            if (dtTemp == null)
            {
                if (!InitOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            foreach (DataRow dr in dtTemp.Rows)
            {
                // Event Name
                sql = string.Format("SHOW CREATE EVENT `{0}`;", dr["Name"]);
                dtTableTemp = Select(conn, sql);

                if (dtTableTemp == null || dtTableTemp.Rows.Count < 1)
                {
                    if (!InitOpenState) { try { conn.Close(); } catch { } }
                    continue;
                }

                // Get Event Sql Script
                strTemp = SqlFormat(dtTableTemp.Rows[0]["Create Event"].ToString());

                try
                {
                    // ��ֹ��ͬ�û��������ݿ⻹ԭ���µĴ���
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

            #region Triggers    (������)

            sIndex = 1;
            dtTriggers.TableName = "Triggers";

            // Show Triggers
            sql = "SHOW TRIGGERS;";
            dtTemp = Select(conn, sql);

            if (dtTemp == null)
            {
                if (!InitOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            foreach (DataRow dr in dtTemp.Rows)
            {
                // Trigger Name
                sql = string.Format("SHOW CREATE TRIGGER `{0}`;", dr["Trigger"]);
                dtTableTemp = Select(conn, sql);

                if (dtTableTemp == null || dtTableTemp.Rows.Count < 1)
                {
                    if (!InitOpenState) { try { conn.Close(); } catch { } }
                    continue;
                }

                // Get Trigger Sql Script
                strTemp = SqlFormat(dtTableTemp.Rows[0]["SQL Original Statement"].ToString());

                try
                {
                    // ��ֹ��ͬ�û��������ݿ⻹ԭ���µĴ���
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

            #region Views   (��ͼ)

            sIndex = 1;
            dtViews.TableName = "Views";

            // Show Events
            sql = string.Format("SHOW FULL TABLES FROM `{0}` WHERE Table_type = 'VIEW';", databaserName);
            dtTemp = Select(conn, sql);

            if (dtTemp == null)
            {
                if (!InitOpenState) { try { conn.Close(); } catch { } }
                return null;
            }

            foreach (DataRow dr in dtTemp.Rows)
            {
                // View Name
                sql = string.Format("SHOW CREATE VIEW `{0}`;", dr[0]);
                dtTableTemp = Select(conn, sql);

                if (dtTableTemp == null || dtTableTemp.Rows.Count < 1)
                {
                    if (!InitOpenState) { try { conn.Close(); } catch { } }
                    continue;
                }

                // Get View Sql Script
                strTemp = SqlFormat(dtTableTemp.Rows[0]["Create View"].ToString());

                try
                {
                    // ��ֹ��ͬ�û��������ݿ⻹ԭ���µĴ���  * ���������ڶ���û������Ĳ�ͬ��ͼ�ͻ�����©��
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
                int i = sql.LastIndexOf(a);

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
        /// MySQL �ֶ����ʹ���
        /// </summary>
        /// <param name="ob"></param>
        /// <param name="wrapStringWithSingleQuote"></param>
        /// <param name="escapeStringSequence"></param>
        /// <param name="dc"></param>
        /// <param name="dr"></param>
        /// <returns></returns>
        static string ConvertToSqlFormat(object ob, bool wrapStringWithSingleQuote, bool escapeStringSequence, DataColumn dc, DataRow dr)
        {
            #region ���� MySQL �� DateTime �����ֶγ���

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

                    // ֧�� Millisecond ΢��
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

                        // ֧�� Microsecond ΢��
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
        /// ��������ģ��
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
        /// SQL �ַ�����ʽ����������\r��\n��
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        static string SqlFormat(string sql)
        {
            return sql.Replace(Environment.NewLine, "^~~~~~~^").Replace("\r", "^~~~~~~^").Replace("\n", "^~~~~~~^").Replace("^~~~~~~^", Environment.NewLine);
        }

        // [loong] add start
        /// <summary>
        /// ������ݿ��û���Ϣ 
        /// (����û�����һ�µ��µĴ���)
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

        #region RestoreDatabase, 2015.6.12 Loong �޸��˻���

        /// <summary>
        /// �ָ����ݿ�(�Ӷ�����ѹ��������ȡ�����ݿ�� XML ���лָ�)
        /// </summary>
        /// <param name="DataBuffer"></param>
        /// <returns></returns>
        public static int RestoreDataFromZipStream(byte[] DataBuffer)
        {
            return RestoreDataFromZipStream(GetConnectionStringFromConfig(), DataBuffer);
        }

        /// <summary>
        /// �ָ����ݿ�(�Ӷ�����ѹ��������ȡ�����ݿ�� XML ���лָ�)
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="DataBuffer"></param>
        /// <returns></returns>
        public static int RestoreDataFromZipStream(string ConnectionString, byte[] DataBuffer)
        {
            MySqlConnection conn = CreateDataConnection<MySqlConnection>(ConnectionString);

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
        /// �ָ����ݿ�(�Ӷ�����ѹ��������ȡ�����ݿ�� XML ���лָ�)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="DataBuffer"></param>
        /// <returns></returns>
        public static int RestoreDataFromZipStream(MySqlConnection conn, byte[] DataBuffer)
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

            cmd.CommandTimeout = 600 * 2; // (20����)
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
            int errorTryCount = 15;  // ���Դ���
            DataTable dtTemp = null;
            DataTable dtViews = ds.Tables["Views"];
            long execResult = -1;

            while (dtViews.Rows.Count > 0 && errorTryCount > 0)
            {
                dtTemp = new DataTable();
                dtTemp = dtViews.Clone();

                foreach (DataRow dr in dtViews.Rows)
                {
                    sIndex = Shove.Convert.StrToLong(dr["Id"].ToString(), -1);

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

            if (!InitOpenState)
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

        #region ShoveDAL ����ͨ��һ���ĸ������ɱ���ͼ�ķ��ʵĳ־û�����

        /// <summary>
        /// ����ֶ��࣬ShoveDAL.30 ����ʹ��
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
                        throw new Exception("the member ��" + Name + "�� is ReadOnly.");
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
            public Field(object parent, string name, string canonicalidentifiername, MySqlDbType dbtype, bool _readonly)
            {
                Parent = parent;
                Name = name;
                CanonicalIdentifierName = canonicalidentifiername;
                DbType = dbtype;
                ReadOnly = _readonly;
            }
        }

        /// <summary>
        /// ����޸ĵ��ֶμ��ϣ�ShoveDAL.30 ����ʹ��
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
        /// ��Ļ��࣬ShoveDAL.30 ����ʹ��
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
            /// �򿪱�
            /// </summary>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <param name="LimitStart">�ӵڼ�����ʼ��С�� 0 ��ʾ���� </param>
            /// <param name="LimitCount">������������¼��С�� 1 ��ʾ����</param>
            /// <returns></returns>
            public DataTable Open(string FieldList, string Condition, string Order, long LimitStart, long LimitCount)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                string Limit = "";

                if (LimitStart >= 0)
                {
                    Limit = " limit " + LimitStart.ToString();

                    if (LimitCount >= 1)
                    {
                        Limit += ", " + LimitCount.ToString();
                    }
                }

                return Select("select " + (FieldList == "" ? "*" : FieldList) + " from `" + TableName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)) + Limit);
            }

            /// <summary>
            /// �򿪱�
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <param name="LimitStart">�ӵڼ�����ʼ��С�� 0 ��ʾ���� </param>
            /// <param name="LimitCount">������������¼��С�� 1 ��ʾ����</param>
            /// <returns></returns>
            public DataTable Open(string ConnectionString, string FieldList, string Condition, string Order, long LimitStart, long LimitCount)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                string Limit = "";

                if (LimitStart >= 0)
                {
                    Limit = " limit " + LimitStart.ToString();

                    if (LimitCount >= 1)
                    {
                        Limit += ", " + LimitCount.ToString();
                    }
                }

                return Select(ConnectionString, "select " + (FieldList == "" ? "*" : FieldList) + " from `" + TableName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)) + Limit);
            }

            /// <summary>
            /// �򿪱�
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <param name="LimitStart">�ӵڼ�����ʼ��С�� 0 ��ʾ���� </param>
            /// <param name="LimitCount">������������¼��С�� 1 ��ʾ����</param>
            /// <returns></returns>
            public DataTable Open(MySqlConnection conn, string FieldList, string Condition, string Order, long LimitStart, long LimitCount)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                string Limit = "";

                if (LimitStart >= 0)
                {
                    Limit = " limit " + LimitStart.ToString();

                    if (LimitCount >= 1)
                    {
                        Limit += ", " + LimitCount.ToString();
                    }
                }

                return Select(conn, "select " + (FieldList == "" ? "*" : FieldList) + " from `" + TableName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)) + Limit);
            }

            #endregion

            #region GetCount

            /// <summary>
            /// ��ȡ���¼��
            /// </summary>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar("select count(*) from `" + TableName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
            }

            /// <summary>
            /// ��ȡ���¼��
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(string ConnectionString, string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar(ConnectionString, "select count(*) from `" + TableName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
            }

            /// <summary>
            /// ��ȡ���¼��
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(MySqlConnection conn, string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar(conn, "select count(*) from `" + TableName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
            }

            #endregion

            #region Insert

            /// <summary>
            /// ���Ӽ�¼
            /// </summary>
            /// <returns>С��0��ʾʧ�ܣ�0��ʾ�ɹ���������ֵ������0��ʾ����ֵ</returns>
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

                string CommandText = "insert into `" + TableName + "` (" + InsertFieldsList + ") values (" + InsertValuesList + "); select ifnull(LAST_INSERT_ID(), -99999999)";

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
            /// ���Ӽ�¼
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <returns>С��0��ʾ��ܣ?ʾɹ��?�����ֵ�?��?ʾ����ֵ</returns>
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

                    InsertFieldsList += "`" + Fields[i].Name + "`";
                    InsertValuesList += "?" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                string CommandText = "insert into `" + TableName + "` (" + InsertFieldsList + ") values (" + InsertValuesList + "); select ifnull(LAST_INSERT_ID(), -99999999)";

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
            /// ���Ӽ�¼
            /// </summary>
            /// <param name="conn"></param>
            /// <returns>С��0��ʾʧ�ܣ�0��ʾ�ɹ���������ֵ������0��ʾ����ֵ</returns>
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

                string CommandText = "insert into `" + TableName + "` (" + InsertFieldsList + ") values (" + InsertValuesList + "); select ifnull(LAST_INSERT_ID(), -99999999)";

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

            #endregion

            #region Delete

            /// <summary>
            /// ɾ�����¼
            /// </summary>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Delete(string Condition)
            {
                Condition = Condition.Trim();

                object objResult = ExecuteScalar("delete from `" + TableName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + "; select ifnull(ROW_COUNT(), -99999999)");

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
            /// ɾ�����¼
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Delete(string ConnectionString, string Condition)
            {
                Condition = Condition.Trim();

                object objResult = ExecuteScalar(ConnectionString, "delete from `" + TableName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + "; select ifnull(ROW_COUNT(), -99999999)");

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
            /// ɾ�����¼
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Delete(MySqlConnection conn, string Condition)
            {
                Condition = Condition.Trim();

                object objResult = ExecuteScalar(conn, "delete from `" + TableName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + "; select ifnull(ROW_COUNT(), -99999999)");

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

            #endregion

            #region Update

            /// <summary>
            /// ���±�
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

                string CommandText = "update `" + TableName + "` set ";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        CommandText += ", ";
                    }

                    CommandText += "`" + Fields[i].Name + "` = ?" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                if (!string.IsNullOrEmpty(Condition))
                {
                    CommandText += " where " + FilteSqlInfusionForCondition(Condition);
                }

                CommandText += "; select ifnull(ROW_COUNT(), -99999999)";

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
            /// ���±�
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

                string CommandText = "update `" + TableName + "` set ";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        CommandText += ", ";
                    }

                    CommandText += "`" + Fields[i].Name + "` = ?" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                if (!string.IsNullOrEmpty(Condition))
                {
                    CommandText += " where " + FilteSqlInfusionForCondition(Condition);
                }

                CommandText += "; select ifnull(ROW_COUNT(), -99999999)";

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
            /// ���±�
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Update(MySqlConnection conn, string Condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                Condition = Condition.Trim();

                string CommandText = "update `" + TableName + "` set ";
                Parameter[] Parameters = new Parameter[Fields.Count];

                for (int i = 0; i < Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        CommandText += ", ";
                    }

                    CommandText += "`" + Fields[i].Name + "` = ?" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                if (!string.IsNullOrEmpty(Condition))
                {
                    CommandText += " where " + FilteSqlInfusionForCondition(Condition);
                }

                CommandText += "; select ifnull(ROW_COUNT(), -99999999)";

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

            #endregion
        }

        /// <summary>
        /// ��ͼ�Ļ��࣬ShoveDAL.30 ����ʹ��
        /// </summary>
        public class ViewBase
        {
            /// <summary>
            /// ��ͼ����
            /// </summary>
            public string ViewName = "";

            #region Open

            /// <summary>
            /// ����ͼ
            /// </summary>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <param name="LimitStart">�ӵڼ�����ʼ��С�� 0 ��ʾ���� </param>
            /// <param name="LimitCount">������������¼��С�� 1 ��ʾ����</param>
            /// <returns></returns>
            public DataTable Open(string FieldList, string Condition, string Order, long LimitStart, long LimitCount)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                string Limit = "";

                if (LimitStart >= 0)
                {
                    Limit = " limit " + LimitStart.ToString();

                    if (LimitCount >= 1)
                    {
                        Limit += ", " + LimitCount.ToString();
                    }
                }

                return Select("select " + (FieldList == "" ? "*" : FieldList) + " from `" + ViewName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)) + Limit);
            }

            /// <summary>
            /// ����ͼ
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <param name="LimitStart">�ӵڼ�����ʼ��С�� 0 ��ʾ���� </param>
            /// <param name="LimitCount">������������¼��С�� 1 ��ʾ����</param>
            /// <returns></returns>
            public DataTable Open(string ConnectionString, string FieldList, string Condition, string Order, long LimitStart, long LimitCount)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                string Limit = "";

                if (LimitStart >= 0)
                {
                    Limit = " limit " + LimitStart.ToString();

                    if (LimitCount >= 1)
                    {
                        Limit += ", " + LimitCount.ToString();
                    }
                }

                return Select(ConnectionString, "select " + (FieldList == "" ? "*" : FieldList) + " from `" + ViewName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)) + Limit);
            }

            /// <summary>
            /// ����ͼ
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <param name="LimitStart">�ӵڼ�����ʼ��С�� 0 ��ʾ���� </param>
            /// <param name="LimitCount">������������¼��С�� 1 ��ʾ����</param>
            /// <returns></returns>
            public DataTable Open(MySqlConnection conn, string FieldList, string Condition, string Order, long LimitStart, long LimitCount)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                string Limit = "";

                if (LimitStart >= 0)
                {
                    Limit = " limit " + LimitStart.ToString();

                    if (LimitCount >= 1)
                    {
                        Limit += ", " + LimitCount.ToString();
                    }
                }

                return Select(conn, "select " + (FieldList == "" ? "*" : FieldList) + " from `" + ViewName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)) + Limit);
            }

            #endregion

            #region GetCount

            /// <summary>
            /// ��ȡ��ͼ��¼��
            /// </summary>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar("select count(*) from `" + ViewName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
            }

            /// <summary>
            /// ��ȡ��ͼ��¼��
            /// </summary>
            /// <param name="ConnectionString"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(string ConnectionString, string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar(ConnectionString, "select count(*) from `" + ViewName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
            }

            /// <summary>
            /// ��ȡ��ͼ��¼��
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(MySqlConnection conn, string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar(conn, "select count(*) from `" + ViewName + "`" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

                if (Result == null)
                {
                    return 0;
                }

                return long.Parse(Result.ToString());
            }

            #endregion
        }

        #endregion
    }
}
