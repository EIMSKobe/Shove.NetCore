using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Shove.DatabaseFactory
{
    /// <summary>
    /// 数据工厂的 MSSQL 类
    /// </summary>
    public class MSSQL : Factory
    {
        /// <summary>
        /// 构造
        /// </summary>
        public MSSQL() : base(string.Empty) { }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="InitConnectionString"></param>
        public MSSQL(string InitConnectionString) : base(InitConnectionString) { }

        /// <summary>
        /// CreateDatabaseConnection
        /// </summary>
        /// <returns></returns>
        protected override DbConnection __CreateDatabaseConnection()
        {
            connection = new SqlConnection(ConnectionString);
            connection.Open();

            return connection;
        }

        /// <summary>
        /// Open
        /// </summary>
        /// <param name="TableOrViewName"></param>
        /// <param name="fieldCollent"></param>
        /// <param name="Condition"></param>
        /// <param name="OrderBy"></param>
        /// <param name="LimitStart"></param>
        /// <param name="LimitCount"></param>
        /// <returns></returns>
        protected override DataTable __Open(string TableOrViewName, FieldCollect fieldCollent, string Condition, string OrderBy, int LimitStart, int LimitCount)
        {
            TableOrViewName = ReplaceDefinitionForObjectName(TableOrViewName);

            Condition = Condition.Trim();
            OrderBy = OrderBy.Trim();
            Condition = string.IsNullOrEmpty(Condition) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(Condition));
            OrderBy = string.IsNullOrEmpty(OrderBy) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(OrderBy));

            if (string.IsNullOrEmpty(OrderBy))
            {
                LimitStart = 0;
                LimitCount = 0;
            }

            if ((LimitStart >= 0) && (LimitCount > 0))
            {
                return __OpenWithLimit(TableOrViewName, fieldCollent, Condition, OrderBy, LimitStart, LimitCount);
            }

            return __OpenWithoutLimit(TableOrViewName, fieldCollent, Condition, OrderBy);
        }

        #region __Open 明细方法

        private DataTable __OpenWithLimit(string TableOrViewName, FieldCollect fieldCollent, string Condition, string OrderBy, int LimitStart, int LimitCount)
        {
            string CommandString = "select * from (select top " + (LimitStart + LimitCount).ToString() + " ";

            if ((fieldCollent == null) || (fieldCollent.Count < 1))
            {
                CommandString += "*, ";
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

                    CommandString += (FieldName + ", ");
                }
            }

            CommandString += "Row_Number() over (order by " + OrderBy + ") as __SE_ROW ";
            CommandString += "from " + TableOrViewName + " ";

            if (!string.IsNullOrEmpty(Condition))
            {
                CommandString += "where " + Condition + " ";
            }

            CommandString += "order by " + OrderBy + ") as a where __SE_ROW > " + LimitStart.ToString() + " order by __SE_ROW";

            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(CommandString, (SqlConnection)connection);

            if (transcation != null)
            {
                da.SelectCommand.Transaction = (SqlTransaction)transcation;
            }

            da.Fill(dt);

            return dt;
        }

        private DataTable __OpenWithoutLimit(string TableOrViewName, FieldCollect fieldCollent, string Condition, string OrderBy)
        {
            string CommandString = "select ";

            if ((fieldCollent == null) || (fieldCollent.Count < 1))
            {
                CommandString += "* ";
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

                    CommandString += (FieldName + ((i == (fieldCollent.Count - 1)) ? " " : ", "));
                }
            }

            CommandString += "from " + TableOrViewName + " ";

            if (!string.IsNullOrEmpty(Condition))
            {
                CommandString += "where " + Condition + " ";
            }

            if (!string.IsNullOrEmpty(OrderBy))
            {
                CommandString += "order by " + OrderBy;
            }

            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(CommandString, (SqlConnection)connection);

            if (transcation != null)
            {
                da.SelectCommand.Transaction = (SqlTransaction)transcation;
            }

            da.Fill(dt);

            return dt;
        }

        #endregion

        /// <summary>
        /// GetRowCount
        /// </summary>
        /// <param name="TableOrViewName"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        protected override long __GetRowCount(string TableOrViewName, string Condition)
        {
            TableOrViewName = ReplaceDefinitionForObjectName(TableOrViewName);

            string CommandString = "select count(*) from " + TableOrViewName + " ";

            Condition = Condition.Trim();
            Condition = string.IsNullOrEmpty(Condition) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(Condition));

            if (!string.IsNullOrEmpty(Condition))
            {
                CommandString += "where " + Condition;
            }

            SqlCommand Cmd = new SqlCommand(CommandString, (SqlConnection)connection);

            if (transcation != null)
            {
                Cmd.Transaction = (SqlTransaction)transcation;
            }

            object Result = Cmd.ExecuteScalar();

            return System.Convert.ToInt64(Result);
        }

        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="fieldCollent"></param>
        /// <param name="fieldValueCollect"></param>
        /// <returns></returns>
        protected override long __Insert(string TableName, FieldCollect fieldCollent, FieldValueCollect fieldValueCollect)
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

            TableName = ReplaceDefinitionForObjectName(TableName);

            string CommandString = "insert into " + TableName + " (";

            for (int i = 0; i < fieldCollent.Count; i++)
            {
                string FieldName = ReplaceDefinitionForObjectName(fieldCollent[i]);

                CommandString += (FieldName + ((i == (fieldCollent.Count - 1)) ? ") values (" : ", "));
            }

            int ParameterCount = 0;

            for (int i = 0; i < fieldValueCollect.Count; i++)
            {
                CommandString += (i > 0) ? ", " : "";
                object FieldValue = fieldValueCollect[i];

                if (FieldValue is FieldValueCalculate)
                {
                    throw new Exception("Insert method does not support FieldValueCalculate.");
                }

                if (FieldValue is FieldValueCalculate)
                {
                    CommandString += ((FieldValueCalculate)FieldValue).ToString();
                }
                else
                {
                    CommandString += "@p" + ParameterCount.ToString();
                    ParameterCount++;
                }
            }

            CommandString += "); select isnull(cast(scope_identity() as bigint), -99999999);";

            SqlCommand Cmd = new SqlCommand(CommandString, (SqlConnection)connection);

            ParameterCount = 0;

            for (int i = 0; i < fieldValueCollect.Count; i++)
            {
                object FieldValue = fieldValueCollect[i];

                if (FieldValue is FieldValueCalculate)
                {
                    continue;
                }

                SqlParameter p = new SqlParameter("@p" + ParameterCount.ToString(), (FieldValue == null ? System.DBNull.Value : (string.IsNullOrEmpty(FieldValue.ToString()) ? System.DBNull.Value : FieldValue)));
                Cmd.Parameters.Add(p);

                ParameterCount++;
            } 
            
            if (transcation != null)
            {
                Cmd.Transaction = (SqlTransaction)transcation;
            }

            object objResult = Cmd.ExecuteScalar();
            long Result = System.Convert.ToInt64(objResult);

            if (Result == -99999999)
            {
                return 0;
            }

            return Result;
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="fieldCollent"></param>
        /// <param name="fieldValueCollect"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        protected override long __Update(string TableName, FieldCollect fieldCollent, FieldValueCollect fieldValueCollect, string Condition)
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

            TableName = ReplaceDefinitionForObjectName(TableName);

            string CommandString = "update " + TableName + " set ";

            int ParameterCount = 0;

            for (int i = 0; i < fieldCollent.Count; i++)
            {
                string FieldName = ReplaceDefinitionForObjectName(fieldCollent[i]);
                object FieldValue = fieldValueCollect[i];

                CommandString += ((i > 0) ? ", " : "") + FieldName + " = ";

                if (FieldValue is FieldValueCalculate)
                {
                    CommandString += ((FieldValueCalculate)FieldValue).ToString();
                }
                else
                {
                    CommandString += "@p" + ParameterCount.ToString();
                    ParameterCount++;
                }
            }

            Condition = Condition.Trim();
            Condition = string.IsNullOrEmpty(Condition) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(Condition));

            if (!string.IsNullOrEmpty(Condition))
            {
                CommandString += " where " + Condition;
            }

            CommandString += "; select isnull(cast(rowcount_big() as bigint), -99999999)";

            SqlCommand Cmd = new SqlCommand(CommandString, (SqlConnection)connection);

            ParameterCount = 0;

            for (int i = 0; i < fieldValueCollect.Count; i++)
            {
                object FieldValue = fieldValueCollect[i];

                if (FieldValue is FieldValueCalculate)
                {
                    continue;
                }

                SqlParameter p = new SqlParameter("@p" + ParameterCount.ToString(), (FieldValue == null ? System.DBNull.Value : (string.IsNullOrEmpty(FieldValue.ToString()) ? System.DBNull.Value : FieldValue)));
                Cmd.Parameters.Add(p);

                ParameterCount++;
            } 
            
            if (transcation != null)
            {
                Cmd.Transaction = (SqlTransaction)transcation;
            }

            object objResult = Cmd.ExecuteScalar();
            long Result = System.Convert.ToInt64(objResult);

            if (Result == -99999999)
            {
                return 0;
            }

            return Result;
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        protected override long __Delete(string TableName, string Condition)
        {
            TableName = ReplaceDefinitionForObjectName(TableName);

            string CommandString = "delete from " + TableName + " ";

            Condition = Condition.Trim();
            Condition = string.IsNullOrEmpty(Condition) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(Condition));

            if (!string.IsNullOrEmpty(Condition))
            {
                CommandString += "where " + Condition;
            }

            CommandString += "; select isnull(cast(rowcount_big() as bigint), -99999999)";

            SqlCommand Cmd = new SqlCommand(CommandString, (SqlConnection)connection);

            if (transcation != null)
            {
                Cmd.Transaction = (SqlTransaction)transcation;
            }

            object objResult = Cmd.ExecuteScalar();
            long Result = System.Convert.ToInt64(objResult);

            if (Result == -99999999)
            {
                return 0;
            }

            return Result;
        }

        /// <summary>
        /// Create Index
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="IndexName"></param>
        /// <param name="isUnique"></param>
        /// <param name="Body"></param>
        /// <returns></returns>
        protected override int __CreateIndex(string TableName, string IndexName, bool isUnique, string Body)
        {
            __DropIndex(TableName, IndexName);

            TableName = ReplaceDefinitionForObjectName(TableName);
            IndexName = ReplaceDefinitionForObjectName(IndexName);

            SqlCommand Cmd = new SqlCommand("create " + (isUnique ? "Unique " : "") + "index " + IndexName + " on " + TableName + " (" + ReplaceDefinitionForStatement(Body) + ")", (SqlConnection)connection);

            if (transcation != null)
            {
                Cmd.Transaction = (SqlTransaction)transcation;
            }

            int Result = Cmd.ExecuteNonQuery();

            return Result;
        }

        /// <summary>
        /// Drop Index
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="IndexName"></param>
        /// <returns></returns>
        protected override int __DropIndex(string TableName, string IndexName)
        {
            TableName = ReplaceDefinitionForObjectName(TableName);
            IndexName = ReplaceDefinitionForObjectName(IndexName);

            SqlCommand Cmd = new SqlCommand("if exists (select 1 from sysindexes where id = (select OBJECT_ID('" + TableName + "')) and name = '" + RemoveDefinitionForObjectName(IndexName) + "') drop index " + TableName + "." + IndexName, (SqlConnection)connection);

            if (transcation != null)
            {
                Cmd.Transaction = (SqlTransaction)transcation;
            }

            int Result = Cmd.ExecuteNonQuery();

            return Result;
        }
        

        ///////////////////////////////////////////////////////////

        private string ReplaceDefinitionForObjectName(string input)
        {
            input = input.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' });

            if (input.StartsWith("[") && input.EndsWith("]"))
            {
                return input;
            }

            string str = input.ToLower();

            if (str.Contains("`") || str.Contains(",") || (str.Contains("(") && str.Contains(")")) || str.Contains("+") || str.Contains("-") || str.Contains("*") || str.Contains("/") || str.Contains(" as ") || str.Contains("."))
            {
                int count = Shove.String.StringAt(str, '`');
                if ((count % 2) != 0)
                {
                    throw new Exception("The number is not an even number of characters “`”.");
                }

                if (count > 0)
                {
                    for (int i = 0; i < input.Length; i++)
                    {
                        if (input[i] == '`')
                        {
                            input = Shove.String.ReplaceAt(input, (((i % 2) == 0) ? '[' : ']'), i);
                        }
                    }
                }

                return input;
            }

            return "[" + input + "]";
        }

        private string ReplaceDefinitionForStatement(string input)
        {
            return input.Replace("`", "");
        }

        private string RemoveDefinitionForObjectName(string input)
        {
            input = input.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' });

            if (input.StartsWith("[") && input.EndsWith("]"))
            {
                return input.Substring(1, input.Length - 2);
            }

            return input;
        }

        ///////////////////////////////////////////////////////////

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        protected override int __ExecuteNonQuery(string CommandString)
        {
            SqlCommand Cmd = new SqlCommand(CommandString, (SqlConnection)connection);
            if (transcation != null)
            {
                Cmd.Transaction = (SqlTransaction)transcation;
            }

            return Cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 执行读取一行一列
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        protected override object __ExecuteScalar(string CommandString)
        {
            SqlCommand Cmd = new SqlCommand(CommandString, (SqlConnection)connection);
            if (transcation != null)
            {
                Cmd.Transaction = (SqlTransaction)transcation;
            }

            return Cmd.ExecuteScalar();
        }

        /// <summary>
        /// Reader
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        protected override DbDataReader __ExecuteReader(string CommandString)
        {
            SqlCommand Cmd = new SqlCommand(CommandString, (SqlConnection)connection);
            if (transcation != null)
            {
                Cmd.Transaction = (SqlTransaction)transcation;
            }

            return Cmd.ExecuteReader();
        }

        /// <summary>
        /// 执行命令返回结果集
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        protected override DataTable __ExecuteQuery(string CommandString)
        {
            SqlDataAdapter da = new SqlDataAdapter(CommandString, (SqlConnection)connection);
            if (transcation != null)
            {
                da.SelectCommand.Transaction = (SqlTransaction)transcation;
            }

            DataTable dt = new DataTable();
            da.Fill(dt);

            return dt;
        }
    }
}
