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
        /// <param name="Condition">过滤条件</param>
        /// <param name="LimitStart">开始行号</param>
        /// <param name="RowCount">总行数</param>
        /// <returns></returns>
        public static DataTable FilterDataTableData(DataTable dt, string Condition, long LimitStart, long RowCount)
        {
            if (dt == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(Condition) && ((LimitStart < 0) || (RowCount < 1)))
            {
                return dt;
            }

            DataRow[] drs = null;

            if (!string.IsNullOrEmpty(Condition))
            {
                drs = dt.Select(Condition);
            }
            else
            {
                drs = new DataRow[dt.Rows.Count];
                dt.Rows.CopyTo(drs, 0);
            }

            DataTable dt2 = dt.Clone();

            if ((LimitStart >= 0) && (RowCount >= 1))
            {
                if (LimitStart >= drs.LongLength)
                {
                    return dt2;
                }

                if ((LimitStart + RowCount) > drs.LongLength)
                {
                    RowCount = drs.LongLength - LimitStart;
                }

                DataRow[] drs2 = new DataRow[drs.LongLength];
                drs.CopyTo(drs2, 0);

                drs = new DataRow[RowCount];

                for (long i = LimitStart; i < LimitStart + RowCount; i++)
                {
                    drs[i - LimitStart] = drs2[i];
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