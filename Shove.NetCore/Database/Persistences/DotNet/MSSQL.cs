using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace Shove.Database.Persistences
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
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Data;");
            sb.AppendLine("using System.Data.SqlClient;");
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("");
            sb.AppendLine("using Shove.Database;");
            sb.AppendLine("");

            sb.AppendLine("namespace " + (m_NamespaceName == "" ? "DAL" : (m_NamespaceName + ".DAL")));
            sb.AppendLine("{");
            sb.AppendLine("\t/*");
            sb.AppendLine("\tProgram Name: Shove.DAL.30");
            sb.AppendLine("\tProgram Version: 3.0");
            sb.AppendLine("\tWriter By: 3km.shovesoft.shove (zhou changjun)");
            sb.AppendLine("\tRelease Time: 2008.9.1");
            sb.AppendLine("");
            sb.AppendLine("\tSystem Request: Shove.dll");
            sb.AppendLine("\tAll Rights saved.");
            sb.AppendLine("\t*/");
            sb.AppendLine("");
            if (m_isUseConnectionStringConfig)
            {
                sb.AppendLine("");
                sb.AppendLine("\t// Please Add a Key in Web.config File's appSetting section, Exemple:");
                sb.AppendLine("\t// <add key=\"ConnectionString\" value=\"server=(local);User id=sa;Pwd=;Database=master\" />");
                sb.AppendLine("");
                sb.AppendLine("");
            }

            #region Table

            if (m_isWithTables)
            {
                sb.AppendLine("\tpublic class Tables");

                sb.AppendLine("\t{");

                Tables(ref sb, false);

                sb.AppendLine("\t}");
                sb.AppendLine("");
            }

            #endregion

            #region Viwes

            if (m_isWithViews)
            {
                sb.AppendLine("\tpublic class Views");
                sb.AppendLine("\t{");

                Views(ref sb, false);

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

        private void Tables(ref StringBuilder sb, bool m_isCLR)
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

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(tableName) + " : " + (m_isCLR ? "Utility.TableBase" : "MSSQL.TableBase"));
                sb.AppendLine("\t\t{");

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

                    sb.AppendLine("\t\t\tpublic " + (m_isCLR ? "Utility.Field" : "MSSQL.Field") + " " + GetCanonicalIdentifier(colName) + ";");
                }
                sb.AppendLine("");

                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(tableName) + "()");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine("\t\t\t\tTableName = \"" + tableName + "\";");
                sb.AppendLine("");

                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];

                    string colName = dr_col["name"].ToString();

                    sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(colName) + " = new " + (m_isCLR ? "Utility.Field" : "MSSQL.Field") + "(this, \"" + colName + "\", \"" + GetCanonicalIdentifier(colName) + "\", SqlDbType." + GetSQLDataType(dr_col["xtypename"].ToString()) + ", " + ((dr_col["IsIdentity"].ToString() == "1") ? "true" : "false") + ");");
                }

                sb.AppendLine("\t\t\t}");
                sb.AppendLine("\t\t}");

                if (i < dt.Rows.Count - 1)
                {
                    sb.AppendLine("");
                }
            }
        }

        private void Views(ref StringBuilder sb, bool m_isCLR)
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

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(viewName) + " : " + (m_isCLR ? "Utility.ViewBase" : "MSSQL.ViewBase"));
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(viewName) + "()");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine("\t\t\t\tViewName = \"" + viewName + "\";");
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
                    sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(functionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "SqlConnection conn")));
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
                    sb.AppendLine(")");
                    sb.AppendLine("\t\t{");
                    sb.Append("\t\t\tobject Result = MSSQL.ExecuteFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + functionName + "\"");
                    for (int j = 1; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string colName = dr_col["name"].ToString();
                        colName = colName.Substring(1, colName.Length - 1);
                        string Type = GetDataType(dr_col["xtypename"].ToString());
                        string SQLType = GetSQLDataType(dr_col["xtypename"].ToString()).ToString();
                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew MSSQL.Parameter(\"" + colName + "\", SqlDbType." + SQLType + ", 0, ParameterDirection.Input, " + GetCanonicalIdentifier(colName) + ((Type == "bool") ? " ? 1 : 0" : "") + ")");
                    }
                    if (dt_col.Rows.Count == 1)
                    {
                        sb.AppendLine(");");
                    }
                    else
                    {
                        sb.AppendLine("");
                        sb.AppendLine("\t\t\t\t);");
                    }

                    sb.AppendLine("");
                    sb.AppendLine("\t\t\treturn " + GetDataTypeForConvert(ReturnType) + "(Result);");
                    sb.AppendLine("\t\t}");
                }
                else
                {
                    // Open 返回表数据类型的特殊函数
                    sb.Append("\t\tpublic static DataTable " + GetCanonicalIdentifier(functionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "SqlConnection conn")));

                    for (int j = 0; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string colName = dr_col["name"].ToString();

                        if (!colName.StartsWith("@", StringComparison.Ordinal))
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

                    sb.AppendLine(")");
                    sb.AppendLine("\t\t{");

                    sb.Append("\t\t\treturn MSSQL.Select(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"select * from \" + MSSQL.GetObjectFullName(\"" + functionName + "\") + \"(");

                    bool hasParamter = false;

                    for (int j = 0; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string colName = dr_col["name"].ToString();

                        if (!colName.StartsWith("@", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        if (hasParamter)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(colName);

                        hasParamter = true;
                    }

                    sb.Append(")\"");

                    for (int j = 0; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string colName = dr_col["name"].ToString();

                        if (!colName.StartsWith("@", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        colName = colName.Substring(1, colName.Length - 1);

                        string Type = GetDataType(dr_col["xtypename"].ToString());
                        string SQLType = GetSQLDataType(dr_col["xtypename"].ToString()).ToString();

                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew MSSQL.Parameter(\"" + colName + "\", SqlDbType." + SQLType + ", 0, ParameterDirection.Input, " + GetCanonicalIdentifier(colName) + ((Type == "bool") ? " ? 1 : 0" : "") + ")");
                    }
                    if (dt_col.Rows.Count == 1)
                        sb.AppendLine(");");
                    else
                    {
                        sb.AppendLine("");
                        sb.AppendLine("\t\t\t\t);");
                    }

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

                // NoQuery
                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(procedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "SqlConnection conn")));
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
                    sb.Append((isOutput ? "ref " : "") + Type + " " + GetCanonicalIdentifier(colName));
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tMSSQL.OutputParameter Outputs = new MSSQL.OutputParameter();");
                sb.AppendLine("");
                sb.Append("\t\t\tint CallResult = MSSQL.ExecuteStoredProcedureNonQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + procedureName + "\", ref Outputs");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];
                    string colName = dr_col["name"].ToString();
                    colName = colName.Substring(1, colName.Length - 1);
                    string Type = GetDataType(dr_col["xtypename"].ToString());
                    string SQLType = GetSQLDataType(dr_col["xtypename"].ToString()).ToString();
                    long Len = long.Parse(dr_col["length"].ToString());
                    if (((SQLType == "NChar") || (SQLType == "NVarChar")) && Len > 4000)
                    {
                        Len = 4000;
                    }
                    // varbinary, varchar, ncarchar 三种类型如果是 max 长度, isOutput 时，数据库中的长度可能是 -1,需要换为 2^30-1 的长度
                    if (((SQLType == "VarChar") || (SQLType == "NVarChar") || (SQLType == "VarBinary")) && Len <= 0)
                    {
                        Len = (long)Math.Pow(2, 30) - 1;
                    }

                    bool isOutput = int.Parse(dr_col["isoutparam"].ToString()) == 0 ? false : true;
                    sb.AppendLine(",");

                    sb.Append("\t\t\t\tnew MSSQL.Parameter(\"" + colName + "\", SqlDbType." + SQLType + ", " + (isOutput ? Len.ToString() : "0") + ", " + (isOutput ? "ParameterDirection.Output" : "ParameterDirection.Input") + ", " + GetCanonicalIdentifier(colName) + ")");
                }
                if (dt_col.Rows.Count == 0)
                {
                    sb.AppendLine(");");
                }
                else
                {
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\t\t);");
                }

                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];
                    string colName = dr_col["name"].ToString();
                    colName = colName.Substring(1, colName.Length - 1);
                    string Type = GetDataType(dr_col["xtypename"].ToString());
                    bool isOutput = int.Parse(dr_col["isoutparam"].ToString()) == 0 ? false : true;
                    if (!isOutput)
                        continue;
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\ttry");
                    sb.AppendLine("\t\t\t{");

                    if (Type.ToLower() == "byte[]")
                    {
                        sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(colName) + " = (byte[])Outputs[\"" + colName + "\"];");
                    }
                    else
                    {
                        sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(colName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + colName + "\"]);");
                    }

                    sb.AppendLine("\t\t\t}");
                    sb.AppendLine("\t\t\tcatch { }");
                }
                sb.AppendLine("");
                sb.AppendLine("\t\t\treturn CallResult;");
                sb.AppendLine("\t\t}");
                sb.AppendLine("");

                // WithQuery
                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(procedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "SqlConnection conn")));
                if (!m_isUseConnectionStringConfig)
                {
                    sb.Append(", ");
                }
                sb.Append("ref DataSet ds");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];
                    string colName = dr_col["name"].ToString();
                    colName = colName.Substring(1, colName.Length - 1);
                    string Type = GetDataType(dr_col["xtypename"].ToString());
                    bool isOutput = int.Parse(dr_col["isoutparam"].ToString()) == 0 ? false : true;
                    sb.Append(", ");
                    sb.Append((isOutput ? "ref " : "") + Type + " " + GetCanonicalIdentifier(colName));
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tMSSQL.OutputParameter Outputs = new MSSQL.OutputParameter();");
                sb.AppendLine("");
                sb.Append("\t\t\tint CallResult = MSSQL.ExecuteStoredProcedureWithQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + procedureName + "\", ref ds, ref Outputs");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];
                    string colName = dr_col["name"].ToString();
                    colName = colName.Substring(1, colName.Length - 1);
                    string Type = GetDataType(dr_col["xtypename"].ToString());
                    string SQLType = GetSQLDataType(dr_col["xtypename"].ToString()).ToString();
                    long Len = long.Parse(dr_col["length"].ToString());
                    if (((SQLType == "NChar") || (SQLType == "NVarChar")) && Len > 4000)
                    {
                        Len = 4000;
                    }
                    // varbinary, varchar, ncarchar 三种类型如果是 max 长度, isOutput 时，数据库中的长度可能是 -1,需要换为 2^30-1 的长度
                    if (((SQLType == "VarChar") || (SQLType == "NVarChar") || (SQLType == "VarBinary")) && Len <= 0)
                    {
                        Len = (long)Math.Pow(2, 30) - 1;
                    }

                    bool isOutput = int.Parse(dr_col["isoutparam"].ToString()) == 0 ? false : true;
                    sb.AppendLine(",");

                    sb.Append("\t\t\t\tnew MSSQL.Parameter(\"" + colName + "\", SqlDbType." + SQLType + ", " + (isOutput ? Len.ToString() : "0") + ", " + (isOutput ? "ParameterDirection.Output" : "ParameterDirection.Input") + ", " + GetCanonicalIdentifier(colName) + ")");
                }
                if (dt_col.Rows.Count == 0)
                {
                    sb.AppendLine(");");
                }
                else
                {
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\t\t);");
                }

                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];
                    string colName = dr_col["name"].ToString();
                    colName = colName.Substring(1, colName.Length - 1);
                    string Type = GetDataType(dr_col["xtypename"].ToString());
                    bool isOutput = int.Parse(dr_col["isoutparam"].ToString()) == 0 ? false : true;
                    if (!isOutput)
                        continue;
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\ttry");
                    sb.AppendLine("\t\t\t{");

                    if (Type.ToLower() == "byte[]")
                    {
                        sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(colName) + " = (byte[])Outputs[\"" + colName + "\"];");
                    }
                    else
                    {
                        sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(colName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + colName + "\"]);");
                    }

                    sb.AppendLine("\t\t\t}");
                    sb.AppendLine("\t\t\tcatch { }");
                }
                sb.AppendLine("");
                sb.AppendLine("\t\t\treturn CallResult;");
                sb.AppendLine("\t\t}");
                // Builder End.

                if (i < dt.Rows.Count - 1)
                    sb.AppendLine("");
            }
        }

        private string GetDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();
            string result = "string";

            switch (SQLType)
            {
                case "bigint":
                    result = "long";
                    break;
                case "binary":
                    result = "byte[]";
                    break;
                case "bit":
                    result = "bool";
                    break;
                case "char":
                    result = "string";
                    break;
                case "datetime":
                    result = "DateTime";
                    break;
                case "decimal":
                    result = "double";
                    break;
                case "float":
                    result = "double";
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
                    result = "string";
                    break;
                case "ntext":
                    result = "string";
                    break;
                case "numeric":
                    result = "double";
                    break;
                case "nvarchar":
                    result = "string";
                    break;
                case "real":
                    result = "float";
                    break;
                case "smalldatetime":
                    result = "DateTime";
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
                    result = "string";
                    break;
                case "timestamp":
                    result = "DateTime";
                    break;
                case "tinyint":
                    result = "short";
                    break;
                case "uniqueidentifier":
                    result = "string";
                    break;
                case "varbinary":
                    result = "byte[]";
                    break;
                case "varchar":
                    result = "string";
                    break;
                case "xml":
                    result = "string";
                    break;
            }

            return result;
        }

        private SqlDbType GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();
            SqlDbType result = SqlDbType.Variant;

            switch (SQLType)
            {
                case "bigint":
                    result = SqlDbType.BigInt;
                    break;
                case "binary":
                    result = SqlDbType.Binary;
                    break;
                case "bit":
                    result = SqlDbType.Bit;
                    break;
                case "char":
                    result = SqlDbType.Char;
                    break;
                case "datetime":
                    result = SqlDbType.DateTime;
                    break;
                case "decimal":
                    result = SqlDbType.Decimal;
                    break;
                case "float":
                    result = SqlDbType.Float;
                    break;
                case "image":
                    result = SqlDbType.Image;
                    break;
                case "int":
                    result = SqlDbType.Int;
                    break;
                case "money":
                    result = SqlDbType.Money;
                    break;
                case "nchar":
                    result = SqlDbType.NChar;
                    break;
                case "ntext":
                    result = SqlDbType.NText;
                    break;
                case "numeric":
                    result = SqlDbType.Float;
                    break;
                case "nvarchar":
                    result = SqlDbType.NVarChar;
                    break;
                case "real":
                    result = SqlDbType.Real;
                    break;
                case "smalldatetime":
                    result = SqlDbType.SmallDateTime;
                    break;
                case "smallint":
                    result = SqlDbType.SmallInt;
                    break;
                case "smallmoney":
                    result = SqlDbType.SmallMoney;
                    break;
                case "sql_variant":
                    result = SqlDbType.Variant;
                    break;
                case "text":
                    result = SqlDbType.Text;
                    break;
                case "timestamp":
                    result = SqlDbType.Timestamp;
                    break;
                case "tinyint":
                    result = SqlDbType.TinyInt;
                    break;
                case "uniqueidentifier":
                    result = SqlDbType.UniqueIdentifier;
                    break;
                case "varbinary":
                    result = SqlDbType.VarBinary;
                    break;
                case "varchar":
                    result = SqlDbType.VarChar;
                    break;
                case "xml":
                    result = SqlDbType.Xml;
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
                case "float":
                    result = "System.Convert.ToSingle";
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

            return identifierName;
        }
    }
}