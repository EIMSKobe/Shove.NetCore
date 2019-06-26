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
        /// ��ȡIP��ַ�ĵ���λ��
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
                return "��IP��ַ�����";
            }

            DataRow[] dr = ds.Tables[0].Select("IPStart <= " + IPAddress.ToString() + " and IPEnd >= " + IPAddress.ToString());
            if (dr.Length < 1)
                return "δ֪��ַ";

            string Result = dr[0]["Country"].ToString().Trim() + dr[0]["City"].ToString().Trim();
            ds.Dispose();

            return Result;
        }

        /// <summary>
        /// ��ȡIP��ַ�ĵ���λ��
        /// </summary>
        /// <param name="sIPAddress"></param>
        /// <param name="DataFileName"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(string sIPAddress, string DataFileName)
        {
            if (sIPAddress == "127.0.0.1")
                return "������ַ";
            Int64 IPAddress = IPAddressToInt64(sIPAddress);
            return GetPlaceFromIPAddress(IPAddress, DataFileName);
        }

        /// <summary>
        /// ��ȡIP��ַ�ĵ���λ��
        /// </summary>
        /// <param name="IPAddress"></param>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(Int64 IPAddress, DataSet ds)
        {
            if (ds == null)
                return "��IP��ַ�����";

            DataRow[] dr = ds.Tables[0].Select("IPStart <= " + IPAddress.ToString() + " and IPEnd >= " + IPAddress.ToString());
            if (dr.Length < 1)
                return "δ֪��ַ";

            return dr[0]["Country"].ToString().Trim() + dr[0]["City"].ToString().Trim();
        }

        /// <summary>
        /// ��ȡIP��ַ�ĵ���λ��
        /// </summary>
        /// <param name="sIPAddress"></param>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(string sIPAddress, DataSet ds)
        {
            if (sIPAddress == "127.0.0.1")
                return "������ַ";
            if (ds == null)
                return "��IP��ַ�����";

            Int64 IPAddress = IPAddressToInt64(sIPAddress);
            return GetPlaceFromIPAddress(IPAddress, ds);
        }

        /// <summary>
        /// ��ȡIP��ַ�ĵ���λ��
        /// </summary>
        /// <param name="IPAddress"></param>
        /// <param name="conn"></param>
        /// <param name="IPTable"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(Int64 IPAddress, SqlConnection conn, string IPTable)
        {
            if (conn.State != ConnectionState.Open)
                return "��IP���ݿ����";

            SqlCommand Cmd = new SqlCommand("select top 1 ltrim(rtrim(isnull(Country, ''))) + ltrim(rtrim(isnull(City, ''))) from " + IPTable + " where IPStart <= " + IPAddress.ToString() + " and IPEnd >= " + IPAddress.ToString(), conn);
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
        /// <param name="sIPAddress"></param>
        /// <param name="conn"></param>
        /// <param name="IPTable"></param>
        /// <returns></returns>
        public static string GetPlaceFromIPAddress(string sIPAddress, SqlConnection conn, string IPTable)
        {
            if (sIPAddress == "127.0.0.1")
                return "������ַ";

            if (conn.State != ConnectionState.Open)
                return "��IP���ݿ����";

            Int64 IPAddress = IPAddressToInt64(sIPAddress);
            return GetPlaceFromIPAddress(IPAddress, conn, IPTable);
        }
    }
}
