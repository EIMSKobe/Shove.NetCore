using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.IO;


namespace Shove.DatabaseFactory
{
    /// <summary>
    /// 数据工厂基类，此类没有考虑线程安全，不应该作为 static 类型，并在后台多线程中使用。
    /// </summary>
    public class Factory
    {
        /// <summary>
        /// 连接串
        /// </summary>
        protected string ConnectionString;

        /// <summary>
        /// 连接对象
        /// </summary>
        protected DbConnection connection = null;

        /// <summary>
        /// 事务对象
        /// </summary>
        protected DbTransaction transcation = null;

        /// <summary>
        /// 数据库只读标志
        /// </summary>
        protected bool DatabaseReadOnlyState = false;

        /// <summary>
        /// 过滤 Sql 注入，过滤字段值
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FilteSqlInfusionForFieldValue(string input)
        {
            return input;
        }

        /// <summary>
        /// 过滤 Sql 注入，过滤 condition 等 html 编辑器的恶意代码注入
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FilteSqlInfusionForCondition(string input)
        {
            if (true)//[shove] (Shove.Web.Security.InjectionInterceptor.__SYS_SHOVE_FLAG_IsUsed_InjectionInterceptor)
            {
                return input;
            }
            else
            {
                //[shove] return Web.Utility.FilteSqlInfusion(input, false);
            }
        }

        ///// <summary>
        ///// 是否关键字，暂未使用
        ///// </summary>
        ///// <param name="input"></param>
        ///// <returns></returns>
        //public static bool isKeyword(string input)
        //{
        //    if (string.IsNullOrEmpty(input))
        //    {
        //        return false;
        //    }

        //    input = input.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' }).ToLower();

        //    string[] keyword = new string[] { "select", "insert", "update", "delete", "order", "group", "asc", "desc", "from", "by", "where", "limit" };

        //    foreach (string str in keyword)
        //    {
        //        if (str == input)
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        /// <summary>
        /// 初始化，打开数据库连接。(从配置文件中读取连接串)
        /// </summary>
        public Factory() : this(string.Empty)
        {
        }

        /// <summary>
        /// 初始化，打开数据库连接。(直接给定连接串，而不是从配置文件读取)
        /// </summary>
        /// <param name="InitConnectionString"></param>
        public Factory(string InitConnectionString)
        {
            InitConnectionString = InitConnectionString.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' });

            if (string.IsNullOrEmpty(InitConnectionString))
            {
                ConnectionString = System.Configuration.ConfigurationManager.AppSettings["ConnectionString"];
                ConnectionString = ConnectionString.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' });
            }
            else
            {
                ConnectionString = InitConnectionString;
            }

            if (this is SQLite)
            {
                if (Path.IsPathRooted(ConnectionString))
                {
                    ConnectionString = "data source=" + ConnectionString + ";Version=3";
                }
                else
                {
                    if (ConnectionString.Contains("/") || ConnectionString.Contains("\\"))
                    {
                        ConnectionString = "data source=" + System.AppDomain.CurrentDomain.BaseDirectory + ConnectionString + ";Version=3";
                    }
                    else
                    {
                        ConnectionString = "data source=" + System.AppDomain.CurrentDomain.BaseDirectory + "App_Data/" + ConnectionString + ";Version=3";
                    }
                }
            }

            connection = this.CreateDatabaseConnection();


            // 读取数据库只读状态

            string _DatabaseReadOnly = System.Configuration.ConfigurationManager.AppSettings["DatabaseReadOnly"];
            string _DatabaseStateLastModifyDateTime = System.Configuration.ConfigurationManager.AppSettings["DatabaseStateLastModifyDateTime"];
            string _DatabaseReadOnlyTimeout = System.Configuration.ConfigurationManager.AppSettings["DatabaseReadOnlyTimeout"];
            bool DatabaseReadOnly = false;
            DateTime DatabaseStateLastModifyDateTime = DateTime.Now;
            int DatabaseReadOnlyTimeout = 120;
            
            if (!bool.TryParse(_DatabaseReadOnly, out DatabaseReadOnly))
            {
                DatabaseReadOnly = false;
            }

            if (!DateTime.TryParse(_DatabaseStateLastModifyDateTime, out DatabaseStateLastModifyDateTime))
            {
                DatabaseStateLastModifyDateTime = DateTime.Now;
            }

            if (!int.TryParse(_DatabaseReadOnlyTimeout, out DatabaseReadOnlyTimeout))
            {
                DatabaseReadOnlyTimeout = 120;
            }

            if (DatabaseReadOnly)
            {
                TimeSpan ts = DateTime.Now - DatabaseStateLastModifyDateTime;

                if (ts.TotalMinutes < DatabaseReadOnlyTimeout)
                {
                    DatabaseReadOnlyState = true;
                }
            }
        }

