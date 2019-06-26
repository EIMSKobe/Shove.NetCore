using System;
using System.Collections.Generic;
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

        string m_ServerName;
        string m_DatabaseName;
        string m_UserID;
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
        /// <param name="ServerName"></param>
        /// <param name="DatabaseName"></param>
        /// <param name="UserID"></param>
        /// <param name="Password"></param>
        /// <param name="Port"></param>
        /// <param name="NamespaceName"></param>
        /// <param name="isUseConnectionStringConfig"></param>
        /// <param name="isUseConnectionString"></param>
        /// <param name="isWithTables"></param>
        /// <param name="isWithViews"></param>
        /// <param name="isWithProcedures"></param>
        /// <param name="isWithFunction"></param>
        public MySQL(string ServerName, string DatabaseName, string UserID, string Password, string Port, string NamespaceName, bool isUseConnectionStringConfig, bool isUseConnectionString, bool isWithTables, bool isWithViews, bool isWithProcedures, bool isWithFunction)
        {
            m_ServerName = ServerName;
            m_DatabaseName = DatabaseName;
            m_UserID = UserID;
            m_Password = Password;
            m_Port = Port;
            m_NamespaceName = NamespaceName.Trim();
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

            ConnStr = Shove.Database.MySQL.BuildConnectString(m_ServerName, m_UserID, m_Password, m_DatabaseName, m_Port);

            MySqlConnection conn = Shove.Database.MySQL.CreateDataConnection<MySqlConnection>(ConnStr);

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
            DataTable dt = Shove.Database.MySQL.Select(ConnStr, "select table_name from information_schema.tables where table_schema = '" + m_DatabaseName + "' and table_type = 'BASE TABLE' order by table_name;");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string TableName = dr["table_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(TableName) + " extends Table {");

                DataTable dt_col = Shove.Database.MySQL.Select(ConnStr, "select column_name, data_type, character_maximum_length, column_type, extra from information_schema.columns where table_schema = '" + m_DatabaseName + "' and table_name = '" + TableName + "' order by ordinal_position;");

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

                    string ColName = dr_col["column_name"].ToString();

                    sb.AppendLine("\t\t\tpublic Field " + GetCanonicalIdentifier(ColName) + " = new Field(this, \"" + GetBracketsedObjectName(ColName) + "\", Types." + GetSQLDataType(dr_col["data_type"].ToString()) + ", " + ((dr_col["extra"].ToString() == "auto_increment") ? "true" : "false") + ");");
                }
                sb.AppendLine("");

                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(TableName) + "() {");
                sb.AppendLine("\t\t\t\tname = \"" + GetBracketsedObjectName(TableName) + "\";");
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
            DataTable dt = Shove.Database.MySQL.Select(ConnStr, "select table_name from information_schema.tables where table_schema = '" + m_DatabaseName + "' and table_type = 'VIEW' order by table_name;");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string ViewName = dr["table_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(ViewName) + " extends View {");
                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(ViewName) + "() {");
                sb.AppendLine("\t\t\t\tname = \"" + GetBracketsedObjectName(ViewName) + "\";");
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
            DataTable dt = Shove.Database.MySQL.Select(ConnStr, "select routine_name, dtd_identifier, routine_body from information_schema.routines where routine_schema = '" + m_DatabaseName + "' and routine_type = 'FUNCTION' order by routine_name;");
            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string FunctionName = dr["routine_name"].ToString();

                //Function Builder...
                DataTable dt_col = Shove.Database.MySQL.Select(ConnStr, "SELECT param_list, returns FROM mysql.proc where db = '" + m_DatabaseName + "' and type = 'FUNCTION' and name = '" + FunctionName + "';");
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
                    ReturnType = GetDataType(FilterLengthDescriptionForReturnType(System.Text.ASCIIEncoding.ASCII.GetString((byte[])dt_col.Rows[0]["returns"])));
                }
                catch
                {
                    ReturnType = GetDataType(FilterLengthDescriptionForReturnType(dt_col.Rows[0]["returns"].ToString()));
                }
                try
                {
                    ReturnSQLType = GetSQLDataType(FilterLengthDescriptionForReturnType(System.Text.ASCIIEncoding.ASCII.GetString((byte[])dt_col.Rows[0]["returns"])));
                }
                catch
                {
                    ReturnSQLType = GetSQLDataType(FilterLengthDescriptionForReturnType(dt_col.Rows[0]["returns"].ToString()));
                }

                sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(FunctionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")));
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
                        string ColName = t_strs[0];
                        string Type = GetDataType(t_strs[1]);
                        if ((j > 0) || !m_isUseConnectionStringConfig)
                            sb.Append(", ");
                        sb.Append(Type + " " + GetCanonicalIdentifier(ColName));
                    }
                }
                sb.AppendLine(") throws SQLException {");

                sb.AppendLine("\t\t\tObject result = MySQL.executeFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(FunctionName) + "\",");
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
                        string ColName = t_strs[0];
                        string Type = GetDataType(t_strs[1]);
                        string SQLType = GetSQLDataType(t_strs[1]).ToString();
                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew Parameter(Types." + SQLType + ", ParameterDirection.IN, " + GetCanonicalIdentifier(ColName) + ((Type == "boolean") ? " ? 1 : 0" : "") + ")");
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
            DataTable dt = Shove.Database.MySQL.Select(ConnStr, "select routine_name, dtd_identifier, routine_body from information_schema.routines where routine_schema = '" + m_DatabaseName + "' and routine_type = 'PROCEDURE' order by routine_name;");
            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string ProcedureName = dr["routine_name"].ToString();

                //Procedure Class Builder...
                DataTable dt_col = Shove.Database.MySQL.Select(ConnStr, "SELECT param_list FROM mysql.proc where db = '" + m_DatabaseName + "' and type = 'PROCEDURE' and name = '" + ProcedureName + "';");
                if (dt_col == null)
                    continue;
                if (dt_col.Rows.Count < 1)
                    continue;

                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(ProcedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")) + ", DataSet ds, List<Object> outParameterValues");
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
                        string ColName = t_strs[1];
                        string Type = GetDataType(t_strs[2]);
                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        if ((j > 0) || !m_isUseConnectionStringConfig)
                        {
                            sb.Append(", ");
                        }
                        sb.Append(Type + " " + GetCanonicalIdentifier(ColName));
                    }
                }
                sb.AppendLine(") throws SQLException, DataException {");
                sb.Append("\t\t\tint result = MySQL.executeProcedure(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(ProcedureName) + "\", ds, outParameterValues");
                if (cols != null)
                {
                    for (int j = 0; j < cols.Length; j++)
                    {
                        string[] t_strs = cols[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if ((t_strs == null) || (t_strs.Length < 3))
                        {
                            continue;
                        }
                        string ColName = t_strs[1];
                        string Type = GetDataType(t_strs[2]);
                        string SQLType = GetSQLDataType(t_strs[2]).ToString();
                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew Parameter(Types." + SQLType + ", " + (isOutput == 0 ? "ParameterDirection.IN" : (isOutput == 1 ? "ParameterDirection.INOUT" : "ParameterDirection.OUT")) + ", " + GetCanonicalIdentifier(ColName) + ")");
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
            string Result = "String";

            switch (SQLType)
            {
                case "tinyint":
                    Result = "short";
                    break;
                case "smallint":
                    Result = "short";
                    break;
                case "mediumint":
                    Result = "int";
                    break;
                case "int":
                    Result = "int";
                    break;
                case "integer":
                    Result = "int";
                    break;
                case "bigint":
                    Result = "long";
                    break;
                case "float":
                    Result = "float";
                    break;
                case "double":
                    Result = "double";
                    break;
                case "decimal":
                    Result = "BigDecimal";
                    break;
                case "date":
                    Result = "Date";
                    break;
                case "datetime":
                    Result = "Date";
                    break;
                case "timestamp":
                    Result = "Date";
                    break;
                case "time":
                    Result = "Date";
                    break;
                case "year":
                    Result = "Date";
                    break;
                case "char":
                    Result = "String";
                    break;
                case "varchar":
                    Result = "String";
                    break;
                case "tinyblob":
                    Result = "byte[]";
                    break;
                case "blob":
                    Result = "byte[]";
                    break;
                case "mediumblob":
                    Result = "byte[]";
                    break;
                case "longblob":
                    Result = "byte[]";
                    break;
                case "tinytext":
                    Result = "String";
                    break;
                case "text":
                    Result = "String";
                    break;
                case "mediumtext":
                    Result = "String";
                    break;
                case "longtext":
                    Result = "String";
                    break;
                case "enum":
                    Result = "String";
                    break;
                case "set":
                    Result = "String";
                    break;
                case "binary":
                    Result = "byte[]";
                    break;
                case "varbinary":
                    Result = "byte[]";
                    break;
                case "bit":
                    Result = "boolean";
                    break;
                case "boolean":
                    Result = "boolean";
                    break;
                case "geometry":
                    Result = "String";
                    break;
                case "point":
                    Result = "int";
                    break;
                case "linestring":
                    Result = "String";
                    break;
                case "polygon":
                    Result = "String";
                    break;
                case "multipoint":
                    Result = "int";
                    break;
                case "multilinestring":
                    Result = "String";
                    break;
                case "multipolygon":
                    Result = "String";
                    break;
                case "geometrycollection":
                    Result = "String";
                    break;
            }

            return Result;
        }

        /// <summary>
        /// 将数据库类型转换为 JDBC.Types.类型
        /// </summary>
        /// <param name="SQLType"></param>
        /// <returns></returns>
        private string GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();//.Split('(')[0];
            string Result = "VARCHAR";

            switch (SQLType)
            {
                case "tinyint":
                    Result = "TINYINT";
                    break;
                case "smallint":
                    Result = "SMALLINT";
                    break;
                case "mediumint":
                    Result = "INTEGER";
                    break;
                case "int":
                    Result = "INTEGER";
                    break;
                case "integer":
                    Result = "INTEGER";
                    break;
                case "bigint":
                    Result = "BIGINT";
                    break;
                case "float":
                    Result = "FLOAT";
                    break;
                case "double":
                    Result = "DOUBLE";
                    break;
                case "decimal":
                    Result = "DECIMAL";
                    break;
                case "date":
                    Result = "DATE";
                    break;
                case "datetime":
                    Result = "TIMESTAMP";
                    break;
                case "timestamp":
                    Result = "TIMESTAMP";
                    break;
                case "time":
                    Result = "TIME";
                    break;
                case "year":
                    Result = "DATE";
                    break;
                case "char":
                    Result = "CHAR";
                    break;
                case "varchar":
                    Result = "VARCHAR";
                    break;
                case "tinyblob":
                    Result = "BLOB";
                    break;
                case "blob":
                    Result = "BLOB";
                    break;
                case "mediumblob":
                    Result = "BLOB";
                    break;
                case "longblob":
                    Result = "BLOB";
                    break;
                case "tinytext":
                    Result = "VARCHAR";
                    break;
                case "text":
                    Result = "VARCHAR";
                    break;
                case "mediumtext":
                    Result = "LONGVARCHAR";
                    break;
                case "longtext":
                    Result = "LONGVARCHAR";
                    break;
                case "enum":
                    Result = "VARCHAR";
                    break;
                case "set":
                    Result = "VARCHAR";
                    break;
                case "binary":
                    Result = "BINARY";
                    break;
                case "varbinary":
                    Result = "VARBINARY";
                    break;
                case "bit":
                    Result = "BIT";
                    break;
                case "boolean":
                    Result = "BOOLEAN";
                    break;
                case "geometry":
                    Result = "VARCHAR";
                    break;
                case "point":
                    Result = "INTEGER";
                    break;
                case "linestring":
                    Result = "VARCHAR";
                    break;
                case "polygon":
                    Result = "VARCHAR";
                    break;
                case "multipoint":
                    Result = "INTEGER";
                    break;
                case "multilinestring":
                    Result = "VARCHAR";
                    break;
                case "multipolygon":
                    Result = "VARCHAR";
                    break;
                case "geometrycollection":
                    Result = "VARCHAR";
                    break;
            }

            return Result;
        }

        private string GetDataTypeForConvert(string Type)
        {
            Type = Type.Trim().ToLower();
            string Result = "(String)";

            switch (Type)
            {
                case "long":
                    Result = "(Long)";
                    break;
                case "boolean":
                    Result = "(Boolean)";
                    break;
                case "string":
                    Result = "(String)";
                    break;
                case "date":
                    Result = "(Date)";
                    break;
                case "timestamp":
                    Result = "(Date)";
                    break;
                case "float":
                    Result = "(Float)";
                    break;
                case "double":
                    Result = "(Double)";
                    break;
                case "bigdecimal":
                    Result = "(BigDecimal)";
                    break;
                case "int":
                    Result = "(Integer)";
                    break;
                case "short":
                    Result = "(Short)";
                    break;
                case "byte[]":
                    Result = "(byte[])";
                    break;
            }

            return Result;
        }

        private string GetCanonicalIdentifier(string IdentifierName)
        {
            IdentifierName = IdentifierName.Replace(" ", "_").Replace("$", "_").Replace("@", "_").Replace("`", "_");

            if (IdentifierName.Length > 0)
            {
                if ("0123456789".IndexOf(IdentifierName[0]) >= 0)
                {
                    IdentifierName = "_" + IdentifierName;
                }
            }

            if ((IdentifierName == "name") || (IdentifierName == "fields") || (IdentifierName == "this") || (IdentifierName == "super"))
            {
                IdentifierName = "_" + IdentifierName;
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
            if (!input.StartsWith("`"))
                return "`" + input + "`";
            else
                return input.Replace("`", "```");
        }

        private string BytesToString(byte[] input)
        {
            return System.Text.ASCIIEncoding.ASCII.GetString(input);
        }

        private string FilterSpace(string input)
        {
            while (input.StartsWith("\n") || input.StartsWith("\r") || input.StartsWith("\t") || input.StartsWith("\v") || input.StartsWith("\f") || input.StartsWith(" "))
            {
                input = input.Substring(1);
            }

            while (input.EndsWith("\n") || input.EndsWith("\r") || input.EndsWith("\t") || input.StartsWith("\v") || input.StartsWith("\f") || input.EndsWith(" "))
            {
                input = input.Substring(0, input.Length - 1);
            }

            return input;
        }

        private string FilterLengthDescriptionForReturnType(string input)
        {
            string Result = input;

            if (Result.Contains("("))
            {
                Result = Result.Substring(0, Result.IndexOf("("));
            }

            return Result;
        }

        private string FilterLengthDescription(string input)
        {
            input = FilterSpace(input);
            string Result = input;
            int Len = 0;

            if (Result.Contains("("))
            {
                Result = Result.Substring(0, Result.IndexOf("("));

                string t = input.Substring(input.IndexOf("("));
                t = t.Substring(1, t.Length - 2);

                if (t.Contains(","))
                {
                    t = t.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0];
                }

                Len = int.Parse(t);
            }

            Result = FilterSpace(Result).Replace("`", "");

            return Result + " " + Len.ToString();
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
