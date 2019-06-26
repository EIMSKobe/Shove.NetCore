using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace Shove.Database.Persistences
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
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Data;");
            sb.AppendLine("using MySql.Data.MySqlClient;");
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("");
            sb.AppendLine("using Shove.Database;");
            sb.AppendLine("");

            sb.AppendLine("namespace " + (m_NamespaceName == "" ? "DAL" : (m_NamespaceName + ".DAL")));
            sb.AppendLine("{");
            sb.AppendLine("\t/*");
            sb.AppendLine("\tProgram Name: Shove.DAL.30 for MySQL");
            sb.AppendLine("\tProgram Version: 3.0");
            sb.AppendLine("\tWriter By: 3km.shovesoft.shove (zhou changjun)");
            sb.AppendLine("\tRelease Time: 2009.7.16");
            sb.AppendLine("");
            sb.AppendLine("\tSystem Request: Shove.dll, MySql.Data.dll, MySql.Data.Entity.dll, MySql.Web.dll");
            sb.AppendLine("\tAll Rights saved.");
            sb.AppendLine("\t*/");
            sb.AppendLine("");
            if (m_isUseConnectionStringConfig)
            {
                sb.AppendLine("");
                sb.AppendLine("\t// Please Add a Key in Web.config File's appSetting section, Exemple:");
                sb.AppendLine("\t// <add key=\"ConnectionString\" value=\"server=localhost;user id=root;password=;database=test;port=3306;\" />");
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
            DataTable dt = Shove.Database.MySQL.Select(ConnStr, "select table_name from information_schema.tables where table_schema = '" + m_DatabaseName + "' and table_type = 'BASE TABLE' order by table_name;");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string TableName = dr["table_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(TableName) + " : MySQL.TableBase");
                sb.AppendLine("\t\t{");

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

                    sb.AppendLine("\t\t\tpublic MySQL.Field " + GetCanonicalIdentifier(ColName) + ";");
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

                    sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(ColName) + " = new MySQL.Field(this, \"" + ColName + "\", \"" + GetCanonicalIdentifier(ColName) + "\", MySqlDbType." + GetSQLDataType(dr_col["data_type"].ToString()) + ", " + ((dr_col["extra"].ToString() == "auto_increment") ? "true" : "false") + ");");
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
            DataTable dt = Shove.Database.MySQL.Select(ConnStr, "select table_name from information_schema.tables where table_schema = '" + m_DatabaseName + "' and table_type = 'VIEW' order by table_name;");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string ViewName = dr["table_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(ViewName) + " : MySQL.ViewBase");
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
                string ReturnType = ""; 
                try
                {
                    ReturnType = GetDataType(FilterLengthDescriptionForReturnType(System.Text.ASCIIEncoding.ASCII.GetString((byte[])dt_col.Rows[0]["returns"])));
                }
                catch
                {
                    ReturnType = GetDataType(FilterLengthDescriptionForReturnType(dt_col.Rows[0]["returns"].ToString()));
                }
                sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(FunctionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "MySqlConnection conn")));
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
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.Append("\t\t\tobject Result = MySQL.ExecuteFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + FunctionName + "\"");
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
                        sb.Append("\t\t\t\tnew MySQL.Parameter(\"" + ColName + "\", MySqlDbType." + SQLType + ", 0, ParameterDirection.Input, " + GetCanonicalIdentifier(ColName) + ((Type == "bool") ? " ? 1 : 0" : "") + ")");
                    }
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
                if (GetDataTypeForConvert(ReturnType) == "System.Convert.ToBoolean")
                {
                    sb.AppendLine("\t\t\treturn " + GetDataTypeForConvert(ReturnType) + "(Result.ToString() == \"1\");");
                }
                else
                {
                    sb.AppendLine("\t\t\treturn " + GetDataTypeForConvert(ReturnType) + "(Result);");
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

                // NoQuery
                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(ProcedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "SqlConnection conn")));
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
                        sb.Append((isOutput != 0 ? "ref " : "") + Type + " " + GetCanonicalIdentifier(ColName));
                    }
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tMySQL.OutputParameter Outputs = new MySQL.OutputParameter();");
                sb.AppendLine("");
                sb.Append("\t\t\tint CallResult = MySQL.ExecuteStoredProcedureNonQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + ProcedureName + "\", ref Outputs");
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
                        string Len = "0";
                        if (t_strs.Length > 3)
                        {
                            Len = t_strs[3];
                        }

                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew MySQL.Parameter(\"" + ColName + "\", MySqlDbType." + SQLType + ", " + Len + ", " + (isOutput == 0 ? "ParameterDirection.Input" : (isOutput == 1 ? "ParameterDirection.InputOutput" : "ParameterDirection.Output")) + ", " + GetCanonicalIdentifier(ColName) + ")");
                    }
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
                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        if (isOutput == 0)
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
                            if (GetDataTypeForConvert(Type) == "System.Convert.ToBoolean")
                            {
                                sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(ColName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + ColName + "\"].ToString() == \"1\");");
                            }
                            else
                            {
                                sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(ColName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + ColName + "\"]);");
                            }
                        }

                        sb.AppendLine("\t\t\t}");
                        sb.AppendLine("\t\t\tcatch { }");
                    }
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
                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        sb.Append(", ");
                        sb.Append((isOutput != 0 ? "ref " : "") + Type + " " + GetCanonicalIdentifier(ColName));
                    }
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tMySQL.OutputParameter Outputs = new MySQL.OutputParameter();");
                sb.AppendLine("");
                sb.Append("\t\t\tint CallResult = MySQL.ExecuteStoredProcedureWithQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + ProcedureName + "\", ref ds, ref Outputs");
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
                        string Len = "0";
                        if (t_strs.Length > 3)
                        {
                            Len = t_strs[3];
                        }

                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        sb.AppendLine(",");

                        sb.Append("\t\t\t\tnew MySQL.Parameter(\"" + ColName + "\", MySqlDbType." + SQLType + ", " + Len + ", " + (isOutput == 0 ? "ParameterDirection.Input" : (isOutput == 1 ? "ParameterDirection.InputOutput" : "ParameterDirection.Output")) + ", " + GetCanonicalIdentifier(ColName) + ")");
                    }
                }
                if (dt_col.Rows.Count == 0)
                    sb.AppendLine(");");
                else
                {
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\t\t);");
                }

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
                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        if (isOutput == 0)
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
                            if (GetDataTypeForConvert(Type) == "System.Convert.ToBoolean")
                            {
                                sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(ColName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + ColName + "\"].ToString() == \"1\");");
                            }
                            else
                            {
                                sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(ColName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + ColName + "\"]);");
                            }
                        }

                        sb.AppendLine("\t\t\t}");
                        sb.AppendLine("\t\t\tcatch { }");
                    }
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
            SQLType = SQLType.Trim().ToLower();//.Split('(')[0];
            string Result = "string";

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
                    Result = "double";
                    break;
                case "double":
                    Result = "double";
                    break;
                case "decimal":
                    Result = "Decimal";
                    break;
                case "date":
                    Result = "DateTime";
                    break;
                case "datetime":
                    Result = "DateTime";
                    break;
                case "timestamp":
                    Result = "string";
                    break;
                case "time":
                    Result = "DateTime";
                    break;
                case "year":
                    Result = "int";
                    break;
                case "char":
                    Result = "string";
                    break;
                case "varchar":
                    Result = "string";
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
                    Result = "string";
                    break;
                case "text":
                    Result = "string";
                    break;
                case "mediumtext":
                    Result = "string";
                    break;
                case "longtext":
                    Result = "string";
                    break;
                case "enum":
                    Result = "string";
                    break;
                case "set":
                    Result = "string";
                    break;
                case "binary":
                    Result = "byte[]";
                    break;
                case "varbinary":
                    Result = "byte[]";
                    break;
                case "bit":
                    Result = "bool";
                    break;
                case "boolean":
                    Result = "short";
                    break;
                case "geometry":
                    Result = "string";
                    break;
                case "point":
                    Result = "int";
                    break;
                case "linestring":
                    Result = "string";
                    break;
                case "polygon":
                    Result = "string";
                    break;
                case "multipoint":
                    Result = "int";
                    break;
                case "multilinestring":
                    Result = "string";
                    break;
                case "multipolygon":
                    Result = "string";
                    break;
                case "geometrycollection":
                    Result = "string";
                    break;
            }

            return Result;
        }

        private MySqlDbType GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();//.Split('(')[0];
            MySqlDbType Result = MySqlDbType.String;

            switch (SQLType)
            {
                case "tinyint":
                    Result = MySqlDbType.Int16;
                    break;
                case "smallint":
                    Result = MySqlDbType.Int24;
                    break;
                case "mediumint":
                    Result = MySqlDbType.Int32;
                    break;
                case "int":
                    Result = MySqlDbType.Int32;
                    break;
                case "integer":
                    Result = MySqlDbType.Int32;
                    break;
                case "bigint":
                    Result = MySqlDbType.Int64;
                    break;
                case "float":
                    Result = MySqlDbType.Float;
                    break;
                case "double":
                    Result = MySqlDbType.Double;
                    break;
                case "decimal":
                    Result = MySqlDbType.Decimal;
                    break;
                case "date":
                    Result = MySqlDbType.Date;
                    break;
                case "datetime":
                    Result = MySqlDbType.DateTime;
                    break;
                case "timestamp":
                    Result = MySqlDbType.Timestamp;
                    break;
                case "time":
                    Result = MySqlDbType.Time;
                    break;
                case "year":
                    Result = MySqlDbType.Year;
                    break;
                case "char":
                    Result = MySqlDbType.VarChar;
                    break;
                case "varchar":
                    Result = MySqlDbType.VarChar;
                    break;
                case "tinyblob":
                    Result = MySqlDbType.TinyBlob;
                    break;
                case "blob":
                    Result = MySqlDbType.Blob;
                    break;
                case "mediumblob":
                    Result = MySqlDbType.MediumBlob;
                    break;
                case "longblob":
                    Result = MySqlDbType.LongBlob;
                    break;
                case "tinytext":
                    Result = MySqlDbType.TinyText;
                    break;
                case "text":
                    Result = MySqlDbType.Text;
                    break;
                case "mediumtext":
                    Result = MySqlDbType.MediumText;
                    break;
                case "longtext":
                    Result = MySqlDbType.LongText;
                    break;
                case "enum":
                    Result = MySqlDbType.Enum;
                    break;
                case "set":
                    Result = MySqlDbType.Set;
                    break;
                case "binary":
                    Result = MySqlDbType.Binary;
                    break;
                case "varbinary":
                    Result = MySqlDbType.VarBinary;
                    break;
                case "bit":
                    Result = MySqlDbType.Bit;
                    break;
                case "boolean":
                    Result = MySqlDbType.Int16;
                    break;
                case "geometry":
                    Result = MySqlDbType.Geometry;
                    break;
                case "point":
                    Result = MySqlDbType.Int32;
                    break;
                case "linestring":
                    Result = MySqlDbType.String;
                    break;
                case "polygon":
                    Result = MySqlDbType.String;
                    break;
                case "multipoint":
                    Result = MySqlDbType.Int32;
                    break;
                case "multilinestring":
                    Result = MySqlDbType.String;
                    break;
                case "multipolygon":
                    Result = MySqlDbType.String;
                    break;
                case "geometrycollection":
                    Result = MySqlDbType.Geometry;
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
            }

            return Result;
        }

        private string GetCanonicalIdentifier(string IdentifierName)
        {
            IdentifierName = IdentifierName.Replace(" ", "_").Replace("$", "_").Replace("@", "_");

            if (IdentifierName.Length > 0)
            {
                if ("0123456789".IndexOf(IdentifierName[0]) >= 0)
                {
                    IdentifierName = "_" + IdentifierName;
                }
            }

            return IdentifierName;
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
