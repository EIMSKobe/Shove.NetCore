using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;

using Shove.DatabaseFactory.Convert.Model;

namespace Shove.DatabaseFactory.Convert
{
    /// <summary>
    /// SQLite 数据库到其他数据库的转换，暂支持 MSSQL, MySQL
    /// </summary>
    public partial class Converter
    {
        //private Regex regex_view_relyon = new Regex(@"(?<L0>[^(]+?)[(](?<L1>[\d]+?)[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        //private string[] SQLKEYWOTDS = new string[] { " LEFT ", " GROUP ", " WHERE " };

        /// <summary>
        /// SQLite 数据库解析到 Model
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="ignoreViewRelyon">忽略视图依赖关系的分析</param>
        /// <returns></returns>
        private Model.Database SQLiteToModel(SQLiteConnection conn, bool ignoreViewRelyon)
        {
            if ((conn == null) || (conn.State != ConnectionState.Open))
            {
                return null;
            }

            DatabaseFactory.Convert.Model.Database model = new DatabaseFactory.Convert.Model.Database();

            #region 分析 Table、Index

            SQLiteDataAdapter da = new SQLiteDataAdapter("select name, sql from sqlite_master where type = 'table' order by name;", conn);
            DataTable dt = new DataTable();
            da.Fill(dt);

            SQLiteDataAdapter da2 = new SQLiteDataAdapter("select tbl_name, name, sql from sqlite_master where type = 'index' order by rootpage;", conn);
            DataTable dt2 = new DataTable();
            da2.Fill(dt2);

            if (dt != null)
            {
                Regex regex_index = new Regex(@"CREATE[\s\t\r\n\v\f]+?INDEX[\s\t\r\n\v\f]+?(?<L0>[^]]+?[]])[\s\t\r\n\v\f]+?on[^(]+?(?<L1>[(][^)]+?[)])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                foreach(DataRow dr in dt.Rows)
                {
                    string TableName = dr["name"].ToString();

                    if ((TableName.ToLower() == "sqlite_master") || (TableName.ToLower() == "sqlite_temp_master") || (TableName.ToLower() == "sqlite_sequence"))
                    {
                        continue;
                    }

                    IList<Field> cols = SplitColumns(conn, TableName, dr["sql"].ToString());

                    if ((cols == null) || (cols.Count < 1))
                    {
                        continue;
                    }

                    Table table = new Table(TableName);

                    for (int i = 0; i < cols.Count; i++)
                    {
                        table.AddField(cols[i]);
                    }

                    DataRow[] drs = dt2.Select("tbl_name='" + TableName + "'");

                    foreach (DataRow dr2 in drs)
                    {
                        if ((dr2["sql"] != null) && (dr2["sql"].ToString().Trim() != ""))
                        {
                            Match m = regex_index.Match(dr2["sql"].ToString());

                            if (m.Success)
                            {
                                Model.Index index = new Model.Index(m.Groups["L0"].Value, m.Groups["L1"].Value);
                                table.AddIndex(index);
                            }
                        }
                    }

                    model.AddTable(table);
                }
            }

            #endregion

            #region 分析 View

            SQLiteCommand cmd = new SQLiteCommand("select name, sql from sqlite_master where type = 'view' order by name desc;", conn);
            SQLiteDataReader dr3 = cmd.ExecuteReader();

            if (dr3 != null)
            {
                while (dr3.Read())
                {
                    string ViewName = dr3["name"].ToString();

                    if (ViewName.StartsWith("`", StringComparison.Ordinal) || ViewName.StartsWith("[", StringComparison.Ordinal) || ViewName.StartsWith("\"", StringComparison.Ordinal))
                    {
                        ViewName = ViewName.Substring(1, ViewName.Length - 2);
                    }

                    model.AddView(new View(ViewName, FilterViewStatement(dr3["sql"].ToString())));
                }
            }

            dr3.Close();

            #endregion

            #region 分析视图依赖关系

            //if (!ignoreViewRelyon)
            //{
            //    for (int i = 0; i < model.Views.Count; i++)
            //    {
            //        string viewName = model.Views[i].Name;

            //        for (int j = 0; j < model.Views.Count; j++)
            //        {
            //            if (j == i)
            //            {
            //                continue;
            //            }


            //        }
            //    }
            //}

            #endregion

            return model;
        }

        #region SQLiteToModel 的辅助方法

        private IList<Field> SplitColumns(SQLiteConnection conn, string tableName, string input)
        {
            input = input.Substring(input.IndexOf("(", StringComparison.Ordinal) + 1);
            input = input.Substring(0, input.LastIndexOf(")", StringComparison.Ordinal));

            string[] strs = input.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if ((strs == null) || (strs.Length < 1))
            {
                return null;
            }

            IList<Field> result = new List<Field>();
            string markPrimaryKeyColumnName = "";

            for (int i = 0; i < strs.Length; i++)
            {
                Field field = ExtractColumnProperties(conn, tableName, strs[i], ref markPrimaryKeyColumnName);

                if (field == null)
                {
                    if (!string.IsNullOrEmpty(markPrimaryKeyColumnName))
                    {
                        for (int j = 0; j < result.Count; j++)
                        {
                            if (result[j].Name.ToLower() == markPrimaryKeyColumnName.ToLower())
                            {
                                result[j].IsPRIMARY_KEY = true;

                                break;
                            }
                        }

                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(field.Name))
                {
                    result.Add(field);
                }
            }

            return result;
        }

        private Regex regex_primary_key = new Regex(@"^PRIMARY[\s\t\r\n\v\f]+?KEY[\s\t\r\n\v\f]+?[(](?<L0>[^)]+?)[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_primary_key_col_name = new Regex(@"[""]*?(?<L0>[^""\s\t\r\n\v\f]+?)[""\s\t\r\n\v\f]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_auto_increment = new Regex(@"[\s\t\r\n\v\f]+?AUTOINCREMENT[\s\t\r\n\v\f]+?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_primary_key2 = new Regex(@"[\s\t\r\n\v\f]+?PRIMARY[\s\t\r\n\v\f]+?KEY[\s\t\r\n\v\f]+?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_not_null = new Regex(@"[\s\t\r\n\v\f]+?NOT[\s\t\r\n\v\f]+?NULL[\s\t\r\n\v\f]+?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_default_value = new Regex(@"[\s\t\r\n\v\f]+?DEFAULT[\s\t\r\n\v\f]+?(?<L0>[^\s\t\r\n\v\f]+?)[\s\t\r\n\v\f]+?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_col_name = new Regex(@"^(?<L0>[^\s\t\r\n\v\f]+?)[\s\t\r\n\v\f]+?(?<L1>[^\s\t\r\n\v\f]+?)[\s\t\r\n\v\f]+?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex regex_datatype = new Regex(@"(?<L0>[^(]+?)[(](?<L1>[\d]+?)[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private Field ExtractColumnProperties(SQLiteConnection conn, string tableName, string input, ref string markPrimaryKeyColumnName)
        {
            input = input.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' }) + " ";
            markPrimaryKeyColumnName = "";

            string name = "";
            string dbtype = "";
            int length = 0;
            bool autoIncrement = false;
            bool PRIMARY_KEY = false;
            bool NOT_NULL = false;
            string DefaultValue = "";

            Match m = regex_primary_key.Match(input);
            if (m.Success)
            {
                string s = m.Groups["L0"].Value;
                m = regex_primary_key_col_name.Match(s);
                if (m.Success)
                {
                    markPrimaryKeyColumnName = m.Groups["L0"].Value;
                }

                return null;
            }

            autoIncrement = regex_auto_increment.IsMatch(input);
            PRIMARY_KEY = regex_primary_key2.IsMatch(input);
            NOT_NULL = regex_not_null.IsMatch(input);

            m = regex_default_value.Match(input);
            if (m.Success)
            {
                DefaultValue = m.Groups["L0"].Value;
            }

            m = regex_col_name.Match(input);
            if (!m.Success)
            {
                return null;
            }
            name = m.Groups["L0"].Value;
            if ((name.StartsWith("[", StringComparison.Ordinal) && name.EndsWith("]", StringComparison.Ordinal)) || (name.StartsWith("\"", StringComparison.Ordinal) && name.EndsWith("\"", StringComparison.Ordinal)) || (name.StartsWith("\'", StringComparison.Ordinal) && name.EndsWith("\'", StringComparison.Ordinal)))
            {
                name = name.Substring(1, name.Length - 2);
            }
            dbtype = m.Groups["L1"].Value;

            m = regex_datatype.Match(dbtype);
            if (m.Success)
            {
                dbtype = m.Groups["L0"].Value;
                length = Shove.Convert.StrToInt(m.Groups["L1"].Value, 0);
            }

            // 校准字段长度
            string t_type = dbtype.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' }).ToUpper();
            if ((t_type == "VARCHAR") || (t_type == "NVARCHAR"))
            {
                SQLiteCommand Cmd = new SQLiteCommand("select ifnull(max(length([" + name + "])), 0) as max_length from " + tableName, conn);
                SQLiteDataReader dr = Cmd.ExecuteReader();
                dr.Read();
                int max_length = Shove.Convert.StrToInt(dr[0].ToString(), 0) * 2;
                dr.Close();

                if (max_length > length)
                {
                    length = max_length;
                }
            }
            // 校准缺省值
            if (!string.IsNullOrEmpty(DefaultValue))
            {
                if ((t_type == "INT") || (t_type == "INTEGER") || (t_type == "LONG"))
                {
                    DefaultValue = Shove.Convert.StrToInt(DefaultValue, 0).ToString();
                }
                else if ((t_type == "FLOAT") || (t_type == "REAL") || (t_type == "NUMERIC"))
                {
                    DefaultValue = Shove.Convert.StrToDouble(DefaultValue, 0).ToString();
                }
            }

            return new Field(name, dbtype, length, autoIncrement, PRIMARY_KEY, NOT_NULL, DefaultValue);
        }

        private string FilterViewStatement(string input)
        {
            input = input.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' });

            if (input.StartsWith("CREATE VIEW \"", StringComparison.OrdinalIgnoreCase))
            {
                input = String.ReplaceAt(input, '[', input.IndexOf('\"'));
                input = String.ReplaceAt(input, ']', input.IndexOf('\"'));
            }

            return input;
        }

        #endregion

        /// <summary>
        /// 表行中是否存在某字段
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        private bool IsColumnExists(DataRow dr, string columnName)
        {
            object obj;

            for (int i = 0; i < dr.ItemArray.Length; i++)
            {
                try
                {
                    obj = dr[columnName];

                    return true;
                }
                catch { }
            }

            return false;
        }
    }
}
