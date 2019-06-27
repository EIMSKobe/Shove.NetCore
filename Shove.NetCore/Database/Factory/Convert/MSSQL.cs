using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;

using Shove.DatabaseFactory.Convert.Model;

namespace Shove.DatabaseFactory.Convert
{
    /// <summary>
    /// SQLite 数据库到其他数据库的转换，暂支持 MSSQL, MySQL
    /// </summary>
    public partial class Converter
    {
        /// <summary>
        /// SQLite 转换到 MSSQL
        /// </summary>
        /// <param name="SQLite_ConnectionString">源 SQLite 数据库连接串</param>
        /// <param name="MSSQL_ConnectionString">目标 SQLServer 数据库连接串</param>
        /// <param name="isWithData">是否携带所有的数据进行转换</param>
        /// <param name="description">错误描述</param>
        /// <returns></returns>
        public bool SQLiteToMSSQL(string SQLite_ConnectionString, string MSSQL_ConnectionString, bool isWithData, ref string description)
        {
            return SQLiteToMSSQL(SQLite_ConnectionString, MSSQL_ConnectionString, isWithData, false, ref description);
        }

        /// <summary>
        /// SQLite 转换到 MSSQL
        /// </summary>
        /// <param name="SQLite_ConnectionString">源 SQLite 数据库连接串</param>
        /// <param name="MSSQL_ConnectionString">目标 SQLServer 数据库连接串</param>
        /// <param name="isWithData">是否携带所有的数据进行转换</param>
        /// <param name="ignoreViewRelyon">是否忽略视图依赖关系</param>
        /// <param name="description">错误描述</param>
        /// <returns></returns>
        public bool SQLiteToMSSQL(string SQLite_ConnectionString, string MSSQL_ConnectionString, bool isWithData, bool ignoreViewRelyon, ref string description)
        {
            description = "";

            #region 连接数据库 & 读取SQLite 结构到 Model

            if (Path.IsPathRooted(SQLite_ConnectionString))
            {
                SQLite_ConnectionString = "data source=" + SQLite_ConnectionString + ";Version=3";
            }

            SQLiteConnection conn_s = Database.DatabaseAccess.CreateDataConnection<SQLiteConnection>(SQLite_ConnectionString);
            if ((conn_s == null) || (conn_s.State != ConnectionState.Open))
            {
                description = "连接源数据库发生错误，请检查网站源数据库文件(基本数据库文件)";

                return false;
            }

            Model.Database model = SQLiteToModel(conn_s, ignoreViewRelyon);

            if (model == null)
            {
                conn_s.Close();
                description = "从原数据中读取表、视图结构发生错误";

                return false;
            }

            SqlConnection conn_t = Database.DatabaseAccess.CreateDataConnection<SqlConnection>(MSSQL_ConnectionString);
            if ((conn_t == null) || (conn_t.State != ConnectionState.Open))
            {
                conn_s.Close();
                description = "连接目标数据库发生错误，请检查连接字符串";

                return false;
            }

            #endregion

            SqlTransaction trans = null;
            SqlCommand cmd_mssql = new SqlCommand();
            cmd_mssql.CommandTimeout = 600;
            cmd_mssql.Connection = conn_t;
            StringBuilder sb = new StringBuilder();
            SQLiteCommand cmd_sqlite = new SQLiteCommand();
            cmd_sqlite.Connection = conn_s;
            SQLiteDataReader dr_sqlite = null;

            #region 升迁表、索引

            for (int i = 0; i < model.Tables.Count; i++)
            {
                Table table = model.Tables[i];

                sb.AppendLine("IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'" + table.Name + "') AND type in (N'U')) DROP TABLE " + table.Name + ";");
                sb.Append("CREATE TABLE " + table.Name + " (");

                for (int j = 0; j < table.FieldCount; j++)
                {
                    if (j > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append("[" + table.Fields[j].Name + "] " + MSSQL_MergeDbType(table.Fields[j].DbType, table.Fields[j].Length) + (table.Fields[j].IsAUTO_INCREMENT ? " IDENTITY(1,1)" : "") + (table.Fields[j].IsPRIMARY_KEY ? " PRIMARY KEY" : "") + (table.Fields[j].IsNOT_NULL ? " NOT NULL" : " NULL") + " " + MSSQL_MergeDefaultValue(table.Fields[j].DbType, table.Fields[j].DefaultValue));
                }

                sb.AppendLine(");");

                foreach (Model.Index index in table.Indexs)
                {
                    sb.AppendLine("CREATE INDEX " + index.Name + " on " + table.Name + " " + index.Body + ";");
                }

                sb.AppendLine();
            }

            #endregion

            #region 升迁数据

            if (isWithData)
            {
                for (int i = 0; i < model.Tables.Count; i++)
                {
                    // 为 Varchar(Max) 设置属性
                    //if (CmdString.ToUpper().Contains("(MAX)"))
                    //{
                    //    Cmd.CommandText = "exec sp_tableoption [" + table.Name + "], 'large value types out of row', 'on'";
                    //    Cmd.ExecuteNonQuery();
                    //}
                    Table table = model.Tables[i];

                    cmd_sqlite.CommandText = "select * from " + table.Name;
                    dr_sqlite = cmd_sqlite.ExecuteReader();

                    if (dr_sqlite.HasRows)
                    {
                        bool HasIDENTITY = MSSQL_GetHasIDENTITY(table);

                        if (HasIDENTITY)
                        {
                            sb.AppendLine("SET IDENTITY_INSERT [" + table.Name + "] ON;");
                        }

                        while (dr_sqlite.Read())
                        {
                            string ColumnList = "INSERT INTO [" + table.Name + "] (";
                            string ValueList = ") VALUES (";

                            for (int j = 0; j < table.FieldCount; j++)
                            {
                                ColumnList += ((j == 0) ? "" : ", ");
                                ValueList += ((j == 0) ? "" : ", ");

                                int quotesType = MSSQL_QuotesDbType(table.Fields[j].DbType);
                                string value = "";
                                bool isDBNull = false;

                                // 此 try 是防止 System.Convert.IsDBNull(dr_sqlite[j]) 这句异常，当 SQLite 中插入了异常数据时，这句会异常，很难排查。
                                try
                                {
                                    isDBNull = System.Convert.IsDBNull(dr_sqlite[j]);
                                }
                                catch
                                {
                                    isDBNull = true;
                                }

                                if (isDBNull)
                                {
                                    value = "NULL";
                                }
                                else
                                {
                                    if (quotesType == 0)
                                    {
                                        value = Shove.Convert.StrToDouble(dr_sqlite[j].ToString(), 0).ToString();
                                    }
                                    else if (quotesType == 1)
                                    {
                                        value = Shove.Convert.StrToBool(dr_sqlite[j].ToString(), false) ? "1" : "0";
                                    }
                                    else
                                    {
                                        value = "'" + dr_sqlite[j].ToString().Replace("'", "''").Replace(((char)0xA0).ToString(), " ") + "'";
                                    }
                                }

                                ColumnList += "[" + table.Fields[j].Name + "]";
                                ValueList += value;
                            }
                            
                            sb.AppendLine(ColumnList + ValueList + ");");
                        }

                        if (HasIDENTITY)
                        {
                            sb.AppendLine("SET IDENTITY_INSERT [" + table.Name + "] OFF;");
                        }
                        
                        sb.AppendLine();
                    }

                    dr_sqlite.Close();
                }

                sb.AppendLine();
            }

            #endregion

            //IO.File.WriteFile("C:\\aaaa\\shovemssql_1.sql", sb.ToString(), Encoding.UTF8);

            #region 执行第一次命令

            trans = conn_t.BeginTransaction();
            cmd_mssql.Transaction = trans;
            cmd_mssql.CommandText = sb.ToString();

            try
            {
                cmd_mssql.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                trans.Rollback();
                conn_s.Close();
                conn_t.Close();
                description = "升迁数据库过程中发生了错误(1)：" + e.Message;

                return false;
            }

            trans.Commit();

            #endregion

            sb = new StringBuilder();

            #region 创建 SQLite 中存在，而 MSSQL 中不存在的时间类函数

            if (!Database.MSSQL.ExecuteSQLScript(MSSQL_ConnectionString, Properties.Resources.SQLiteToMSSQL_VIEW))
            {
                conn_s.Close();
                conn_t.Close();
                description = "升迁数据库过程中发生了错误：创建模拟 SQLite 时间类视图遇到错误。";

                return false;
            }

            #endregion
            
            #region 升迁视图(忽略视图依赖关系的方式)

            if (ignoreViewRelyon && (model.Views.Count > 0))
            {
                for (int i = 0; i < model.Views.Count; i++)
                {
                    string ViewName = model.Views[i].Name;
                    sb.AppendLine();

                    sb.AppendLine("IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[" + ViewName + "]')) DROP VIEW [" + ViewName + "]\r\nGO");
                    sb.AppendLine(MSSQL_ReplaceViewKeyword(model.Views[i].Body) + "\r\nGO");
                }

                //IO.File.WriteFile("C:\\aaaa\\shovemssql_2.sql", sb.ToString(), Encoding.UTF8);

                #region 执行第二次命令

                if (!Database.MSSQL.ExecuteSQLScript(MSSQL_ConnectionString, sb.ToString()))
                {
                    conn_s.Close();
                    conn_t.Close();
                    description = "升迁数据库过程中发生了错误(2)：以忽略依赖关系创建视图遇到错误。";

                    return false;
                }

                #endregion
            }

            #endregion

            #region 升迁视图(第二种方式，需要处理视图依赖关系的方式，与第一种方式互斥)

            trans = conn_t.BeginTransaction();
            cmd_mssql.Transaction = trans;

            if ((!ignoreViewRelyon) && (model.Views.Count > 0))
            {
                IList<View> RemainViews = new List<View>();
                IList<View> RemainViews_2 = new List<View>();

                for (int i = 0; i < model.Views.Count; i++)
                {
                    string ViewName = model.Views[i].Name;
                    cmd_mssql.CommandText = "IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[" + ViewName + "]')) BEGIN DROP VIEW [" + ViewName + "] END";

                    try
                    {
                        cmd_mssql.ExecuteNonQuery();
                        cmd_mssql.CommandText = MSSQL_ReplaceViewKeyword(model.Views[i].Body);
                        cmd_mssql.ExecuteNonQuery();
                    }
                    catch
                    {
                        // 记录下错误的视图，第一轮结束后，再创建一次。因为有视图嵌套的情况
                        RemainViews.Add(model.Views[i]);
                    }
                }

                for (int i = 0; i < RemainViews.Count; i++)
                {
                    string ViewName = RemainViews[i].Name;
                    cmd_mssql.CommandText = "IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[" + ViewName + "]')) BEGIN DROP VIEW [" + ViewName + "] END";

                    try
                    {
                        cmd_mssql.ExecuteNonQuery();
                        cmd_mssql.CommandText = MSSQL_ReplaceViewKeyword(RemainViews[i].Body);
                        cmd_mssql.ExecuteNonQuery();
                    }
                    catch
                    {
                        // 记录下错误的视图，第二轮结束后，再创建一次。因为有视图嵌套的情况
                        RemainViews_2.Add(RemainViews[i]);
                    }
                }

                for (int i = 0; i < RemainViews_2.Count; i++)
                {
                    string ViewName = RemainViews_2[i].Name;
                    cmd_mssql.CommandText = "IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[" + ViewName + "]')) BEGIN DROP VIEW [" + ViewName + "] END";

                    try
                    {
                        cmd_mssql.ExecuteNonQuery();
                        cmd_mssql.CommandText = MSSQL_ReplaceViewKeyword(RemainViews_2[i].Body);
                        cmd_mssql.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        conn_s.Close();
                        conn_t.Close();
                        description = "更新视图“" + ViewName + "”发生错误：" + e.Message;

                        return false;
                    }
                }
            }

            #endregion

            trans.Commit();
            conn_s.Close();
            conn_t.Close();

            return true;
        }

        private string MSSQL_MergeDbType(string dbType, int length)
        {
            dbType = dbType.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' }).ToUpper();

            switch (dbType)
            {
                case "INT":
                    return "INT";
                case "INTEGER":
                    return "INT";
                case "LONG":
                    return "BIGINT";
                case "FLOAT":
                    return "FLOAT";
                case "REAL":
                    return "REAL";
                case "NUMERIC":
                    return "FLOAT";
                case "BOOL":
                    return "BIT";
                case "BOOLEAN":
                    return "BIT";
                case "BIT":
                    return "BIT";
                case "DATE":
                    return "DATE";
                case "DATETIME":
                    return "DATETIME";
                case "TIMESTAMP":
                    return "DATETIME";
                case "VARCHAR":
                    return (length > 0) ? "VARCHAR(" + (length >= 2000 ? "MAX" : length.ToString()) + ")" : "VARCHAR(MAX)";
                case "NVARCHAR":
                    return (length > 0) ? "NVARCHAR(" + (length >= 2000 ? "MAX" : length.ToString()) + ")" : "NVARCHAR(MAX)";
                case "TEXT":
                    return "NVARCHAR(MAX)";
                case "BLOB":
                    return "BINARY";
                default:
                    return "NVARCHAR(MAX)";
            }
        }

        private string MSSQL_MergeDefaultValue(string dbType, string defaultValue)
        {
            string t_default = defaultValue.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' }).ToUpper();

            if (string.IsNullOrEmpty(defaultValue) || string.IsNullOrEmpty(t_default))
            {
                return "";
            }

            dbType = dbType.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' }).ToUpper();

            if ((t_default == "NULL") || (t_default == "\"NULL\"") || (t_default == "\'NULL\'"))
            {
                if ((dbType == "TEXT") || (dbType == "LONGTEXT"))
                {
                    return "";
                }
                else
                {
                    return "DEFAULT NULL";
                }
            }

            string result = "DEFAULT ";

            if ((dbType == "INT") || (dbType == "INTEGER") || (dbType == "LONG") || (dbType == "FLOAT") || (dbType == "REAL") || (dbType == "NUMERIC") || (dbType == "BOOL") || (dbType == "BOOLEAN") || (dbType == "BIT"))
            {
                if ((defaultValue.StartsWith("\"", StringComparison.Ordinal) && defaultValue.EndsWith("\"", StringComparison.Ordinal)) || (defaultValue.StartsWith("\'", StringComparison.Ordinal) && defaultValue.EndsWith("\'", StringComparison.Ordinal)))
                {
                    result += defaultValue.Substring(1, defaultValue.Length - 2);
                }
                else
                {
                    result += defaultValue;
                }
            }
            else
            {
                if (!((defaultValue.StartsWith("\"", StringComparison.Ordinal) && defaultValue.EndsWith("\"", StringComparison.Ordinal)) || (defaultValue.StartsWith("\'", StringComparison.Ordinal) && defaultValue.EndsWith("\'", StringComparison.Ordinal))))
                {
                    result += "'" + defaultValue + "'";
                }
                else
                {
                    result += defaultValue;
                }
            }

            return result;
        }

        private int MSSQL_QuotesDbType(string dbType)
        {
            dbType = dbType.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' }).ToUpper();

            switch (dbType)
            {
                case "INT":
                    return 0;
                case "INTEGER":
                    return 0;
                case "LONG":
                    return 0;
                case "FLOAT":
                    return 0;
                case "REAL":
                    return 0;
                case "NUMERIC":
                    return 0;
                case "BOOL":
                    return 1;
                case "BOOLEAN":
                    return 1;
                case "BIT":
                    return 1;
                default:
                    return 2;
            }
        }

        private Regex regex_random_mssql = new Regex(@"\brandom[\s\t\r\n\v\f]*?[(][\s\t\r\n\v\f]*?[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_date_mssql = new Regex(@"\bdate[\s\t\r\n\v\f]*?[(][\s\t\r\n\v\f]*?[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_datetime_mssql = new Regex(@"\bdatetime[\s\t\r\n\v\f]*?[(][\s\t\r\n\v\f]*?[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_time_mssql = new Regex(@"\btime[\s\t\r\n\v\f]*?[(][\s\t\r\n\v\f]*?[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_strftime_mssql = new Regex(@"\bstrftime[\s\t\r\n\v\f]*?[(]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_ifnull_mssql = new Regex(@"\bifnull[\s\t\r\n\v\f]*?[(]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_length_mssql = new Regex(@"\blength[\s\t\r\n\v\f]*?[(]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_substr_mssql = new Regex(@"\bsubstr[\s\t\r\n\v\f]*?[(]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_limit_mssql = new Regex(@"\bselect\b((?!select )[\S\s])+?\blimit\b[\s\t\r\n\v\f]*?0[\s\t\r\n\v\f]*?[,][\s\t\r\n\v\f]*?[\d]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_limit_mssql_2 = new Regex(@"\bselect\b((?!select )[\S\s])+?\bselect top\b [\S\s]+?\blimit\b[\s\t\r\n\v\f]*?0[\s\t\r\n\v\f]*?[,][\s\t\r\n\v\f]*?[\d]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private string MSSQL_ReplaceViewKeyword(string input)
        {
            input = input.Replace("||", "+");
            input = regex_random_mssql.Replace(input, "floor((rand() - 0.5) * 10000000000000000000)");
            input = regex_date_mssql.Replace(input, "dbo.date()");
            input = regex_datetime_mssql.Replace(input, "getdate()");
            input = regex_time_mssql.Replace(input, "dbo.time()");
            input = regex_strftime_mssql.Replace(input, "dbo.strftime(");
            input = regex_ifnull_mssql.Replace(input, "isnull(");
            input = regex_length_mssql.Replace(input, "len(");
            input = regex_substr_mssql.Replace(input, "substring(");

            MatchEvaluator evaluator = new MatchEvaluator(MSSQL_MatchEvaluator_FilteLimit);
            input = regex_limit_mssql.Replace(input, evaluator);

            int count = 0;
            while ((count++ < 32) && (input.IndexOf(" limit ", StringComparison.Ordinal) >= 0))
            {
                input = regex_limit_mssql_2.Replace(input, evaluator);
            }

            return input;
        }

        private string MSSQL_MatchEvaluator_FilteLimit(Match match)
        {
            string m = match.Value;

            int pos = m.LastIndexOf(",", StringComparison.Ordinal);
            int N = int.Parse(m.Substring(pos + 1));
            pos = m.IndexOf(" limit ", StringComparison.OrdinalIgnoreCase);

            return "SELECT TOP " + N + " " + m.Substring(7, pos - 7) + " ";
        }

        private bool MSSQL_GetHasIDENTITY(Table table)
        {
            foreach (Field field in table.Fields)
            {
                if (field.IsAUTO_INCREMENT)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
