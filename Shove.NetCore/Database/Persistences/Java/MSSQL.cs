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

        string m_Server;
        string m_Database;
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
        /// <param name="database"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="namespaceName"></param>
        /// <param name="isUseConnectionStringConfig"></param>
        /// <param name="isUseConnectionString"></param>
        /// <param name="isWithTables"></param>
        /// <param name="isWithViews"></param>
        /// <param name="isWithProcedures"></param>
        /// <param name="isWithFunction"></param>
        public MSSQL(string server, string database, string user, string password, string namespaceName, bool isUseConnectionStringConfig, bool isUseConnectionString, bool isWithTables, bool isWithViews, bool isWithProcedures, bool isWithFunction)
        {
            m_Server = server;
            m_Database = database;
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

            ConnStr = Database.MSSQL.BuildConnectString(m_Server, m_Database, m_User, m_Password);

            SqlConnection conn = DatabaseAccess.CreateDataConnection<SqlConnection>(ConnStr);

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
            DataTable dt = Database.MSSQL.Select(ConnStr, "Select [name], [id] from sysobjects where OBJECTPROPERTY(id, N'IsUserTable') = 1 and OBJECTPROPERTY(id,N'IsMSShipped')=0 order by [name]");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string tableName = dr["Name"].ToString();

                if (tableName == "sysdiagrams")
                {
                    continue;
                }

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(tableName) + " extends SQLServerTable {");

                DataTable dt_col = Database.MSSQL.Select(ConnStr, "SELECT a.name, a.length, COLUMNPROPERTY(a.id, a.name, 'IsIdentity') IsIdentity, b.name AS xtypename, a.isoutparam FROM syscolumns a LEFT OUTER JOIN systypes b ON a.xtype = b.xtype WHERE (a.id = " + dr["id"].ToString() + ") and (lower(b.name) <> 'sysname') ORDER BY a.colorder");

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

                    string colName = dr_col["name"].ToString();

                    sb.AppendLine("\t\t\tpublic Field " + GetCanonicalIdentifier(colName) + " = new Field(this, \"" + GetBracketsedObjectName(colName) + "\", Types." + GetSQLDataType(dr_col["xtypename"].ToString()) + ", " + ((dr_col["IsIdentity"].ToString() == "1") ? "true" : "false") + ");");
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
            DataTable dt = Database.MSSQL.Select(ConnStr, "Select [name], [id] from sysobjects where OBJECTPROPERTY(id, N'IsView') = 1 and OBJECTPROPERTY(id,N'IsMSShipped')=0 order by [name]");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string viewName = dr["Name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(viewName) + " extends SQLServerView {");
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
            DataTable dt = Database.MSSQL.Select(ConnStr, "Select [name], [id] from sysobjects where xtype in (N'FN', N'IF', N'TF', N'FS') and OBJECTPROPERTY(id,N'IsMSShipped')=0 order by [name]");
            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string functionName = dr["Name"].ToString();

                if (functionName == "fn_diagramobjects")
                {
                    continue;
                }

                //Function Builder...
                DataTable dt_col = Database.MSSQL.Select(ConnStr, "SELECT a.name, a.length, COLUMNPROPERTY(a.id, a.name, 'IsIdentity') IsIdentity, b.name AS xtypename, a.isoutparam FROM syscolumns a LEFT OUTER JOIN systypes b ON a.xtype = b.xtype WHERE (a.id = " + dr["id"].ToString() + ") and (lower(b.name) <> 'sysname') ORDER BY a.colorder");
                if (dt_col == null)
                    continue;
                if (dt_col.Rows.Count < 1)
                    continue;

                if (dt_col.Rows[0]["name"].ToString() == "")
                {
                    // call 普通函数，返回除 table 以外的数据类型
                    string ReturnType = GetDataType(dt_col.Rows[0]["xtypename"].ToString());
                    string ReturnSQLType = GetSQLDataType(dt_col.Rows[0]["xtypename"].ToString());

                    sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(functionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")));
                    for (int j = 1; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string colName = dr_col["name"].ToString();
                        colName = colName.Substring(1, colName.Length - 1);
                        string Type = GetDataType(dr_col["xtypename"].ToString());
                        if ((j > 1) || !m_isUseConnectionStringConfig)
                            sb.Append(", ");
                        sb.Append(Type + " " + GetCanonicalIdentifier(colName));
                    }
                    sb.AppendLine(") throws SQLException {");

                    sb.AppendLine("\t\t\tObject result = SQLServer.executeFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(functionName) + "\", ");
                    sb.Append("\t\t\t\tnew Parameter(Types." + ReturnSQLType + ", ParameterDirection.RETURN, null)"); 
                    for (int j = 1; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string colName = dr_col["name"].ToString();
                        colName = colName.Substring(1, colName.Length - 1);
                        string Type = GetDataType(dr_col["xtypename"].ToString());
                        string SQLType = GetSQLDataType(dr_col["xtypename"].ToString()).ToString();
                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew Parameter(Types." + SQLType + ", ParameterDirection.IN, " + GetCanonicalIdentifier(colName) + ((Type == "boolean") ? " ? 1 : 0" : "") + ")");
                    }
                    sb.AppendLine(");");

                    sb.AppendLine("");
                    sb.AppendLine("\t\t\treturn " + GetDataTypeForConvert(ReturnType) + "result;");
                    sb.AppendLine("\t\t}");
                }
                else
                {
                    // Open 返回表数据类型的特殊函数
                    sb.Append("\t\tpublic static DataSet " + GetCanonicalIdentifier(functionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")));

                    for (int j = 0; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string colName = dr_col["name"].ToString();

                        if (!colName.StartsWith("@", System.StringComparison.Ordinal))
                        {
                            continue;
                        }

                        colName = colName.Substring(1, colName.Length - 1);

                        string Type = GetDataType(dr_col["xtypename"].ToString());

                        if ((j > 1) || !m_isUseConnectionStringConfig)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(Type + " " + GetCanonicalIdentifier(colName));
                    }

                    sb.AppendLine(") throws SQLException, DataException {");

                    sb.Append("\t\t\treturn SQLServer.executeQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"select * from \" + SQLServer.getObjectFullName(\"" + GetBracketsedObjectName(functionName) + "\") + \"(");

                    bool hasParamter = false;

                    for (int j = 0; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string colName = dr_col["name"].ToString();

                        if (!colName.StartsWith("@", System.StringComparison.Ordinal))
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
                        string colName = dr_col["name"].ToString();

                        if (!colName.StartsWith("@", System.StringComparison.Ordinal))
                        {
                            continue;
                        }

                        colName = colName.Substring(1, colName.Length - 1);

                        string Type = GetDataType(dr_col["xtypename"].ToString());
                        string SQLType = GetSQLDataType(dr_col["xtypename"].ToString()).ToString();

                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew Parameter(Types." + SQLType + ", ParameterDirection.IN, " + GetCanonicalIdentifier(colName) + ((Type == "boolean") ? " ? 1 : 0" : "") + ")");
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
            DataTable dt = Database.MSSQL.Select(ConnStr, "Select [name], [id] from sysobjects where OBJECTPROPERTY(id, N'IsProcedure') = 1 and OBJECTPROPERTY(id,N'IsMSShipped')=0 order by [name]");
            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string procedureName = dr["Name"].ToString();

                if ((procedureName == "sp_upgraddiagrams") || (procedureName == "sp_helpdiagrams") || (procedureName == "sp_helpdiagramdefinition") || (procedureName == "sp_creatediagram") || (procedureName == "sp_renamediagram") || (procedureName == "sp_alterdiagram") || (procedureName == "sp_dropdiagram"))
                {
                    continue;
                }

                //Procedure Class Builder...
                DataTable dt_col = Database.MSSQL.Select(ConnStr, "SELECT a.name, a.length, COLUMNPROPERTY(a.id, a.name, 'IsIdentity') IsIdentity, b.name AS xtypename, a.isoutparam FROM syscolumns a LEFT OUTER JOIN systypes b ON a.xtype = b.xtype WHERE (a.id = " + dr["id"].ToString() + ") and (lower(b.name) <> 'sysname') ORDER BY a.colorder");
                if (dt_col == null)
                    continue;

                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(procedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "Connection conn")) + ", DataSet ds, List<Object> outParameterValues");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];
                    string colName = dr_col["name"].ToString();
                    colName = colName.Substring(1, colName.Length - 1);
                    string Type = GetDataType(dr_col["xtypename"].ToString());
                    bool isOutput = int.Parse(dr_col["isoutparam"].ToString()) == 0 ? false : true;
                    if ((j > 0) || !m_isUseConnectionStringConfig)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(Type + " " + GetCanonicalIdentifier(colName));
                }
                sb.AppendLine(") throws SQLException, DataException {");
                sb.Append("\t\t\tint result = SQLServer.executeProcedure(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + GetBracketsedObjectName(procedureName) + "\", ds, outParameterValues");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];
                    string colName = dr_col["name"].ToString();
                    colName = colName.Substring(1, colName.Length - 1);
                    string Type = GetDataType(dr_col["xtypename"].ToString());
                    string SQLType = GetSQLDataType(dr_col["xtypename"].ToString()).ToString();
                    bool isOutput = int.Parse(dr_col["isoutparam"].ToString()) == 0 ? false : true;
                    sb.AppendLine(",");

                    sb.Append("\t\t\t\tnew Parameter(Types." + SQLType + ", " + (isOutput ? "ParameterDirection.OUT" : "ParameterDirection.IN") + ", " + GetCanonicalIdentifier(colName) + ")");
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
                case "bigint":
                    result = "long";
                    break;
                case "binary":
                    result = "byte[]";
                    break;
                case "bit":
                    result = "boolean";
                    break;
                case "char":
                    result = "String";
                    break;
                case "datetime":
                    result = "Timestamp";
                    break;
                case "decimal":
                    result = "BigDecimal";
                    break;
                case "float":
                    result = "float";
                    break;
                case "image":
                    result = "byte[]";
                    break;
                case "int":
                    result = "int";
                    break;
                case "money":
                    result = "double";
                    break;
                case "nchar":
                    result = "String";
                    break;
                case "ntext":
                    result = "String";
                    break;
                case "numeric":
                    result = "BigDecimal";
                    break;
                case "nvarchar":
                    result = "String";
                    break;
                case "real":
                    result = "float";
                    break;
                case "smalldatetime":
                    result = "Timestamp";
                    break;
                case "smallint":
                    result = "short";
                    break;
                case "smallmoney":
                    result = "double";
                    break;
                case "sql_variant":
                    result = "byte[]";
                    break;
                case "text":
                    result = "String";
                    break;
                case "timestamp":
                    result = "Timestamp";
                    break;
                case "tinyint":
                    result = "short";
                    break;
                case "uniqueidentifier":
                    result = "String";
                    break;
                case "varbinary":
                    result = "byte[]";
                    break;
                case "varchar":
                    result = "String";
                    break;
                case "xml":
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
            SQLType = SQLType.Trim().ToLower();
            string result = "VARCHAR";

            switch (SQLType)
            {
                case "bigint":
                    result = "BIGINT";
                    break;
                case "binary":
                    result = "BINARY";
                    break;
                case "bit":
                    result = "BIT";
                    break;
                case "char":
                    result = "CHAR";
                    break;
                case "datetime":
                    result = "TIMESTAMP";
                    break;
                case "decimal":
                    result = "DECIMAL";
                    break;
                case "float":
                    result = "FLOAT";
                    break;
                case "image":
                    result = "BINARY";
                    break;
                case "int":
                    result = "INTEGER";
                    break;
                case "money":
                    result = "DOUBLE";
                    break;
                case "nchar":
                    result = "NCHAR";
                    break;
                case "ntext":
                    result = "NVARCHAR";
                    break;
                case "numeric":
                    result = "NUMERIC";
                    break;
                case "nvarchar":
                    result = "NVARCHAR";
                    break;
                case "real":
                    result = "FLOAT";
                    break;
                case "smalldatetime":
                    result = "TIMESTAMP";
                    break;
                case "smallint":
                    result = "SMALLINT";
                    break;
                case "smallmoney":
                    result = "DOUBLE";
                    break;
                case "sql_variant":
                    result = "VARCHAR";
                    break;
                case "text":
                    result = "VARCHAR";
                    break;
                case "timestamp":
                    result = "TIMESTAMP";
                    break;
                case "tinyint":
                    result = "TINYINT";
                    break;
                case "uniqueidentifier":
                    result = "VARCHAR";
                    break;
                case "varbinary":
                    result = "VARBINARY";
                    break;
                case "varchar":
                    result = "VARCHAR";
                    break;
                case "xml":
                    result = "SQLXML";
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
            identifierName = identifierName.Replace(" ", "_");

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
            if (!input.StartsWith("[", System.StringComparison.Ordinal) && !input.EndsWith("]", System.StringComparison.Ordinal))
                return "[" + input + "]";
            else
                return input;
        }
    }
}