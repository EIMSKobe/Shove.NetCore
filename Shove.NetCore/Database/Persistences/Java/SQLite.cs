using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Data;

namespace Shove.Database.Persistences.Java
{
    /// <summary>
    /// SQLite 生成持久化功能
    /// </summary>
    public class SQLite
    {
        private string ConnStr = "";

        string m_Database;
        string m_Password;
        string m_NamespaceName;
        bool m_isUseConnectionStringConfig;
        bool m_isUseConnectionString;
        bool m_isWithTables;
        bool m_isWithViews;
        bool m_isWithProcedures;
        bool m_isWithFunction;

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="database"></param>
        /// <param name="password"></param>
        /// <param name="namespaceName"></param>
        /// <param name="isUseConnectionStringConfig"></param>
        /// <param name="isUseConnectionString"></param>
        /// <param name="isWithTables"></param>
        /// <param name="isWithViews"></param>
        public SQLite(string database, string password, string namespaceName, bool isUseConnectionStringConfig, bool isUseConnectionString, bool isWithTables, bool isWithViews)
        {
            m_Database = database;
            m_Password = password;
            m_NamespaceName = namespaceName.Trim();
            m_isUseConnectionStringConfig = isUseConnectionStringConfig;
            m_isUseConnectionString = isUseConnectionString;
            m_isWithTables = isWithTables;
            m_isWithViews = isWithViews;
            m_isWithProcedures = false;
            m_isWithFunction = false;
        }

        /// <summary>
        /// 开始生成
        /// </summary>
        /// <returns></returns>
        public string Generation()
        {
            if (!m_isWithTables && !m_isWithViews)
            {
                return "Request a Compent from table or view.";
            }

            ConnStr = Database.SQLite.BuildConnectString(m_Database);

            SQLiteConnection conn = DatabaseAccess.CreateDataConnection<SQLiteConnection>(ConnStr);

            if (conn == null)
            {
                return "Database Connect Fail.";
            }
            conn.Close();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("/**");
            sb.AppendLine("* Program Name: Shove.DAL.40 for SQLite");
            sb.AppendLine("* Program Version: 4.0");
            sb.AppendLine("* @author: 3km.shovesoft.shove (zhou changjun)");
            sb.AppendLine("* Release Time: 2012.12.11");
            sb.AppendLine("*");
            sb.AppendLine("* System Request: com.shovesoft.jar, sqlitejdbc.x.xx.jar");
            sb.AppendLine("* All Rights saved.");
            sb.AppendLine("*/");
            sb.AppendLine("");

            sb.AppendLine("package " + (m_NamespaceName == "" ? "database;" : (m_NamespaceName + ".database;")));
            sb.AppendLine("");
            sb.AppendLine("import java.sql.*;");
            sb.AppendLine("");
            sb.AppendLine("import com.shove.data.dao.*;");
            sb.AppendLine("");
            sb.AppendLine("");

            sb.AppendLine("public class Dao {");
            sb.AppendLine("");

            #region Table

            if (m_isWithTables)
            {
                sb.AppendLine("\tpublic class Tables {");
                sb.AppendLine("");

                Tables(ref sb);

                sb.AppendLine("\t}");
                sb.AppendLine("");
            }

            #endregion

            #region Viwes

            if (m_isWithViews)
            {
                sb.AppendLine("\tpublic class Views {");
                sb.AppendLine("");

                Views(ref sb);

                sb.AppendLine("\t}");
                sb.AppendLine("");
            }

            #endregion

            #region Functions

            if (m_isWithFunction)
            {
                sb.AppendLine("\tpublic class Functions {");
                sb.AppendLine("");

                Functions(ref sb);

                sb.AppendLine("\t}");
                sb.AppendLine("");
            }

            #endregion

            #region Procedures

            if (m_isWithProcedures)
            {
                sb.AppendLine("\tpublic class Procedures ");
                sb.AppendLine("");

                Procedures(ref sb);

                sb.AppendLine("\t}");
            }

            #endregion

            sb.AppendLine("}");

            conn.Close();

            return sb.ToString();
        }

        private void Tables(ref StringBuilder sb)
        {
            DataTable dt = Database.SQLite.Select(ConnStr, "select name, sql from sqlite_master where type = 'table' order by name;");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string tableName = dr["name"].ToString();

                if ((tableName.ToLower() == "sqlite_master") || (tableName.ToLower() == "sqlite_temp_master") || (tableName.ToLower() == "sqlite_sequence"))
                {
                    continue;
                }

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(tableName) + " extends Table {");

                IList<string[]> cols = SplitParameters(dr["sql"].ToString());

                if ((cols == null) || (cols.Count < 1))
                {
                    sb.AppendLine("\t\t}");
                    if (i < dt.Rows.Count - 1)
                    {
                        sb.AppendLine("");
                    }

                    continue;
                }

                for (int j = 0; j < cols.Count; j++)
                {
                    string[] t_strs = cols[j];
                    string colName = t_strs[0];

                    sb.AppendLine("\t\t\tpublic Field " + GetCanonicalIdentifier(colName) + " = new Field(this, \"" + GetBracketsedObjectName(colName) + "\", Types." + GetSQLDataType(t_strs[1]) + ", " + ((t_strs[3] == "1") ? "true" : "false") + ");");
                }
                sb.AppendLine("");

                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(tableName) + "() {");
                sb.AppendLine("\t\t\t\tname = \"" + GetBracketsedObjectName(tableName) + "\";");
                sb.AppendLine("\t\t\t}");
                sb.AppendLine("\t\t}");

