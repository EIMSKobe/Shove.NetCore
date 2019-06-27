using System;
using System.Data;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Shove.Database
{
    /// <summary>
    /// Shove 的专用的 SQLite 访问组件类
    /// </summary>
    public class SQLite : DatabaseAccess
    {
        #region BuildConnectString

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static string BuildConnectString(string database)
        {
            return string.Format("data source={0}", database);
        }

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="database"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public static string BuildConnectString(string database, string version)
        {
            return string.Format("data source={0};version={1}", database, version);
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
            public DbType Type;
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
            public Parameter(string name, DbType type, int size, ParameterDirection direction, object value)
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

        private static void AddParameter(ref SQLiteCommand cmd, params Parameter[] _params)
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

                SQLiteParameter param = new SQLiteParameter();
                param.ParameterName = _params[i].Name.StartsWith("@", StringComparison.Ordinal) ? _params[i].Name : ("@" + _params[i].Name);
                param.DbType = _params[i].Type;

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

        private static void AddOutputParameter(SQLiteCommand cmd, ref OutputParameter outputs)
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
                SQLiteParameter param = (SQLiteParameter)cmd.Parameters[i];

                if ((param.Direction != ParameterDirection.InputOutput) &&
                    (param.Direction != ParameterDirection.Output))
                {
                    continue;
                }

                outputs.Add(param.ParameterName, param.Value);
            }
        }

        private static SQLiteParameter GetReturnParameter(SQLiteCommand cmd)
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
                SQLiteParameter param = (SQLiteParameter)cmd.Parameters[i];

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
            SQLiteConnection conn = CreateDataConnection<SQLiteConnection>(connectionString);

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

            return result;
        }

        /// <summary>
        /// 执行数据库命令
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(SQLiteConnection conn, string commandText, params Parameter[] _params)
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

            SQLiteCommand cmd = new SQLiteCommand(commandText, conn);
            AddParameter(ref cmd, _params);

            SQLiteTransaction trans;
            try
            {
                trans = (SQLiteTransaction)conn.BeginTransaction();
            }
            catch
            {
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

            return result ? 0 : -1002;
        }

        /// <summary>
        /// 执行数据库命令(不用事务)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="commandText"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static int ExecuteNonQueryNoTranscation(SQLiteConnection conn, string commandText, params Parameter[] _params)
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

            SQLiteCommand cmd = new SQLiteCommand(commandText, conn);
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
            SQLiteConnection conn = CreateDataConnection<SQLiteConnection>(connectionString);

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
        public static DataTable Select(SQLiteConnection conn, string commandText, params Parameter[] _params)
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

            SQLiteDataAdapter da = new SQLiteDataAdapter("", conn);
            SQLiteCommand cmd = new SQLiteCommand(commandText, conn);

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
            SQLiteConnection conn = CreateDataConnection<SQLiteConnection>(connectionString);

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
        public static object ExecuteScalar(SQLiteConnection conn, string commandText, params Parameter[] _params)
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

            SQLiteCommand cmd = new SQLiteCommand(commandText, conn);
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
            SQLiteConnection conn = CreateDataConnection<SQLiteConnection>(connectionString);

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
        public static byte[] BackupDataToZipStream(SQLiteConnection conn)
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

            DataTable dt = Select(conn, "select name from sqlite_master where type = 'table' and name <> 'sqlite_master' and name <> 'sqlite_temp_master' and name <> 'sqlite_sequence' order by name;");
            
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

                DataTable Table = Select(conn, "select * from [" + TableName + "]");

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
            SQLiteConnection conn = CreateDataConnection<SQLiteConnection>(connectionString);

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
        public static int RestoreDataFromZipStream(SQLiteConnection conn, byte[] dataBuffer)
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
            SQLiteTransaction trans;
            try
            {
                trans = (SQLiteTransaction)conn.BeginTransaction();
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
                    SQLiteCommand cmd = new SQLiteCommand("delete from [" + dt.TableName + "]", conn);
                    cmd.Transaction = trans;
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "update sqlite_sequence set seq=0 where name='" + dt.TableName + "'";
                    cmd.ExecuteNonQuery();

                    SQLiteDataAdapter da = new SQLiteDataAdapter("select * from [" + dt.TableName + "]", conn);
                    System.Data.DataTable dtUpdate = new System.Data.DataTable();
                    da.Fill(dtUpdate);

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        dtUpdate.ImportRow(dt.Rows[i]);
                    }

                    SQLiteCommandBuilder cb = new SQLiteCommandBuilder();
                    cb.DataAdapter = da;

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
            public DbType DbType;
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
            public Field(object parent, string name, string canonicalIdentifierName, DbType dbtype, bool _readonly)
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
            /// <returns></returns>
            public DataTable Open(string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select("select " + (fieldList == "" ? "*" : fieldList) + " from [" + TableName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
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

                return Select(connectionString, "select " + (fieldList == "" ? "*" : fieldList) + " from [" + TableName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
            }

            /// <summary>
            /// 打开表
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <returns></returns>
            public DataTable Open(SQLiteConnection conn, string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select(conn, "select " + (fieldList == "" ? "*" : fieldList) + " from [" + TableName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
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

                object result = ExecuteScalar("select count(*) from [" + TableName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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

                object result = ExecuteScalar(connectionString, "select count(*) from [" + TableName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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
            public long GetCount(SQLiteConnection conn, string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar(conn, "select count(*) from [" + TableName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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

                    InsertFieldsList += "[" + Fields[i].Name + "]";
                    InsertValuesList += "@" + Fields[i].CanonicalIdentifierName;

                    Parameters[i] = new Parameter(Fields[i].CanonicalIdentifierName, Fields[i].DbType, 0, ParameterDirection.Input, Fields[i].Value);
                }

                string commandText = "insert into [" + TableName + "] (" + InsertFieldsList + ") values (" + InsertValuesList + "); select ifnull(last_insert_rowid(), -99999999)";

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

                string commandText = "insert into [" + TableName + "] (" + InsertFieldsList + ") values (" + InsertValuesList + "); select ifnull(last_insert_rowid(), -99999999)";

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
            public long Insert(SQLiteConnection conn)
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

                string commandText = "insert into [" + TableName + "] (" + InsertFieldsList + ") values (" + InsertValuesList + "); select ifnull(last_insert_rowid(), -99999999)";

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

                object objResult = ExecuteScalar("delete from [" + TableName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + "; select ifnull(changes(), -99999999)");

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

                object objResult = ExecuteScalar(connectionString, "delete from [" + TableName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + "; select ifnull(changes(), -99999999)");

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
            public long Delete(SQLiteConnection conn, string condition)
            {
                condition = condition.Trim();

                object objResult = ExecuteScalar(conn, "delete from [" + TableName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + "; select ifnull(changes(), -99999999)");

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

                string commandText = "update [" + TableName + "] set ";
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

                commandText += "; select ifnull(changes(), -99999999)";

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

                string commandText = "update [" + TableName + "] set ";
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

                commandText += "; select ifnull(changes(), -99999999)";

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
            public long Update(SQLiteConnection conn, string condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                condition = condition.Trim();

                string commandText = "update [" + TableName + "] set ";
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

                commandText += "; select ifnull(changes(), -99999999)";

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
            /// <returns></returns>
            public DataTable Open(string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select("select " + (fieldList == "" ? "*" : fieldList) + " from [" + ViewName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
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

                return Select(connectionString, "select " + (fieldList == "" ? "*" : fieldList) + " from [" + ViewName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
            }

            /// <summary>
            /// 打开视图
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="fieldList"></param>
            /// <param name="condition"></param>
            /// <param name="order"></param>
            /// <returns></returns>
            public DataTable Open(SQLiteConnection conn, string fieldList, string condition, string order)
            {
                fieldList = fieldList.Trim();
                condition = condition.Trim();
                order = order.Trim();

                return Select(conn, "select " + (fieldList == "" ? "*" : fieldList) + " from [" + ViewName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)) + (order == "" ? "" : " order by " + FilteSqlInfusionForCondition(order)));
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

                object result = ExecuteScalar("select count(*) from [" + ViewName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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

                object result = ExecuteScalar(connectionString, "select count(*) from [" + ViewName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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
            public long GetCount(SQLiteConnection conn, string condition)
            {
                condition = condition.Trim();

                object result = ExecuteScalar(conn, "select count(*) from [" + ViewName + "]" + (condition == "" ? "" : " where " + FilteSqlInfusionForCondition(condition)));

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
