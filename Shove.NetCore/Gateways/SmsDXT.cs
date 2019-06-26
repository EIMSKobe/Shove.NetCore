using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

using LitJson;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace Shove.Gateways
{
    /// <summary>
    /// 封装的 http://dxtapi.xf.cc/smg 晓风短信通 HTTP 协议网关
    /// </summary>
    public class SmsDXT
    {
        /// <summary>
        /// 短信发送状态
        /// </summary>
        public class SendState
        {
            string sysId;
            int state;
            string description;

            /// <summary>
            /// SendState
            /// </summary>
            /// <param name="sysId"></param>
            /// <param name="state"></param>
            /// <param name="description"></param>
            public SendState(string sysId, int state, string description)
            {
                this.sysId = sysId;
                this.state = state;
                this.description = description;
            }
        }

        private static string gatewayUrl = "http://dxtapi.xf.cc/smg";

        /// <summary>
        /// Adds the template.
        /// </summary>
        /// <returns>The template.</returns>
        /// <param name="account">Account.</param>
        /// <param name="key">Key.</param>
        /// <param name="templateId">Template identifier.</param>
        /// <param name="content">Content.</param>
        /// <param name="templateCode">Template code.</param>
        /// <param name="errorDescription">Error description.</param>
        public static int AddTemplate(string account, string key, string templateId, string content, ref string templateCode, ref string errorDescription)
        {
            templateCode = "";
            errorDescription = "";

            string body = string.Format(
            @"{{
                ""action"":""AddTemplate"",
                ""templateId"":""{0}"",
                ""content"":""{1}"",
                ""version"":""1.0.0""
            }}", templateId, content);

            JsonData json = null;
            int ret = request(account, key, body, ref json, ref errorDescription);
            if (ret != 0)
            {
                return ret;
            }

            string res_templateCode = "";

            try
            {
                res_templateCode = json["templateCode"].ToString();
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return -10001;
            }

            templateCode = res_templateCode;

            return 0;
        }

        /// <summary>
        /// Queries the template.
        /// </summary>
        /// <returns>The template.</returns>
        /// <param name="account">Account.</param>
        /// <param name="key">Key.</param>
        /// <param name="templateCode">Template code.</param>
        /// <param name="templateState">If set to <c>true</c> template state.</param>
        /// <param name="errorDescription">Error description.</param>
        public static int QueryTemplate(string account, string key, string templateCode, ref bool templateState, ref string errorDescription)
        {
            templateState = false;
            errorDescription = "";

            string body = string.Format(
            @"{{
                ""action"":""QueryTemplate"",
                ""templateCode"":""{0}"",
                ""version"":""1.0.0""
            }}", templateCode);

            JsonData json = null;
            int ret = request(account, key, body, ref json, ref errorDescription);
            if (ret != 0)
            {
                return ret;
            }

            string res_templateState = "False";

            try
            {
                res_templateState = json["templateState"].ToString();
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return -10001;
            }

            templateState = Convert.StrToBool(res_templateState, false);

            return 0;
        }

        /// <summary>
        /// Adds the sign.
        /// </summary>
        /// <returns>The sign.</returns>
        /// <param name="account">Account.</param>
        /// <param name="key">Key.</param>
        /// <param name="signId">Sign identifier.</param>
        /// <param name="content">Content.</param>
        /// <param name="signCode">Sign code.</param>
        /// <param name="errorDescription">Error description.</param>
        public static int AddSign(string account, string key, string signId, string content, ref string signCode, ref string errorDescription)
        {
            signCode = "";
            errorDescription = "";

            string body = string.Format(
            @"{{
                ""action"":""AddSign"",
                ""signId"":""{0}"",
                ""content"":""{1}"",
                ""version"":""1.0.0""
            }}", signId, content);

            JsonData json = null;
            int ret = request(account, key, body, ref json, ref errorDescription);
            if (ret != 0)
            {
                return ret;
            }

            string res_signCode = "";

            try
            {
                res_signCode = json["signeCode"].ToString();
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return -10001;
            }

            signCode = res_signCode;

            return 0;
        }

        /// <summary>
        /// Queries the sign.
        /// </summary>
        /// <returns>The sign.</returns>
        /// <param name="account">Account.</param>
        /// <param name="key">Key.</param>
        /// <param name="signCode">Sign code.</param>
        /// <param name="signState">If set to <c>true</c> sign state.</param>
        /// <param name="errorDescription">Error description.</param>
        public static int QuerySign(string account, string key, string signCode, ref bool signState, ref string errorDescription)
        {
            signState = false;
            errorDescription = "";

            string body = string.Format(
            @"{{
                ""action"":""QuerySign"",
                ""signCode"":""{0}"",
                ""version"":""1.0.0""
            }}", signCode);

            JsonData json = null;
            int ret = request(account, key, body, ref json, ref errorDescription);
            if (ret != 0)
            {
                return ret;
            }

            string res_signState = "False";

            try
            {
                res_signState = json["signState"].ToString();
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return -10001;
            }

            signState = Convert.StrToBool(res_signState, false);

            return 0;
        }

        /// <summary>
        /// Queries the balance.
        /// </summary>
        /// <returns>The balance.</returns>
        /// <param name="account">Account.</param>
        /// <param name="key">Key.</param>
        /// <param name="balance">Balance.</param>
        /// <param name="balanceMarketing">balanceMarketing.</param>
        /// <param name="balanceNotice">balanceNotice.</param>
        /// <param name="errorDescription">Error description.</param>
        public static int QueryBalance(string account, string key, ref long balance, ref long balanceMarketing, ref long balanceNotice, ref string errorDescription)
        {
            balance = 0L;
            errorDescription = "";

            string body = string.Format(
            @"{{
                ""action"":""QueryBalance"",
                ""version"":""1.0.0""
            }}");

            JsonData json = null;
            int ret = request(account, key, body, ref json, ref errorDescription);
            if (ret != 0)
            {
                return ret;
            }

            string res_balance = "0";
            string res_balanceMarketing = "0";
            string res_balanceNotice = "0";

            try
            {
                res_balance = json["balance"].ToString();
                res_balanceMarketing = json["balanceMarketing"].ToString();
                res_balanceNotice = json["balanceNotice"].ToString();
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return -10001;
            }

            balance = Convert.StrToLong(res_balance, 0L);
            balanceMarketing = Convert.StrToLong(res_balanceMarketing, 0L);
            balanceNotice = Convert.StrToLong(res_balanceNotice, 0L);

            return 0;
        }

        /// <summary>
        /// SendSms the specified account, key, id, mobile, templateCode, templateParams, smsSign, sendTime, sysIdList and errorDescription.
        /// </summary>
        /// <returns>The send.</returns>
        /// <param name="account">Account.</param>
        /// <param name="key">Key.</param>
        /// <param name="id">Identifier.</param>
        /// <param name="mobile">Mobile.</param>
        /// <param name="templateCode">Template code.</param>
        /// <param name="templateParams">Template parameters.</param>
        /// <param name="smsSign">Sms sign.</param>
        /// <param name="sendTime">Send time.</param>
        /// <param name="sysIdList">Sys identifier list.</param>
        /// <param name="errorDescription">Error description.</param>
        public static int SendSms(string account, string key, string id, string mobile, string templateCode, string templateParams, string smsSign, DateTime sendTime, List<string> sysIdList, ref string errorDescription)
        {
            sysIdList.Clear();
            errorDescription = "";

            string body = string.Format(
            @"{{
                ""action"":""SendSms"",
                ""smsid"":""{0}"",
                ""phone"":""{1}"",
                ""signName"":""{2}"",
                ""templateCode"":""{3}"",
                ""templateParams"":""{{{4}}}"",
                ""sendTime"":""{5}"",
                ""version"":""1.0.0""
            }}", id, mobile, smsSign, templateCode, templateParams, sendTime.ToString("yyyy-MM-dd HH:mm:ss"));

            JsonData json = null;
            int ret = request(account, key, body, ref json, ref errorDescription);
            if ((ret != 0) && (ret != 1400))
            {
                return ret;
            }

            List<string> res_sysIdList = new List<string>();

            try
            {
                foreach (JsonData j in json["sysid"])
                {
                    res_sysIdList.Add(j["id"].ToString() + ":" + j["phone"].ToString());
                }
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return -10001;
            }

            sysIdList.AddRange(res_sysIdList);

            return ret;
        }

        /// <summary>
        /// SendNotice the specified account, key, id, mobile, content, params, smsSign, sendTime, sysIdList and errorDescription.
        /// </summary>
        /// <returns>The send.</returns>
        /// <param name="account">Account.</param>
        /// <param name="key">Key.</param>
        /// <param name="id">Identifier.</param>
        /// <param name="mobile">Mobile.</param>
        /// <param name="content">Content.</param>
        /// <param name="_params">Parameters.</param>
        /// <param name="smsSign">Sms sign.</param>
        /// <param name="sendTime">Send time.</param>
        /// <param name="sysIdList">Sys identifier list.</param>
        /// <param name="errorDescription">Error description.</param>
        public static int SendNotice(string account, string key, string id, string mobile, string content, string _params, string smsSign, DateTime sendTime, List<string> sysIdList, ref string errorDescription)
        {
            sysIdList.Clear();
            errorDescription = "";

            string body = string.Format(
            @"{{
                ""action"":""SendNotice"",
                ""smsid"":""{0}"",
                ""phone"":""{1}"",
                ""signName"":""{2}"",
                ""content"":""{3}"",
                ""params"":""{{{4}}}"",
                ""sendTime"":""{5}"",
                ""version"":""1.0.0""
            }}", id, mobile, smsSign, content, _params, sendTime.ToString("yyyy-MM-dd HH:mm:ss"));

            JsonData json = null;
            int ret = request(account, key, body, ref json, ref errorDescription);
            if ((ret != 0) && (ret != 1400))
            {
                return ret;
            }

            List<string> res_sysIdList = new List<string>();

            try
            {
                foreach (JsonData j in json["sysid"])
                {
                    res_sysIdList.Add(j["id"].ToString() + ":" + j["phone"].ToString());
                }
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return -10001;
            }

            sysIdList.AddRange(res_sysIdList);

            return ret;
        }

        /// <summary>
        /// Queries the state of the send.
        /// </summary>
        /// <returns>The send state.</returns>
        /// <param name="account">Account.</param>
        /// <param name="key">Key.</param>
        /// <param name="id">Identifier.</param>
        /// <param name="sysid">Sysid. 如果此参数不为空，只查询这一条，否则查询 smsid = 112 的全部拆分条</param>
        /// <param name="sendStateList">Send state list.</param>
        /// <param name="errorDescription">Error description.</param>
        public static int QuerySendState(string account, string key, string id, string sysid, List<SendState> sendStateList, ref string errorDescription)
        {
            sendStateList.Clear();
            errorDescription = "";

            string body = string.Format(
            @"{{
                ""action"":""QuerySendState"",
                ""smsid"":""{0}"",
                ""sysid"":""{1}"",
                ""version"":""1.0.0""
            }}", id, sysid);

            JsonData json = null;
            int ret = request(account, key, body, ref json, ref errorDescription);
            if (ret != 0)
            {
                return ret;
            }

            List<SendState> res_sendStateList = new List<SendState>();

            try
            {
                foreach (JsonData j in json["sysid"])
                {
                    res_sendStateList.Add(new SendState(j["id"].ToString(), int.Parse(j["state"].ToString()), j["description"].ToString()));
                }
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return -10001;
            }

            sendStateList.AddRange(res_sendStateList);

            return 0;
        }

        private static int request(string account, string key, string body, ref JsonData json, ref string errorDescription)
        {
            errorDescription = "";
            json = null;

            string url = buildRequestUrl(account, key, encryptAES(body, key));
            string res = httpRequest(url, ref errorDescription);

            if (string.IsNullOrEmpty(res))
            {
                return -1001;
            }

            string resCode = "", resMessage = "", resTimestamp = "", resBody = "", resSign = "";
            if (!parseResponse(res, ref resCode, ref resMessage, ref resTimestamp, ref resBody, ref resSign, ref errorDescription))
            {
                return -1002;
            }

            if (!checkResponseSign(account, key, resCode, resMessage, resBody, resTimestamp, resSign))
            {
                errorDescription = "Data Check Error Returned by Server";
                return -1003;
            }

            if ((resCode != "0") && (resCode != "1400"))
            {
                errorDescription = resMessage;
                return Convert.StrToInt(resCode, -1004);
            }

            try
            {
                json = JsonMapper.ToObject(decryptAES(resBody, key));
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return -1005;
            }

            if (json.Count < 1)
            {
                json = null;
                errorDescription = "Data format error returned by server.";

                return -1006;
            }

            return Convert.StrToInt(resCode, 0);
        }

        private static string buildRequestUrl(string account, string key, string body)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string sign = Shove.Security.Encrypt.MD5(string.Format("apiAccount={0}&body={1}&timestamp={2}&apikey={3}", account, body, timestamp, key));

            return string.Format("{0}?apiAccount={1}&body={2}&timestamp={3}&sign={4}", gatewayUrl, account, HttpUtility.UrlEncode(body), HttpUtility.UrlEncode(timestamp), sign);
        }

        private static string httpRequest(string url, ref string errorDescription)
        {
            errorDescription = "";

            try
            {
                WebRequest webRequest = WebRequest.Create(url);
                webRequest.Method = "GET";
                webRequest.Timeout = 120 * 1000;
                WebResponse res = webRequest.GetResponse();
                return new StreamReader(res.GetResponseStream(), Encoding.UTF8).ReadToEnd();
            }
            catch (Exception e)
            {
                errorDescription = e.Message;
                return "";
            }
        }

        private static bool parseResponse(string res, ref string resCode, ref string resMessage, ref string resTimestamp, ref string resBody, ref string resSign, ref string errorDescription)
        {
            resCode = "";
            resMessage = "";
            resTimestamp = "";
            resBody = "";
            resSign = "";
            errorDescription = "";

            JsonData json;

            try
            {
                json = JsonMapper.ToObject(res);
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return false;
            }

            if (json.Count < 1)
            {
                errorDescription = "Data format error returned by server.";

                return false;
            }

            string res_code = "";
            string res_message = "";
            string res_timestamp = "";
            string res_body = "";
            string res_sign = "";

            try
            {
                res_code = json["code"].ToString();
                res_message = HttpUtility.UrlDecode(json["message"].ToString());
                res_timestamp = HttpUtility.UrlDecode(json["timestamp"].ToString());
                res_body = HttpUtility.UrlDecode(json["body"].ToString());
                res_sign = json["sign"].ToString();
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return false;
            }

            resCode = res_code;
            resMessage = res_message;
            resTimestamp = res_timestamp;
            resBody = res_body;
            resSign = res_sign;

            return true;
        }

        private static bool checkResponseSign(string account, string key, string code, string message, string body, string timestamp, string sign)
        {
            string local_sign = Shove.Security.Encrypt.MD5(string.Format("body={0}&code={1}&message={2}&timestamp={3}&apikey={4}", body, code, message, timestamp, key));
            return (local_sign == sign.ToUpper());
        }

        private static byte[] iv = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        private static string encryptAES(string input, string key)
        {
            byte[] inputData = Encoding.UTF8.GetBytes(input);
            byte[] bkey = MD5(key);

            RijndaelManaged rijndaelProvider = new RijndaelManaged();
            rijndaelProvider.Key = bkey;
            rijndaelProvider.IV = iv;
            rijndaelProvider.Mode = CipherMode.CBC;
            rijndaelProvider.Padding = PaddingMode.PKCS7;
            ICryptoTransform rijndaelEncrypt = rijndaelProvider.CreateEncryptor();
            byte[] encryptedData = rijndaelEncrypt.TransformFinalBlock(inputData, 0, inputData.Length);

            return System.Convert.ToBase64String(encryptedData);
        }

        private static string decryptAES(string input, string key)
        {
            byte[] inputData = System.Convert.FromBase64String(input);
            byte[] bkey = MD5(key);

            RijndaelManaged rijndaelProvider = new RijndaelManaged();
            rijndaelProvider.Key = bkey;
            rijndaelProvider.IV = iv;
            rijndaelProvider.Mode = CipherMode.CBC;
            rijndaelProvider.Padding = PaddingMode.PKCS7;
            ICryptoTransform rijndaelDecrypt = rijndaelProvider.CreateDecryptor();
            byte[] decryptedData = rijndaelDecrypt.TransformFinalBlock(inputData, 0, inputData.Length);

            return Encoding.UTF8.GetString(decryptedData);
        }

        private static byte[] MD5(string input)
        {
            return new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(input));
        }
    }
}
