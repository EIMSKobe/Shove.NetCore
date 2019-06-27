using System.Text;
using System.Data.OracleClient;
using System.Data;

namespace Shove.Database.Persistences
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
            DataTable dt = Database.Oracle.Select(ConnStr, "select table_name from user_tables order by table_name");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string tableName = dr["table_name"].ToString();
                
                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(tableName) + " : Oracle.TableBase");
                sb.AppendLine("\t\t{");

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

                    sb.AppendLine("\t\t\tpublic Oracle.Field " + GetCanonicalIdentifier(colName) + ";");
                }
                sb.AppendLine("");

                sb.AppendLine("\t\t\tpublic " + GetCanonicalIdentifier(tableName) + "()");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine("\t\t\t\tTableName = \"" + tableName + "\";");
                sb.AppendLine("");

                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    DataRow dr_col = dt_col.Rows[j];

                    string colName = dr_col["column_name"].ToString();

                    sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(colName) + " = new Oracle.Field(this, \"" + colName + "\", \"" + GetCanonicalIdentifier(colName) + "\", OracleType." + GetSQLDataType(dr_col["data_type"].ToString()) + ");");
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
            DataTable dt = Database.Oracle.Select(ConnStr, "select view_name from user_views order by view_name");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string viewName = dr["view_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(viewName) + " : Oracle.ViewBase");
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

                if (IsContainOracleTypeCursor(dt_col))
                {
                    goto WithQuery;
                }

                // NonQuery
                bool hasOutput = false;
                string ReturnType = GetDataType(dt_col.Rows[0]["data_type"].ToString());
                sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(functionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "OracleConnection conn")));
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 1) || !m_isUseConnectionStringConfig)
                        sb.Append(", ");
                    sb.Append((In_Out != "IN" ? "ref " : "") + Type + " " + GetCanonicalIdentifier(colName));
                    if (In_Out != "IN")
                    {
                        hasOutput = true;
                    }
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tOracle.OutputParameter Outputs = new Oracle.OutputParameter();");
                sb.Append("\t\t\tobject Result = Oracle.ExecuteFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + functionName + "\", OracleType.");
                string ReturnLength = dt_col.Rows[0]["data_length"].ToString();
                if (string.IsNullOrEmpty(ReturnLength))
                {
                    ReturnLength = (GetDataType(dt_col.Rows[0]["data_type"].ToString()) == "string") ? "4000" : "0";
                }
                sb.Append(GetSQLDataType(dt_col.Rows[0]["data_type"].ToString()).ToString() + ", " + ReturnLength + ", ref Outputs");
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
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
                    sb.Append("\t\t\t\tnew Oracle.Parameter(\"" + colName + "\", OracleType." + SQLType + ", " + ReturnLength + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(colName) + ")");
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
                        string colName = dt_col.Rows[j]["argument_name"].ToString();
                        string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                        string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                        string In_Out = dt_col.Rows[j]["in_out"].ToString();
                        if (In_Out != "IN")
                        {
                            sb.AppendLine("\t\t\t" + GetCanonicalIdentifier(colName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + colName + "\"]);");
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
                sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(functionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "OracleConnection conn")));
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 1) || !m_isUseConnectionStringConfig)
                        sb.Append(", ");
                    sb.Append((In_Out != "IN" ? "ref " : "") + Type + " " + GetCanonicalIdentifier(colName));
                    if (In_Out != "IN")
                    {
                        hasOutput = true;
                    }
                }
                sb.AppendLine(((dt_col.Rows.Count > 1) ? ", " : "") + "ref DataSet ds)");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tOracle.OutputParameter Outputs = new Oracle.OutputParameter();");
                sb.Append("\t\t\tobject Result = Oracle.ExecuteFunctionWithQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + functionName + "\", OracleType.");
                ReturnLength = dt_col.Rows[0]["data_length"].ToString();
                if (string.IsNullOrEmpty(ReturnLength))
                {
                    ReturnLength = (GetDataType(dt_col.Rows[0]["data_type"].ToString()) == "string") ? "4000" : "0";
                }
                sb.Append(GetSQLDataType(dt_col.Rows[0]["data_type"].ToString()).ToString() + ", " + ReturnLength + ", ref ds, ref Outputs");
                for (int j = 1; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
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
                    sb.Append("\t\t\t\tnew Oracle.Parameter(\"" + colName + "\", OracleType." + SQLType + ", " + ReturnLength + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(colName) + ")");
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
                        string colName = dt_col.Rows[j]["argument_name"].ToString();
                        string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                        string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                        string In_Out = dt_col.Rows[j]["in_out"].ToString();
                        if (In_Out != "IN")
                        {
                            sb.AppendLine("\t\t\t" + GetCanonicalIdentifier(colName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + colName + "\"]);");
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

                if (IsContainOracleTypeCursor(dt_col))
                {
                    goto WithQuery;
                }

                // NonQuery
                bool hasOutput = false;
                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(procedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "OracleConnection conn")));
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 0) || !m_isUseConnectionStringConfig)
                        sb.Append(", ");
                    sb.Append((In_Out != "IN" ? "ref " : "") + Type + " " + GetCanonicalIdentifier(colName));
                    if (In_Out != "IN")
                    {
                        hasOutput = true;
                    }
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tOracle.OutputParameter Outputs = new Oracle.OutputParameter();");
                sb.Append("\t\t\tint Result = Oracle.ExecuteProcedure(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + procedureName + "\", ref Outputs");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
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
                    sb.Append("\t\t\t\tnew Oracle.Parameter(\"" + colName + "\", OracleType." + SQLType + ", " + ReturnLength + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(colName) + ")");
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
                        string colName = dt_col.Rows[j]["argument_name"].ToString();
                        string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                        string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                        string In_Out = dt_col.Rows[j]["in_out"].ToString();
                        if (In_Out != "IN")
                        {
                            sb.AppendLine("\t\t\t" + GetCanonicalIdentifier(colName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + colName + "\"]);");
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
                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(procedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "OracleConnection conn")));
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
                    string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                    string In_Out = dt_col.Rows[j]["in_out"].ToString();
                    if ((j > 0) || !m_isUseConnectionStringConfig)
                        sb.Append(", ");
                    sb.Append((In_Out != "IN" ? "ref " : "") + Type + " " + GetCanonicalIdentifier(colName));
                    if (In_Out != "IN")
                    {
                        hasOutput = true;
                    }
                }
                sb.AppendLine(((dt_col.Rows.Count > 0) ? ", " : "") + "ref DataSet ds)");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tOracle.OutputParameter Outputs = new Oracle.OutputParameter();");
                sb.Append("\t\t\tint Result = Oracle.ExecuteProcedureWithQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + procedureName + "\", ref ds, ref Outputs");
                for (int j = 0; j < dt_col.Rows.Count; j++)
                {
                    string colName = dt_col.Rows[j]["argument_name"].ToString();
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
                    sb.Append("\t\t\t\tnew Oracle.Parameter(\"" + colName + "\", OracleType." + SQLType + ", " + ReturnLength + ", ParameterDirection." + In_Out + ", " + GetCanonicalIdentifier(colName) + ")");
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
                        string colName = dt_col.Rows[j]["argument_name"].ToString();
                        string Type = GetDataType(dt_col.Rows[j]["data_type"].ToString());
                        string SQLType = GetSQLDataType(dt_col.Rows[j]["data_type"].ToString()).ToString();
                        string In_Out = dt_col.Rows[j]["in_out"].ToString();
                        if (In_Out != "IN")
                        {
                            sb.AppendLine("\t\t\t" + GetCanonicalIdentifier(colName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + colName + "\"]);");
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
            string result = "string";

            switch (SQLType)
            {
                case "char":
                    result = "string";
                    break;
                case "nchar":
                    result = "string";
                    break;
                case "varchar":
                    result = "string";
                    break;
                case "nvarchar":
                    result = "string";
                    break;
                case "varchar2":
                    result = "string";
                    break;
                case "nvarchar2":
                    result = "string";
                    break;
                case "clob":
                    result = "string";
                    break;
                case "nclob":
                    result = "string";
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
                    result = "DateTime";
                    break;
                case "interval day to second":
                    result = "TimeSpan";
                    break;
                case "interval year to month":
                    result = "int";
                    break;
                case "timestamp":
                    result = "DateTime";
                    break;
                case "timestamp with time zone":
                    result = "DateTime";
                    break;
                case "timestamp with local time zone":
                    result = "DateTime";
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
                    result = "string";
                    break;
                case "character":
                    result = "string";
                    break;
                case "character varying":
                    result = "string";
                    break;
                case "char varying":
                    result = "string";
                    break;
                case "national character":
                    result = "string";
                    break;
                case "national char":
                    result = "string";
                    break;
                case "national character varying":
                    result = "string";
                    break;
                case "national char varying":
                    result = "string";
                    break;
                case "nchar varying":
                    result = "string";
                    break;
                case "numeric":
                    result = "double";
                    break;
                case "decimal":
                    result = "double";
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
                    result = "object";
                    break;
            }

            return result;
        }

        private OracleType GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();
            OracleType result = OracleType.NVarChar;

            switch (SQLType)
            {
                case "char":
                    result = OracleType.Char;
                    break;
                case "nchar":
                    result = OracleType.NChar;
                    break;
                case "varchar":
                    result = OracleType.VarChar;
                    break;
                case "nvarchar":
                    result = OracleType.NVarChar;
                    break;
                case "varchar2":
                    result = OracleType.LongVarChar;
                    break;
                case "nvarchar2":
                    result = OracleType.LongVarChar;
                    break;
                case "clob":
                    result = OracleType.Clob;
                    break;
                case "nclob":
                    result = OracleType.NClob;
                    break;
                case "long":
                    result = OracleType.Int32;
                    break;
                case "number":
                    result = OracleType.Number;
                    break;
                case "binary_float":
                    result = OracleType.Float;
                    break;
                case "binary_double":
                    result = OracleType.Double;
                    break;
                case "date":
                    result = OracleType.DateTime;
                    break;
                case "interval day to second":
                    result = OracleType.IntervalDayToSecond;
                    break;
                case "interval year to month":
                    result = OracleType.IntervalYearToMonth;
                    break;
                case "timestamp":
                    result = OracleType.Timestamp;
                    break;
                case "timestamp with time zone":
                    result = OracleType.TimestampWithTZ;
                    break;
                case "timestamp with local time zone":
                    result = OracleType.TimestampLocal;
                    break;
                case "blob":
                    result = OracleType.Blob;
                    break;
                case "bfile":
                    result = OracleType.BFile;
                    break;
                case "raw":
                    result = OracleType.Raw;
                    break;
                case "long raw":
                    result = OracleType.LongRaw;
                    break;
                case "rowid":
                    result = OracleType.RowId;
                    break;
                case "character":
                    result = OracleType.NVarChar;
                    break;
                case "character varying":
                    result = OracleType.NVarChar;
                    break;
                case "char varying":
                    result = OracleType.NVarChar;
                    break;
                case "national character":
                    result = OracleType.NVarChar;
                    break;
                case "national char":
                    result = OracleType.NVarChar;
                    break;
                case "national character varying":
                    result = OracleType.NVarChar;
                    break;
                case "national char varying":
                    result = OracleType.NVarChar;
                    break;
                case "nchar varying":
                    result = OracleType.NVarChar;
                    break;
                case "numeric":
                    result = OracleType.Double;
                    break;
                case "decimal":
                    result = OracleType.Double;
                    break;
                case "integer":
                    result = OracleType.Int32;
                    break;
                case "int":
                    result = OracleType.Int32;
                    break;
                case "smallint":
                    result = OracleType.Int16;
                    break;
                case "float":
                    result = OracleType.Float;
                    break;
                case "double precision":
                    result = OracleType.Double;
                    break;
                case "real":
                    result = OracleType.Double;
                    break;
                case "ref cursor":
                    result = OracleType.Cursor;
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
                case "float":
                    result = "System.Convert.ToSingle";
                    break;
                case "decimal":
                    result = "System.Convert.ToDecimal";
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
                case "timespan":
                    result = "(TimeSpan)";
                    break;
                case "object":
                    result = "";
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

            return identifierName;
        }

        private bool IsContainOracleTypeCursor(DataTable dt)
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
