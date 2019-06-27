using System.Text;
using System.Data.OracleClient;
using System.Data;

namespace Shove.Database.Persistences.Java
{
    /// <summary>
    /// Oracle 持久化
    /// </summary>
    public class Oracle
    {
        private string ConnStr = "";

        string m_Server;
        string m_User;
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
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="namespaceName"></param>
        /// <param name="isUseConnectionStringConfig"></param>
        /// <param name="isUseConnectionString"></param>
        /// <param name="isWithTables"></param>
        /// <param name="isWithViews"></param>
        /// <param name="isWithProcedures"></param>
        /// <param name="isWithFunction"></param>
        public Oracle(string server, string user, string password, string namespaceName, bool isUseConnectionStringConfig, bool isUseConnectionString, bool isWithTables, bool isWithViews, bool isWithProcedures, bool isWithFunction)
        {
            m_Server = server;
            m_User = user;
            m_Password = password;
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

            ConnStr = Database.Oracle.BuildConnectString(m_Server, m_User, m_Password);

            OracleConnection conn = DatabaseAccess.CreateDataConnection<OracleConnection>(ConnStr);

            if (conn == null)
            {
                return "Database Connect Fail.";
            }
            conn.Close();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("/**");
            sb.AppendLine("* Program Name: Shove.DAL.40 for Oracle");
            sb.AppendLine("* Program Version: 4.0");
            sb.AppendLine("* @author: 3km.shovesoft.shove (zhou changjun)");
            sb.AppendLine("* Release Time: 2012.12.11");
            sb.AppendLine("*");
            sb.AppendLine("* System Request: com.shovesoft.jar, ojdbc.x.xx.jar");
            sb.AppendLine("* All Rights saved.");
            sb.AppendLine("*/");
            sb.AppendLine("");

            sb.AppendLine("package " + (m_NamespaceName == "" ? "database;" : (m_NamespaceName + ".database;")));
            sb.AppendLine("");
            sb.AppendLine("import java.util.*;");
            sb.AppendLine("import java.sql.*;");
            sb.AppendLine("import java.math.*;");
            sb.AppendLine("");
            sb.AppendLine("import oracle.jdbc.driver.OracleTypes;");
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
            DataTable dt = Database.Oracle.Select(ConnStr, "select table_name from user_tables order by table_name");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string tableName = dr["table_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(tableName) + " extends OracleTable {");

                DataTable dt_col = Database.Oracle.Select(ConnStr, "select column_name, data_type, data_length from user_tab_cols where table_name = '" + tableName + "' order by column_id");

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

                    sb.AppendLine("\t\t\tpublic OracleField " + GetCanonicalIdentifier(colName) + " = new OracleField(this, \"" + GetBracketsedObjectName(colName) + "\", OracleTypes." + GetSQLDataType(dr_col["data_type"].ToString()) + ", false);");
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
            DataTable dt = Database.Oracle.Select(ConnStr, "select view_name from user_views order by view_name");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string viewName = dr["view_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(viewName) + " extends OracleView {");
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
            DataTable dt = Database.Oracle.Select(ConnStr, "select object_name, procedure_name, overload from user_procedures where object_type = 'FUNCTION' or (object_type = 'PACKAGE' and not procedure_name is null and procedure_name in (select object_name from all_arguments where argument_name is null)) order by object_name");

            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];

                string PackageName = "";
                string functionName = dr["object_name"].ToString();
                string functionName_sub = functionName;
                string OverLoad = dr["overload"].ToString();

                if (!string.IsNullOrEmpty(dr["procedure_name"].ToString()))
                {
                    PackageName = functionName;
                    functionName_sub = dr["procedure_name"].ToString();
                    functionName += "." + functionName_sub;
                }

                OverLoad = string.IsNullOrEmpty(OverLoad) ? "NVL(overload, 0) = 0" : ("NVL(overload, 0) = " + OverLoad);

                //Function Builder...
                DataTable dt_col = null;
                if (string.IsNullOrEmpty(PackageName))
                {
                    dt_col = Database.Oracle.Select(ConnStr, "select argument_name, data_type, data_length, data_scale, in_out from all_arguments where object_name='" + functionName_sub + "' and OWNER='" + m_User.ToUpper() + "' and DATA_LEVEL = 0 and " + OverLoad + " order by position");
                }
                else
                {
                    dt_col = Database.Oracle.Select(ConnStr, "select argument_name, data_type, data_length, data_scale, in_out from all_arguments where package_name = '" + PackageName + "' and object_name='" + functionName_sub + "' and OWNER='" + m_User.ToUpper() + "' and DATA_LEVEL = 0 and " + OverLoad + " order by position");
                }

                if (dt_col == null)
                    continue;
                if (dt_col.Rows.Count < 1)
                    continue;

                if (isContainOracleTypeCursor(dt_col))
                {
                    goto WithQuery;
                }

                // NonQuery
                string ReturnType = GetDataType(dt_col.Rows[0]["data_type"].ToString());
                string ReturnSQLType = GetSQLDataType(dt_col.Rows[0]["data_type"].ToString());

                sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(functionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")) + ", DataSet ds, List<Object> outParameterValues");
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 1) || !m_isUseConnectionStringConfig)
                        sb.Append(", ");
                    sb.Append(Type + " " + GetCanonicalIdentifier(colName));
                }
                sb.AppendLine(") throws SQLException, DataException {");

                sb.AppendLine("\t\t\tObject result = Oracle.executeFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(functionName) + "\", ds, outParameterValues,");
                sb.Append("\t\t\t\tnew Parameter(OracleTypes." + ReturnSQLType + ", ParameterDirection.RETURN, null)");
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
                    string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    In_Out = ((In_Out == "IN") ? "IN" : ((In_Out == "OUT") ? "OUT" : "INOUT"));
                    sb.AppendLine(",");
                    sb.Append("\t\t\t\tnew Parameter(OracleTypes." + SQLType + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(colName) + ")");
                }
                sb.AppendLine(");");
                sb.AppendLine("");
                sb.AppendLine("\t\t\treturn " + GetDataTypeForConvert(ReturnType) + "result;");
                sb.AppendLine("\t\t}");

                goto end;
            WithQuery:

                // WithQuery
                ReturnType = GetDataType(dt_col.Rows[0]["data_type"].ToString());
                ReturnSQLType = GetSQLDataType(dt_col.Rows[0]["data_type"].ToString());
                sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(functionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")) + ", DataSet ds, List<Object> outParameterValues");
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 1) || !m_isUseConnectionStringConfig)
                        sb.Append(", ");
                    sb.Append(Type + " " + GetCanonicalIdentifier(colName));
                }
                sb.AppendLine(") throws SQLException, DataException {");

