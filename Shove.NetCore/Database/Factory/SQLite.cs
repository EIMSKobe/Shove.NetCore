using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace Shove.DatabaseFactory
{
    /// <summary>
    /// 数据工厂的 SQLite 类
    /// </summary>
    public class SQLite : Factory
    {
        /// <summary>
        /// 
        /// </summary>
        public SQLite() : base(string.Empty) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="InitConnectionString"></param>
        public SQLite(string InitConnectionString) : base(InitConnectionString) { }

        /// <summary>
        /// CreateDatabaseConnection
        /// </summary>
        /// <returns></returns>
        protected override DbConnection __CreateDatabaseConnection()
        {
            connection = new SQLiteConnection(ConnectionString);
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

            Condition = Condition.Trim();
            Condition = string.IsNullOrEmpty(Condition) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(Condition));

            if (!string.IsNullOrEmpty(Condition))
            {
                CommandString += "where " + Condition + " ";
            }

            OrderBy = OrderBy.Trim();
            OrderBy = string.IsNullOrEmpty(OrderBy) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(OrderBy));

            if (!string.IsNullOrEmpty(OrderBy))
            {
                CommandString += "order by " + OrderBy + " ";
            }

            if ((LimitStart >= 0) && (LimitCount > 0))
            {
                CommandString += "limit " + LimitStart.ToString() + ", " + LimitCount.ToString();
            }

            WriteCommandToLogger(1, CommandString);

            DataTable dt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(CommandString, (SQLiteConnection)connection);

            if (transcation != null)
            {
                da.SelectCommand.Transaction = (SQLiteTransaction)transcation;
            }

            da.Fill(dt);

            return dt;
        }

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

            WriteCommandToLogger(2, CommandString);

            SQLiteCommand Cmd = new SQLiteCommand(CommandString, (SQLiteConnection)connection);

            if (transcation != null)
            {
                Cmd.Transaction = (SQLiteTransaction)transcation;
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
            if (DatabaseReadOnlyState)
            {
                return -9999;
            }

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

            CommandString += "); select ifnull(last_insert_rowid(), -99999999);";

            SQLiteCommand Cmd = new SQLiteCommand(CommandString, (SQLiteConnection)connection);

            ParameterCount = 0;
            for (int i = 0; i < fieldValueCollect.Count; i++)
            {
                object FieldValue = fieldValueCollect[i];

                if (FieldValue is FieldValueCalculate)
                {
                    continue;
                }

                SQLiteParameter p = new SQLiteParameter("@p" + ParameterCount.ToString(), (FieldValue == null ? System.DBNull.Value : (string.IsNullOrEmpty(FieldValue.ToString()) ? System.DBNull.Value : FieldValue)));
                Cmd.Parameters.Add(p);

                ParameterCount++;
            }

            if (transcation != null)
            {
                Cmd.Transaction = (SQLiteTransaction)transcation;
            }

            WriteCommandToLogger(3, CommandString + "\tParameterCount: " + ParameterCount.ToString());

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
            if (DatabaseReadOnlyState)
            {
                return -9999;
            }

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

            CommandString += "; select ifnull(changes(), -99999999);";

            SQLiteCommand Cmd = new SQLiteCommand(CommandString, (SQLiteConnection)connection);

            ParameterCount = 0;

            for (int i = 0; i < fieldValueCollect.Count; i++)
            {
                object FieldValue = fieldValueCollect[i];

                if (FieldValue is FieldValueCalculate)
                {
                    continue;
                }

                SQLiteParameter p = new SQLiteParameter("@p" + ParameterCount.ToString(), (FieldValue == null ? System.DBNull.Value : (string.IsNullOrEmpty(FieldValue.ToString()) ? System.DBNull.Value : FieldValue)));
                Cmd.Parameters.Add(p);

                ParameterCount++;
            }

            if (transcation != null)
            {
                Cmd.Transaction = (SQLiteTransaction)transcation;
            }

            WriteCommandToLogger(4, CommandString + "\tParameterCount: " + ParameterCount.ToString());

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
            if (DatabaseReadOnlyState)
            {
                return -9999;
            }

            TableName = ReplaceDefinitionForObjectName(TableName);

            string CommandString = "delete from " + TableName + " ";

            Condition = Condition.Trim();
            Condition = string.IsNullOrEmpty(Condition) ? "" : FilteSqlInfusionForCondition(ReplaceDefinitionForStatement(Condition));

            if (!string.IsNullOrEmpty(Condition))
            {
                CommandString += "where " + Condition;
            }

            CommandString += "; select ifnull(changes(), -99999999)";

            SQLiteCommand Cmd = new SQLiteCommand(CommandString, (SQLiteConnection)connection);

            if (transcation != null)
            {
                Cmd.Transaction = (SQLiteTransaction)transcation;
            }

            WriteCommandToLogger(5, CommandString);

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

            SQLiteCommand Cmd = new SQLiteCommand("create " + (isUnique ? "Unique " : "") + "index " + IndexName + " on " + TableName + " (" + ReplaceDefinitionForStatement(Body) + ")", (SQLiteConnection)connection);

            if (transcation != null)
            {
                Cmd.Transaction = (SQLiteTransaction)transcation;
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
            //TableName = ReplaceDefinitionForObjectName(TableName);
            IndexName = ReplaceDefinitionForObjectName(IndexName);

            SQLiteCommand Cmd = new SQLiteCommand("drop index " + IndexName, (SQLiteConnection)connection);

            if (transcation != null)
            {
                Cmd.Transaction = (SQLiteTransaction)transcation;
            }

            int Result = -1;

            try
            {
                Result = Cmd.ExecuteNonQuery();
            }
            catch { }

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

        private static string isWriteCommandToLogger;
        private static Shove.IO.Log log;
        private static void WriteCommandToLogger(int No, string CommandString)
        {
            if (string.IsNullOrEmpty(isWriteCommandToLogger))
            {
                isWriteCommandToLogger = System.Configuration.ConfigurationManager.AppSettings["isWriteCommandToLogger"];

                if (string.IsNullOrEmpty(isWriteCommandToLogger))
                {
                    isWriteCommandToLogger = "False";
                }
            }

            if (isWriteCommandToLogger.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' }).ToLower() == "true")
            {
                if (log == null)
                {
                    log = new Shove.IO.Log("DataCommand");
                }

                log.Write(No.ToString() + ", " + CommandString);
            }
        }

        ///////////////////////////////////////////////////////////

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        protected override int __ExecuteNonQuery(string CommandString)
        {
            if (DatabaseReadOnlyState)
            {
                return -9999;
            }

            SQLiteCommand Cmd = new SQLiteCommand(CommandString, (SQLiteConnection)connection);
            if (transcation != null)
            {
                Cmd.Transaction = (SQLiteTransaction)transcation;
            }

            WriteCommandToLogger(11, CommandString);

            return Cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 执行读取一行一列
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        protected override object __ExecuteScalar(string CommandString)
        {
            SQLiteCommand Cmd = new SQLiteCommand(CommandString, (SQLiteConnection)connection);
            if (transcation != null)
            {
                Cmd.Transaction = (SQLiteTransaction)transcation;
            }

            WriteCommandToLogger(12, CommandString);

            return Cmd.ExecuteScalar();
        }

        /// <summary>
        /// Reader
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        protected override DbDataReader __ExecuteReader(string CommandString)
        {
            SQLiteCommand Cmd = new SQLiteCommand(CommandString, (SQLiteConnection)connection);
            if (transcation != null)
            {
                Cmd.Transaction = (SQLiteTransaction)transcation;
            }

            WriteCommandToLogger(13, CommandString);

            return Cmd.ExecuteReader();
        }

        /// <summary>
        /// 执行命令返回结果集
        /// </summary>
        /// <param name="CommandString"></param>
        /// <returns></returns>
        protected override DataTable __ExecuteQuery(string CommandString)
        {
            SQLiteDataAdapter da = new SQLiteDataAdapter(CommandString, (SQLiteConnection)connection);
            if (transcation != null)
            {
                da.SelectCommand.Transaction = (SQLiteTransaction)transcation;
            }

            WriteCommandToLogger(14, CommandString);

            DataTable dt = new DataTable();
            da.Fill(dt);

            return dt;
        }
    }
}
