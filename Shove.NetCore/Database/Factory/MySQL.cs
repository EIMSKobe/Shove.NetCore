using System;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace Shove.DatabaseFactory
{
    /// <summary>
    /// 数据工厂的 MySQL 类
    /// </summary>
    public class MySQL : Factory
    {
        /// <summary>
        /// 
        /// </summary>
        public MySQL() : base(string.Empty) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initConnectionString"></param>
        public MySQL(string initConnectionString) : base(initConnectionString) { }

        /// <summary>
        /// CreateDatabaseConnection
        /// </summary>
        /// <returns></returns>
        protected override DbConnection __CreateDatabaseConnection()
        {
            connection = new MySqlConnection(ConnectionString);
            connection.Open();

            return connection;
        }

        /// <summary>
        /// Open
        /// </summary>
        /// <param name="tableOrViewName"></param>
        /// <param name="fieldCollent"></param>
        /// <param name="condition"></param>
        /// <param name="orderBy"></param>
        /// <param name="limitStart"></param>
        /// <param name="limitCount"></param>
        /// <returns></returns>
        protected override DataTable __Open(string tableOrViewName, FieldCollect fieldCollent, string condition, string orderBy, int limitStart, int limitCount)
        {
            tableOrViewName = ReplaceDefinitionForObjectName(tableOrViewName);

            string commandString = "select ";

            if ((fieldCollent == null) || (fieldCollent.Count < 1))
            {
                commandString += "* ";
            }
            else
            {
                for (int i = 0; i < fieldCollent.Count; i++)
                {
                    string FieldName = fieldCollent[i].Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' });

                    if (FieldName != "*")
                    {
                        FieldName = ReplaceDefinitionForObjectName(FieldName);
                    }

                    commandString += (FieldName + ((i == (fieldCollent.Count - 1)) ? " " : ", "));
                }
            }

            commandString += "from " + tableOrViewName + " ";

            condition = condition.Trim();
            condition = string.IsNullOrEmpty(condition) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(condition));

            if (!string.IsNullOrEmpty(condition))
            {
                commandString += "where " + condition + " ";
            }

            orderBy = orderBy.Trim();
            orderBy = string.IsNullOrEmpty(orderBy) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(orderBy));

            if (!string.IsNullOrEmpty(orderBy))
            {
                commandString += "order by " + orderBy + " ";
            }

            if ((limitStart >= 0) && (limitCount > 0))
            {
                commandString += "limit " + limitStart.ToString() + ", " + limitCount.ToString();
            }

            DataTable dt = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(commandString, (MySqlConnection)connection);

            if (transcation != null)
            {
                da.SelectCommand.Transaction = (MySqlTransaction)transcation;
            }

            da.Fill(dt);

            return dt;
        }

        /// <summary>
        /// GetRowCount
        /// </summary>
        /// <param name="tableOrViewName"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        protected override long __GetRowCount(string tableOrViewName, string condition)
        {
            tableOrViewName = ReplaceDefinitionForObjectName(tableOrViewName);

            string commandString = "select count(*) from " + tableOrViewName + " ";

            condition = condition.Trim();
            condition = string.IsNullOrEmpty(condition) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(condition));

            if (!string.IsNullOrEmpty(condition))
            {
                commandString += "where " + condition;
            }

            MySqlCommand cmd = new MySqlCommand(commandString, (MySqlConnection)connection);

            if (transcation != null)
            {
                cmd.Transaction = (MySqlTransaction)transcation;
            }

            object result = cmd.ExecuteScalar();
            
            return System.Convert.ToInt64(result);
        }

        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldCollent"></param>
        /// <param name="fieldValueCollect"></param>
        /// <returns></returns>
        protected override long __Insert(string tableName, FieldCollect fieldCollent, FieldValueCollect fieldValueCollect)
        {
            if ((fieldCollent == null) || (fieldCollent.Count < 1))
            {
                throw new Exception("the fieldCollent can't is null.");
            }

            if ((fieldValueCollect == null) || (fieldValueCollect.Count < 1))
            {
                throw new Exception("the fieldValueCollect can't is null.");
            }

            if (fieldCollent.Count != fieldValueCollect.Count)
            {
                throw new Exception("fieldCollent and fieldValueCollect's Number of inconsistencies.");
            }

            tableName = ReplaceDefinitionForObjectName(tableName);

            string commandString = "insert into " + tableName + " (";

            for (int i = 0; i < fieldCollent.Count; i++)
            {
                string FieldName = ReplaceDefinitionForObjectName(fieldCollent[i]);

                commandString += (FieldName + ((i == (fieldCollent.Count - 1)) ? ") values (" : ", "));
            }

            int parameterCount = 0;

            for (int i = 0; i < fieldValueCollect.Count; i++)
            {
                commandString += (i > 0) ? ", " : "";
                object FieldValue = fieldValueCollect[i];

                if (FieldValue is FieldValueCalculate)
                {
                    throw new Exception("Insert method does not support FieldValueCalculate.");
                }

                if (FieldValue is FieldValueCalculate)
                {
                    commandString += ((FieldValueCalculate)FieldValue).ToString();
                }
                else
                {
                    commandString += "?p" + parameterCount.ToString();
                    parameterCount++;
                }
            }

            commandString += "); select ifnull(LAST_INSERT_ID(), -99999999);";

            MySqlCommand cmd = new MySqlCommand(commandString, (MySqlConnection)connection);

            parameterCount = 0;

            for (int i = 0; i < fieldValueCollect.Count; i++)
            {
                object FieldValue = fieldValueCollect[i];

                if (FieldValue is FieldValueCalculate)
                {
                    continue;
                }

                MySqlParameter p = new MySqlParameter("?p" + parameterCount.ToString(), (FieldValue == null ? System.DBNull.Value : (string.IsNullOrEmpty(FieldValue.ToString()) ? System.DBNull.Value : FieldValue)));
                cmd.Parameters.Add(p);

                parameterCount++;
            } 
            
            if (transcation != null)
            {
                cmd.Transaction = (MySqlTransaction)transcation;
            }

            object objResult = cmd.ExecuteScalar();
            long result = System.Convert.ToInt64(objResult);

            if (result == -99999999)
            {
                return 0;
            }

            return result;
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldCollent"></param>
        /// <param name="fieldValueCollect"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        protected override long __Update(string tableName, FieldCollect fieldCollent, FieldValueCollect fieldValueCollect, string condition)
        {
            if ((fieldCollent == null) || (fieldCollent.Count < 1))
            {
                throw new Exception("the fieldCollent can't is null.");
            }

            if ((fieldValueCollect == null) || (fieldValueCollect.Count < 1))
            {
                throw new Exception("the fieldValueCollect can't is null.");
            }

            if (fieldCollent.Count != fieldValueCollect.Count)
            {
                throw new Exception("fieldCollent and fieldValueCollect's Number of inconsistencies.");
            }

            tableName = ReplaceDefinitionForObjectName(tableName);

            string commandString = "update " + tableName + " set ";

            int parameterCount = 0;

            for (int i = 0; i < fieldCollent.Count; i++)
            {
                string FieldName = ReplaceDefinitionForObjectName(fieldCollent[i]);
                object FieldValue = fieldValueCollect[i];

                commandString += ((i > 0) ? ", " : "") + FieldName + " = ";

                if (FieldValue is FieldValueCalculate)
                {
                    commandString += ((FieldValueCalculate)FieldValue).ToString();
                }
                else
                {
                    commandString += "?p" + parameterCount.ToString();
                    parameterCount++;
                }
            }

            condition = condition.Trim();
            condition = string.IsNullOrEmpty(condition) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(condition));

            if (!string.IsNullOrEmpty(condition))
            {
                commandString += " where " + condition;
            }

            commandString += "; select ifnull(ROW_COUNT(), -99999999)";

            MySqlCommand cmd = new MySqlCommand(commandString, (MySqlConnection)connection);

            parameterCount = 0;

            for (int i = 0; i < fieldValueCollect.Count; i++)
            {
                object FieldValue = fieldValueCollect[i];

                if (FieldValue is FieldValueCalculate)
                {
                    continue;
                }

                MySqlParameter p = new MySqlParameter("?p" + parameterCount.ToString(), (FieldValue == null ? System.DBNull.Value : (string.IsNullOrEmpty(FieldValue.ToString()) ? System.DBNull.Value : FieldValue)));
                cmd.Parameters.Add(p);

                parameterCount++;
            }

            if (transcation != null)
            {
                cmd.Transaction = (MySqlTransaction)transcation;
            }

            object objResult = cmd.ExecuteScalar();
            long result = System.Convert.ToInt64(objResult);

            if (result == -99999999)
            {
                return 0;
            }

            return result;
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        protected override long __Delete(string tableName, string condition)
        {
            tableName = ReplaceDefinitionForObjectName(tableName);

            string commandString = "delete from " + tableName + " ";

            condition = condition.Trim();
            condition = string.IsNullOrEmpty(condition) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(condition));

            if (!string.IsNullOrEmpty(condition))
            {
                commandString += "where " + condition;
            }

            commandString += "; select ifnull(ROW_COUNT(), -99999999)";

            MySqlCommand cmd = new MySqlCommand(commandString, (MySqlConnection)connection);

            if (transcation != null)
            {
                cmd.Transaction = (MySqlTransaction)transcation;
            }

            object objResult = cmd.ExecuteScalar();
            long result = System.Convert.ToInt64(objResult);

            if (result == -99999999)
            {
                return 0;
            }

            return result;
        }

        /// <summary>
        /// Create Index
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="indexName"></param>
        /// <param name="isUnique"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        protected override int __CreateIndex(string tableName, string indexName, bool isUnique, string body)
        {
            __DropIndex(tableName, indexName);

            tableName = ReplaceDefinitionForObjectName(tableName);
            indexName = ReplaceDefinitionForObjectName(indexName);

            MySqlCommand cmd = new MySqlCommand("create " + (isUnique ? "Unique " : "") + "index " + indexName + " on " + tableName + " (" + ReplaceDefinitionForStatement(body) + ")", (MySqlConnection)connection);

            if (transcation != null)
            {
                cmd.Transaction = (MySqlTransaction)transcation;
            }

            int result = cmd.ExecuteNonQuery();

            return result;
        }

        /// <summary>
        /// Drop Index
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="indexName"></param>
        /// <returns></returns>
        protected override int __DropIndex(string tableName, string indexName)
        {
            tableName = ReplaceDefinitionForObjectName(tableName);
            indexName = ReplaceDefinitionForObjectName(indexName);

            MySqlCommand cmd = new MySqlCommand("drop index " + indexName + " on " + tableName, (MySqlConnection)connection);

            if (transcation != null)
            {
                cmd.Transaction = (MySqlTransaction)transcation;
            }

            int result = -1;

            try
            {
                result = cmd.ExecuteNonQuery();
            }
            catch { }

            return result;
        }

        ///////////////////////////////////////////////////////////

        private string ReplaceDefinitionForObjectName(string input)
        {
            input = input.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' });

            if (input.StartsWith("`", StringComparison.Ordinal) && input.EndsWith("`", StringComparison.Ordinal))
            {
                return input;
            }

            string str = input.ToLower();

            if (str.Contains("[") || str.Contains("]") || str.Contains(",") || (str.Contains("(") && str.Contains(")")) || str.Contains("+") || str.Contains("-") || str.Contains("*") || str.Contains("/") || str.Contains(" as ") || str.Contains("."))
            {
                int count = String.StringAt(str, '[') + String.StringAt(str, ']');
                if ((count % 2) != 0)
                {
                    throw new Exception("The number is not an even number of characters “[” and “]”.");
                }

                if (count > 0)
                {
                    input = input.Replace("[", "`").Replace("]", "`");
                }

                return input;
            }

            return "`" + input + "`";
        }

        private string ReplaceDefinitionForStatement(string input)
        {
            return input.Replace("[", "`").Replace("]", "`");
        }

        private string RemoveDefinitionForObjectName(string input)
        {
            input = input.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' });

            if (input.StartsWith("`", StringComparison.Ordinal) && input.EndsWith("`", StringComparison.Ordinal))
            {
                return input.Substring(1, input.Length - 2);
            }

            return input;
        }

        ///////////////////////////////////////////////////////////

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="commandString"></param>
        /// <returns></returns>
        protected override int __ExecuteNonQuery(string commandString)
        {
            MySqlCommand cmd = new MySqlCommand(commandString, (MySqlConnection)connection);
            if (transcation != null)
            {
                cmd.Transaction = (MySqlTransaction)transcation;
            }

            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 执行读取一行一列
        /// </summary>
        /// <param name="commandString"></param>
        /// <returns></returns>
        protected override object __ExecuteScalar(string commandString)
        {
            MySqlCommand cmd = new MySqlCommand(commandString, (MySqlConnection)connection);
            if (transcation != null)
            {
                cmd.Transaction = (MySqlTransaction)transcation;
            }

            return cmd.ExecuteScalar();
        }

        /// <summary>
        /// Reader
        /// </summary>
        /// <param name="commandString"></param>
        /// <returns></returns>
        protected override DbDataReader __ExecuteReader(string commandString)
        {
            MySqlCommand cmd = new MySqlCommand(commandString, (MySqlConnection)connection);
            if (transcation != null)
            {
                cmd.Transaction = (MySqlTransaction)transcation;
            }

            return cmd.ExecuteReader();
        }

        /// <summary>
        /// 执行命令返回结果集
        /// </summary>
        /// <param name="commandString"></param>
        /// <returns></returns>
        protected override DataTable __ExecuteQuery(string commandString)
        {
            MySqlDataAdapter da = new MySqlDataAdapter(commandString, (MySqlConnection)connection);
            if (transcation != null)
            {
                da.SelectCommand.Transaction = (MySqlTransaction)transcation;
            }

            DataTable dt = new DataTable();
            da.Fill(dt);

            return dt;
        }
    }
}
