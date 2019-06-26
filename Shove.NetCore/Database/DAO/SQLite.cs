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
        /// <param name="DatabaseFileName"></param>
        /// <returns></returns>
        public static string BuildConnectString(string DatabaseFileName)
        {
            return string.Format("data source={0}", DatabaseFileName);
        }

        /// <summary>
        /// 构建连接串
        /// </summary>
        /// <param name="DatabaseFileName"></param>
        /// <param name="Version"></param>
        /// <returns></returns>
        public static string BuildConnectString(string DatabaseFileName, string Version)
        {
            return string.Format("data source={0};version={1}", DatabaseFileName, Version);
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
                ParametersName.Add(Name.StartsWith("@") ? Name.Substring(1) : Name);
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

        private static void AddParameter(ref SQLiteCommand Cmd, params Parameter[] Params)
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

                SQLiteParameter param = new SQLiteParameter();
                param.ParameterName = Params[i].Name.StartsWith("@") ? Params[i].Name : ("@" + Params[i].Name);
                param.DbType = Params[i].Type;

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

        private static void AddOutputParameter(SQLiteCommand Cmd, ref OutputParameter Outputs)
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
                SQLiteParameter param = (SQLiteParameter)Cmd.Parameters[i];

                if ((param.Direction != ParameterDirection.InputOutput) &&
                    (param.Direction != ParameterDirection.Output))
                {
                    continue;
                }

                Outputs.Add(param.ParameterName, param.Value);
            }
        }

        private static SQLiteParameter GetReturnParameter(SQLiteCommand Cmd)
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
                SQLiteParameter param = (SQLiteParameter)Cmd.Parameters[i];

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
            SQLiteConnection conn = CreateDataConnection<SQLiteConnection>(ConnectionString);

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
        public static int ExecuteNonQuery(SQLiteConnection conn, string CommandText, params Parameter[] Params)
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

            SQLiteCommand Cmd = new SQLiteCommand(CommandText, conn);
            AddParameter(ref Cmd, Params);

            SQLiteTransaction trans;
            try
            {
                trans = (SQLiteTransaction)conn.BeginTransaction();
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
        public static int ExecuteNonQueryNoTranscation(SQLiteConnection conn, string CommandText, params Parameter[] Params)
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

            SQLiteCommand Cmd = new SQLiteCommand(CommandText, conn);
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
            SQLiteConnection conn = CreateDataConnection<SQLiteConnection>(ConnectionString);

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
        public static DataTable Select(SQLiteConnection conn, string CommandText, params Parameter[] Params)
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

            SQLiteDataAdapter da = new SQLiteDataAdapter("", conn);
            SQLiteCommand Cmd = new SQLiteCommand(CommandText, conn);

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
            SQLiteConnection conn = CreateDataConnection<SQLiteConnection>(ConnectionString);

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
        public static object ExecuteScalar(SQLiteConnection conn, string CommandText, params Parameter[] Params)
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

            SQLiteCommand Cmd = new SQLiteCommand(CommandText, conn);
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
            SQLiteConnection conn = CreateDataConnection<SQLiteConnection>(ConnectionString);

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
        public static byte[] BackupDataToZipStream(SQLiteConnection conn)
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

            DataTable dt = Select(conn, "select name from sqlite_master where type = 'table' and name <> 'sqlite_master' and name <> 'sqlite_temp_master' and name <> 'sqlite_sequence' order by name;");
            
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
            SQLiteConnection conn = CreateDataConnection<SQLiteConnection>(ConnectionString);

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
        public static int RestoreDataFromZipStream(SQLiteConnection conn, byte[] DataBuffer)
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
            SQLiteTransaction trans;
            try
            {
                trans = (SQLiteTransaction)conn.BeginTransaction();
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
                    SQLiteCommand Cmd = new SQLiteCommand("delete from [" + dt.TableName + "]", conn);
                    Cmd.Transaction = trans;
                    Cmd.ExecuteNonQuery();
                    Cmd.CommandText = "update sqlite_sequence set seq=0 where name='" + dt.TableName + "'";
                    Cmd.ExecuteNonQuery();

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
            /// <param name="canonicalidentifiername"></param>
            /// <param name="dbtype"></param>
            /// <param name="_readonly"></param>
            public Field(object parent, string name, string canonicalidentifiername, DbType dbtype, bool _readonly)
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
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <returns></returns>
            public DataTable Open(string FieldList, string Condition, string Order)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                return Select("select " + (FieldList == "" ? "*" : FieldList) + " from [" + TableName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
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

                return Select(ConnectionString, "select " + (FieldList == "" ? "*" : FieldList) + " from [" + TableName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            /// <summary>
            /// 打开表
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <returns></returns>
            public DataTable Open(SQLiteConnection conn, string FieldList, string Condition, string Order)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                return Select(conn, "select " + (FieldList == "" ? "*" : FieldList) + " from [" + TableName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
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

                object Result = ExecuteScalar("select count(*) from [" + TableName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

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

                object Result = ExecuteScalar(ConnectionString, "select count(*) from [" + TableName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

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
            public long GetCount(SQLiteConnection conn, string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar(conn, "select count(*) from [" + TableName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

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

                string CommandText = "insert into [" + TableName + "] (" + InsertFieldsList + ") values (" + InsertValuesList + "); select ifnull(last_insert_rowid(), -99999999)";

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

                string CommandText = "insert into [" + TableName + "] (" + InsertFieldsList + ") values (" + InsertValuesList + "); select ifnull(last_insert_rowid(), -99999999)";

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

                string CommandText = "insert into [" + TableName + "] (" + InsertFieldsList + ") values (" + InsertValuesList + "); select ifnull(last_insert_rowid(), -99999999)";

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
            /// 删除表记录
            /// </summary>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long Delete(string Condition)
            {
                Condition = Condition.Trim();

                object objResult = ExecuteScalar("delete from [" + TableName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + "; select ifnull(changes(), -99999999)");

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

                object objResult = ExecuteScalar(ConnectionString, "delete from [" + TableName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + "; select ifnull(changes(), -99999999)");

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
            public long Delete(SQLiteConnection conn, string Condition)
            {
                Condition = Condition.Trim();

                object objResult = ExecuteScalar(conn, "delete from [" + TableName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + "; select ifnull(changes(), -99999999)");

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

                string CommandText = "update [" + TableName + "] set ";
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

                CommandText += "; select ifnull(changes(), -99999999)";

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

                string CommandText = "update [" + TableName + "] set ";
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

                CommandText += "; select ifnull(changes(), -99999999)";

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
            public long Update(SQLiteConnection conn, string Condition)
            {
                if (Fields.Count < 1)
                {
                    return -101;
                }

                Condition = Condition.Trim();

                string CommandText = "update [" + TableName + "] set ";
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

                CommandText += "; select ifnull(changes(), -99999999)";

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
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <returns></returns>
            public DataTable Open(string FieldList, string Condition, string Order)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                return Select("select " + (FieldList == "" ? "*" : FieldList) + " from [" + ViewName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
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

                return Select(ConnectionString, "select " + (FieldList == "" ? "*" : FieldList) + " from [" + ViewName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            /// <summary>
            /// 打开视图
            /// </summary>
            /// <param name="conn"></param>
            /// <param name="FieldList"></param>
            /// <param name="Condition"></param>
            /// <param name="Order"></param>
            /// <returns></returns>
            public DataTable Open(SQLiteConnection conn, string FieldList, string Condition, string Order)
            {
                FieldList = FieldList.Trim();
                Condition = Condition.Trim();
                Order = Order.Trim();

                return Select(conn, "select " + (FieldList == "" ? "*" : FieldList) + " from [" + ViewName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)) + (Order == "" ? "" : " order by " + FilteSqlInfusionForCondition(Order)));
            }

            #endregion

            #region GetCount

            /// <summary>
            /// 获取视图记录数
            /// </summary>
            /// <param name="Condition"></param>
            /// <returns></returns>
            public long GetCount(string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar("select count(*) from [" + ViewName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

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

                object Result = ExecuteScalar(ConnectionString, "select count(*) from [" + ViewName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

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
            public long GetCount(SQLiteConnection conn, string Condition)
            {
                Condition = Condition.Trim();

                object Result = ExecuteScalar(conn, "select count(*) from [" + ViewName + "]" + (Condition == "" ? "" : " where " + FilteSqlInfusionForCondition(Condition)));

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
