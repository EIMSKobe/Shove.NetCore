using System;
using System.Collections.Generic;
using System.Web;

namespace Shove.Gateways.Weixin.Shanghu
{
    /// <summary>
    /// 通用、配置类
    /// </summary>
    public class Utility
    {
        #region Config 

        /// <summary>
        /// 凭证 (微信分配的公众账号 ID)
        /// </summary>
        public static string AppID = string.Empty;

        /// <summary>
        /// 商户号 (微信支付分配的商户号)
        /// </summary>
        public static string MchID = string.Empty;

        /// <summary>
        /// 商户支付密钥 Key
        /// <para>
        ///     * 注意：是商户平台支付独立使用的 Key
        /// </para>
        /// </summary>
        public static string PayKey = string.Empty;

        #region System Config

        /// <summary>
        /// 微信统一下单支付 Url
        /// </summary>
        public readonly static string WeChatPayUrl = "https://api.mch.weixin.qq.com/pay/unifiedorder";

        /// <summary>
        /// 签名编码集
        /// </summary>
        private static string Encoding = "utf-8";

        #endregion

        /// <summary>
        /// 初始化 Pay Agragment
        /// </summary>
        /// <param name="_AppID">凭证 (微信分配的公众账号 ID)</param>
        /// <param name="_MchID">商户号 (微信支付分配的商户号)</param>
        /// <param name="_PayKey">
        ///     密钥 Key
        ///     <para>* 注意：是商户平台支付独立使用的 Key</para
        /// ></param>
        public static void InitializePayConfig(string _AppID, string _MchID, string _PayKey)
        {
            AppID = _AppID;
            MchID = _MchID;
            PayKey = _PayKey;
        }

        /// <summary>
        /// 带 XML 参数的请求
        /// </summary>
        /// <param name="url">请求服务器路径</param>
        /// <param name="data">json数据</param>
        /// <param name="errorDescription">错误描述</param>
        /// <param name="xmlResult">XML 结果</param>
        /// <returns></returns>
        public bool Post(string url, string data, ref string errorDescription, ref string xmlResult)
        {
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                client.Headers["Content-Type"] = "application/xml";
                client.Encoding = System.Text.Encoding.UTF8;
                string by = string.Empty;

                try
                {
                    by = client.UploadString(url, "post", data);
                }
                catch (Exception ex)
                {
                    errorDescription = ex.Message;

                    return false;
                }

                xmlResult = by;

                return true;
            }
        }

        #endregion

        #region Tools

        /// <summary>
        /// 构建签名
        /// </summary>
        /// <param name="sParaTemp"></param>
        /// <param name="_key"></param>
        /// <returns></returns>
        public string CreateSign(SortedDictionary<string, string> sParaTemp, string _key)
        {
            //待签名请求参数数组
            Dictionary<string, string> sPara = new Dictionary<string, string>();
            //签名结果
            string mysign = "";

            //过滤签名参数数组
            sPara = FilterPara(sParaTemp);

            //获得签名结果
            mysign = BuildMysign(sPara, _key, "md5", Utility.Encoding);
            return mysign;
        }

        /// <summary>
        /// 除去数组中的空值和签名参数并以字母a到z的顺序排序
        /// </summary>
        /// <param name="dicArrayPre">过滤前的参数组</param>
        /// <returns>过滤后的参数组</returns>
        private Dictionary<string, string> FilterPara(SortedDictionary<string, string> dicArrayPre)
        {
            Dictionary<string, string> dicArray = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> temp in dicArrayPre)
            {
                if (temp.Key.ToLower() != "sign" && temp.Key.ToLower() != "sign_type" && temp.Value != null && temp.Value != "")
                {
                    dicArray.Add(temp.Key.ToLower(), temp.Value);
                }
            }

            return dicArray;
        }

        /// <summary>
        /// 生成签名结果
        /// </summary>
        /// <param name="dicArray">要签名的数组</param>
        /// <param name="key">安全校验码</param>
        /// <param name="sign_type">签名类型</param>
        /// <param name="_input_charset">编码格式</param>
        /// <returns>签名结果字符串</returns>
        private string BuildMysign(Dictionary<string, string> dicArray, string key, string sign_type, string _input_charset)
        {
            string prestr = CreateLinkString(dicArray);  //把数组所有元素，按照“参数=参数值”的模式用“&”字符拼接成字符串

            prestr = prestr + "&key=" + key;                      //把拼接后的字符串再与安全校验码直接连接起来
            string mysign = Sign(prestr, sign_type, _input_charset);    //把最终的字符串签名，获得签名结果

            return mysign;
        }

        /// <summary>
        /// 把数组所有元素，按照“参数=参数值”的模式用“＆”字符拼接成字符串
        /// </summary>
        /// <param name="dicArray">需要拼接的数组</param>
        /// <returns>拼接完成以后的字符串</returns>
        private string CreateLinkString(Dictionary<string, string> dicArray)
        {
            System.Text.StringBuilder prestr = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, string> temp in dicArray)
            {
                prestr.Append(temp.Key + "=" + temp.Value + "&");
            }
            
            int nLen = prestr.Length;
            prestr.Remove(nLen - 1, 1);

            return prestr.ToString();
        }

        /// <summary>
        /// 签名字符串
        /// </summary>
        /// <param name="prestr">需要签名的字符串</param>
        /// <param name="sign_type">签名类型</param>
        /// <param name="_input_charset">编码格式</param>
        /// <returns>签名结果</returns>
        private string Sign(string prestr, string sign_type, string _input_charset)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(32);
            if (sign_type.ToUpper() == "MD5")
            {
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] t = md5.ComputeHash(System.Text.Encoding.GetEncoding(_input_charset).GetBytes(prestr));
                for (int i = 0; i < t.Length; i++)
                {
                    sb.Append(t[i].ToString("x").PadLeft(2, '0'));
                }
            }
            return sb.ToString();
        }

        #endregion
    }
}