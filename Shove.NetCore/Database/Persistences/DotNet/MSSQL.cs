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

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(TableName) + " : " + (m_isCLR ? "Utility.TableBase" : "MSSQL.TableBase"));
                sb.AppendLine("\t\t{");

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

                    sb.AppendLine("\t\t\tpublic " + (m_isCLR ? "Utility.Field" : "MSSQL.Field") + " " + GetCanonicalIdentifier(ColName) + ";");
                }
                sb.AppendLine("");

                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(TableName) + "()");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine("\t\t\t\tTableName = \"" + TableName + "\";");
                sb.AppendLine("");

                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];

                    string ColName = dr_col["name"].ToString();

                    sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(ColName) + " = new " + (m_isCLR ? "Utility.Field" : "MSSQL.Field") + "(this, \"" + ColName + "\", \"" + GetCanonicalIdentifier(ColName) + "\", SqlDbType." + GetSQLDataType(dr_col["xtypename"].ToString()) + ", " + ((dr_col["IsIdentity"].ToString() == "1") ? "true" : "false") + ");");
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
            DataTable dt = Shove.Database.MSSQL.Select(ConnStr, "Select [name], [id] from sysobjects where OBJECTPROPERTY(id, N'IsView') = 1 and OBJECTPROPERTY(id,N'IsMSShipped')=0 order by [name]");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string ViewName = dr["Name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(ViewName) + " : " + (m_isCLR ? "Utility.ViewBase" : "MSSQL.ViewBase"));
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(ViewName) + "()");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine("\t\t\t\tViewName = \"" + ViewName + "\";");
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
                    sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(FunctionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "SqlConnection conn")));
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
                    sb.AppendLine(")");
                    sb.AppendLine("\t\t{");
                    sb.Append("\t\t\tobject Result = MSSQL.ExecuteFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + FunctionName + "\"");
                    for (int j = 1; j < dt_col.Rows.Count; j++)
                    {
                        DataRow dr_col = dt_col.Rows[j];
                        string ColName = dr_col["name"].ToString();
                        ColName = ColName.Substring(1, ColName.Length - 1);
                        string Type = GetDataType(dr_col["xtypename"].ToString());
                        string SQLType = GetSQLDataType(dr_col["xtypename"].ToString()).ToString();
                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew MSSQL.Parameter(\"" + ColName + "\", SqlDbType." + SQLType + ", 0, ParameterDirection.Input, " + GetCanonicalIdentifier(ColName) + ((Type == "bool") ? " ? 1 : 0" : "") + ")");
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
                    sb.Append("\t\tpublic static DataTable " + GetCanonicalIdentifier(FunctionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "SqlConnection conn")));

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

                    sb.AppendLine(")");
                    sb.AppendLine("\t\t{");

                    sb.Append("\t\t\treturn MSSQL.Select(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"select * from \" + MSSQL.GetObjectFullName(\"" + FunctionName + "\") + \"(");

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

                        sb.Append(ColName);

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
                        sb.Append("\t\t\t\tnew MSSQL.Parameter(\"" + ColName + "\", SqlDbType." + SQLType + ", 0, ParameterDirection.Input, " + GetCanonicalIdentifier(ColName) + ((Type == "bool") ? " ? 1 : 0" : "") + ")");
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

                // NoQuery
                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(ProcedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "SqlConnection conn")));
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
                    sb.Append((isOutput ? "ref " : "") + Type + " " + GetCanonicalIdentifier(ColName));
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tMSSQL.OutputParameter Outputs = new MSSQL.OutputParameter();");
                sb.AppendLine("");
                sb.Append("\t\t\tint CallResult = MSSQL.ExecuteStoredProcedureNonQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + ProcedureName + "\", ref Outputs");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];
                    string ColName = dr_col["name"].ToString();
                    ColName = ColName.Substring(1, ColName.Length - 1);
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

                    sb.Append("\t\t\t\tnew MSSQL.Parameter(\"" + ColName + "\", SqlDbType." + SQLType + ", " + (isOutput ? Len.ToString() : "0") + ", " + (isOutput ? "ParameterDirection.Output" : "ParameterDirection.Input") + ", " + GetCanonicalIdentifier(ColName) + ")");
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
                    string ColName = dr_col["name"].ToString();
                    ColName = ColName.Substring(1, ColName.Length - 1);
                    string Type = GetDataType(dr_col["xtypename"].ToString());
                    bool isOutput = int.Parse(dr_col["isoutparam"].ToString()) == 0 ? false : true;
                    if (!isOutput)
                        continue;
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\ttry");
                    sb.AppendLine("\t\t\t{");

                    if (Type.ToLower() == "byte[]")
                    {
                        sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(ColName) + " = (byte[])Outputs[\"" + ColName + "\"];");
                    }
                    else
                    {
                        sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(ColName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + ColName + "\"]);");
                    }

                    sb.AppendLine("\t\t\t}");
                    sb.AppendLine("\t\t\tcatch { }");
                }
                sb.AppendLine("");
                sb.AppendLine("\t\t\treturn CallResult;");
                sb.AppendLine("\t\t}");
                sb.AppendLine("");

                // WithQuery
                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(ProcedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "SqlConnection conn")));
                if (!m_isUseConnectionStringConfig)
                {
                    sb.Append(", ");
                }
                sb.Append("ref DataSet ds");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];
                    string ColName = dr_col["name"].ToString();
                    ColName = ColName.Substring(1, ColName.Length - 1);
                    string Type = GetDataType(dr_col["xtypename"].ToString());
                    bool isOutput = int.Parse(dr_col["isoutparam"].ToString()) == 0 ? false : true;
                    sb.Append(", ");
                    sb.Append((isOutput ? "ref " : "") + Type + " " + GetCanonicalIdentifier(ColName));
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tMSSQL.OutputParameter Outputs = new MSSQL.OutputParameter();");
                sb.AppendLine("");
                sb.Append("\t\t\tint CallResult = MSSQL.ExecuteStoredProcedureWithQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + ProcedureName + "\", ref ds, ref Outputs");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];
                    string ColName = dr_col["name"].ToString();
                    ColName = ColName.Substring(1, ColName.Length - 1);
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

                    sb.Append("\t\t\t\tnew MSSQL.Parameter(\"" + ColName + "\", SqlDbType." + SQLType + ", " + (isOutput ? Len.ToString() : "0") + ", " + (isOutput ? "ParameterDirection.Output" : "ParameterDirection.Input") + ", " + GetCanonicalIdentifier(ColName) + ")");
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
                    string ColName = dr_col["name"].ToString();
                    ColName = ColName.Substring(1, ColName.Length - 1);
                    string Type = GetDataType(dr_col["xtypename"].ToString());
                    bool isOutput = int.Parse(dr_col["isoutparam"].ToString()) == 0 ? false : true;
                    if (!isOutput)
                        continue;
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\ttry");
                    sb.AppendLine("\t\t\t{");

                    if (Type.ToLower() == "byte[]")
                    {
                        sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(ColName) + " = (byte[])Outputs[\"" + ColName + "\"];");
                    }
                    else
                    {
                        sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(ColName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + ColName + "\"]);");
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
            string Result = "string";

            switch (SQLType)
            {
                case "bigint":
                    Result = "long";
                    break;
                case "binary":
                    Result = "byte[]";
                    break;
                case "bit":
                    Result = "bool";
                    break;
                case "char":
                    Result = "string";
                    break;
                case "datetime":
                    Result = "DateTime";
                    break;
                case "decimal":
                    Result = "double";
                    break;
                case "float":
                    Result = "double";
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
                    Result = "string";
                    break;
                case "ntext":
                    Result = "string";
                    break;
                case "numeric":
                    Result = "double";
                    break;
                case "nvarchar":
                    Result = "string";
                    break;
                case "real":
                    Result = "float";
                    break;
                case "smalldatetime":
                    Result = "DateTime";
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
                    Result = "string";
                    break;
                case "timestamp":
                    Result = "DateTime";
                    break;
                case "tinyint":
                    Result = "short";
                    break;
                case "uniqueidentifier":
                    Result = "string";
                    break;
                case "varbinary":
                    Result = "byte[]";
                    break;
                case "varchar":
                    Result = "string";
                    break;
                case "xml":
                    Result = "string";
                    break;
            }

            return Result;
        }

        private SqlDbType GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();
            SqlDbType Result = SqlDbType.Variant;

            switch (SQLType)
            {
                case "bigint":
                    Result = SqlDbType.BigInt;
                    break;
                case "binary":
                    Result = SqlDbType.Binary;
                    break;
                case "bit":
                    Result = SqlDbType.Bit;
                    break;
                case "char":
                    Result = SqlDbType.Char;
                    break;
                case "datetime":
                    Result = SqlDbType.DateTime;
                    break;
                case "decimal":
                    Result = SqlDbType.Decimal;
                    break;
                case "float":
                    Result = SqlDbType.Float;
                    break;
                case "image":
                    Result = SqlDbType.Image;
                    break;
                case "int":
                    Result = SqlDbType.Int;
                    break;
                case "money":
                    Result = SqlDbType.Money;
                    break;
                case "nchar":
                    Result = SqlDbType.NChar;
                    break;
                case "ntext":
                    Result = SqlDbType.NText;
                    break;
                case "numeric":
                    Result = SqlDbType.Float;
                    break;
                case "nvarchar":
                    Result = SqlDbType.NVarChar;
                    break;
                case "real":
                    Result = SqlDbType.Real;
                    break;
                case "smalldatetime":
                    Result = SqlDbType.SmallDateTime;
                    break;
                case "smallint":
                    Result = SqlDbType.SmallInt;
                    break;
                case "smallmoney":
                    Result = SqlDbType.SmallMoney;
                    break;
                case "sql_variant":
                    Result = SqlDbType.Variant;
                    break;
                case "text":
                    Result = SqlDbType.Text;
                    break;
                case "timestamp":
                    Result = SqlDbType.Timestamp;
                    break;
                case "tinyint":
                    Result = SqlDbType.TinyInt;
                    break;
                case "uniqueidentifier":
                    Result = SqlDbType.UniqueIdentifier;
                    break;
                case "varbinary":
                    Result = SqlDbType.VarBinary;
                    break;
                case "varchar":
                    Result = SqlDbType.VarChar;
                    break;
                case "xml":
                    Result = SqlDbType.Xml;
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
                case "float":
                    Result = "System.Convert.ToSingle";
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

            return IdentifierName;
        }
    }
}