using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Shove.Net
{
    /// <summary>
    /// IPAddress ��ժҪ˵����
    /// </summary>
    public class IPAddress
    {
        /// <summary>
        /// 
        /// </summary>
        public IPAddress()
        {
            //
            // TODO: �ڴ˴���ӹ��캯���߼�
            //
        }

        /// <summary>
        /// IP��ַתΪ����
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static Int64 IPAddressToInt64(string ip)
        {
            Int64 Result = 0;
            ip = ip.Replace('.', '#');
            string[] strs = Regex.Split(ip, "#", RegexOptions.IgnoreCase);

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
        /// ��ȡIP��ַ�ĵ���λ��
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="dataFileName"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(Int64 ip, string dataFileName)
        {
            DataSet ds = new DataSet();

            try
            {
                ds.ReadXml(dataFileName);
            }
            catch
            {
                return "��IP��ַ�����";
            }

            DataRow[] dr = ds.Tables[0].Select("IPStart <= " + ip.ToString() + " and IPEnd >= " + ip.ToString());
            if (dr.Length < 1)
                return "δ֪��ַ";

            string Result = dr[0]["Country"].ToString().Trim() + dr[0]["City"].ToString().Trim();
            ds.Dispose();

            return Result;
        }

        /// <summary>
        /// ��ȡIP��ַ�ĵ���λ��
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="dataFileName"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(string ip, string dataFileName)
        {
            if (ip == "127.0.0.1")
                return "������ַ";
            Int64 address = IPAddressToInt64(ip);
            return GetPlaceFromIPAddress(address, dataFileName);
        }

        /// <summary>
        /// ��ȡIP��ַ�ĵ���λ��
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(Int64 ip, DataSet ds)
        {
            if (ds == null)
                return "��IP��ַ�����";

            DataRow[] dr = ds.Tables[0].Select("IPStart <= " + ip.ToString() + " and IPEnd >= " + ip.ToString());
            if (dr.Length < 1)
                return "δ֪��ַ";

            return dr[0]["Country"].ToString().Trim() + dr[0]["City"].ToString().Trim();
        }

        /// <summary>
        /// ��ȡIP��ַ�ĵ���λ��
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(string ip, DataSet ds)
        {
            if (ip == "127.0.0.1")
                return "������ַ";
            if (ds == null)
                return "��IP��ַ�����";

            Int64 address = IPAddressToInt64(ip);
            return GetPlaceFromIPAddress(address, ds);
        }

        /// <summary>
        /// ��ȡIP��ַ�ĵ���λ��
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="conn"></param>
        /// <param name="IPTable"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(Int64 ip, SqlConnection conn, string IPTable)
        {
            if (conn.State != ConnectionState.Open)
                return "��IP���ݿ����";

            SqlCommand Cmd = new SqlCommand("select top 1 ltrim(rtrim(isnull(Country, ''))) + ltrim(rtrim(isnull(City, ''))) from " + IPTable + " where IPStart <= " + ip.ToString() + " and IPEnd >= " + ip.ToString(), conn);
            SqlDataReader dr = null;
            try
            {
                dr = Cmd.ExecuteReader();
            }
            catch
            {
                return "��IP���ݿ����";
            }

            if (!dr.Read())
            {
                dr.Close();
                return "δ֪��ַ";
            }

            string Result = dr[0].ToString();
            dr.Close();
            return Result;
        }

        /// <summary>
        /// ��ȡIP��ַ�ĵ���λ��
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="conn"></param>
        /// <param name="IPTable"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(string ip, SqlConnection conn, string IPTable)
        {
            if (ip == "127.0.0.1")
                return "������ַ";

            if (conn.State != ConnectionState.Open)
                return "��IP���ݿ����";

            Int64 address = IPAddressToInt64(ip);
            return GetPlaceFromIPAddress(address, conn, IPTable);
        }
    }
}
