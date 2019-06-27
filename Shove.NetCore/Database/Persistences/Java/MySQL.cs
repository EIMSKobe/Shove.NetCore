using System;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace Shove.Database.Persistences.Java
{
    /// <summary>
    /// MySQL 持久化
    /// </summary>
    public class MySQL
    {
        private string ConnStr = "";

        string m_Server;
        string m_Database;
        string m_User;
        string m_Password;
        string m_Port;
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
        /// <param name="server"></param>
        /// <param name="database"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="Port"></param>
        /// <param name="namespaceName"></param>
        /// <param name="isUseConnectionStringConfig"></param>
        /// <param name="isUseConnectionString"></param>
        /// <param name="isWithTables"></param>
        /// <param name="isWithViews"></param>
        /// <param name="isWithProcedures"></param>
        /// <param name="isWithFunction"></param>
        public MySQL(string server, string database, string user, string password, string Port, string namespaceName, bool isUseConnectionStringConfig, bool isUseConnectionString, bool isWithTables, bool isWithViews, bool isWithProcedures, bool isWithFunction)
        {
            m_Server = server;
            m_Database = database;
            m_User = user;
            m_Password = password;
            m_Port = Port;
            m_NamespaceName = namespaceName.Trim();
            m_isUseConnectionStringConfig = isUseConnectionStringConfig;
            m_isUseConnectionString = isUseConnectionString;
            m_isWithTables = isWithTables;
            m_isWithViews = isWithViews;
            m_isWithProcedures = isWithProcedures;
            m_isWithFunction = isWithFunction;
        }

        /// <summary>
        /// 开始生成
        /// </summary>
        /// <returns></returns>
        public string Generation()
        {
            if (!m_isWithTables && !m_isWithViews && !m_isWithProcedures && !m_isWithFunction)
            {
                return "Request a Compent from table, view, procedure or function.";
            }

            ConnStr = Database.MySQL.BuildConnectString(m_Server, m_User, m_Password, m_Database, m_Port);

            MySqlConnection conn = DatabaseAccess.CreateDataConnection<MySqlConnection>(ConnStr);

            if (conn == null)
            {
                return "Database Connect Fail.";
            }
            conn.Close();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("/**");
            sb.AppendLine("* Program Name: Shove.DAL.40 for MySQL");
            sb.AppendLine("* Program Version: 4.0");
            sb.AppendLine("* @author: 3km.shovesoft.shove (zhou changjun)");
            sb.AppendLine("* Release Time: 2012.12.11");
            sb.AppendLine("*");
            sb.AppendLine("* System Request: com.shovesoft.jar, mysql-connector-x.xx.jar");
            sb.AppendLine("* All Rights saved.");
            sb.AppendLine("*/");
            sb.AppendLine("");

            sb.AppendLine("package " + (m_NamespaceName == "" ? "database;" : (m_NamespaceName + ".database;")));
            sb.AppendLine("");
            sb.AppendLine("import java.util.*;");
            sb.AppendLine("import java.sql.*;");
            sb.AppendLine("import java.math.*;");
            sb.AppendLine("");
            sb.AppendLine("import com.shove.data.*;");
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
                sb.AppendLine("\tpublic static class Functions {");
                sb.AppendLine("");

                Functions(ref sb);

                sb.AppendLine("\t}");
                sb.AppendLine("");
            }

            #endregion

            #region Procedures

            if (m_isWithProcedures)
            {
                sb.AppendLine("\tpublic static class Procedures {");
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
            DataTable dt = Database.MySQL.Select(ConnStr, "select table_name from information_schema.tables where table_schema = '" + m_Database + "' and table_type = 'BASE TABLE' order by table_name;");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string tableName = dr["table_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(tableName) + " extends Table {");

                DataTable dt_col = Database.MySQL.Select(ConnStr, "select column_name, data_type, character_maximum_length, column_type, extra from information_schema.columns where table_schema = '" + m_Database + "' and table_name = '" + tableName + "' order by ordinal_position;");

                if ((dt_col == null) || (dt_col.Rows.Count < 1))
                {
                    sb.AppendLine("\t\t}");
                    if (i < dt.Rows.Count - 1)
                    {
                        sb.AppendLine("");
                    }

                    continue;
                }

                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];

                    string colName = dr_col["column_name"].ToString();

                    sb.AppendLine("\t\t\tpublic Field " + GetCanonicalIdentifier(colName) + " = new Field(this, \"" + GetBracketsedObjectName(colName) + "\", Types." + GetSQLDataType(dr_col["data_type"].ToString()) + ", " + ((dr_col["extra"].ToString() == "auto_increment") ? "true" : "false") + ");");
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
            DataTable dt = Database.MySQL.Select(ConnStr, "select table_name from information_schema.tables where table_schema = '" + m_Database + "' and table_type = 'VIEW' order by table_name;");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string viewName = dr["table_name"].ToString();

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
            DataTable dt = Database.MySQL.Select(ConnStr, "select routine_name, dtd_identifier, routine_body from information_schema.routines where routine_schema = '" + m_Database + "' and routine_type = 'FUNCTION' order by routine_name;");
            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string functionName = dr["routine_name"].ToString();

                //Function Builder...
                DataTable dt_col = Database.MySQL.Select(ConnStr, "SELECT param_list, returns FROM mysql.proc where db = '" + m_Database + "' and type = 'FUNCTION' and name = '" + functionName + "';");
                if (dt_col == null)
                    continue;
                if (dt_col.Rows.Count < 1)
                    continue;

                //[shove] 2013.2.1 临时该为下面几句，具体原因未分析
                //string ReturnType = GetDataType(FilterLengthDescriptionForReturnType(System.Text.ASCIIEncoding.ASCII.GetString((byte[])dt_col.Rows[0]["returns"])));
                //string ReturnSQLType = GetSQLDataType(FilterLengthDescriptionForReturnType(System.Text.ASCIIEncoding.ASCII.GetString((byte[])dt_col.Rows[0]["returns"])));

                string ReturnType = "";
                string ReturnSQLType = "";
                try
                {
                    ReturnType = GetDataType(FilterLengthDescriptionForReturnType(Encoding.ASCII.GetString((byte[])dt_col.Rows[0]["returns"])));
                }
                catch
                {
                    ReturnType = GetDataType(FilterLengthDescriptionForReturnType(dt_col.Rows[0]["returns"].ToString()));
                }
                try
                {
                    ReturnSQLType = GetSQLDataType(FilterLengthDescriptionForReturnType(Encoding.ASCII.GetString((byte[])dt_col.Rows[0]["returns"])));
                }
                catch
                {
                    ReturnSQLType = GetSQLDataType(FilterLengthDescriptionForReturnType(dt_col.Rows[0]["returns"].ToString()));
                }

                sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(functionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")));
                string[] cols = SplitParameters(BytesToString((byte[])dt_col.Rows[0]["param_list"]));
                if (cols != null)
                {
                    for (int j = 0; j < cols.Length; j++)
                    {
                        string[] t_strs = cols[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if ((t_strs == null) || (t_strs.Length < 2) || (t_strs[0].Trim() == ""))
                        {
                            continue;
                        }
                        string colName = t_strs[0];
                        string Type = GetDataType(t_strs[1]);
                        if ((j > 0) || !m_isUseConnectionStringConfig)
                            sb.Append(", ");
                        sb.Append(Type + " " + GetCanonicalIdentifier(colName));
                    }
                }
                sb.AppendLine(") throws SQLException {");

                sb.AppendLine("\t\t\tObject result = MySQL.executeFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(functionName) + "\",");
                sb.Append("\t\t\t\tnew Parameter(Types." + ReturnSQLType + ", ParameterDirection.RETURN, null)");
                if (cols != null)
                {
                    for (int j = 0; j < cols.Length; j++)
                    {
                        string[] t_strs = cols[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if ((t_strs == null) || (t_strs.Length < 2) || (t_strs[0].Trim() == ""))
                        {
                            continue;
                        }
                        string colName = t_strs[0];
                        string Type = GetDataType(t_strs[1]);
                        string SQLType = GetSQLDataType(t_strs[1]).ToString();
                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew Parameter(Types." + SQLType + ", ParameterDirection.IN, " + GetCanonicalIdentifier(colName) + ((Type == "boolean") ? " ? 1 : 0" : "") + ")");
                    }
                }
                sb.AppendLine(");");

                sb.AppendLine("");
                if (GetDataTypeForConvert(ReturnType) == "(Boolean)")
                {
                    sb.AppendLine("\t\t\treturn " + GetDataTypeForConvert(ReturnType) + "(result.ToString() == \"1\");");
                }
                else
                {
                    sb.AppendLine("\t\t\treturn " + GetDataTypeForConvert(ReturnType) + "result;");
                }
                sb.AppendLine("\t\t}");
                // Builder End.

                if (i < dt.Rows.Count - 1)
                    sb.AppendLine("");
            }
        }

        private void Procedures(ref StringBuilder sb)
        {
            DataTable dt = Database.MySQL.Select(ConnStr, "select routine_name, dtd_identifier, routine_body from information_schema.routines where routine_schema = '" + m_Database + "' and routine_type = 'PROCEDURE' order by routine_name;");
            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string procedureName = dr["routine_name"].ToString();

                //Procedure Class Builder...
                DataTable dt_col = Database.MySQL.Select(ConnStr, "SELECT param_list FROM mysql.proc where db = '" + m_Database + "' and type = 'PROCEDURE' and name = '" + procedureName + "';");
                if (dt_col == null)
                    continue;
                if (dt_col.Rows.Count < 1)
                    continue;

                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(procedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")) + ", DataSet ds, List<Object> outParameterValues");
                string[] cols = SplitParameters(BytesToString((byte[])dt_col.Rows[0]["param_list"]));
                if (cols != null)
                {
                    for (int j = 0; j < cols.Length; j++)
                    {
                        if (!cols[j].StartsWith("in ", StringComparison.OrdinalIgnoreCase) && !cols[j].StartsWith("out ", StringComparison.OrdinalIgnoreCase) && !cols[j].StartsWith("inout ", StringComparison.OrdinalIgnoreCase))
                        {
                            cols[j] = "IN " + cols[j];
                        }
                        string[] t_strs = cols[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if ((t_strs == null) || (t_strs.Length < 3))
                        {
                            continue;
                        }
                        string colName = t_strs[1];
                        string Type = GetDataType(t_strs[2]);
                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        if ((j > 0) || !m_isUseConnectionStringConfig)
                        {
                            sb.Append(", ");
                        }
                        sb.Append(Type + " " + GetCanonicalIdentifier(colName));
                    }
                }
                sb.AppendLine(") throws SQLException, DataException {");
                sb.Append("\t\t\tint result = MySQL.executeProcedure(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(procedureName) + "\", ds, outParameterValues");
                if (cols != null)
                {
                    for (int j = 0; j < cols.Length; j++)
                    {
                        string[] t_strs = cols[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if ((t_strs == null) || (t_strs.Length < 3))
                        {
                            continue;
                        }
                        string colName = t_strs[1];
                        string Type = GetDataType(t_strs[2]);
                        string SQLType = GetSQLDataType(t_strs[2]).ToString();
                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew Parameter(Types." + SQLType + ", " + (isOutput == 0 ? "ParameterDirection.IN" : (isOutput == 1 ? "ParameterDirection.INOUT" : "ParameterDirection.OUT")) + ", " + GetCanonicalIdentifier(colName) + ")");
                    }
                }
                sb.AppendLine(");");

                sb.AppendLine("");
                sb.AppendLine("\t\t\treturn result;");
                sb.AppendLine("\t\t}");

                if (i < dt.Rows.Count - 1)
                {
                    sb.AppendLine("");
                }
            }
        }

        /// <summary>
        /// 将数据库类型转换为 Java 数据类型
        /// </summary>
        /// <param name="SQLType"></param>
        /// <returns></returns>
        private string GetDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();//.Split('(')[0];
            string result = "String";

            switch (SQLType)
            {
                case "tinyint":
                    result = "short";
                    break;
                case "smallint":
                    result = "short";
                    break;
                case "mediumint":
                    result = "int";
                    break;
                case "int":
                    result = "int";
                    break;
                case "integer":
                    result = "int";
                    break;
                case "bigint":
                    result = "long";
                    break;
                case "float":
                    result = "float";
                    break;
                case "double":
                    result = "double";
                    break;
                case "decimal":
                    result = "BigDecimal";
                    break;
                case "date":
                    result = "Date";
                    break;
                case "datetime":
                    result = "Date";
                    break;
                case "timestamp":
                    result = "Date";
                    break;
                case "time":
                    result = "Date";
                    break;
                case "year":
                    result = "Date";
                    break;
                case "char":
                    result = "String";
                    break;
                case "varchar":
                    result = "String";
                    break;
                case "tinyblob":
                    result = "byte[]";
                    break;
                case "blob":
                    result = "byte[]";
                    break;
                case "mediumblob":
                    result = "byte[]";
                    break;
                case "longblob":
                    result = "byte[]";
                    break;
                case "tinytext":
                    result = "String";
                    break;
                case "text":
                    result = "String";
                    break;
                case "mediumtext":
                    result = "String";
                    break;
                case "longtext":
                    result = "String";
                    break;
                case "enum":
                    result = "String";
                    break;
                case "set":
                    result = "String";
                    break;
                case "binary":
                    result = "byte[]";
                    break;
                case "varbinary":
                    result = "byte[]";
                    break;
                case "bit":
                    result = "boolean";
                    break;
                case "boolean":
                    result = "boolean";
                    break;
                case "geometry":
                    result = "String";
                    break;
                case "point":
                    result = "int";
                    break;
                case "linestring":
                    result = "String";
                    break;
                case "polygon":
                    result = "String";
                    break;
                case "multipoint":
                    result = "int";
                    break;
                case "multilinestring":
                    result = "String";
                    break;
                case "multipolygon":
                    result = "String";
                    break;
                case "geometrycollection":
                    result = "String";
                    break;
            }

            return result;
        }

        /// <summary>
        /// 将数据库类型转换为 JDBC.Types.类型
        /// </summary>
        /// <param name="SQLType"></param>
        /// <returns></returns>
        private string GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();//.Split('(')[0];
            string result = "VARCHAR";

            switch (SQLType)
            {
                case "tinyint":
                    result = "TINYINT";
                    break;
                case "smallint":
                    result = "SMALLINT";
                    break;
                case "mediumint":
                    result = "INTEGER";
                    break;
                case "int":
                    result = "INTEGER";
                    break;
                case "integer":
                    result = "INTEGER";
                    break;
                case "bigint":
                    result = "BIGINT";
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
                case "date":
                    result = "DATE";
                    break;
                case "datetime":
                    result = "TIMESTAMP";
                    break;
                case "timestamp":
                    result = "TIMESTAMP";
                    break;
                case "time":
                    result = "TIME";
                    break;
                case "year":
                    result = "DATE";
                    break;
                case "char":
                    result = "CHAR";
                    break;
                case "varchar":
                    result = "VARCHAR";
                    break;
                case "tinyblob":
                    result = "BLOB";
                    break;
                case "blob":
                    result = "BLOB";
                    break;
                case "mediumblob":
                    result = "BLOB";
                    break;
                case "longblob":
                    result = "BLOB";
                    break;
                case "tinytext":
                    result = "VARCHAR";
                    break;
                case "text":
                    result = "VARCHAR";
                    break;
                case "mediumtext":
                    result = "LONGVARCHAR";
                    break;
                case "longtext":
                    result = "LONGVARCHAR";
                    break;
                case "enum":
                    result = "VARCHAR";
                    break;
                case "set":
                    result = "VARCHAR";
                    break;
                case "binary":
                    result = "BINARY";
                    break;
                case "varbinary":
                    result = "VARBINARY";
                    break;
                case "bit":
                    result = "BIT";
                    break;
                case "boolean":
                    result = "BOOLEAN";
                    break;
                case "geometry":
                    result = "VARCHAR";
                    break;
                case "point":
                    result = "INTEGER";
                    break;
                case "linestring":
                    result = "VARCHAR";
                    break;
                case "polygon":
                    result = "VARCHAR";
                    break;
                case "multipoint":
                    result = "INTEGER";
                    break;
                case "multilinestring":
                    result = "VARCHAR";
                    break;
                case "multipolygon":
                    result = "VARCHAR";
                    break;
                case "geometrycollection":
                    result = "VARCHAR";
                    break;
            }

            return result;
        }

        private string GetDataTypeForConvert(string type)
        {
            type = type.Trim().ToLower();
            string result = "(String)";

            switch (type)
            {
                case "long":
                    result = "(Long)";
                    break;
                case "boolean":
                    result = "(Boolean)";
                    break;
                case "string":
                    result = "(String)";
                    break;
                case "date":
                    result = "(Date)";
                    break;
                case "timestamp":
                    result = "(Date)";
                    break;
                case "float":
                    result = "(Float)";
                    break;
                case "double":
                    result = "(Double)";
                    break;
                case "bigdecimal":
                    result = "(BigDecimal)";
                    break;
                case "int":
                    result = "(Integer)";
                    break;
                case "short":
                    result = "(Short)";
                    break;
                case "byte[]":
                    result = "(byte[])";
                    break;
            }

            return result;
        }

        private string GetCanonicalIdentifier(string identifierName)
        {
            identifierName = identifierName.Replace(" ", "_").Replace("$", "_").Replace("@", "_").Replace("`", "_");

            if (identifierName.Length > 0)
            {
                if ("0123456789".IndexOf(identifierName[0]) >= 0)
                {
                    identifierName = "_" + identifierName;
                }
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
            if (!input.StartsWith("`", System.StringComparison.Ordinal))
                return "`" + input + "`";
            else
                return input.Replace("`", "```");
        }

        private string BytesToString(byte[] input)
        {
            return Encoding.ASCII.GetString(input);
        }

        private string FilterSpace(string input)
        {
            while (input.StartsWith("\n", System.StringComparison.Ordinal) || input.StartsWith("\r", System.StringComparison.Ordinal) || input.StartsWith("\t", System.StringComparison.Ordinal) || input.StartsWith("\v", System.StringComparison.Ordinal) || input.StartsWith("\f", System.StringComparison.Ordinal) || input.StartsWith(" ", System.StringComparison.Ordinal))
            {
                input = input.Substring(1);
            }

            while (input.EndsWith("\n", System.StringComparison.Ordinal) || input.EndsWith("\r", System.StringComparison.Ordinal) || input.EndsWith("\t", System.StringComparison.Ordinal) || input.StartsWith("\v", System.StringComparison.Ordinal) || input.StartsWith("\f", System.StringComparison.Ordinal) || input.EndsWith(" ", System.StringComparison.Ordinal))
            {
                input = input.Substring(0, input.Length - 1);
            }

            return input;
        }

        private string FilterLengthDescriptionForReturnType(string input)
        {
            string result = input;

            if (result.Contains("("))
            {
                result = result.Substring(0, result.IndexOf("(", System.StringComparison.Ordinal));
            }

            return result;
        }

        private string FilterLengthDescription(string input)
        {
            input = FilterSpace(input);
            string result = input;
            int Len = 0;

            if (result.Contains("("))
            {
                result = result.Substring(0, result.IndexOf("(", System.StringComparison.Ordinal));

                string t = input.Substring(input.IndexOf("(", System.StringComparison.Ordinal));
                t = t.Substring(1, t.Length - 2);

                if (t.Contains(","))
                {
                    t = t.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0];
                }

                Len = int.Parse(t);
            }

            result = FilterSpace(result).Replace("`", "");

            return result + " " + Len.ToString();
        }

        private string[] SplitParameters(string input)
        {
            Regex regex = new Regex(@"#[^\n]*?[\n]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            input = regex.Replace(input, "");

            regex = new Regex(@"[,][\s\t\r\n\v]*?[\d]+?[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            input = regex.Replace(input, ")");

            input = Regex.Replace(input, @"[/][*][\s\S]*?[*][/]", "", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            string[] strs = input.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if ((strs == null) || (strs.Length < 1))
            {
                return null;
            }

            for (int i = 0; i < strs.Length; i++)
            {
                strs[i] = FilterLengthDescription(strs[i]);
            }

            return strs;
        }
    }
}
