using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Shove.Net
{
    /// <summary>
    /// IPAddress 的摘要说明。
    /// </summary>
    public class IPAddress
    {
        /// <summary>
        /// 
        /// </summary>
        public IPAddress()
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
        }

        /// <summary>
        /// IP地址转为整数
        /// </summary>
        /// <param name="IPAddress"></param>
        /// <returns></returns>
        public static Int64 IPAddressToInt64(string IPAddress)
        {
            Int64 Result = 0;
            IPAddress = IPAddress.Replace('.', '#');
            string[] strs = Regex.Split(IPAddress, "#", RegexOptions.IgnoreCase);

            try
            {
                Int64 ip1 = System.Convert.ToInt64(strs[0]);
                Int64 ip2 = System.Convert.ToInt64(strs[1]);
                Int64 ip3 = System.Convert.ToInt64(strs[2]);
                Int64 ip4 = System.Convert.ToInt64(strs[3]);

                Result = ip1 * (Int64)256 * (Int64)256 * (Int64)256;
                Result += ip2 * (Int64)256 * (Int64)256;
                Result += ip3 * (Int64)256;
                Result += ip4;
            }
            catch
            { }

            return Result;
        }

        /// <summary>
        /// 获取IP地址的地理位置
        /// </summary>
        /// <param name="IPAddress"></param>
        /// <param name="DataFileName"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(Int64 IPAddress, string DataFileName)
        {
            DataSet ds = new DataSet();

            try
            {
                ds.ReadXml(DataFileName);
            }
            catch
            {
                return "读IP地址库错误";
            }

            DataRow[] dr = ds.Tables[0].Select("IPStart <= " + IPAddress.ToString() + " and IPEnd >= " + IPAddress.ToString());
            if (dr.Length < 1)
                return "未知地址";

            string Result = dr[0]["Country"].ToString().Trim() + dr[0]["City"].ToString().Trim();
            ds.Dispose();

            return Result;
        }

        /// <summary>
        /// 获取IP地址的地理位置
        /// </summary>
        /// <param name="sIPAddress"></param>
        /// <param name="DataFileName"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(string sIPAddress, string DataFileName)
        {
            if (sIPAddress == "127.0.0.1")
                return "本机地址";
            Int64 IPAddress = IPAddressToInt64(sIPAddress);
            return GetPlaceFromIPAddress(IPAddress, DataFileName);
        }

        /// <summary>
        /// 获取IP地址的地理位置
        /// </summary>
        /// <param name="IPAddress"></param>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(Int64 IPAddress, DataSet ds)
        {
            if (ds == null)
                return "读IP地址库错误";

            DataRow[] dr = ds.Tables[0].Select("IPStart <= " + IPAddress.ToString() + " and IPEnd >= " + IPAddress.ToString());
            if (dr.Length < 1)
                return "未知地址";

            return dr[0]["Country"].ToString().Trim() + dr[0]["City"].ToString().Trim();
        }

        /// <summary>
        /// 获取IP地址的地理位置
        /// </summary>
        /// <param name="sIPAddress"></param>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(string sIPAddress, DataSet ds)
        {
            if (sIPAddress == "127.0.0.1")
                return "本机地址";
            if (ds == null)
                return "读IP地址库错误";

            Int64 IPAddress = IPAddressToInt64(sIPAddress);
            return GetPlaceFromIPAddress(IPAddress, ds);
        }

        /// <summary>
        /// 获取IP地址的地理位置
        /// </summary>
        /// <param name="IPAddress"></param>
        /// <param name="conn"></param>
        /// <param name="IPTable"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(Int64 IPAddress, SqlConnection conn, string IPTable)
        {
            if (conn.State != ConnectionState.Open)
                return "读IP数据库错误";

            SqlCommand Cmd = new SqlCommand("select top 1 ltrim(rtrim(isnull(Country, ''))) + ltrim(rtrim(isnull(City, ''))) from " + IPTable + " where IPStart <= " + IPAddress.ToString() + " and IPEnd >= " + IPAddress.ToString(), conn);
            SqlDataReader dr = null;
            try
            {
                dr = Cmd.ExecuteReader();
            }
            catch
            {
                return "读IP数据库错误";
            }

            if (!dr.Read())
            {
                dr.Close();
                return "未知地址";
            }

            string Result = dr[0].ToString();
            dr.Close();
            return Result;
        }

        /// <summary>
        /// 获取IP地址的地理位置
        /// </summary>
        /// <param name="sIPAddress"></param>
        /// <param name="conn"></param>
        /// <param name="IPTable"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(string sIPAddress, SqlConnection conn, string IPTable)
        {
            if (sIPAddress == "127.0.0.1")
                return "本机地址";

            if (conn.State != ConnectionState.Open)
                return "读IP数据库错误";

            Int64 IPAddress = IPAddressToInt64(sIPAddress);
            return GetPlaceFromIPAddress(IPAddress, conn, IPTable);
        }
    }
}
