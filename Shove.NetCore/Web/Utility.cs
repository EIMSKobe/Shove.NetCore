using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using System.Web;
using System.Net;
using System.IO;

namespace Shove.Web
{
    /// <summary>
    /// 
    /// </summary>
    public class Utility
    {
        /// <summary>
        /// ��ȡ��ǰվ�������
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetUrl(HttpContext context)
        {
            string scheme = context.Request.Scheme;
            string url = scheme + "://" + context.Request.Host.Host;
            int? port = context.Request.Host.Port;

            if (port.HasValue && (((scheme == "http") && (port != 80)) || ((scheme == "https") && (port != 443))))
            {
                url += ":" + port.ToString();
            }

            string path = context.Request.PathBase;

            if ((path != "") && (path != "/"))
            {
                url += path;
            }

            return url;
        }

        /// <summary>
        /// ��ȡ��ǰվ�����������������ǰ��� http://
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetUrlWithoutHttp(HttpContext context)
        {
            string scheme = context.Request.Scheme;
            string url = context.Request.Host.Host;
            int? port = context.Request.Host.Port;

            if (port.HasValue && (((scheme == "http") && (port != 80)) || ((scheme == "https") && (port != 443))))
            {
                url += ":" + port.ToString();
            }

            string path = context.Request.PathBase;

            if ((path != "") && (path != "/"))
            {
                url += path;
            }

            return url;
        }

        /// <summary>
        /// ��ȡ Url Request ������������SQLע��
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key">������Key</param>
        /// <returns>�������˺�Ĳ���ֵ</returns>
        public static string GetRequest(HttpContext context, string key)
        {
            string Result = context.Request.Query[key];

            if (Result == null)
            {
                return "";
            }

            Result = HttpUtility.UrlDecode(Result).Trim();

            if (true)//[shove] Shove.Web.Security.InjectionInterceptor.__SYS_SHOVE_FLAG_IsUsed_InjectionInterceptor)
            {
                return Result;
            }
            else
            {
                //[shove] return FilteSqlInfusion(Result);
            }
        }

        private static bool IsCloseMethod_Shove_FilteSqlInfusion = Read_IsCloseMethod_Shove_FilteSqlInfusion();
        private static bool Read_IsCloseMethod_Shove_FilteSqlInfusion()
        {
            bool result = false;
            string str = AppConfigurtaionServices.GetAppSettingsString("IsCloseMethod_Shove_FilteSqlInfusion");

            if (string.IsNullOrEmpty(str))
            {
                str = "false";
            }

            Boolean.TryParse(str, out result);

            return result;
        }

        /// <summary>
        /// ����Sqlע�룬html �༭���Ķ������ע��
        /// </summary>
        /// <param name="input">SQL ���򹹳�����ĳ�����ֵ��ַ���</param>
        /// <returns></returns>
        public static string FilteSqlInfusion(string input)
        {
            return FilteSqlInfusion(input, true);
        }

        /// <summary>
        /// ����Sqlע�룬html �༭���Ķ������ע��
        /// ��� Web.Config �� AppSetting �а��� <add key="IsCloseMethod_Shove_FilteSqlInfusion" value="true" />, ��˷�����������
        /// </summary>
        /// <param name="input">SQL ���򹹳�����ĳ�����ֵ��ַ���</param>
        /// <param name="replaceSingleQuoteMark">�Ƿ��滻������</param>
        /// <returns></returns>
        public static string FilteSqlInfusion(string input, bool replaceSingleQuoteMark)
        {
            if (IsCloseMethod_Shove_FilteSqlInfusion)
            {
                return input;
            }

            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(input.Trim(" ��\t\r\n\v\f".ToCharArray())))
            {
                    return "";
            }

            double d;
            if (Double.TryParse(input, out d))
            {
                return input;
            }

            if (replaceSingleQuoteMark)
            {
                input = input.Replace("'", "��");
            }

            MatchEvaluator evaluator = new MatchEvaluator(MatchEvaluator_FilteSqlInfusion);

