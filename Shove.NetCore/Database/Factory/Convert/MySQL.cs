using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;

using MySql.Data.MySqlClient;

using Shove.DatabaseFactory.Convert.Model;

namespace Shove.DatabaseFactory.Convert
{
    /// <summary>
    /// SQLite 数据库到其他数据库的转换，暂支持 MSSQL, MySQL
    /// </summary>
    public partial class Converter
    {
        /// <summary>
        /// SQLite 转换到 MySQL
        /// </summary>
        /// <param name="SQLite_ConnectionString">源 SQLite 数据库连接串</param>
        /// <param name="MySQL_ConnectionString">目标 MySQL 数据库连接串</param>
        /// <param name="isWithData">是否携带所有的数据进行转换</param>
        /// <param name="description">错误描述</param>
        /// <returns></returns>
        public bool SQLiteToMySQL(string SQLite_ConnectionString, string MySQL_ConnectionString, bool isWithData, ref string description)
        {
            return SQLiteToMySQL(SQLite_ConnectionString, MySQL_ConnectionString, isWithData, false, ref description);
        }

        /// <summary>
        /// SQLite 转换到 MySQL
        /// </summary>
        /// <param name="SQLite_ConnectionString">源 SQLite 数据库连接串</param>
        /// <param name="MySQL_ConnectionString">目标 MySQL 数据库连接串</param>
        /// <param name="isWithData">是否携带所有的数据进行转换</param>
        /// <param name="ignoreViewRelyon">是否忽略视图依赖关系</param>
        /// <param name="description">错误描述</param>
        /// <returns></returns>
        public bool SQLiteToMySQL(string SQLite_ConnectionString, string MySQL_ConnectionString, bool isWithData, bool ignoreViewRelyon, ref string description)
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

            MySqlConnection conn_t = Database.DatabaseAccess.CreateDataConnection<MySqlConnection>(MySQL_ConnectionString);
            if ((conn_t == null) || (conn_t.State != ConnectionState.Open))
            {
                conn_s.Close();
                description = "连接目标数据库发生错误，请检查连接字符串";

                return false;
            }

            #endregion

            MySqlTransaction trans = null;
            MySqlCommand cmd_mysql = new MySqlCommand();
            cmd_mysql.CommandTimeout = 600;
            cmd_mysql.Connection = conn_t;
            StringBuilder sb = new StringBuilder();
            SQLiteCommand cmd_sqlite = new SQLiteCommand();
            cmd_sqlite.Connection = conn_s;
            SQLiteDataReader dr_sqlite = null;

            #region 升迁表、索引

            sb.AppendLine("SET FOREIGN_KEY_CHECKS=0;\r\n");

            for (int i = 0; i < model.Tables.Count; i++)
            {
                Table table = model.Tables[i];

                sb.AppendLine("DROP TABLE IF EXISTS `" + table.Name + "`;");
                sb.Append( "CREATE TABLE `" + table.Name + "` (");

                for (int j = 0; j < table.FieldCount; j++)
                {
                    if (j > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append("`" + table.Fields[j].Name + "` " + MySQL_MergeDbType(table.Fields[j].DbType, table.Fields[j].Length) + (table.Fields[j].IsPRIMARY_KEY ? " PRIMARY KEY" : "") + (table.Fields[j].IsNOT_NULL ? " NOT NULL" : " NULL") + " " + MySQL_MergeDefaultValue(table.Fields[j].DbType, table.Fields[j].DefaultValue));

                    if (table.Fields[j].IsAUTO_INCREMENT)
                    {
                        sb.Append(" AUTO_INCREMENT");
                    }
                }

                if (table.Indexs.Count > 0)
                {
                    foreach (Model.Index index in table.Indexs)
                    {
                        sb.Append(", ");
                        sb.Append("KEY " + index.Name.Replace("[", "`").Replace("]", "`") + " " + index.Body.Replace("[", "`").Replace("]", "`"));
                    }
                }

                if (string.Compare(table.Name, "T_Products", true) == 0)
                {
                    sb.AppendLine(") ENGINE=MyISAM DEFAULT CHARSET=utf8;");
                }
                else
                {
                    sb.AppendLine(") ENGINE=InnoDB DEFAULT CHARSET=utf8;");
                }
            }

            #endregion

            #region 升迁数据

            if (isWithData)
            {
                for (int i = 0; i < model.Tables.Count; i++)
                {
                    Table table = model.Tables[i];

                    cmd_sqlite.CommandText = "select * from " + table.Name;
                    dr_sqlite = cmd_sqlite.ExecuteReader();

                    if (dr_sqlite.HasRows)
                    {
                        sb.AppendLine("\r\nLOCK TABLES `" + table.Name + "` WRITE;");
                        int no = 0;

                        while (dr_sqlite.Read())
                        {
                            sb.Append((no == 0) ? ("INSERT INTO `" + table.Name + "` VALUES ") : ",");
                            no++;

                            sb.Append("(");

                            for (int j = 0; j < table.FieldCount; j++)
                            {
                                sb.Append((j == 0) ? "" : ", ");
                                int quotesType = MySQL_QuotesDbType(table.Fields[j].DbType);
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
                                        if ((table.Name == "T_WXUsers") && (table.Fields[j].Name == "Name"))
                                        {
                                            value = "'" + String.ConvertEncoding(dr_sqlite[j].ToString(), Encoding.Default, Encoding.UTF8).Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\r\n", "\\r\\n") + "'";
                                        }
                                        else
                                        {
                                            value = "'" + dr_sqlite[j].ToString().Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\r\n", "\\r\\n") + "'";
                                            //value = "'" + Regex.Escape(dr_sqlite[j].ToString()) + "'";
                                        }
                                    }
                                }

                                sb.Append(value);
                            }
                            
                            sb.Append(")");
                        }

                        sb.AppendLine(";");
                        sb.AppendLine("UNLOCK TABLES;");
                    }

                    dr_sqlite.Close();
                }
            }

