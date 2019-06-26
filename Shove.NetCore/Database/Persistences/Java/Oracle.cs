using System;
using System.Collections.Generic;
using System.Text;
using System.Data.OracleClient;
using System.Data;
using System.Text.RegularExpressions;

namespace Shove.Database.Persistences.Java
{
    /// <summary>
    /// Oracle 持久化
    /// </summary>
    public class Oracle
    {
        private string ConnStr = "";

        string m_ServerName;
        string m_UserID;
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
        /// <param name="ServerName"></param>
        /// <param name="UserID"></param>
        /// <param name="Password"></param>
        /// <param name="NamespaceName"></param>
        /// <param name="isUseConnectionStringConfig"></param>
        /// <param name="isUseConnectionString"></param>
        /// <param name="isWithTables"></param>
        /// <param name="isWithViews"></param>
        /// <param name="isWithProcedures"></param>
        /// <param name="isWithFunction"></param>
        public Oracle(string ServerName, string UserID, string Password, string NamespaceName, bool isUseConnectionStringConfig, bool isUseConnectionString, bool isWithTables, bool isWithViews, bool isWithProcedures, bool isWithFunction)
        {
            m_ServerName = ServerName;
            m_UserID = UserID;
            m_Password = Password;
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

            ConnStr = Shove.Database.Oracle.BuildConnectString(m_ServerName, m_UserID, m_Password);

            OracleConnection conn = Shove.Database.Oracle.CreateDataConnection<OracleConnection>(ConnStr);

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
            DataTable dt = Shove.Database.Oracle.Select(ConnStr, "select table_name from user_tables order by table_name");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string TableName = dr["table_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(TableName) + " extends OracleTable {");

                DataTable dt_col = Shove.Database.Oracle.Select(ConnStr, "select column_name, data_type, data_length from user_tab_cols where table_name = '" + TableName + "' order by column_id");

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

                    sb.AppendLine("\t\t\tpublic OracleField " + GetCanonicalIdentifier(ColName) + " = new OracleField(this, \"" + GetBracketsedObjectName(ColName) + "\", OracleTypes." + GetSQLDataType(dr_col["data_type"].ToString()) + ", false);");
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
            DataTable dt = Shove.Database.Oracle.Select(ConnStr, "select view_name from user_views order by view_name");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string ViewName = dr["view_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(ViewName) + " extends OracleView {");
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
            DataTable dt = Shove.Database.Oracle.Select(ConnStr, "select object_name, procedure_name, overload from user_procedures where object_type = 'FUNCTION' or (object_type = 'PACKAGE' and not procedure_name is null and procedure_name in (select object_name from all_arguments where argument_name is null)) order by object_name");

            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];

                string PackageName = "";
                string FunctionName = dr["object_name"].ToString();
                string FunctionName_sub = FunctionName;
                string OverLoad = dr["overload"].ToString();

                if (!string.IsNullOrEmpty(dr["procedure_name"].ToString()))
                {
                    PackageName = FunctionName;
                    FunctionName_sub = dr["procedure_name"].ToString();
                    FunctionName += "." + FunctionName_sub;
                }

                OverLoad = string.IsNullOrEmpty(OverLoad) ? "NVL(overload, 0) = 0" : ("NVL(overload, 0) = " + OverLoad);

                //Function Builder...
                DataTable dt_col = null;
                if (string.IsNullOrEmpty(PackageName))
                {
                    dt_col = Shove.Database.Oracle.Select(ConnStr, "select argument_name, data_type, data_length, data_scale, in_out from all_arguments where object_name='" + FunctionName_sub + "' and OWNER='" + m_UserID.ToUpper() + "' and DATA_LEVEL = 0 and " + OverLoad + " order by position");
                }
                else
                {
                    dt_col = Shove.Database.Oracle.Select(ConnStr, "select argument_name, data_type, data_length, data_scale, in_out from all_arguments where package_name = '" + PackageName + "' and object_name='" + FunctionName_sub + "' and OWNER='" + m_UserID.ToUpper() + "' and DATA_LEVEL = 0 and " + OverLoad + " order by position");
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

                sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(FunctionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")) + ", DataSet ds, List<Object> outParameterValues");
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 1) || !m_isUseConnectionStringConfig)
                        sb.Append(", ");
                    sb.Append(Type + " " + GetCanonicalIdentifier(ColName));
                }
                sb.AppendLine(") throws SQLException, DataException {");

                sb.AppendLine("\t\t\tObject result = Oracle.executeFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(FunctionName) + "\", ds, outParameterValues,");
                sb.Append("\t\t\t\tnew Parameter(OracleTypes." + ReturnSQLType + ", ParameterDirection.RETURN, null)");
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    In_Out = ((In_Out == "IN") ? "IN" : ((In_Out == "OUT") ? "OUT" : "INOUT"));
                    sb.AppendLine(",");
                    sb.Append("\t\t\t\tnew Parameter(OracleTypes." + SQLType + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(ColName) + ")");
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
                sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(FunctionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")) + ", DataSet ds, List<Object> outParameterValues");
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 1) || !m_isUseConnectionStringConfig)
                        sb.Append(", ");
                    sb.Append(Type + " " + GetCanonicalIdentifier(ColName));
                }
                sb.AppendLine(") throws SQLException, DataException {");

                sb.AppendLine("\t\t\tObject result = Oracle.executeFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(FunctionName) + "\", ds, outParameterValues,");
                sb.Append("\t\t\t\tnew Parameter(OracleTypes." + ReturnSQLType + ", ParameterDirection.RETURN, null)");
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    In_Out = ((In_Out == "IN") ? "IN" : ((In_Out == "OUT") ? "OUT" : "INOUT"));
                    sb.AppendLine(",");
                    sb.Append("\t\t\t\tnew Parameter(OracleTypes." + SQLType + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(ColName) + ")");
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
            DataTable dt = Shove.Database.Oracle.Select(ConnStr, "select object_name, procedure_name, overload from user_procedures where object_type = 'PROCEDURE' or (object_type = 'PACKAGE' and not procedure_name is null and procedure_name not in (select object_name from all_arguments where argument_name is null)) order by object_name");
            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];

