using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.DatabaseFactory.Convert.Model
{
    /// <summary>
    /// 数据库模型
    /// </summary>
    public class Database
    {
        /// <summary>
        /// 
        /// </summary>
        public IList<Table> Tables = new List<Table>();
        /// <summary>
        /// 
        /// </summary>
        public IList<View> Views = new List<View>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        public void AddTable(Table table)
        {
            Tables.Add(table);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="view"></param>
        public void AddView(View view)
        {
            Views.Add(view);
        }

        /// <summary>
        /// 
        /// </summary>
        public int TableCount
        {
            get
            {
                return Tables.Count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int ViewCount
        {
            get
            {
                return Views.Count;
            }
        }
    }
}
