using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace Shove.Database.Persistences.Java
{
    /// <summary>
    /// MSSQL 生成持久化功能
    /// </summary>
    public class MSSQL
    {
        private string ConnStr = "";

        string m_ServerName;
        string m_DatabaseName;
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
        /// <param name="DatabaseName"></param>
        /// <param name="UserID"></param>
        /// <param name="Password"></param>
        /// <param name="NamespaceName"></param>
        /// <param name="isUseConnectionStringConfig"></param>
        /// <param name="isUseConnectionString"></param>
        /// <param name="isWithTables"></param>
        /// <param name="isWithViews"></param>
        /// <param name="isWithProcedures"></param>
        /// <param name="isWithFunction"></param>
        public MSSQL(string ServerName, string DatabaseName, string UserID, string Password, string NamespaceName, bool isUseConnectionStringConfig, bool isUseConnectionString, bool isWithTables, bool isWithViews, bool isWithProcedures, bool isWithFunction)
        {
            m_ServerName = ServerName;
            m_DatabaseName = DatabaseName;
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

            ConnStr = Shove.Database.MSSQL.BuildConnectString(m_ServerName, m_DatabaseName, m_UserID, m_Password);

            SqlConnection conn = Shove.Database.MSSQL.CreateDataConnection<SqlConnection>(ConnStr);

            if (conn == null)
            {
                return "Database Connect Fail.";
            }
            conn.Close();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("/**");
            sb.AppendLine("* Program Name: Shove.DAL.40 for SQLServer");
            sb.AppendLine("* Program Version: 4.0");
            sb.AppendLine("* @author: 3km.shovesoft.shove (zhou changjun)");
            sb.AppendLine("* Release Time: 2012.12.11");
            sb.AppendLine("*");
            sb.AppendLine("* System Request: com.shovesoft.jar,sqljbc4.jar");
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
            DataTable dt = Shove.Database.MSSQL.Select(ConnStr, "Select [name], [id] from sysobjects where OBJECTPROPERTY(id, N'IsUserTable') = 1 and OBJECTPROPERTY(id,N'IsMSShipped')=0 order by [name]");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string TableName = dr["Name"].ToString();

                if (TableName == "sysdiagrams")
                {
                    continue;
                }

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(TableName) + " extends SQLServerTable {");

                DataTable dt_col = Shove.Database.MSSQL.Select(ConnStr, "SELECT a.name, a.length, COLUMNPROPERTY(a.id, a.name, 'IsIdentity') IsIdentity, b.name AS xtypename, a.isoutparam FROM syscolumns a LEFT OUTER JOIN systypes b ON a.xtype = b.xtype WHERE (a.id = " + dr["id"].ToString() + ") and (lower(b.name) <> 'sysname') ORDER BY a.colorder");

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

                    string ColName = dr_col["name"].ToString();

                    sb.AppendLine("\t\t\tpublic Field " + GetCanonicalIdentifier(ColName) + " = new Field(this, \"" + GetBracketsedObjectName(ColName) + "\", Types." + GetSQLDataType(dr_col["xtypename"].ToString()) + ", " + ((dr_col["IsIdentity"].ToString() == "1") ? "true" : "false") + ");");
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
            DataTable dt = Shove.Database.MSSQL.Select(ConnStr, "Select [name], [id] from sysobjects where OBJECTPROPERTY(id, N'IsView') = 1 and OBJECTPROPERTY(id,N'IsMSShipped')=0 order by [name]");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string ViewName = dr["Name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(ViewName) + " extends SQLServerView {");
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
            DataTable dt = Shove.Database.MSSQL.Select(ConnStr, "Select [name], [id] from sysobjects where xtype in (N'FN', N'IF', N'TF', N'FS') and OBJECTPROPERTY(id,N'IsMSShipped')=0 order by [name]");
            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string FunctionName = dr["Name"].ToString();

                if (FunctionName == "fn_diagramobjects")
                {
                    continue;
                }

                //Function Builder...
                DataTable dt_col = Shove.Database.MSSQL.Select(ConnStr, "SELECT a.name, a.length, COLUMNPROPERTY(a.id, a.name, 'IsIdentity') IsIdentity, b.name AS xtypename, a.isoutparam FROM syscolumns a LEFT OUTER JOIN systypes b ON a.xtype = b.xtype WHERE (a.id = " + dr["id"].ToString() + ") and (lower(b.name) <> 'sysname') ORDER BY a.colorder");
                if (dt_col == null)
                    continue;
                if (dt_col.Rows.Count < 1)
                    continue;

                if (dt_col.Rows[0]["name"].ToString() == "")
                {
                    // call 普通函数，返回除 table 以外的数据类型
                    string ReturnType = GetDataType(dt_col.Rows[0]["xtypename"].ToString());
                    string ReturnSQLType = GetSQLDataType(dt_col.Rows[0]["xtypename"].ToString());

                    sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(FunctionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")));
                    for (int j = 1; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string ColName = dr_col["name"].ToString();
                        ColName = ColName.Substring(1, ColName.Length - 1);
                        string Type = GetDataType(dr_col["xtypename"].ToString());
                        if ((j > 1) || !m_isUseConnectionStringConfig)
                            sb.Append(", ");
                        sb.Append(Type + " " + GetCanonicalIdentifier(ColName));
                    }
                    sb.AppendLine(") throws SQLException {");

                    sb.AppendLine("\t\t\tObject result = SQLServer.executeFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(FunctionName) + "\", ");
                    sb.Append("\t\t\t\tnew Parameter(Types." + ReturnSQLType + ", ParameterDirection.RETURN, null)"); 
                    for (int j = 1; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string ColName = dr_col["name"].ToString();
                        ColName = ColName.Substring(1, ColName.Length - 1);
                        string Type = GetDataType(dr_col["xtypename"].ToString());
                        string SQLType = GetSQLDataType(dr_col["xtypename"].ToString()).ToString();
                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew Parameter(Types." + SQLType + ", ParameterDirection.IN, " + GetCanonicalIdentifier(ColName) + ((Type == "boolean") ? " ? 1 : 0" : "") + ")");
                    }
                    sb.AppendLine(");");

                    sb.AppendLine("");
                    sb.AppendLine("\t\t\treturn " + GetDataTypeForConvert(ReturnType) + "result;");
                    sb.AppendLine("\t\t}");
                }
                else
                {
                    // Open 返回表数据类型的特殊函数
                    sb.Append("\t\tpublic static DataSet " + GetCanonicalIdentifier(FunctionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")));

                    for (int j = 0; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string ColName = dr_col["name"].ToString();

                        if (!ColName.StartsWith("@"))
                        {
                            continue;
                        }

                        ColName = ColName.Substring(1, ColName.Length - 1);

                        string Type = GetDataType(dr_col["xtypename"].ToString());

                        if ((j > 1) || !m_isUseConnectionStringConfig)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(Type + " " + GetCanonicalIdentifier(ColName));
                    }

                    sb.AppendLine(") throws SQLException, DataException {");

                    sb.Append("\t\t\treturn SQLServer.executeQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"select * from \" + SQLServer.getObjectFullName(\"" + GetBracketsedObjectName(FunctionName) + "\") + \"(");

                    bool hasParamter = false;

                    for (int j = 0; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string ColName = dr_col["name"].ToString();

                        if (!ColName.StartsWith("@"))
                        {
                            continue;
                        }

                        if (hasParamter)
                        {
                            sb.Append(", ");
                        }

                        sb.Append("?");

                        hasParamter = true;
                    }

                    sb.Append(")\"");

                    for (int j = 0; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string ColName = dr_col["name"].ToString();

                        if (!ColName.StartsWith("@"))
                        {
                            continue;
                        }

                        ColName = ColName.Substring(1, ColName.Length - 1);

                        string Type = GetDataType(dr_col["xtypename"].ToString());
                        string SQLType = GetSQLDataType(dr_col["xtypename"].ToString()).ToString();

                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew Parameter(Types." + SQLType + ", ParameterDirection.IN, " + GetCanonicalIdentifier(ColName) + ((Type == "boolean") ? " ? 1 : 0" : "") + ")");
                    }
                    sb.AppendLine(");");
                    sb.AppendLine("\t\t}");
                }
                // Builder End.

                if (i < dt.Rows.Count - 1)
                    sb.AppendLine("");
            }
        }

        private void Procedures(ref StringBuilder sb)
        {
            DataTable dt = Shove.Database.MSSQL.Select(ConnStr, "Select [name], [id] from sysobjects where OBJECTPROPERTY(id, N'IsProcedure') = 1 and OBJECTPROPERTY(id,N'IsMSShipped')=0 order by [name]");
            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string ProcedureName = dr["Name"].ToString();

                if ((ProcedureName == "sp_upgraddiagrams") || (ProcedureName == "sp_helpdiagrams") || (ProcedureName == "sp_helpdiagramdefinition") || (ProcedureName == "sp_creatediagram") || (ProcedureName == "sp_renamediagram") || (ProcedureName == "sp_alterdiagram") || (ProcedureName == "sp_dropdiagram"))
                {
                    continue;
                }

                //Procedure Class Builder...
                DataTable dt_col = Shove.Database.MSSQL.Select(ConnStr, "SELECT a.name, a.length, COLUMNPROPERTY(a.id, a.name, 'IsIdentity') IsIdentity, b.name AS xtypename, a.isoutparam FROM syscolumns a LEFT OUTER JOIN systypes b ON a.xtype = b.xtype WHERE (a.id = " + dr["id"].ToString() + ") and (lower(b.name) <> 'sysname') ORDER BY a.colorder");
                if (dt_col == null)
                    continue;

                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(ProcedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")) + ", DataSet ds, List<Object> outParameterValues");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];
                    string ColName = dr_col["name"].ToString();
                    ColName = ColName.Substring(1, ColName.Length - 1);
                    string Type = GetDataType(dr_col["xtypename"].ToString());
                    bool isOutput = int.Parse(dr_col["isoutparam"].ToString()) == 0 ? false : true;
                    if ((j > 0) || !m_isUseConnectionStringConfig)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(Type + " " + GetCanonicalIdentifier(ColName));
                }
                sb.AppendLine(") throws SQLException, DataException {");
                sb.Append("\t\t\tint result = SQLServer.executeProcedure(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(ProcedureName) + "\", ds, outParameterValues");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];
                    string ColName = dr_col["name"].ToString();
                    ColName = ColName.Substring(1, ColName.Length - 1);
                    string Type = GetDataType(dr_col["xtypename"].ToString());
                    string SQLType = GetSQLDataType(dr_col["xtypename"].ToString()).ToString();
                    bool isOutput = int.Parse(dr_col["isoutparam"].ToString()) == 0 ? false : true;
                    sb.AppendLine(",");

                    sb.Append("\t\t\t\tnew Parameter(Types." + SQLType + ", " + (isOutput ? "ParameterDirection.OUT" : "ParameterDirection.IN") + ", " + GetCanonicalIdentifier(ColName) + ")");
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
                case "bigint":
                    Result = "long";
                    break;
                case "binary":
                    Result = "byte[]";
                    break;
                case "bit":
                    Result = "boolean";
                    break;
                case "char":
                    Result = "String";
                    break;
                case "datetime":
                    Result = "Timestamp";
                    break;
                case "decimal":
                    Result = "BigDecimal";
                    break;
                case "float":
                    Result = "float";
                    break;
                case "image":
                    Result = "byte[]";
                    break;
                case "int":
                    Result = "int";
                    break;
                case "money":
                    Result = "double";
                    break;
                case "nchar":
                    Result = "String";
                    break;
                case "ntext":
                    Result = "String";
                    break;
                case "numeric":
                    Result = "BigDecimal";
                    break;
                case "nvarchar":
                    Result = "String";
                    break;
                case "real":
                    Result = "float";
                    break;
                case "smalldatetime":
                    Result = "Timestamp";
                    break;
                case "smallint":
                    Result = "short";
                    break;
                case "smallmoney":
                    Result = "double";
                    break;
                case "sql_variant":
                    Result = "byte[]";
                    break;
                case "text":
                    Result = "String";
                    break;
                case "timestamp":
                    Result = "Timestamp";
                    break;
                case "tinyint":
                    Result = "short";
                    break;
                case "uniqueidentifier":
                    Result = "String";
                    break;
                case "varbinary":
                    Result = "byte[]";
                    break;
                case "varchar":
                    Result = "String";
                    break;
                case "xml":
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
            SQLType = SQLType.Trim().ToLower();
            string Result = "VARCHAR";

            switch (SQLType)
            {
                case "bigint":
                    Result = "BIGINT";
                    break;
                case "binary":
                    Result = "BINARY";
                    break;
                case "bit":
                    Result = "BIT";
                    break;
                case "char":
                    Result = "CHAR";
                    break;
                case "datetime":
                    Result = "TIMESTAMP";
                    break;
                case "decimal":
                    Result = "DECIMAL";
                    break;
                case "float":
                    Result = "FLOAT";
                    break;
                case "image":
                    Result = "BINARY";
                    break;
                case "int":
                    Result = "INTEGER";
                    break;
                case "money":
                    Result = "DOUBLE";
                    break;
                case "nchar":
                    Result = "NCHAR";
                    break;
                case "ntext":
                    Result = "NVARCHAR";
                    break;
                case "numeric":
                    Result = "NUMERIC";
                    break;
                case "nvarchar":
                    Result = "NVARCHAR";
                    break;
                case "real":
                    Result = "FLOAT";
                    break;
                case "smalldatetime":
                    Result = "TIMESTAMP";
                    break;
                case "smallint":
                    Result = "SMALLINT";
                    break;
                case "smallmoney":
                    Result = "DOUBLE";
                    break;
                case "sql_variant":
                    Result = "VARCHAR";
                    break;
                case "text":
                    Result = "VARCHAR";
                    break;
                case "timestamp":
                    Result = "TIMESTAMP";
                    break;
                case "tinyint":
                    Result = "TINYINT";
                    break;
                case "uniqueidentifier":
                    Result = "VARCHAR";
                    break;
                case "varbinary":
                    Result = "VARBINARY";
                    break;
                case "varchar":
                    Result = "VARCHAR";
                    break;
                case "xml":
                    Result = "SQLXML";
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
            IdentifierName = IdentifierName.Replace(" ", "_");

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
            if (!input.StartsWith("[") && !input.EndsWith("]"))
                return "[" + input + "]";
            else
                return input;
        }
    }
}