                string PackageName = "";
                string ProcedureName = dr["object_name"].ToString();
                string ProcedureName_sub = ProcedureName;
                string OverLoad = dr["overload"].ToString();

                if (!string.IsNullOrEmpty(dr["procedure_name"].ToString()))
                {
                    PackageName = ProcedureName;
                    ProcedureName_sub = dr["procedure_name"].ToString();
                    ProcedureName += "." + ProcedureName_sub;
                }

                OverLoad = string.IsNullOrEmpty(OverLoad) ? "NVL(overload, 0) = 0" : ("NVL(overload, 0) = " + OverLoad);

                //Procedure Builder...
                DataTable dt_col = null;
                if (string.IsNullOrEmpty(PackageName))
                {
                    dt_col = Shove.Database.Oracle.Select(ConnStr, "select argument_name, data_type, data_length, data_scale, in_out from all_arguments where object_name='" + ProcedureName_sub + "' and OWNER='" + m_UserID.ToUpper() + "' and DATA_LEVEL = 0 and " + OverLoad + " order by position");
                }
                else
                {
                    dt_col = Shove.Database.Oracle.Select(ConnStr, "select argument_name, data_type, data_length, data_scale, in_out from all_arguments where package_name = '" + PackageName + "' and object_name='" + ProcedureName_sub + "' and OWNER='" + m_UserID.ToUpper() + "' and DATA_LEVEL = 0 and " + OverLoad + " order by position");
                }
                
                if (dt_col == null)
                    continue;
                if (dt_col.Rows.Count < 1)
                    continue;

                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(ProcedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")) + ", DataSet ds, List<Object> outParameterValues");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 0) || !m_isUseConnectionStringConfig)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(Type + " " + GetCanonicalIdentifier(ColName));
                }
                sb.AppendLine(") throws SQLException, DataException {");
                sb.Append("\t\t\tint result = Oracle.executeProcedure(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(ProcedureName) + "\", ds, outParameterValues");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    In_Out = ((In_Out == "IN") ? "IN" : ((In_Out == "OUT") ? "OUT" : "INOUT"));
                    sb.AppendLine(",");
                    sb.Append("\t\t\t\tnew Parameter(OracleTypes." + SQLType + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(ColName) + ")");
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
            string Result = "String";

