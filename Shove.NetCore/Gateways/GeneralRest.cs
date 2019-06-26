using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace Shove.Gateways
{
    /// <summary>
    /// 通用的 REST 类型的网关
    /// </summary>
    public class GeneralRestGateway
    {
        /// <summary>
        /// 申明委托，回调应用程序处理接口参数，应用程序返回需要返回给客户端的字符串，如：xml, josn, 其他字符串等。
        /// </summary>
        /// <param name="parameters">已经校验过安全的接口参数列表</param>
        /// <param name="errorDescription">错误描述，应用程序中如果遇到逻辑错误，则返回 “” 或 null，并且用此变量标明错误原因</param>
        /// <returns></returns>
        public delegate string DelegateHandleRequest(Dictionary<string, object> parameters, ref string errorDescription);

        /// <summary>
        /// 接口提供程序，在收到请求后，调用此方法，并提供业务逻辑处理的委托方法
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key">安全 Key</param>
        /// <param name="allowTimespanSeconds">时间戳允许相差的秒数，一般为 30 秒</param>
        /// <param name="HandleRequest">委托回调的方法，用于应用程序处理业务逻辑，返回接口应返回的字符串</param>
        /// <param name="errorDescription">如果有错误发生，此为错误描述</param>
        /// <returns></returns>
        public static int Handle(HttpContext context, string key, int allowTimespanSeconds, DelegateHandleRequest HandleRequest, ref string errorDescription)
        {
            errorDescription = "";

            #region 参数及安全性校验

            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("在使用 Shove.Gateways.GeneralRestGateway.Handle 方法解析参数并处理业务时，必须提供一个用于摘要签名用的 key (俗称 MD5 加盐)。");
            }

            HttpRequest req = null;
            HttpResponse res = null;

            if (context == null)
            {
                errorDescription = "Http 上下文错误。";

                return -1;
            }

            req = context.Request;
            res = context.Response;

            if ((req == null) || (res == null))
            {
                errorDescription = "Http 上下文错误。";

                return -2;
            }

            string[] keys = new string[req.Query.Keys.Count];
            req.Query.Keys.CopyTo(keys, 0);

            if ((keys == null) || (keys.Length == 0))
            {
                errorDescription = "没有找到 Query 参数，接口数据非法。";

                return -3;
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string _s = "", _t = "";

            for (int i = 0; i < req.Query.Count; i++)
            {
                if (keys[i] == "_s")
                {
                    _s = req.Query["_s"];

                    continue;
                }
                else if (keys[i] == "_t")
                {
                    _t = HttpUtility.UrlDecode(req.Query["_t"], Encoding.UTF8);
                }

                parameters.Add(keys[i], HttpUtility.UrlDecode(req.Query[keys[i]], Encoding.UTF8));
            }

            if (string.IsNullOrEmpty(_s) || string.IsNullOrEmpty(_t))
            {
                errorDescription = "缺少 _s 或 _t 参数，接口数据非法。";

                return -4;
            }

            string[] ParameterNames = new string[parameters.Count];
            parameters.Keys.CopyTo(ParameterNames, 0);
            Array.Sort(ParameterNames);

            string signData = "";

            for (int i = 0; i < parameters.Count; i++)
            {
                signData += (ParameterNames[i] + "=" + parameters[ParameterNames[i]].ToString() + ((i < parameters.Count - 1) ? "&" : ""));
            }

            DateTime timestamp = Convert.StrToDateTime(_t, DateTime.Now.AddYears(-30).ToString("yyyy-MM-dd HH:mm:ss"));
            TimeSpan ts = DateTime.Now - timestamp;

            if ((allowTimespanSeconds > 0) && (Math.Abs(ts.TotalSeconds) > allowTimespanSeconds))
            {
                errorDescription = "访问超时。";

                return -5;
            }

            if (string.Compare(Security.Encrypt.MD5(signData + key, Encoding.UTF8), _s, true) != 0)
            {
                errorDescription = "签名错误，接口数据非法。";

                return -6;
            }

            parameters.Remove("_t");

            #endregion

            #region 委托应用程序处理接口参数，参数不包括 _s, _t 等安全变量

            string result = "";

            try
            {
                result = HandleRequest(parameters, ref errorDescription);
            }
            catch(Exception e)
            {
                errorDescription = "应用程序的代理回调程序遇到异常，详细原因是：" + e.Message;

                return -7;
            }

            if (!string.IsNullOrEmpty(result))
            {
                res.WriteAsync(result);
            }

            #endregion

            //[shove]
            //res.End();

            return 0;
        }

        /// <summary>
        /// 构建通用网关的请求 url，参数为键值对形式，不分顺序。不需要包含时间戳、签名等参数 ，系统会自动增加。
        /// </summary>
        /// <param name="urlBase"></param>
        /// <param name="key"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string BuildUrl(string urlBase, string key, Dictionary<string, object> parameters)
        {
            if (parameters.ContainsKey("_s") || parameters.ContainsKey("_t"))
            {
                throw new Exception("在使用 Shove.Gateways.GeneralRestGateway.BuildUrl 方法构建通用 REST 接口 Url 时，不能使用 _s, _t 此保留字作为参数名。");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("在使用 Shove.Gateways.GeneralRestGateway.BuildUrl 方法构建通用 REST 接口 Url 时，必须提供一个用于摘要签名用的 key (俗称 MD5 加盐)。");
            }

            parameters.Add("_t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            string[] ParameterNames = new string[parameters.Count];
            parameters.Keys.CopyTo(ParameterNames, 0);
            Array.Sort(ParameterNames);

            if (!urlBase.EndsWith("?", StringComparison.Ordinal) && !urlBase.EndsWith("&", StringComparison.Ordinal))
            {
                Uri uri = new Uri(urlBase);
                urlBase += (string.IsNullOrEmpty(uri.Query)) ? "?" : "&";
            }
            
            string signData = "";

            for (int i = 0; i < parameters.Count; i++)
            {
                signData += (ParameterNames[i] + "=" + parameters[ParameterNames[i]].ToString());
                urlBase += (ParameterNames[i] + "=" + HttpUtility.UrlEncode(parameters[ParameterNames[i]].ToString(), Encoding.UTF8));

                if (i < parameters.Count - 1)
                {
                    signData += "&";
                    urlBase += "&";
                }
            }

            urlBase += "&_s=" + Security.Encrypt.MD5(signData + key, Encoding.UTF8);

            return urlBase;
        }
    }
}
