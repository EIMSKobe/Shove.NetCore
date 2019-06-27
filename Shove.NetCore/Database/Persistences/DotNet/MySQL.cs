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

        string m_Server;
        string m_Database;
        string m_User;
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
        /// <param name="server"></param>
        /// <param name="database"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="Port"></param>
        /// <param name="namespaceName"></param>
        /// <param name="isUseConnectionStringConfig"></param>
        /// <param name="isUseConnectionString"></param>
        /// <param name="isWithTables"></param>
        /// <param name="isWithViews"></param>
        /// <param name="isWithProcedures"></param>
        /// <param name="isWithFunction"></param>
        public MySQL(string server, string database, string user, string password, string Port, string namespaceName, bool isUseConnectionStringConfig, bool isUseConnectionString, bool isWithTables, bool isWithViews, bool isWithProcedures, bool isWithFunction)
        {
            m_Server = server;
            m_Database = database;
            m_User = user;
            m_Password = password;
            m_Port = Port;
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

            ConnStr = Database.MySQL.BuildConnectString(m_Server, m_User, m_Password, m_Database, m_Port);

            MySqlConnection conn = DatabaseAccess.CreateDataConnection<MySqlConnection>(ConnStr);

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
            DataTable dt = Database.MySQL.Select(ConnStr, "select table_name from information_schema.tables where table_schema = '" + m_Database + "' and table_type = 'BASE TABLE' order by table_name;");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string tableName = dr["table_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(tableName) + " : MySQL.TableBase");
                sb.AppendLine("\t\t{");

                DataTable dt_col = Database.MySQL.Select(ConnStr, "select column_name, data_type, character_maximum_length, column_type, extra from information_schema.columns where table_schema = '" + m_Database + "' and table_name = '" + tableName + "' order by ordinal_position;");

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

                    sb.AppendLine("\t\t\tpublic MySQL.Field " + GetCanonicalIdentifier(colName) + ";");
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

                    sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(colName) + " = new MySQL.Field(this, \"" + colName + "\", \"" + GetCanonicalIdentifier(colName) + "\", MySqlDbType." + GetSQLDataType(dr_col["data_type"].ToString()) + ", " + ((dr_col["extra"].ToString() == "auto_increment") ? "true" : "false") + ");");
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
            DataTable dt = Database.MySQL.Select(ConnStr, "select table_name from information_schema.tables where table_schema = '" + m_Database + "' and table_type = 'VIEW' order by table_name;");

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                return;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string viewName = dr["table_name"].ToString();

                sb.AppendLine("\t\tpublic class " + GetCanonicalIdentifier(viewName) + " : MySQL.ViewBase");
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
            DataTable dt = Database.MySQL.Select(ConnStr, "select routine_name, dtd_identifier, routine_body from information_schema.routines where routine_schema = '" + m_Database + "' and routine_type = 'FUNCTION' order by routine_name;");
            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string functionName = dr["routine_name"].ToString();

                //Function Builder...
                DataTable dt_col = Database.MySQL.Select(ConnStr, "SELECT param_list, returns FROM mysql.proc where db = '" + m_Database + "' and type = 'FUNCTION' and name = '" + functionName + "';");
                if (dt_col == null)
                    continue;
                if (dt_col.Rows.Count < 1)
                    continue;

                //[shove] 2013.2.1 临时该为下面几句，具体原因未分析
                //string ReturnType = GetDataType(FilterLengthDescriptionForReturnType(Encoding.ASCII.GetString((byte[])dt_col.Rows[0]["returns"])));
                string ReturnType = ""; 
                try
                {
                    ReturnType = GetDataType(FilterLengthDescriptionForReturnType(Encoding.ASCII.GetString((byte[])dt_col.Rows[0]["returns"])));
                }
                catch
                {
                    ReturnType = GetDataType(FilterLengthDescriptionForReturnType(dt_col.Rows[0]["returns"].ToString()));
                }
                sb.Append("\t\tpublic static " + ReturnType + " " + GetCanonicalIdentifier(functionName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "MySqlConnection conn")));
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
                        string colName = t_strs[0];
                        string Type = GetDataType(t_strs[1]);
                        if ((j > 0) || !m_isUseConnectionStringConfig)
                            sb.Append(", ");
                        sb.Append(Type + " " + GetCanonicalIdentifier(colName));
                    }
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.Append("\t\t\tobject Result = MySQL.ExecuteFunction(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + functionName + "\"");
                if (cols != null)
                {
                    for (int j = 0; j < cols.Length; j++)
                    {
                        string[] t_strs = cols[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if ((t_strs == null) || (t_strs.Length < 2) || (t_strs[0].Trim() == ""))
                        {
                            continue;
                        }
                        string colName = t_strs[0];
                        string Type = GetDataType(t_strs[1]);
                        string SQLType = GetSQLDataType(t_strs[1]).ToString();
                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew MySQL.Parameter(\"" + colName + "\", MySqlDbType." + SQLType + ", 0, ParameterDirection.Input, " + GetCanonicalIdentifier(colName) + ((Type == "bool") ? " ? 1 : 0" : "") + ")");
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
            DataTable dt = Database.MySQL.Select(ConnStr, "select routine_name, dtd_identifier, routine_body from information_schema.routines where routine_schema = '" + m_Database + "' and routine_type = 'PROCEDURE' order by routine_name;");
            if (dt == null)
                return;
            if (dt.Rows.Count < 1)
                return;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string procedureName = dr["routine_name"].ToString();

                //Procedure Class Builder...
                DataTable dt_col = Database.MySQL.Select(ConnStr, "SELECT param_list FROM mysql.proc where db = '" + m_Database + "' and type = 'PROCEDURE' and name = '" + procedureName + "';");
                if (dt_col == null)
                    continue;
                if (dt_col.Rows.Count < 1)
                    continue;

                // NoQuery
                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(procedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "SqlConnection conn")));
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
                        string colName = t_strs[1];
                        string Type = GetDataType(t_strs[2]);
                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        if ((j > 0) || !m_isUseConnectionStringConfig)
                        {
                            sb.Append(", ");
                        }
                        sb.Append((isOutput != 0 ? "ref " : "") + Type + " " + GetCanonicalIdentifier(colName));
                    }
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tMySQL.OutputParameter Outputs = new MySQL.OutputParameter();");
                sb.AppendLine("");
                sb.Append("\t\t\tint CallResult = MySQL.ExecuteStoredProcedureNonQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + procedureName + "\", ref Outputs");
                if (cols != null)
                {
                    for (int j = 0; j < cols.Length; j++)
                    {
                        string[] t_strs = cols[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if ((t_strs == null) || (t_strs.Length < 3))
                        {
                            continue;
                        }
                        string colName = t_strs[1];
                        string Type = GetDataType(t_strs[2]);
                        string SQLType = GetSQLDataType(t_strs[2]).ToString();
                        string Len = "0";
                        if (t_strs.Length > 3)
                        {
                            Len = t_strs[3];
                        }

                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        sb.AppendLine(",");
                        sb.Append("\t\t\t\tnew MySQL.Parameter(\"" + colName + "\", MySqlDbType." + SQLType + ", " + Len + ", " + (isOutput == 0 ? "ParameterDirection.Input" : (isOutput == 1 ? "ParameterDirection.InputOutput" : "ParameterDirection.Output")) + ", " + GetCanonicalIdentifier(colName) + ")");
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
                        string colName = t_strs[1];
                        string Type = GetDataType(t_strs[2]);
                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        if (isOutput == 0)
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
                            if (GetDataTypeForConvert(Type) == "System.Convert.ToBoolean")
                            {
                                sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(colName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + colName + "\"].ToString() == \"1\");");
                            }
                            else
                            {
                                sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(colName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + colName + "\"]);");
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
                sb.Append("\t\tpublic static int " + GetCanonicalIdentifier(procedureName) + "(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "string ConnectionString" : "SqlConnection conn")));
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
                        string colName = t_strs[1];
                        string Type = GetDataType(t_strs[2]);
                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        sb.Append(", ");
                        sb.Append((isOutput != 0 ? "ref " : "") + Type + " " + GetCanonicalIdentifier(colName));
                    }
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tMySQL.OutputParameter Outputs = new MySQL.OutputParameter();");
                sb.AppendLine("");
                sb.Append("\t\t\tint CallResult = MySQL.ExecuteStoredProcedureWithQuery(" + (m_isUseConnectionStringConfig ? "" : (m_isUseConnectionString ? "ConnectionString, " : "conn, ")) + "\"" + procedureName + "\", ref ds, ref Outputs");
                if (cols != null)
                {
                    for (int j = 0; j < cols.Length; j++)
                    {
                        string[] t_strs = cols[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if ((t_strs == null) || (t_strs.Length < 3))
                        {
                            continue;
                        }
                        string colName = t_strs[1];
                        string Type = GetDataType(t_strs[2]);
                        string SQLType = GetSQLDataType(t_strs[2]).ToString();
                        string Len = "0";
                        if (t_strs.Length > 3)
                        {
                            Len = t_strs[3];
                        }

                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        sb.AppendLine(",");

                        sb.Append("\t\t\t\tnew MySQL.Parameter(\"" + colName + "\", MySqlDbType." + SQLType + ", " + Len + ", " + (isOutput == 0 ? "ParameterDirection.Input" : (isOutput == 1 ? "ParameterDirection.InputOutput" : "ParameterDirection.Output")) + ", " + GetCanonicalIdentifier(colName) + ")");
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
                        string colName = t_strs[1];
                        string Type = GetDataType(t_strs[2]);
                        int isOutput = ((t_strs[0].ToLower() == "in") ? 0 : (t_strs[0].ToLower() == "inout" ? 1 : 2));
                        if (isOutput == 0)
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
                            if (GetDataTypeForConvert(Type) == "System.Convert.ToBoolean")
                            {
                                sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(colName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + colName + "\"].ToString() == \"1\");");
                            }
                            else
                            {
                                sb.AppendLine("\t\t\t\t" + GetCanonicalIdentifier(colName) + " = " + GetDataTypeForConvert(Type) + "(Outputs[\"" + colName + "\"]);");
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
            string result = "string";

            switch (SQLType)
            {
                case "tinyint":
                    result = "short";
                    break;
                case "smallint":
                    result = "short";
                    break;
                case "mediumint":
                    result = "int";
                    break;
                case "int":
                    result = "int";
                    break;
                case "integer":
                    result = "int";
                    break;
                case "bigint":
                    result = "long";
                    break;
                case "float":
                    result = "double";
                    break;
                case "double":
                    result = "double";
                    break;
                case "decimal":
                    result = "Decimal";
                    break;
                case "date":
                    result = "DateTime";
                    break;
                case "datetime":
                    result = "DateTime";
                    break;
                case "timestamp":
                    result = "string";
                    break;
                case "time":
                    result = "DateTime";
                    break;
                case "year":
                    result = "int";
                    break;
                case "char":
                    result = "string";
                    break;
                case "varchar":
                    result = "string";
                    break;
                case "tinyblob":
                    result = "byte[]";
                    break;
                case "blob":
                    result = "byte[]";
                    break;
                case "mediumblob":
                    result = "byte[]";
                    break;
                case "longblob":
                    result = "byte[]";
                    break;
                case "tinytext":
                    result = "string";
                    break;
                case "text":
                    result = "string";
                    break;
                case "mediumtext":
                    result = "string";
                    break;
                case "longtext":
                    result = "string";
                    break;
                case "enum":
                    result = "string";
                    break;
                case "set":
                    result = "string";
                    break;
                case "binary":
                    result = "byte[]";
                    break;
                case "varbinary":
                    result = "byte[]";
                    break;
                case "bit":
                    result = "bool";
                    break;
                case "boolean":
                    result = "short";
                    break;
                case "geometry":
                    result = "string";
                    break;
                case "point":
                    result = "int";
                    break;
                case "linestring":
                    result = "string";
                    break;
                case "polygon":
                    result = "string";
                    break;
                case "multipoint":
                    result = "int";
                    break;
                case "multilinestring":
                    result = "string";
                    break;
                case "multipolygon":
                    result = "string";
                    break;
                case "geometrycollection":
                    result = "string";
                    break;
            }

            return result;
        }

        private MySqlDbType GetSQLDataType(string SQLType)
        {
            SQLType = SQLType.Trim().ToLower();//.Split('(')[0];
            MySqlDbType result = MySqlDbType.String;

            switch (SQLType)
            {
                case "tinyint":
                    result = MySqlDbType.Int16;
                    break;
                case "smallint":
                    result = MySqlDbType.Int24;
                    break;
                case "mediumint":
                    result = MySqlDbType.Int32;
                    break;
                case "int":
                    result = MySqlDbType.Int32;
                    break;
                case "integer":
                    result = MySqlDbType.Int32;
                    break;
                case "bigint":
                    result = MySqlDbType.Int64;
                    break;
                case "float":
                    result = MySqlDbType.Float;
                    break;
                case "double":
                    result = MySqlDbType.Double;
                    break;
                case "decimal":
                    result = MySqlDbType.Decimal;
                    break;
                case "date":
                    result = MySqlDbType.Date;
                    break;
                case "datetime":
                    result = MySqlDbType.DateTime;
                    break;
                case "timestamp":
                    result = MySqlDbType.Timestamp;
                    break;
                case "time":
                    result = MySqlDbType.Time;
                    break;
                case "year":
                    result = MySqlDbType.Year;
                    break;
                case "char":
                    result = MySqlDbType.VarChar;
                    break;
                case "varchar":
                    result = MySqlDbType.VarChar;
                    break;
                case "tinyblob":
                    result = MySqlDbType.TinyBlob;
                    break;
                case "blob":
                    result = MySqlDbType.Blob;
                    break;
                case "mediumblob":
                    result = MySqlDbType.MediumBlob;
                    break;
                case "longblob":
                    result = MySqlDbType.LongBlob;
                    break;
                case "tinytext":
                    result = MySqlDbType.TinyText;
                    break;
                case "text":
                    result = MySqlDbType.Text;
                    break;
                case "mediumtext":
                    result = MySqlDbType.MediumText;
                    break;
                case "longtext":
                    result = MySqlDbType.LongText;
                    break;
                case "enum":
                    result = MySqlDbType.Enum;
                    break;
                case "set":
                    result = MySqlDbType.Set;
                    break;
                case "binary":
                    result = MySqlDbType.Binary;
                    break;
                case "varbinary":
                    result = MySqlDbType.VarBinary;
                    break;
                case "bit":
                    result = MySqlDbType.Bit;
                    break;
                case "boolean":
                    result = MySqlDbType.Int16;
                    break;
                case "geometry":
                    result = MySqlDbType.Geometry;
                    break;
                case "point":
                    result = MySqlDbType.Int32;
                    break;
                case "linestring":
                    result = MySqlDbType.String;
                    break;
                case "polygon":
                    result = MySqlDbType.String;
                    break;
                case "multipoint":
                    result = MySqlDbType.Int32;
                    break;
                case "multilinestring":
                    result = MySqlDbType.String;
                    break;
                case "multipolygon":
                    result = MySqlDbType.String;
                    break;
                case "geometrycollection":
                    result = MySqlDbType.Geometry;
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
            }

            return result;
        }

        private string GetCanonicalIdentifier(string identifierName)
        {
            identifierName = identifierName.Replace(" ", "_").Replace("$", "_").Replace("@", "_");

            if (identifierName.Length > 0)
            {
                if ("0123456789".IndexOf(identifierName[0]) >= 0)
                {
                    identifierName = "_" + identifierName;
                }
            }

            return identifierName;
        }

        private string BytesToString(byte[] input)
        {
            return Encoding.ASCII.GetString(input);
        }

        private string FilterSpace(string input)
        {
            while (input.StartsWith("\n", StringComparison.Ordinal) || input.StartsWith("\r", StringComparison.Ordinal) || input.StartsWith("\t", StringComparison.Ordinal) || input.StartsWith("\v", StringComparison.Ordinal) || input.StartsWith("\f", StringComparison.Ordinal) || input.StartsWith(" ", StringComparison.Ordinal))
            {
                input = input.Substring(1);
            }

            while (input.EndsWith("\n", StringComparison.Ordinal) || input.EndsWith("\r", StringComparison.Ordinal) || input.EndsWith("\t", StringComparison.Ordinal) || input.StartsWith("\v", StringComparison.Ordinal) || input.StartsWith("\f", StringComparison.Ordinal) || input.EndsWith(" ", StringComparison.Ordinal))
            {
                input = input.Substring(0, input.Length - 1);
            }

            return input;
        }

        private string FilterLengthDescriptionForReturnType(string input)
        {
            string result = input;

            if (result.Contains("("))
            {
                result = result.Substring(0, result.IndexOf("(", StringComparison.Ordinal));
            }
            
            return result;
        }

        private string FilterLengthDescription(string input)
        {
            input = FilterSpace(input);
            string result = input;
            int Len = 0;

            if (result.Contains("("))
            {
                result = result.Substring(0, result.IndexOf("(", StringComparison.Ordinal));

                string t = input.Substring(input.IndexOf("(", StringComparison.Ordinal));
                t = t.Substring(1, t.Length - 2);

                if (t.Contains(","))
                {
                    t = t.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0];
                }

                Len = int.Parse(t);
            }

            result = FilterSpace(result).Replace("`", "");

            return result + " " + Len.ToString();
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