            return Regex.Replace(input, @"(update[\t\s\r\n\f\v+��])|(drop[\t\s\r\n\f\v+��])|(delete[\t\s\r\n\f\v+��])|(exec[\t\s\r\n\f\v+��])|(create[\t\s\r\n\f\v+��])|(execute[\t\s\r\n\f\v+��])|(truncate[\t\s\r\n\f\v+��])|(insert[\t\s\r\n\f\v+��])", evaluator, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        private static string MatchEvaluator_FilteSqlInfusion(Match match)
        {
            string m = match.Value;
            return Convert.ToSBC(m.Substring(0, m.Length - 1)) + m[m.Length - 1].ToString();
        }

        #region Request ������MD5 Sign

        /// <summary>
        /// ��ȡð�����򷨺�� Request Key��
        /// <param name="context"></param>
        /// </summary>
        public static string[] GetRequestKeyAndSort(HttpContext context)
        {
            if (context.Request.Query.Count < 1)
            {
                return null;
            }

            string[] r = new string[context.Request.Query.Count];

            context.Request.Query.Keys.CopyTo(r, 0);

            return Sort(r, ' ');
        }

        /// <summary>
        /// ���� REST ģʽ������ Url �Ĳ����������������������ǩ��������ǩ����Ĵ�
        /// </summary>
        /// <param name="paramters"></param>
        /// <param name="key"></param>
        /// <param name="signatureParamteName"></param>
        /// <returns></returns>
        public static string BuildUrlParamtersAndSignature(string[] paramters, string key, string signatureParamteName)
        {
            string[] sortedParamters = Sort(paramters, '=');

            string signatureSource = "";
            string url = "";

            for (int i = 0; i < sortedParamters.Length; i++)
            {
                if (i > 0)
                {
                    url += "&";
                }

                signatureSource += sortedParamters[i].Split('=')[0] + HttpUtility.UrlDecode(sortedParamters[i].Split('=')[1]);
                url += sortedParamters[i].Split('=')[0] + "=" + sortedParamters[i].Split('=')[1];
            }

            if (url != "")
            {
                url += "&";
            }

            url += signatureParamteName + "=" + Shove.Security.Encrypt.MD5(key + signatureSource);

            return url;
        }

        /// <summary>
        /// ���� REST ģʽ������ Url �Ĳ��������� Sign�������ػ�ȡ���Ĳ���
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <param name="signatureParamteName"></param>
        /// <param name="errorDescription"></param>
        /// <returns></returns>
        public static string[] GetUrlParamtersAndVasidSignature(HttpContext context, string key, string signatureParamteName, ref string errorDescription)
        {
            errorDescription = "";

            string[] requestKey = GetRequestKeyAndSort(context);

            if (requestKey == null)
            {
                errorDescription = "û���ҵ�����";

                return null;
            }

            string sign = Shove.Web.Utility.GetRequest(context, signatureParamteName);

            if (string.IsNullOrEmpty(sign))
            {
                errorDescription = "û���ṩǩ��";

                return null;
            }

            string SignSourceString = key;
            string[] Result = new string[requestKey.Length - 1];
            int i = 0;

            foreach (string str in requestKey)
            {
                if (str.Trim().ToLower() == signatureParamteName.ToLower())
                {
                    continue;
                }

                string Value = Shove.Web.Utility.GetRequest(context, str);

                SignSourceString += (str + Value);
                Result[i++] = str + "=" + HttpUtility.UrlEncode(Value);
            }

            if (string.Compare(Shove.Security.Encrypt.MD5(SignSourceString), sign, StringComparison.OrdinalIgnoreCase) != 0)
            {
                errorDescription = "ǩ����Ч";

                return null;
            }

            return Result;
        }

        /// <summary>
        /// �ַ�����������
        /// </summary>
        /// <param name="input"></param>
        /// <param name="splitChar"></param>
        /// <returns></returns>
        private static string[] Sort(string[] input, char splitChar)
        {
            if ((input == null) || (input.Length < 2))
            {
                return input;
            }

            string[] r = new string[input.Length];
            input.CopyTo(r, 0);

            int i, j; //������־ 
            string temp;

            bool exchange;

            for (i = 0; i < r.Length; i++) //�����R.Length-1������ 
            {
                exchange = false; //��������ʼǰ��������־ӦΪ��

                for (j = r.Length - 2; j >= i; j--)
                {
                    if (splitChar == ' ')
                    {
                        exchange |= string.CompareOrdinal(r[j + 1], r[j]) < 0;
                    }
                    else
                    {
                        exchange |= string.CompareOrdinal(r[j + 1].Split(splitChar)[0], r[j].Split(splitChar)[0]) < 0;
                    }

                    if (exchange)
                    {
                        temp = r[j + 1];
                        r[j + 1] = r[j];
                        r[j] = temp;
                    }
                }

                if (!exchange) //��������δ������������ǰ��ֹ�㷨 
                {
                    break;
                }
            }

            return r;
        }

        #endregion

        /// <summary>
        /// Post �ύ
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="encodeing"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public static string Post(string Url, string encodeing, int timeoutSeconds)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamReader reader = null;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(Url);
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022)";

                if (timeoutSeconds > 0)
                {
                    request.Timeout = 1000 * timeoutSeconds;
                }

                request.AllowAutoRedirect = true;
                response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string CharSet = response.CharacterSet;

                    if (string.IsNullOrEmpty(encodeing))
                    {
                        if (string.IsNullOrEmpty(CharSet) || (CharSet == "ISO-8859-1"))
                        {
                            string head = response.Headers["Content-Type"];
                            Regex regex = new Regex(@"charset=[^""]?[""](?<G0>([^""]+?))[""]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            Match m = regex.Match(head);

                            if (m.Success)
                            {
                                CharSet = m.Groups["G0"].Value;
                            }
                        }

                        if (CharSet == "ISO-8859-1")
                        {
                            CharSet = "GB2312";
                        }
                        if (string.IsNullOrEmpty(CharSet))
                        {
                            CharSet = "UTF-8";
                        }
                    }

                    Stream s = null;

                    if (response.ContentEncoding.ToLower() == "gzip")
                    {
                        s = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    }
                    else if (response.ContentEncoding.ToLower() == "deflate")
                    {
                        s = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    }
                    else
                    {
                        s = response.GetResponseStream();
                    }

                    reader = new StreamReader(s, System.Text.Encoding.GetEncoding(string.IsNullOrEmpty(encodeing) ? CharSet : encodeing));
                    string html = reader.ReadToEnd();

                    return html;
                }
                else
                {
                    return "";
                }
            }
            catch
            {
                return "";
            }
        }
    }
}