        /// <summary>
        /// 析构方法，用于关闭数据库连接
        /// </summary>
        ~Factory()
        {
            Close(false);
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        public void Close()
        {
            Close(true);
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        public void Close(bool releaseConnectionCounter)
        {
            // 关闭一个连接时，计数器回收一个计数。
            if (releaseConnectionCounter && (connection != null) && (connection.State != ConnectionState.Closed))
            {
                FactoryManager.currentConnections--;
            }

            try
            {
                connection.Close();
            }
            catch { }
        }

        #region CreateDatabaseConnection

        /// <summary>
        /// 虚拟的 CreateDatabaseConnection
        /// </summary>
        /// <returns></returns>
        protected virtual DbConnection __CreateDatabaseConnection()
        {
            return null;
        }

        /// <summary>
        /// CreateDatabaseConnection
        /// </summary>
        /// <returns></returns>
        public DbConnection CreateDatabaseConnection()
        {
            DbConnection conn = this.__CreateDatabaseConnection();

            // 如果打开了一个连接，则让连接计数器增加一个计数。在 Close 方法中会反之减掉一个计数。
            if ((conn != null) && (conn.State == ConnectionState.Open))
            {
                FactoryManager.currentConnections++;
            }

            return conn;
        }

        #endregion

        #region Transcation

        /// <summary>
        /// 开始事务
        /// </summary>
        public void BeginTransaction()
        {
            transcation = connection.BeginTransaction();
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void CommitTranscation()
        {
            transcation.Commit();
            transcation = null;
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void RollbackTranscation()
        {
            transcation.Rollback();
            transcation = null;
        }

        #endregion

        #region 字段及字段值列表

        /// <summary>
        /// 字段列表
        /// </summary>
        public class FieldCollect
        {
            IList<string> Fields = new List<string>();

            /// <summary>
            /// 构造
            /// </summary>
            /// <param name="fields"></param>
            public FieldCollect(params string[] fields)
            {
                foreach (string s in fields)
                {
                    Fields.Add(s);
                }
            }

            /// <summary>
            /// 增加字段
            /// </summary>
            /// <param name="field"></param>
            public void Add(string field)
            {
                Fields.Add(field);
            }

            /// <summary>
            /// 增加字段数组
            /// </summary>
            /// <param name="fields"></param>
            public void Add(string[] fields)
            {
                foreach (string s in fields)
                {
                    Fields.Add(s);
                }
            }

            /// <summary>
            /// 字段统计
            /// </summary>
            public int Count
            {
                get
                {
                    return Fields.Count;
                }
            }

            /// <summary>
            /// 字段索引
            /// </summary>
            /// <param name="Index"></param>
            /// <returns></returns>
            public string this[int Index]
            {
                get
                {
                    if ((Index < 0) || (Index >= Count))
                    {
                        return null;
                    }

                    return Fields[Index];
                }
            }
        }

        /// <summary>
        /// 字段值列表
        /// </summary>
        public class FieldValueCollect
        {
            IList<object> Values = new List<object>();

            /// <summary>
            /// 构造
            /// </summary>
            /// <param name="values"></param>
            public FieldValueCollect(params object[] values)
            {
                foreach (object obj in values)
                {
                    Values.Add(obj);
                }
            }

            /// <summary>
            /// 增加字段值
            /// </summary>
            /// <param name="value"></param>
            public void Add(object value)
            {
                Values.Add(value);
            }

            /// <summary>
            /// 增加字段值数组
            /// </summary>
            /// <param name="values"></param>
            public void Add(object[] values)
            {
                foreach (object obj in values)
                {
                    Values.Add(obj);
                }
            }

            /// <summary>
            /// 字段值统计
            /// </summary>
            public int Count
            {
                get
                {
                    return Values.Count;
                }
            }

            /// <summary>
            /// 字段值索引
            /// </summary>
            /// <param name="Index"></param>
            /// <returns></returns>
            public object this[int Index]
            {
                get
                {
                    if ((Index < 0) || (Index >= Count))
                    {
                        return null;
                    }

                    object value = Values[Index];
                    
                    return value;
                }
                set
                {
                    if ((Index < 0) || (Index >= Count))
                    {
                        throw new IndexOutOfRangeException();
                    }

                    Values[Index] = value;
                }
            }
        }

        /// <summary>
        /// 计算列的值
        /// </summary>
        public class FieldValueCalculate
        {
            /// <summary>
            /// 
            /// </summary>
            public string FieldName;
            /// <summary>
            /// 
            /// </summary>
            private string Ormula;

            /// <summary>
            /// 构造
            /// </summary>
            /// <param name="fieldName"></param>
            /// <param name="ormula"></param>
            public FieldValueCalculate(string fieldName, string ormula)
            {
                FieldName = fieldName;
                Ormula = ormula;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public new string ToString()
            {
                return "(" + FilteSqlInfusionForCondition(Ormula) + ")";
            }
        }

        #endregion

        #region Open

        /// <summary>
        /// 打开表或视图
        /// </summary>
        /// <param name="TableOrViewName"></param>
        /// <param name="fieldCollent"></param>
        /// <param name="Condition"></param>
        /// <param name="OrderBy"></param>
        /// <param name="LimitStart"></param>
        /// <param name="LimitCount"></param>
        /// <returns></returns>
        protected virtual DataTable __Open(string TableOrViewName, FieldCollect fieldCollent, string Condition, string OrderBy, int LimitStart, int LimitCount)
        {
            return null;
        }

        /// <summary>
        /// 打开表或视图
        /// </summary>
        /// <param name="TableOrViewName"></param>
        /// <param name="fieldCollent"></param>
        /// <param name="Condition"></param>
        /// <param name="OrderBy"></param>
        /// <param name="LimitStart"></param>
        /// <param name="LimitCount"></param>
        /// <returns></returns>
        public DataTable Open(string TableOrViewName, FieldCollect fieldCollent, string Condition, string OrderBy, int LimitStart, int LimitCount)
        {
            return this.__Open(TableOrViewName, fieldCollent, Condition, OrderBy, LimitStart, LimitCount);
        }

        #endregion

        #region GetRowCount

        /// <summary>
        /// 获取符合条件的记录数
        /// </summary>
        /// <param name="TableOrViewName"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        protected virtual long __GetRowCount(string TableOrViewName, string Condition)
        {
            return 0;
        }

        /// <summary>
        /// 获取符合条件的记录数
        /// </summary>
        /// <param name="TableOrViewName"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        public long GetRowCount(string TableOrViewName, string Condition)
        {
            return this.__GetRowCount(TableOrViewName, Condition);
        }

        #endregion

        #region Insert

        /// <summary>
        /// 插入记录
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="fieldCollent"></param>
        /// <param name="fieldValueCollect"></param>
        /// <returns></returns>
        protected virtual long __Insert(string TableName, FieldCollect fieldCollent, FieldValueCollect fieldValueCollect)
        {
            return 0;
        }

        /// <summary>
        /// 插入记录
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="fieldCollent"></param>
        /// <param name="fieldValueCollect"></param>
        /// <returns></returns>
        public long Insert(string TableName, FieldCollect fieldCollent, FieldValueCollect fieldValueCollect)
        {
            return this.__Insert(TableName, fieldCollent, fieldValueCollect);
        }

        #endregion

        #region Update

        /// <summary>
        /// 更新记录
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="fieldCollent"></param>
        /// <param name="fieldValueCollect"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        protected virtual long __Update(string TableName, FieldCollect fieldCollent, FieldValueCollect fieldValueCollect, string Condition)
        {
            return 0;
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="fieldCollent"></param>
        /// <param name="fieldValueCollect"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        public long Update(string TableName, FieldCollect fieldCollent, FieldValueCollect fieldValueCollect, string Condition)
        {
            return this.__Update(TableName, fieldCollent, fieldValueCollect, Condition);
        }

        #endregion

        #region Delete

        /// <summary>
        /// 删除记录
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        protected virtual long __Delete(string TableName, string Condition)
        {
            return 0;
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        public long Delete(string TableName, string Condition)
        {
            return this.__Delete(TableName, Condition);
        }

        #endregion

        #region Index

        /// <summary>
        /// Create Index
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="IndexName"></param>
        /// <param name="isUnique"></param>
        /// <param name="Body"></param>
        /// <returns></returns>
        protected virtual int __CreateIndex(string TableName, string IndexName, bool isUnique, string Body)
        {
            return 0;
        }

        /// <summary>
        /// Create Index
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="IndexName"></param>
        /// <param name="isUnique"></param>
        /// <param name="Body"></param>
        /// <returns></returns>
        public int CreateIndex(string TableName, string IndexName, bool isUnique, string Body)
        {
            return this.__CreateIndex(TableName, IndexName, isUnique, Body);
        }

        /// <summary>
        /// Drop Index
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="IndexName"></param>
        /// <returns></returns>
        protected virtual int __DropIndex(string TableName, string IndexName)
        {
            return 0;
        }

        /// <summary>
        /// Drop Index
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="IndexName"></param>
        /// <returns></returns>
        public int DropIndex(string TableName, string IndexName)
        {
            return this.__DropIndex(TableName, IndexName);
        }

        #endregion

        ///////////////////////////////////////////////////////////

        #region ExecuteNonQuery

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        protected virtual int __ExecuteNonQuery(string CommandString)
        {
            return 0;
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string CommandString)
        {
            return this.__ExecuteNonQuery(CommandString);
        }

        #endregion

        #region ExecuteScalar

        /// <summary>
        /// 执行读取一行一列
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        protected virtual object __ExecuteScalar(string CommandString)
        {
            return null;
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        public object ExecuteScalar(string CommandString)
        {
            return this.__ExecuteScalar(CommandString);
        }

        #endregion

        #region ExecuteReader

        /// <summary>
        /// Reader
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        protected virtual DbDataReader __ExecuteReader(string CommandString)
        {
            return null;
        }

        /// <summary>
        /// Reader
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        public DbDataReader ExecuteReader(string CommandString)
        {
            return this.__ExecuteReader(CommandString);
        }

        #endregion

        #region ExecuteQuery

        /// <summary>
        /// 执行命令返回结果集
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        protected virtual DataTable __ExecuteQuery(string CommandString)
        {
            return null;
        }

        /// <summary>
        /// 执行命令返回结果集
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        public DataTable ExecuteQuery(string CommandString)
        {
            return this.__ExecuteQuery(CommandString);
        }

        #endregion
    }
}