                sb.AppendLine("\t\t\tObject result = Oracle.executeFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(functionName) + "\", ds, outParameterValues,");
                sb.Append("\t\t\t\tnew Parameter(OracleTypes." + ReturnSQLType + ", ParameterDirection.RETURN, null)");
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
                    string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    In_Out = ((In_Out == "IN") ? "IN" : ((In_Out == "OUT") ? "OUT" : "INOUT"));
                    sb.AppendLine(",");
                    sb.Append("\t\t\t\tnew Parameter(OracleTypes." + SQLType + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(colName) + ")");
                }
                sb.AppendLine(");");
                sb.AppendLine("");
                sb.AppendLine("\t\t\treturn " + GetDataTypeForConvert(ReturnType) + "result;");
                sb.AppendLine("\t\t}");
                // Builder End.

            end:

                if (i < dt.Rows.Count - 1)
                {
                    sb.AppendLine("");
                }
            }
        }

        private void Procedures(ref StringBuilder sb)
        {
            DataTable dt = Database.Oracle.Select(ConnStr, "select object_name, procedure_name, overload from user_procedures where object_type = 'PROCEDURE' or (object_type = 'PACKAGE' and not procedure_name is null and procedure_name not in (select object_name from all_arguments where argument_name is null)) order by object_name");
            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];

                string PackageName = "";
                string procedureName = dr["object_name"].ToString();
                string procedureName_sub = procedureName;
                string OverLoad = dr["overload"].ToString();

                if (!string.IsNullOrEmpty(dr["procedure_name"].ToString()))
                {
                    PackageName = procedureName;
                    procedureName_sub = dr["procedure_name"].ToString();
                    procedureName += "." + procedureName_sub;
                }

                OverLoad = string.IsNullOrEmpty(OverLoad) ? "NVL(overload, 0) = 0" : ("NVL(overload, 0) = " + OverLoad);

                //Procedure Builder...
                DataTable dt_col = null;
                if (string.IsNullOrEmpty(PackageName))
                {
                    dt_col = Database.Oracle.Select(ConnStr, "select argument_name, data_type, data_length, data_scale, in_out from all_arguments where object_name='" + procedureName_sub + "' and OWNER='" + m_User.ToUpper() + "' and DATA_LEVEL = 0 and " + OverLoad + " order by position");
                }
                else
                {
                    dt_col = Database.Oracle.Select(ConnStr, "select argument_name, data_type, data_length, data_scale, in_out from all_arguments where package_name = '" + PackageName + "' and object_name='" + procedureName_sub + "' and OWNER='" + m_User.ToUpper() + "' and DATA_LEVEL = 0 and " + OverLoad + " order by position");
                }
                
                if (dt_col == null)
                    continue;
                if (dt_col.Rows.Count < 1)
                    continue;

                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(procedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")) + ", DataSet ds, List<Object> outParameterValues");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 0) || !m_isUseConnectionStringConfig)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(Type + " " + GetCanonicalIdentifier(colName));
                }
                sb.AppendLine(") throws SQLException, DataException {");
                sb.Append("\t\t\tint result = Oracle.executeProcedure(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(procedureName) + "\", ds, outParameterValues");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    In_Out = ((In_Out == "IN") ? "IN" : ((In_Out == "OUT") ? "OUT" : "INOUT"));
                    sb.AppendLine(",");
                    sb.Append("\t\t\t\tnew Parameter(OracleTypes." + SQLType + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(colName) + ")");
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
            SQLType = SQLType.Trim().ToLower();
            string result = "String";

            switch (SQLType)
            {
                case "char":
                    result = "String";
                    break;
                case "nchar":
                    result = "String";
                    break;
                case "varchar":
                    result = "String";
                    break;
                case "nvarchar":
                    result = "String";
                    break;
                case "varchar2":
                    result = "String";
                    break;
                case "nvarchar2":
                    result = "String";
                    break;
                case "clob":
                    result = "String";
                    break;
                case "nclob":
                    result = "String";
                    break;
                case "long":
                    result = "long";
                    break;
                case "number":
                    result = "double";
                    break;
                case "binary_float":
                    result = "float";
                    break;
                case "binary_double":
                    result = "double";
                    break;
                case "date":
                    result = "Date";
                    break;
                case "interval day to second":
                    result = "long";
                    break;
                case "interval year to month":
                    result = "int";
                    break;
                case "timestamp":
                    result = "Timestamp";
                    break;
                case "timestamp with time zone":
                    result = "Timestamp";
                    break;
                case "timestamp with local time zone":
                    result = "Timestamp";
                    break;
                case "blob":
                    result = "byte[]";
                    break;
                case "bfile":
                    result = "byte[]";
                    break;
                case "raw":
                    result = "byte[]";
                    break;
                case "long raw":
                    result = "byte[]";
                    break;
                case "rowid":
                    result = "String";
                    break;
                case "character":
                    result = "String";
                    break;
                case "character varying":
                    result = "String";
                    break;
                case "char varying":
                    result = "String";
                    break;
                case "national character":
                    result = "String";
                    break;
                case "national char":
                    result = "String";
                    break;
                case "national character varying":
                    result = "String";
                    break;
                case "national char varying":
                    result = "String";
                    break;
                case "nchar varying":
                    result = "String";
                    break;
                case "numeric":
                    result = "double";
                    break;
                case "decimal":
                    result = "BigDecimal";
                    break;
                case "integer":
                    result = "int";
                    break;
                case "int":
                    result = "int";
                    break;
                case "smallint":
                    result = "short";
                    break;
                case "float":
                    result = "float";
                    break;
                case "double precision":
                    result = "double";
                    break;
                case "real":
                    result = "double";
                    break;
                case "ref cursor":
                    result = "DataSet";
                    break;
            }

            return result;
        }

        /// <summary>
        /// 将数据库类型转换为 OracleTypes.类型
        /// </summary>
        /// <param name="SQLType"></param>
        /// <returns></returns>
        private string GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();
            string result = "NVARCHAR";

            switch (SQLType)
            {
                case "char":
                    result = "CHAR";
                    break;
                case "nchar":
                    result = "CHAR";
                    break;
                case "varchar":
                    result = "VARCHAR";
                    break;
                case "nvarchar":
                    result = "VARCHAR";
                    break;
                case "varchar2":
                    result = "VARCHAR";
                    break;
                case "nvarchar2":
                    result = "VARCHAR";
                    break;
                case "clob":
                    result = "CLOB";
                    break;
                case "nclob":
                    result = "CLOB";
                    break;
                case "long":
                    result = "BIGINT";
                    break;
                case "number":
                    result = "NUMBER";
                    break;
                case "binary_float":
                    result = "FLOAT";
                    break;
                case "binary_double":
                    result = "DOUBLE";
                    break;
                case "date":
                    result = "DATE";
                    break;
                case "interval day to second":
                    result = "INTERVALDS";
                    break;
                case "interval year to month":
                    result = "INTERVALYM";
                    break;
                case "timestamp":
                    result = "TIMESTAMP";
                    break;
                case "timestamp with time zone":
                    result = "TIMESTAMPTZ";
                    break;
                case "timestamp with local time zone":
                    result = "TIMESTAMPLTZ";
                    break;
                case "blob":
                    result = "BLOB";
                    break;
                case "bfile":
                    result = "BFILE";
                    break;
                case "raw":
                    result = "RAW";
                    break;
                case "long raw":
                    result = "RAW";
                    break;
                case "rowid":
                    result = "ROWID";
                    break;
                case "character":
                    result = "VARCHAR";
                    break;
                case "character varying":
                    result = "VARCHAR";
                    break;
                case "char varying":
                    result = "VARCHAR";
                    break;
                case "national character":
                    result = "VARCHAR";
                    break;
                case "national char":
                    result = "VARCHAR";
                    break;
                case "national character varying":
                    result = "VARCHAR";
                    break;
                case "national char varying":
                    result = "VARCHAR";
                    break;
                case "nchar varying":
                    result = "VARCHAR";
                    break;
                case "numeric":
                    result = "NUMERIC";
                    break;
                case "decimal":
                    result = "DECIMAL";
                    break;
                case "integer":
                    result = "INTEGER";
                    break;
                case "int":
                    result = "INTEGER";
                    break;
                case "smallint":
                    result = "SMALLINT";
                    break;
                case "float":
                    result = "FLOAT";
                    break;
                case "double precision":
                    result = "DOUBLE";
                    break;
                case "real":
                    result = "REAL";
                    break;
                case "ref cursor":
                    result = "CURSOR";
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
                case "dataset":
                    result = "(DataSet)";
                    break;
            }

            return result;
        }

        private string GetCanonicalIdentifier(string identifierName)
        {
            identifierName = identifierName.Replace(" ", "_").Replace("$", "_").Replace("@", "_").Replace(".", "_");

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
            return input;
        }

        private bool isContainOracleTypeCursor(DataTable dt)
        {
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["data_type"].ToString() == "REF CURSOR")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
