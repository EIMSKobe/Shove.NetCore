using System;
using System.Collections.Generic;
using System.Text;
using System.Data.OracleClient;
using System.Data;
using System.Text.RegularExpressions;

namespace Shove.Database.Persistences
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
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Data;");
            sb.AppendLine("using System.Data.OracleClient;");
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("");
            sb.AppendLine("using Shove.Database;");
            sb.AppendLine("");

            sb.AppendLine("namespace " + (m_NamespaceName == "" ? "DAL" : (m_NamespaceName + ".DAL")));
            sb.AppendLine("{");
            sb.AppendLine("\t/*");
            sb.AppendLine("\tProgram Name: Shove.DAL.30 for Oracle");
            sb.AppendLine("\tProgram Version: 3.0");
            sb.AppendLine("\tWriter By: 3km.shovesoft.shove (zhou changjun)");
            sb.AppendLine("\tRelease Time: 2011.4.15");
            sb.AppendLine("");
            sb.AppendLine("\tSystem Request: Shove.dll");
            sb.AppendLine("\tAll Rights saved.");
            sb.AppendLine("\t*/");
            sb.AppendLine("");
            if (m_isUseConnectionStringConfig)
            {
                sb.AppendLine("");
                sb.AppendLine("\t// Please Add a Key in Web.config File's appSetting section, Exemple:");
                sb.AppendLine("\t// <add key=\"ConnectionString\" value=\"Data Source=orcl;user id=sysdba;password=;\" />");
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
            DataTable dt = Shove.Database.Oracle.Select(ConnStr, "select table_name from user_tables order by table_name");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string TableName = dr["table_name"].ToString();
                
                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(TableName) + " : Oracle.TableBase");
                sb.AppendLine("\t\t{");

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

                    sb.AppendLine("\t\t\tpublic Oracle.Field " + GetCanonicalIdentifier(ColName) + ";");
                }
                sb.AppendLine("");

                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(TableName) + "()");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine("\t\t\t\tTableName = \"" + TableName + "\";");
                sb.AppendLine("");

                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];

                    string ColName = dr_col["column_name"].ToString();

                    sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(ColName) + " = new Oracle.Field(this, \"" + ColName + "\", \"" + GetCanonicalIdentifier(ColName) + "\", OracleType." + GetSQLDataType(dr_col["data_type"].ToString()) + ");");
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
            DataTable dt = Shove.Database.Oracle.Select(ConnStr, "select view_name from user_views order by view_name");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string ViewName = dr["view_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(ViewName) + " : Oracle.ViewBase");
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
                bool hasOutput = false;
                string ReturnType = GetDataType(dt_col.Rows[0]["data_type"].ToString());
                sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(FunctionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "OracleConnection conn")));
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 1) || !m_isUseConnectionStringConfig)
                        sb.Append(", ");
                    sb.Append((In_Out != "IN" ? "ref " : "") + Type + " " + GetCanonicalIdentifier(ColName));
                    if (In_Out != "IN")
                    {
                        hasOutput = true;
                    }
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tOracle.OutputParameter Outputs = new Oracle.OutputParameter();");
                sb.Append("\t\t\tobject Result = Oracle.ExecuteFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + FunctionName + "\", OracleType.");
                string ReturnLength = dt_col.Rows[0]["data_length"].ToString();
                if (string.IsNullOrEmpty(ReturnLength))
                {
                    ReturnLength = (GetDataType(dt_col.Rows[0]["data_type"].ToString()) == "string") ? "4000" : "0";
                }
                sb.Append(GetSQLDataType(dt_col.Rows[0]["data_type"].ToString()).ToString() + ", " + ReturnLength + ", ref Outputs");
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    In_Out = ((In_Out == "IN") ? "Input" : ((In_Out == "OUT") ? "Output" : "InputOutput"));
                    sb.AppendLine(",");
                    ReturnLength = dt_col.Rows[j]["data_length"].ToString();
                    if (string.IsNullOrEmpty(ReturnLength))
                    {
                        ReturnLength = (GetDataType(dt_col.Rows[j]["data_type"].ToString()) == "string") ? "4000" : "0";
                    }
                    sb.Append("\t\t\t\tnew Oracle.Parameter(\"" + ColName + "\", OracleType." + SQLType + ", " + ReturnLength + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(ColName) + ")");
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

                if (hasOutput)
                {
                    sb.AppendLine("");
                    for (int j = 1; j < dt_col.Rows.Count; j++)
                    {
                        string ColName = dt_col.Rows[j]["argument_name"].ToString();
                        string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                        string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                        string In_Out = dt_col.Rows[j]["in_out"].ToString();
                        if (In_Out != "IN")
                        {
                            sb.AppendLine("\t\t\t" + GetCanonicalIdentifier(ColName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + ColName + "\"]);");
                        }
                    }
                }

                sb.AppendLine("");
                sb.AppendLine("\t\t\treturn " + GetDataTypeForConvert(ReturnType) + "(Result);");
                sb.AppendLine("\t\t}");

                goto end;
            WithQuery:

                // WithQuery
                hasOutput = false;
                ReturnType = GetDataType(dt_col.Rows[0]["data_type"].ToString());
                sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(FunctionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "OracleConnection conn")));
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 1) || !m_isUseConnectionStringConfig)
                        sb.Append(", ");
                    sb.Append((In_Out != "IN" ? "ref " : "") + Type + " " + GetCanonicalIdentifier(ColName));
                    if (In_Out != "IN")
                    {
                        hasOutput = true;
                    }
                }
                sb.AppendLine(((dt_col.Rows.Count > 1) ? ", " : "") + "ref DataSet ds)");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tOracle.OutputParameter Outputs = new Oracle.OutputParameter();");
                sb.Append("\t\t\tobject Result = Oracle.ExecuteFunctionWithQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + FunctionName + "\", OracleType.");
                ReturnLength = dt_col.Rows[0]["data_length"].ToString();
                if (string.IsNullOrEmpty(ReturnLength))
                {
                    ReturnLength = (GetDataType(dt_col.Rows[0]["data_type"].ToString()) == "string") ? "4000" : "0";
                }
                sb.Append(GetSQLDataType(dt_col.Rows[0]["data_type"].ToString()).ToString() + ", " + ReturnLength + ", ref ds, ref Outputs");
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    In_Out = ((In_Out == "IN") ? "Input" : ((In_Out == "OUT") ? "Output" : "InputOutput"));
                    sb.AppendLine(",");
                    ReturnLength = dt_col.Rows[j]["data_length"].ToString();
                    if (string.IsNullOrEmpty(ReturnLength))
                    {
                        ReturnLength = (GetDataType(dt_col.Rows[j]["data_type"].ToString()) == "string") ? "4000" : "0";
                    }
                    sb.Append("\t\t\t\tnew Oracle.Parameter(\"" + ColName + "\", OracleType." + SQLType + ", " + ReturnLength + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(ColName) + ")");
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

                if (hasOutput)
                {
                    sb.AppendLine("");
                    for (int j = 1; j < dt_col.Rows.Count; j++)
                    {
                        string ColName = dt_col.Rows[j]["argument_name"].ToString();
                        string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                        string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                        string In_Out = dt_col.Rows[j]["in_out"].ToString();
                        if (In_Out != "IN")
                        {
                            sb.AppendLine("\t\t\t" + GetCanonicalIdentifier(ColName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + ColName + "\"]);");
                        }
                    }
                }

                sb.AppendLine("");
                sb.AppendLine("\t\t\treturn " + GetDataTypeForConvert(ReturnType) + "(Result);");
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

                if (isContainOracleTypeCursor(dt_col))
                {
                    goto WithQuery;
                }

                // NonQuery
                bool hasOutput = false;
                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(ProcedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "OracleConnection conn")));
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 0) || !m_isUseConnectionStringConfig)
                        sb.Append(", ");
                    sb.Append((In_Out != "IN" ? "ref " : "") + Type + " " + GetCanonicalIdentifier(ColName));
                    if (In_Out != "IN")
                    {
                        hasOutput = true;
                    }
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tOracle.OutputParameter Outputs = new Oracle.OutputParameter();");
                sb.Append("\t\t\tint Result = Oracle.ExecuteProcedure(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + ProcedureName + "\", ref Outputs");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    In_Out = ((In_Out == "IN") ? "Input" : ((In_Out == "OUT") ? "Output" : "InputOutput"));
                    sb.AppendLine(",");
                    string ReturnLength = dt_col.Rows[j]["data_length"].ToString();
                    if (string.IsNullOrEmpty(ReturnLength))
                    {
                        ReturnLength = (GetDataType(dt_col.Rows[j]["data_type"].ToString()) == "string") ? "4000" : "0";
                    }
                    sb.Append("\t\t\t\tnew Oracle.Parameter(\"" + ColName + "\", OracleType." + SQLType + ", " + ReturnLength + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(ColName) + ")");
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

                if (hasOutput)
                {
                    sb.AppendLine("");
                    for (int j = 0; j < dt_col.Rows.Count; j++)
                    {
                        string ColName = dt_col.Rows[j]["argument_name"].ToString();
                        string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                        string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                        string In_Out = dt_col.Rows[j]["in_out"].ToString();
                        if (In_Out != "IN")
                        {
                            sb.AppendLine("\t\t\t" + GetCanonicalIdentifier(ColName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + ColName + "\"]);");
                        }
                    }
                }

                sb.AppendLine("");
                sb.AppendLine("\t\t\treturn Result;");
                sb.AppendLine("\t\t}");

                goto end;
            WithQuery:

                // WithQuery
                hasOutput = false;
                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(ProcedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "OracleConnection conn")));
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 0) || !m_isUseConnectionStringConfig)
                        sb.Append(", ");
                    sb.Append((In_Out != "IN" ? "ref " : "") + Type + " " + GetCanonicalIdentifier(ColName));
                    if (In_Out != "IN")
                    {
                        hasOutput = true;
                    }
                }
                sb.AppendLine(((dt_col.Rows.Count > 0) ? ", " : "") + "ref DataSet ds)");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tOracle.OutputParameter Outputs = new Oracle.OutputParameter();");
                sb.Append("\t\t\tint Result = Oracle.ExecuteProcedureWithQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + ProcedureName + "\", ref ds, ref Outputs");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    string ColName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    In_Out = ((In_Out == "IN") ? "Input" : ((In_Out == "OUT") ? "Output" : "InputOutput"));
                    sb.AppendLine(",");
                    string ReturnLength = dt_col.Rows[j]["data_length"].ToString();
                    if (string.IsNullOrEmpty(ReturnLength))
                    {
                        ReturnLength = (GetDataType(dt_col.Rows[j]["data_type"].ToString()) == "string") ? "4000" : "0";
                    }
                    sb.Append("\t\t\t\tnew Oracle.Parameter(\"" + ColName + "\", OracleType." + SQLType + ", " + ReturnLength + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(ColName) + ")");
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

                if (hasOutput)
                {
                    sb.AppendLine("");
                    for (int j = 0; j < dt_col.Rows.Count; j++)
                    {
                        string ColName = dt_col.Rows[j]["argument_name"].ToString();
                        string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                        string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                        string In_Out = dt_col.Rows[j]["in_out"].ToString();
                        if (In_Out != "IN")
                        {
                            sb.AppendLine("\t\t\t" + GetCanonicalIdentifier(ColName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + ColName + "\"]);");
                        }
                    }
                }

                sb.AppendLine("");
                sb.AppendLine("\t\t\treturn Result;");
                sb.AppendLine("\t\t}");
                // Builder End.

            end:

                if (i < dt.Rows.Count - 1)
                {
                    sb.AppendLine("");
                }
            }
        }

        private string GetDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();
            string Result = "string";

            switch (SQLType)
            {
                case "char":
                    Result = "string";
                    break;
                case "nchar":
                    Result = "string";
                    break;
                case "varchar":
                    Result = "string";
                    break;
                case "nvarchar":
                    Result = "string";
                    break;
                case "varchar2":
                    Result = "string";
                    break;
                case "nvarchar2":
                    Result = "string";
                    break;
                case "clob":
                    Result = "string";
                    break;
                case "nclob":
                    Result = "string";
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
                    Result = "DateTime";
                    break;
                case "interval day to second":
                    Result = "TimeSpan";
                    break;
                case "interval year to month":
                    Result = "int";
                    break;
                case "timestamp":
                    Result = "DateTime";
                    break;
                case "timestamp with time zone":
                    Result = "DateTime";
                    break;
                case "timestamp with local time zone":
                    Result = "DateTime";
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
                    Result = "string";
                    break;
                case "character":
                    Result = "string";
                    break;
                case "character varying":
                    Result = "string";
                    break;
                case "char varying":
                    Result = "string";
                    break;
                case "national character":
                    Result = "string";
                    break;
                case "national char":
                    Result = "string";
                    break;
                case "national character varying":
                    Result = "string";
                    break;
                case "national char varying":
                    Result = "string";
                    break;
                case "nchar varying":
                    Result = "string";
                    break;
                case "numeric":
                    Result = "double";
                    break;
                case "decimal":
                    Result = "double";
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
                    Result = "object";
                    break;
            }

            return Result;
        }

        private OracleType GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();
            OracleType Result = OracleType.NVarChar;

            switch (SQLType)
            {
                case "char":
                    Result = OracleType.Char;
                    break;
                case "nchar":
                    Result = OracleType.NChar;
                    break;
                case "varchar":
                    Result = OracleType.VarChar;
                    break;
                case "nvarchar":
                    Result = OracleType.NVarChar;
                    break;
                case "varchar2":
                    Result = OracleType.LongVarChar;
                    break;
                case "nvarchar2":
                    Result = OracleType.LongVarChar;
                    break;
                case "clob":
                    Result = OracleType.Clob;
                    break;
                case "nclob":
                    Result = OracleType.NClob;
                    break;
                case "long":
                    Result = OracleType.Int32;
                    break;
                case "number":
                    Result = OracleType.Number;
                    break;
                case "binary_float":
                    Result = OracleType.Float;
                    break;
                case "binary_double":
                    Result = OracleType.Double;
                    break;
                case "date":
                    Result = OracleType.DateTime;
                    break;
                case "interval day to second":
                    Result = OracleType.IntervalDayToSecond;
                    break;
                case "interval year to month":
                    Result = OracleType.IntervalYearToMonth;
                    break;
                case "timestamp":
                    Result = OracleType.Timestamp;
                    break;
                case "timestamp with time zone":
                    Result = OracleType.TimestampWithTZ;
                    break;
                case "timestamp with local time zone":
                    Result = OracleType.TimestampLocal;
                    break;
                case "blob":
                    Result = OracleType.Blob;
                    break;
                case "bfile":
                    Result = OracleType.BFile;
                    break;
                case "raw":
                    Result = OracleType.Raw;
                    break;
                case "long raw":
                    Result = OracleType.LongRaw;
                    break;
                case "rowid":
                    Result = OracleType.RowId;
                    break;
                case "character":
                    Result = OracleType.NVarChar;
                    break;
                case "character varying":
                    Result = OracleType.NVarChar;
                    break;
                case "char varying":
                    Result = OracleType.NVarChar;
                    break;
                case "national character":
                    Result = OracleType.NVarChar;
                    break;
                case "national char":
                    Result = OracleType.NVarChar;
                    break;
                case "national character varying":
                    Result = OracleType.NVarChar;
                    break;
                case "national char varying":
                    Result = OracleType.NVarChar;
                    break;
                case "nchar varying":
                    Result = OracleType.NVarChar;
                    break;
                case "numeric":
                    Result = OracleType.Double;
                    break;
                case "decimal":
                    Result = OracleType.Double;
                    break;
                case "integer":
                    Result = OracleType.Int32;
                    break;
                case "int":
                    Result = OracleType.Int32;
                    break;
                case "smallint":
                    Result = OracleType.Int16;
                    break;
                case "float":
                    Result = OracleType.Float;
                    break;
                case "double precision":
                    Result = OracleType.Double;
                    break;
                case "real":
                    Result = OracleType.Double;
                    break;
                case "ref cursor":
                    Result = OracleType.Cursor;
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
                case "float":
                    Result = "System.Convert.ToSingle";
                    break;
                case "decimal":
                    Result = "System.Convert.ToDecimal";
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
                case "timespan":
                    Result = "(TimeSpan)";
                    break;
                case "object":
                    Result = "";
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

            return IdentifierName;
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