            #endregion

            #region 创建 SQLite 中存在，而 MySQL 中不存在的时间类函数

            sb.AppendLine();
            sb.AppendLine("DROP FUNCTION IF EXISTS `strftime`;");
            //sb.AppendLine("DELIMITER ;;");
            sb.AppendLine("CREATE FUNCTION `strftime`(`format` varchar(100),`timestring` datetime) RETURNS varchar(1000) CHARSET utf8");
            sb.AppendLine("BEGIN");
            sb.AppendLine("	DECLARE result VARCHAR(1000);");
            sb.AppendLine("	DECLARE len INT;");
            sb.AppendLine("	DECLARE i INT;");
            sb.AppendLine("	DECLARE format_start BIT;");
            sb.AppendLine("	DECLARE current_char VARCHAR(1);");
            sb.AppendLine("");
            sb.AppendLine("	SET result = '';");
            sb.AppendLine("	SET format_start = 0;");
            sb.AppendLine("	SET len = LENGTH(format);");
            sb.AppendLine("	SET i = 1;");
            sb.AppendLine("");
            sb.AppendLine("	LOOP1: WHILE i <= len DO");
            sb.AppendLine("		SET current_char = SUBSTR(format, i, 1);");
            sb.AppendLine("");
            sb.AppendLine("		IF format_start = 0 THEN");
            sb.AppendLine("			IF current_char <> '%' THEN");
            sb.AppendLine("				SET result = CONCAT(result, current_char);");
            sb.AppendLine("			ELSE");
            sb.AppendLine("				SET format_start = 1;");
            sb.AppendLine("			END IF;");
            sb.AppendLine("");
            sb.AppendLine("			SET i = i + 1;");
            sb.AppendLine("			ITERATE LOOP1;");
            sb.AppendLine("		END IF;");
            sb.AppendLine("");
            sb.AppendLine("		SET format_start = 0;");
            sb.AppendLine("");
            sb.AppendLine("		IF current_char LIKE BINARY 'Y' THEN");
            sb.AppendLine("			SET result = CONCAT(result, YEAR(timestring));");
            sb.AppendLine("		ELSEIF current_char LIKE BINARY 'm' THEN");
            sb.AppendLine("			SET result = CONCAT(result, MONTH(timestring));");
            sb.AppendLine("		ELSEIF current_char LIKE BINARY 'd' THEN");
            sb.AppendLine("			SET result = CONCAT(result, DAYOFMONTH(timestring));");
            sb.AppendLine("		ELSEIF current_char LIKE BINARY 'H' THEN");
            sb.AppendLine("			SET result = CONCAT(result, HOUR(timestring));");
            sb.AppendLine("		ELSEIF current_char LIKE BINARY 'M' THEN");
            sb.AppendLine("			SET result = CONCAT(result, MINUTE(timestring));");
            sb.AppendLine("		ELSEIF current_char LIKE BINARY 'S' THEN");
            sb.AppendLine("			SET result = CONCAT(result, SECOND(timestring));");
            sb.AppendLine("		ELSEIF current_char LIKE BINARY 'j' THEN");
            sb.AppendLine("			SET result = CONCAT(result, DAYOFYEAR(timestring));");
            sb.AppendLine("		ELSEIF current_char LIKE BINARY 'f' THEN");
            sb.AppendLine("			SET result = CONCAT(result, SECOND(timestring), '.000');");
            sb.AppendLine("		ELSEIF current_char LIKE BINARY 's' THEN");
            sb.AppendLine("			SET result = CONCAT(result, UNIX_TIMESTAMP(now()) - UNIX_TIMESTAMP('1970-01-01 0:0:0'));");
            sb.AppendLine("		ELSEIF current_char LIKE BINARY 'w' THEN");
            sb.AppendLine("			SET result = CONCAT(result, WEEKDAY(timestring) + 1);");
            sb.AppendLine("		ELSEIF current_char LIKE BINARY 'W' THEN");
            sb.AppendLine("			SET result = CONCAT(result, WEEK(timestring));");
            sb.AppendLine("		ELSE");
            sb.AppendLine("			SET result = CONCAT(result, current_char);");
            sb.AppendLine("		END IF;");
            sb.AppendLine("");
            sb.AppendLine("		SET i = i + 1;");
            sb.AppendLine("	END WHILE LOOP1;");
            sb.AppendLine("");
            sb.AppendLine("	RETURN result;");
            sb.AppendLine("END");
            sb.AppendLine(";");
            //sb.AppendLine(";;");
            //sb.AppendLine("DELIMITER ;");
            sb.AppendLine("");