            switch (SQLType)
            {
                case "char":
                    Result = "String";
                    break;
                case "nchar":
                    Result = "String";
                    break;
                case "varchar":
                    Result = "String";
                    break;
                case "nvarchar":
                    Result = "String";
                    break;
                case "varchar2":
                    Result = "String";
                    break;
                case "nvarchar2":
                    Result = "String";
                    break;
                case "clob":
                    Result = "String";
                    break;
                case "nclob":
                    Result = "String";
                    break;
                case "long":
                    Result = "long";
                    break;
                case "number":
                    Result = "double";
                    break;
                case "binary_float":
                    Result = "float";
                    break;
                case "binary_double":
                    Result = "double";
                    break;
                case "date":
                    Result = "Date";
                    break;
                case "interval day to second":
                    Result = "long";
                    break;
                case "interval year to month":
                    Result = "int";
                    break;
                case "timestamp":
                    Result = "Timestamp";
                    break;
                case "timestamp with time zone":
                    Result = "Timestamp";
                    break;
                case "timestamp with local time zone":
                    Result = "Timestamp";
                    break;
                case "blob":
                    Result = "byte[]";
                    break;
                case "bfile":
                    Result = "byte[]";
                    break;
                case "raw":
                    Result = "byte[]";
                    break;
                case "long raw":
                    Result = "byte[]";
                    break;
                case "rowid":
                    Result = "String";
                    break;
                case "character":
                    Result = "String";
                    break;
                case "character varying":
                    Result = "String";
                    break;
                case "char varying":
                    Result = "String";
                    break;
                case "national character":
                    Result = "String";
                    break;
                case "national char":
                    Result = "String";
                    break;
                case "national character varying":
                    Result = "String";
                    break;
                case "national char varying":
                    Result = "String";
                    break;
                case "nchar varying":
                    Result = "String";
                    break;
                case "numeric":
                    Result = "double";
                    break;
                case "decimal":
                    Result = "BigDecimal";
                    break;
                case "integer":
                    Result = "int";
                    break;
                case "int":
                    Result = "int";
                    break;
                case "smallint":
                    Result = "short";
                    break;
                case "float":
                    Result = "float";
                    break;
                case "double precision":
                    Result = "double";
                    break;
                case "real":
                    Result = "double";
                    break;
                case "ref cursor":
                    Result = "DataSet";
                    break;
            }

            return Result;
        }

        /// <summary>
        /// 将数据库类型转换为 OracleTypes.类型
        /// </summary>
        /// <param name="SQLType"></param>
        /// <returns></returns>
        private string GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();
            string Result = "NVARCHAR";

            switch (SQLType)
            {
                case "char":
                    Result = "CHAR";
                    break;
                case "nchar":
                    Result = "CHAR";
                    break;
                case "varchar":
                    Result = "VARCHAR";
                    break;
                case "nvarchar":
                    Result = "VARCHAR";
                    break;
                case "varchar2":
                    Result = "VARCHAR";
                    break;
                case "nvarchar2":
                    Result = "VARCHAR";
                    break;
                case "clob":
                    Result = "CLOB";
                    break;
                case "nclob":
                    Result = "CLOB";
                    break;
                case "long":
                    Result = "BIGINT";
                    break;
                case "number":
                    Result = "NUMBER";
                    break;
                case "binary_float":
                    Result = "FLOAT";
                    break;
                case "binary_double":
                    Result = "DOUBLE";
                    break;
                case "date":
                    Result = "DATE";
                    break;
                case "interval day to second":
                    Result = "INTERVALDS";
                    break;
                case "interval year to month":
                    Result = "INTERVALYM";
                    break;
                case "timestamp":
                    Result = "TIMESTAMP";
                    break;
                case "timestamp with time zone":
                    Result = "TIMESTAMPTZ";
                    break;
                case "timestamp with local time zone":
                    Result = "TIMESTAMPLTZ";
                    break;
                case "blob":
                    Result = "BLOB";
                    break;
                case "bfile":
                    Result = "BFILE";
                    break;
                case "raw":
                    Result = "RAW";
                    break;
                case "long raw":
                    Result = "RAW";
                    break;
                case "rowid":
                    Result = "ROWID";
                    break;
                case "character":
                    Result = "VARCHAR";
                    break;
                case "character varying":
                    Result = "VARCHAR";
                    break;
                case "char varying":
                    Result = "VARCHAR";
                    break;
                case "national character":
                    Result = "VARCHAR";
                    break;
                case "national char":
                    Result = "VARCHAR";
                    break;
                case "national character varying":
                    Result = "VARCHAR";
                    break;
                case "national char varying":
                    Result = "VARCHAR";
                    break;
                case "nchar varying":
                    Result = "VARCHAR";
                    break;
                case "numeric":
                    Result = "NUMERIC";
                    break;
                case "decimal":
                    Result = "DECIMAL";
                    break;
                case "integer":
                    Result = "INTEGER";
                    break;
                case "int":
                    Result = "INTEGER";
                    break;
                case "smallint":
                    Result = "SMALLINT";
                    break;
                case "float":
                    Result = "FLOAT";
                    break;
                case "double precision":
                    Result = "DOUBLE";
                    break;
                case "real":
                    Result = "REAL";
                    break;
                case "ref cursor":
                    Result = "CURSOR";
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
                case "dataset":
                    Result = "(DataSet)";
                    break;
            }

            return Result;
        }

        private string GetCanonicalIdentifier(string IdentifierName)
        {
            IdentifierName = IdentifierName.Replace(" ", "_").Replace("$", "_").Replace("@", "_").Replace(".", "_");

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
