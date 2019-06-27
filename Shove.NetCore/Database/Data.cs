using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace Shove.Database
{
    /// <summary>
    /// Data 相关，System.Data 的扩展
    /// </summary>
    public class Data
    {
        /// <summary>
        /// 将 DataTable 进行过滤
        /// </summary>
        /// <param name="dt">源 DataTable</param>
        /// <param name="condition">过滤条件</param>
        /// <param name="limitStart">开始行号</param>
        /// <param name="rowCount">总行数</param>
        /// <returns></returns>
        public static DataTable FilterDataTableData(DataTable dt, string condition, long limitStart, long rowCount)
        {
            if (dt == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(condition) && ((limitStart < 0) || (rowCount < 1)))
            {
                return dt;
            }

            DataRow[] drs = null;

            if (!string.IsNullOrEmpty(condition))
            {
                drs = dt.Select(condition);
            }
            else
            {
                drs = new DataRow[dt.Rows.Count];
                dt.Rows.CopyTo(drs, 0);
            }

            DataTable dt2 = dt.Clone();

            if ((limitStart >= 0) && (rowCount >= 1))
            {
                if (limitStart >= drs.LongLength)
                {
                    return dt2;
                }

                if ((limitStart + rowCount) > drs.LongLength)
                {
                    rowCount = drs.LongLength - limitStart;
                }

                DataRow[] drs2 = new DataRow[drs.LongLength];
                drs.CopyTo(drs2, 0);

                drs = new DataRow[rowCount];

                for (long i = limitStart; i < limitStart + rowCount; i++)
                {
                    drs[i - limitStart] = drs2[i];
                }
            }

            foreach (DataRow dr in drs)
            {
                DataRow NewRow = dt2.NewRow();
                NewRow.ItemArray = dr.ItemArray;

                dt2.Rows.Add(NewRow);
            }

            return dt2;
        }
    }
}