            #endregion

            #region 升迁视图(忽略视图依赖关系的方式)

            if (ignoreViewRelyon && (model.Views.Count > 0))
            {
                sb.AppendLine();

                for (int i = 0; i < model.Views.Count; i++)
                {
                    string ViewName = model.Views[i].Name;
                    sb.AppendLine();

                    sb.AppendLine("DROP VIEW IF EXISTS `" + ViewName + "`;");
                    sb.AppendLine(MySQL_ReplaceViewKeyword(model.Views[i].Body));
                }
            }

            #endregion

            //IO.File.WriteFile("C:\\aaaa\\shovemysql.sql", sb.ToString(), Encoding.UTF8);

            #region 执行命令

            trans = conn_t.BeginTransaction();
            cmd_mysql.Transaction = trans; 
            cmd_mysql.CommandText = sb.ToString();

            try
            {
                cmd_mysql.ExecuteNonQuery();
            }
            catch(Exception e)
            {
                trans.Rollback();
                conn_s.Close();
                conn_t.Close();
                description = "升迁数据库过程中发生了错误：" + e.Message;

                return false;
            }

            #endregion

            #region 升迁视图(第二种方式，需要处理视图依赖关系的方式，与第一种方式互斥)

            if ((!ignoreViewRelyon) && (model.Views.Count > 0))
            {
                IList<View> RemainViews = new List<View>();
                IList<View> RemainViews_2 = new List<View>();

                for (int i = 0; i < model.Views.Count; i++)
                {
                    string ViewName = model.Views[i].Name;
                    string CmdString = "DROP VIEW IF EXISTS `" + ViewName + "`;\r\n";
                    CmdString += MySQL_ReplaceViewKeyword(model.Views[i].Body);

                    cmd_mysql.CommandText = CmdString;

                    try
                    {
                        cmd_mysql.ExecuteNonQuery();
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
                    string CmdString = "DROP VIEW IF EXISTS `" + ViewName + "`;\r\n";
                    CmdString += MySQL_ReplaceViewKeyword(RemainViews[i].Body);

                    cmd_mysql.CommandText = CmdString;

                    try
                    {
                        cmd_mysql.ExecuteNonQuery();
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
                    string CmdString = "DROP VIEW IF EXISTS `" + ViewName + "`;\r\n";
                    CmdString += MySQL_ReplaceViewKeyword(RemainViews_2[i].Body);

                    cmd_mysql.CommandText = CmdString;

                    try
                    {
                        cmd_mysql.ExecuteNonQuery();
                    }
                    catch(Exception e)
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

        private string MySQL_MergeDbType(string dbType, int Length)
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
                    return ((Length > 0) && (Length <= 4000)) ? "VARCHAR(" + Length.ToString() + ")" : "LONGTEXT";
                case "NVARCHAR":
                    return ((Length > 0) && (Length <= 4000)) ? "VARCHAR(" + Length.ToString() + ")" : "LONGTEXT";
                case "TEXT":
                    return "LONGTEXT";
                case "BLOB":
                    return "LONGBLOB";
                default:
                    return "VARCHAR(1000)";
            }
        }

        private string MySQL_MergeDefaultValue(string dbType, string defaultValue)
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

        private int MySQL_QuotesDbType(string dbType)
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

        private Regex regex_random_mysql = new Regex(@"random[\s\t\r\n\v\f]*?[(][\s\t\r\n\v\f]*?[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_datetime_mysql = new Regex(@"datetime[\s\t\r\n\v\f]*?[(][\s\t\r\n\v\f]*?[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_date_mysql = new Regex(@"date[\s\t\r\n\v\f]*?[(][\s\t\r\n\v\f]*?[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_time_mysql = new Regex(@"time[\s\t\r\n\v\f]*?[(][\s\t\r\n\v\f]*?[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private string MySQL_ReplaceViewKeyword(string input)
        {
            input = input.Replace("[", "`").Replace("]", "`").Replace("||", "+");
            input = regex_random_mysql.Replace(input, "floor((rand() - 0.5) * 10000000000000000000)");
            input = regex_datetime_mysql.Replace(input, "now()");
            input = regex_date_mysql.Replace(input, "cast(now() as date)");
            input = regex_time_mysql.Replace(input, "cast(now() as time)");

            return input + ";";
        }
    }
}
