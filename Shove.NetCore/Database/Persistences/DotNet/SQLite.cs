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

        string m_DatabaseName;
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
        /// <param name="DatabaseName"></param>
        /// <param name="Password"></param>
        /// <param name="NamespaceName"></param>
        /// <param name="isUseConnectionStringConfig"></param>
        /// <param name="isUseConnectionString"></param>
        /// <param name="isWithTables"></param>
        /// <param name="isWithViews"></param>
        public SQLite(string DatabaseName, string Password, string NamespaceName, bool isUseConnectionStringConfig, bool isUseConnectionString, bool isWithTables, bool isWithViews)
        {
            m_DatabaseName = DatabaseName;
            m_Password = Password;
            m_NamespaceName = NamespaceName.Trim();
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

            ConnStr = Shove.Database.SQLite.BuildConnectString(m_DatabaseName);

            SQLiteConnection conn = Shove.Database.SQLite.CreateDataConnection<SQLiteConnection>(ConnStr);

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
            DataTable dt = Shove.Database.SQLite.Select(ConnStr, "select name, sql from sqlite_master where type = 'table' order by name;");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string TableName = dr["name"].ToString();

                if ((TableName.ToLower() == "sqlite_master") || (TableName.ToLower() == "sqlite_temp_master") || (TableName.ToLower() == "sqlite_sequence"))
                {
                    continue;
                }

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(TableName) + " : SQLite.TableBase");
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
                    string ColName = t_strs[0];

                    sb.AppendLine("\t\t\tpublic SQLite.Field " + GetCanonicalIdentifier(ColName) + ";");
                }
                sb.AppendLine("");

                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(TableName) + "()");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine("\t\t\t\tTableName = \"" + GetBracketsedObjectName(TableName) + "\";");
                sb.AppendLine("");

                for (int j = 0; j < cols.Count; j++)
                {
                    string[] t_strs = cols[j];
                    string ColName = t_strs[0];

                    sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(ColName) + " = new SQLite.Field(this, \"" + GetBracketsedObjectName(ColName) + "\", \"" + GetCanonicalIdentifier(ColName) + "\", DbType." + GetSQLDataType(t_strs[1]) + ", " + ((t_strs[3] == "1") ? "true" : "false") + ");");
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
            DataTable dt = Shove.Database.SQLite.Select(ConnStr, "select name from sqlite_master where type = 'view' order by name;");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string ViewName = dr["name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(ViewName) + " : SQLite.ViewBase");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(ViewName) + "()");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine("\t\t\t\tViewName = \"" + GetBracketsedObjectName(ViewName) + "\";");
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
            string Result = "string";

            switch (SQLType)
            {
                case "smallint":
                    Result = "int";
                    break;
                case "integer":
                    Result = "int";
                    break;
                case "int":
                    Result = "int";
                    break;
                case "bigint":
                    Result = "long";
                    break;
                case "real":
                    Result = "double";
                    break;
                case "float":
                    Result = "double";
                    break;
                case "double":
                    Result = "double";
                    break;
                case "decimal":
                    Result = "Decimal";
                    break;
                case "datetime":
                    Result = "DateTime";
                    break;
                case "timestamp":
                    Result = "string";
                    break;
                case "char":
                    Result = "string";
                    break;
                case "varchar":
                    Result = "string";
                    break;
                case "graphic":
                    Result = "string";
                    break;
                case "vargraphic":
                    Result = "string";
                    break;
                case "text":
                    Result = "string";
                    break;
                case "blob":
                    Result = "byte[]";
                    break;
            }

            return Result;
        }

        private DbType GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();
            DbType Result = DbType.String;

            switch (SQLType)
            {
                case "smallint":
                    Result = DbType.Int32;
                    break;
                case "integer":
                    Result = DbType.Int32;
                    break;
                case "int":
                    Result = DbType.Int32;
                    break;
                case "bigint":
                    Result = DbType.Int64;
                    break;
                case "real":
                    Result = DbType.Double;
                    break;
                case "float":
                    Result = DbType.Double;
                    break;
                case "double":
                    Result = DbType.Double;
                    break;
                case "decimal":
                    Result = DbType.Decimal;
                    break;
                case "datetime":
                    Result = DbType.DateTime;
                    break;
                case "timestamp":
                    Result = DbType.String;
                    break;
                case "char":
                    Result = DbType.String;
                    break;
                case "varchar":
                    Result = DbType.String;
                    break;
                case "graphic":
                    Result = DbType.String;
                    break;
                case "vargraphic":
                    Result = DbType.String;
                    break;
                case "text":
                    Result = DbType.String;
                    break;
                case "blob":
                    Result = DbType.Binary;
                    break;
            }

            return Result;
        }

        private string GetDataTypeForConvert(string Type)
        {
            Type = Type.Trim().ToLower();
            string Result = "System.Convert.ToString";

            switch (Type)
            {
                case "long":
                    Result = "System.Convert.ToInt64";
                    break;
                case "bool":
                    Result = "System.Convert.ToBoolean";
                    break;
                case "string":
                    Result = "System.Convert.ToString";
                    break;
                case "datetime":
                    Result = "System.Convert.ToDateTime";
                    break;
                case "double":
                    Result = "System.Convert.ToDouble";
                    break;
                case "int":
                    Result = "System.Convert.ToInt32";
                    break;
                case "short":
                    Result = "System.Convert.ToInt16";
                    break;
                case "byte[]":
                    Result = "(byte[])";
                    break;
            }

            return Result;
        }

        private string GetCanonicalIdentifier(string IdentifierName)
        {
            IdentifierName = IdentifierName.Replace(" ", "_").Replace("[", "_").Replace("]", "_").Replace("\"", "_").Replace("\'", "_").Replace("\t", "");

            if (IdentifierName.Length > 0)
            {
                if ("0123456789".IndexOf(IdentifierName[0]) >= 0)
                {
                    IdentifierName = "_" + IdentifierName;
                }
            }

            if (IdentifierName.StartsWith("\"") && IdentifierName.EndsWith("\""))
            {
                IdentifierName = IdentifierName.Substring(1, IdentifierName.Length - 2);
            }

            if (IdentifierName.StartsWith("[") && IdentifierName.EndsWith("]"))
            {
                IdentifierName = IdentifierName.Substring(1, IdentifierName.Length - 2);
            }

            return IdentifierName;
        }

        /// <summary>
        /// 获取增加括号的对象名称
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string GetBracketsedObjectName(string input)
        {
            input = input.Replace("\t", "");

            if (input.StartsWith("\"") && input.EndsWith("\""))
            {
                input = input.Substring(1, input.Length - 2);
            }

            if (input.StartsWith("[") && input.EndsWith("]"))
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

            if (input.StartsWith("["))
            {
                int end = input.LastIndexOf(']');
                fieldName = input.Substring(1, end - 1);
                fieldType = input.Substring(end + 1).TrimStart(' ').Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries)[0];
            }
            else if (input.StartsWith("\""))
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
                string t = fieldType.Substring(fieldType.IndexOf("("));
                t = t.Substring(1, t.Length - 2);

                Len = int.Parse(t);

                fieldType = fieldType.Substring(0, fieldType.IndexOf("("));
            }

            return new string[] { fieldName, fieldType, Len.ToString(), (AutoIncrement ? "1" : "0") };
        }

        private IList<string[]> SplitParameters(string input)
        {
            input = input.Substring(input.IndexOf("(") + 1);
            input = input.Substring(0, input.LastIndexOf(")"));

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