                if (i < dt.Rows.Count - 1)
                {
                    sb.AppendLine("");
                }
            }
        }

        private void Views(ref StringBuilder sb)
        {
            DataTable dt = Database.SQLite.Select(ConnStr, "select name from sqlite_master where type = 'view' order by name;");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string viewName = dr["name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(viewName) + " extends View {");
                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(viewName) + "() {");
                sb.AppendLine("\t\t\t\tname = \"" + GetBracketsedObjectName(viewName) + "\";");
                sb.AppendLine("\t\t\t}");
                sb.AppendLine("\t\t}");

                if (i < dt.Rows.Count - 1)
                {
                    sb.AppendLine("");
                }
            }
        }

        private void Functions(ref StringBuilder sb)
        {
        }

        private void Procedures(ref StringBuilder sb)
        {
        }

        /// <summary>
        /// 将数据库类型转换为 JDBC.Types.类型
        /// </summary>
        /// <param name="SQLType"></param>
        /// <returns></returns>
        private string GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();
            string result = "VARCHAR";

            switch (SQLType)
            {
                case "smallint":
                    result = "SMALLINT";
                    break;
                case "integer":
                    result = "INTEGER";
                    break;
                case "int":
                    result = "INTEGER";
                    break;
                case "bigint":
                    result = "BIGINT";
                    break;
                case "real":
                    result = "FLOAT";
                    break;
                case "float":
                    result = "FLOAT";
                    break;
                case "double":
                    result = "DOUBLE";
                    break;
                case "decimal":
                    result = "DECIMAL";
                    break;
                case "datetime":
                    result = "TIMESTAMP";
                    break;
                case "timestamp":
                    result = "TIMESTAMP";
                    break;
                case "char":
                    result = "CHAR";
                    break;
                case "varchar":
                    result = "VARCHAR";
                    break;
                case "graphic":
                    result = "VARCHAR";
                    break;
                case "vargraphic":
                    result = "VARCHAR";
                    break;
                case "text":
                    result = "VARCHAR";
                    break;
                case "blob":
                    result = "BLOB";
                    break;
            }

            return result;
        }

        private string GetCanonicalIdentifier(string identifierName)
        {
            identifierName = identifierName.Replace(" ", "_").Replace("[", "_").Replace("]", "_").Replace("\"", "_").Replace("\'", "_").Replace("\t", "");

            if (identifierName.Length > 0)
            {
                if ("0123456789".IndexOf(identifierName[0]) >= 0)
                {
                    identifierName = "_" + identifierName;
                }
            }

            if (identifierName.StartsWith("\"", System.StringComparison.Ordinal) && identifierName.EndsWith("\"", System.StringComparison.Ordinal))
            {
                identifierName = identifierName.Substring(1, identifierName.Length - 2);
            }

            if (identifierName.StartsWith("[", System.StringComparison.Ordinal) && identifierName.EndsWith("]", System.StringComparison.Ordinal))
            {
                identifierName = identifierName.Substring(1, identifierName.Length - 2);
            }

            if ((identifierName == "name") || (identifierName == "fields") || (identifierName == "this") || (identifierName == "super"))
            {
                identifierName = "_" + identifierName;
            }

            return identifierName;
        }

        /// <summary>
        /// 获取增加括号的对象名称
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string GetBracketsedObjectName(string input)
        {
            input = input.Replace("\t", "");

            if (input.StartsWith("\"", System.StringComparison.Ordinal) && input.EndsWith("\"", System.StringComparison.Ordinal))
            {
                input = input.Substring(1, input.Length - 2);
            }

            if (!input.StartsWith("[", System.StringComparison.Ordinal) && !input.EndsWith("]", System.StringComparison.Ordinal))
                return "[" + input + "]";
            else
                return input;
        }

        private string[] FilterLengthDescription(string input)
        {
            input = input.Replace("\r", "").Replace("\n", "").TrimStart(' ').TrimEnd(' ');
            bool AutoIncrement = input.ToUpper().Contains(" AUTOINCREMENT");

            string fieldName = "";
            string fieldType = "";

            if (input.StartsWith("[", System.StringComparison.Ordinal))
            {
                int end = input.LastIndexOf(']');
                fieldName = input.Substring(1, end - 1);
                fieldType = input.Substring(end + 1).TrimStart(' ').Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries)[0];
            }
            else if (input.StartsWith("\"", System.StringComparison.Ordinal))
            {
                int end = input.LastIndexOf('\"');
                fieldName = input.Substring(1, end - 1);
                fieldType = input.Substring(end + 1).TrimStart(' ').Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries)[0];
            }
            else
            {
                int end = input.IndexOf(' ');
                fieldName = input.Substring(0, end);
                fieldType = input.Substring(end + 1).TrimStart(' ').Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries)[0];
            }

            int Len = 0;

            if (fieldType.Contains("("))
            {
                string t = fieldType.Substring(fieldType.IndexOf("(", System.StringComparison.Ordinal));
                t = t.Substring(1, t.Length - 2);

                Len = int.Parse(t);

                fieldType = fieldType.Substring(0, fieldType.IndexOf("(", System.StringComparison.Ordinal));
            }

            return new string[] { fieldName, fieldType, Len.ToString(), (AutoIncrement ? "1" : "0") };
        }

        private IList<string[]> SplitParameters(string input)
        {
            input = input.Substring(input.IndexOf("(", System.StringComparison.Ordinal) + 1);
            input = input.Substring(0, input.LastIndexOf(")", System.StringComparison.Ordinal));

            string[] strs = input.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if ((strs == null) || (strs.Length < 1))
            {
                return null;
            }

            IList<string[]> result = new List<string[]>();

            for (int i = 0; i < strs.Length; i++)
            {
                result.Add(FilterLengthDescription(strs[i]));
            }

            return result;
        }
    }
}
