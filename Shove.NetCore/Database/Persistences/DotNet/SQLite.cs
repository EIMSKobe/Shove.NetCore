using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Data;

namespace Shove.Database.Persistences
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
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Data;");
            sb.AppendLine("using System.Data.SQLite;");
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("");
            sb.AppendLine("using Shove.Database;");
            sb.AppendLine("");

            sb.AppendLine("namespace " + (m_NamespaceName == "" ? "DAL" : (m_NamespaceName + ".DAL")));
            sb.AppendLine("{");
            sb.AppendLine("\t/*");
            sb.AppendLine("\tProgram Name: Shove.DAL.30 for SQLite");
            sb.AppendLine("\tProgram Version: 3.0");
            sb.AppendLine("\tWriter By: 3km.shovesoft.shove (zhou changjun)");
            sb.AppendLine("\tRelease Time: 2010.1.27");
            sb.AppendLine("");
            sb.AppendLine("\tSystem Request: Shove.dll, System.Data.SQLite.DLL, System.Data.SQLite.Linq.dll");
            sb.AppendLine("\tAll Rights saved.");
            sb.AppendLine("\t*/");
            sb.AppendLine("");
            if (m_isUseConnectionStringConfig)
            {
                sb.AppendLine("");
                sb.AppendLine("\t// Please Add a Key in Web.config File's appSetting section, Exemple:");
                sb.AppendLine("\t// <add key=\"ConnectionString\" value=\"data source=d:\\db\\xxx.s3db\" />");
                sb.AppendLine("");
                sb.AppendLine("");
            }

            #region Table

            if (m_isWithTables)
            {
                sb.AppendLine("\tpublic class Tables");

                sb.AppendLine("\t{");

                Tables(ref sb);

                sb.AppendLine("\t}");
                sb.AppendLine("");
            }

            #endregion

            #region Viwes

            if (m_isWithViews)
            {
                sb.AppendLine("\tpublic class Views");
                sb.AppendLine("\t{");

                Views(ref sb);

                sb.AppendLine("\t}");
                sb.AppendLine("");
            }

            #endregion

            // Functions
            if (m_isWithFunction)
            {
                sb.AppendLine("\tpublic class Functions");
                sb.AppendLine("\t{");
                Functions(ref sb);
                sb.AppendLine("\t}");
                sb.AppendLine("");
            }

            // Procedures
            if (m_isWithProcedures)
            {
                sb.AppendLine("\tpublic class Procedures");
                sb.AppendLine("\t{");
                Procedures(ref sb);
                sb.AppendLine("\t}");
            }

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

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(tableName) + " : SQLite.TableBase");
                sb.AppendLine("\t\t{");

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

                    sb.AppendLine("\t\t\tpublic SQLite.Field " + GetCanonicalIdentifier(colName) + ";");
                }
                sb.AppendLine("");

                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(tableName) + "()");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine("\t\t\t\tTableName = \"" + GetBracketsedObjectName(tableName) + "\";");
                sb.AppendLine("");

                for (int j = 0; j < cols.Count; j++)
                {
                    string[] t_strs = cols[j];
                    string colName = t_strs[0];

                    sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(colName) + " = new SQLite.Field(this, \"" + GetBracketsedObjectName(colName) + "\", \"" + GetCanonicalIdentifier(colName) + "\", DbType." + GetSQLDataType(t_strs[1]) + ", " + ((t_strs[3] == "1") ? "true" : "false") + ");");
                }

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

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(viewName) + " : SQLite.ViewBase");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(viewName) + "()");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine("\t\t\t\tViewName = \"" + GetBracketsedObjectName(viewName) + "\";");
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

        private string GetDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();
            string result = "string";

            switch (SQLType)
            {
                case "smallint":
                    result = "int";
                    break;
                case "integer":
                    result = "int";
                    break;
                case "int":
                    result = "int";
                    break;
                case "bigint":
                    result = "long";
                    break;
                case "real":
                    result = "double";
                    break;
                case "float":
                    result = "double";
                    break;
                case "double":
                    result = "double";
                    break;
                case "decimal":
                    result = "Decimal";
                    break;
                case "datetime":
                    result = "DateTime";
                    break;
                case "timestamp":
                    result = "string";
                    break;
                case "char":
                    result = "string";
                    break;
                case "varchar":
                    result = "string";
                    break;
                case "graphic":
                    result = "string";
                    break;
                case "vargraphic":
                    result = "string";
                    break;
                case "text":
                    result = "string";
                    break;
                case "blob":
                    result = "byte[]";
                    break;
            }

            return result;
        }

        private DbType GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();
            DbType result = DbType.String;

            switch (SQLType)
            {
                case "smallint":
                    result = DbType.Int32;
                    break;
                case "integer":
                    result = DbType.Int32;
                    break;
                case "int":
                    result = DbType.Int32;
                    break;
                case "bigint":
                    result = DbType.Int64;
                    break;
                case "real":
                    result = DbType.Double;
                    break;
                case "float":
                    result = DbType.Double;
                    break;
                case "double":
                    result = DbType.Double;
                    break;
                case "decimal":
                    result = DbType.Decimal;
                    break;
                case "datetime":
                    result = DbType.DateTime;
                    break;
                case "timestamp":
                    result = DbType.String;
                    break;
                case "char":
                    result = DbType.String;
                    break;
                case "varchar":
                    result = DbType.String;
                    break;
                case "graphic":
                    result = DbType.String;
                    break;
                case "vargraphic":
                    result = DbType.String;
                    break;
                case "text":
                    result = DbType.String;
                    break;
                case "blob":
                    result = DbType.Binary;
                    break;
            }

            return result;
        }

        private string GetDataTypeForConvert(string type)
        {
            type = type.Trim().ToLower();
            string result = "System.Convert.ToString";

            switch (type)
            {
                case "long":
                    result = "System.Convert.ToInt64";
                    break;
                case "bool":
                    result = "System.Convert.ToBoolean";
                    break;
                case "string":
                    result = "System.Convert.ToString";
                    break;
                case "datetime":
                    result = "System.Convert.ToDateTime";
                    break;
                case "double":
                    result = "System.Convert.ToDouble";
                    break;
                case "int":
                    result = "System.Convert.ToInt32";
                    break;
                case "short":
                    result = "System.Convert.ToInt16";
                    break;
                case "byte[]":
                    result = "(byte[])";
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

            if (identifierName.StartsWith("\"", StringComparison.Ordinal) && identifierName.EndsWith("\"", StringComparison.Ordinal))
            {
                identifierName = identifierName.Substring(1, identifierName.Length - 2);
            }

            if (identifierName.StartsWith("[", StringComparison.Ordinal) && identifierName.EndsWith("]", StringComparison.Ordinal))
            {
                identifierName = identifierName.Substring(1, identifierName.Length - 2);
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

            if (input.StartsWith("\"", StringComparison.Ordinal) && input.EndsWith("\"", StringComparison.Ordinal))
            {
                input = input.Substring(1, input.Length - 2);
            }

            if (input.StartsWith("[", StringComparison.Ordinal) && input.EndsWith("]", StringComparison.Ordinal))
            {
                input = input.Substring(1, input.Length - 2);
            }
            
            return input;
        }

        private string[] FilterLengthDescription(string input)
        {
            input = input.Replace("\r", "").Replace("\n", "").TrimStart(' ').TrimEnd(' ');
            bool AutoIncrement = input.ToUpper().Contains(" AUTOINCREMENT");

            string fieldName = "";
            string fieldType = "";

            if (input.StartsWith("[", StringComparison.Ordinal))
            {
                int end = input.LastIndexOf(']');
                fieldName = input.Substring(1, end - 1);
                fieldType = input.Substring(end + 1).TrimStart(' ').Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries)[0];
            }
            else if (input.StartsWith("\"", StringComparison.Ordinal))
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
                string t = fieldType.Substring(fieldType.IndexOf("(", StringComparison.Ordinal));
                t = t.Substring(1, t.Length - 2);

                Len = int.Parse(t);

                fieldType = fieldType.Substring(0, fieldType.IndexOf("(", StringComparison.Ordinal));
            }

            return new string[] { fieldName, fieldType, Len.ToString(), (AutoIncrement ? "1" : "0") };
        }

        private IList<string[]> SplitParameters(string input)
        {
            input = input.Substring(input.IndexOf("(", StringComparison.Ordinal) + 1);
            input = input.Substring(0, input.LastIndexOf(")", StringComparison.Ordinal));

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
