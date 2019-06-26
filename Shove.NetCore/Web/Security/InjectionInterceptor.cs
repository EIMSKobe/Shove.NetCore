//using System.Collections.Generic;
//using System.Text.RegularExpressions;

//namespace Shove.Web.Security
//{
//    /// <summary>
//    /// SQL，脚本注入拦截器
//    /// 请在所有的项目的 Global.asax.cs 文件的 Application_BeginRequest 方法的首行，增加一行代码：Shove.Web.Security.InjectionInterceptor.Run();
//    /// 如果增加了，拦截器将自动工作，并且组件中的 GetRequest, DAO 等组件将不再进行注入过滤，否则，他们将继续工作。
//    /// </summary>
//    public class InjectionInterceptor
//    {
//        /// <summary>
//        /// Shove 组件的系统标志，默认值为 False，当执行过本方法的 Check 时，标志置位 True，表示应用程序使用了本组建提供的 Injection 拦截功能。
//        /// 如果没有使用，则本组件的 GetRequest, DAO 等组件将按原来的过滤方法进行过滤。
//        /// 如果使用了，则本组件的 GetRequest, DAO 等组件将不再执行过滤，而依赖与本拦截器工作。
//        /// </summary>
//        internal static bool __SYS_SHOVE_FLAG_IsUsed_InjectionInterceptor = false;
//        private static List<InjectionInterceptorSettingItem> ExceptionRules = null;
//        private static List<string> ValidImgExtName = new List<string>();

//        private static IO.Log log = new IO.Log("InjectionInterceptor");

//        #region 拦截器的 Regex

//        private const string Rule0 = @"<[^>]+?style=[\w]+?:expression\(|@[\s\t\r\n]*import\b|<[^>]*?\b(alert|confirm|prompt|javascript|document|cookie|onerror|onmousemove|onload|onclick|onmouseover)\b[^>]*?>|<[^>]*?(\\(u|x|ux)[0-9,a-f,A-F]+?|\\[0-9]+?|&#[0-9,a-f,A-F]+?)[^>]*?>|^\+\/v(8|9)|<[^>]*?=[^>]*?&#[^>]*?>|\b(and|or)\b.{1,6}?(=|>|<|\bin\b|\blike\b)|/\*.+?\*/|<\s*script\b|<\s*iframe\b|<\s*frame\b|<\s*object\b|<\s*embed\b|<\s*input\b|\bEVAL\s*\(|\bfunction\b\s*\(|<\s*a\b|<\s*img\b|\bEXEC\b|UNION.+?SELECT|UPDATE.+?SET|INSERT\s+INTO.+?VALUES|(SELECT|DELETE).+?FROM|(CREATE|ALTER|DROP|TRUNCATE)\s+(TABLE|DATABASE)|[\']+?.*?(OR|AND|[-]{2,}|UPDATE|CREATE|ALTER|DROP|TRUNCATE|SELECT|DELETE|EXEC|INSERT)\b|\b(OR|AND|[-]{2,}|UPDATE|CREATE|ALTER|DROP|TRUNCATE|SELECT|DELETE|EXEC|INSERT)\b.*?[\']+?";
//        private const string Rule1 = @"<[^>]+?style=[\w]+?:expression\(|@[\s\t\r\n]*import\b|<[^>]*?\b(alert|confirm|prompt|javascript|document|cookie|onerror|onmousemove|onload|onclick|onmouseover)\b[^>]*?>|<[^>]*?(\\(u|x|ux)[0-9,a-f,A-F]+?|\\[0-9]+?|&#[0-9,a-f,A-F]+?)[^>]*?>|^\+\/v(8|9)|<[^>]*?=[^>]*?&#[^>]*?>|\b(and|or)\b.{1,6}?(=|>|<|\bin\b|\blike\b)|/\*.+?\*/|<\s*script\b|<\s*iframe\b|<\s*frame\b|<\s*object\b|<\s*embed\b|<\s*input\b|\bEVAL\s*\(|\bfunction\b\s*\(|\bEXEC\b|UNION.+?SELECT|UPDATE.+?SET|INSERT\s+INTO.+?VALUES|(SELECT|DELETE).+?FROM|(CREATE|ALTER|DROP|TRUNCATE)\s+(TABLE|DATABASE)|[\']+?.*?(OR|AND|[-]{2,}|UPDATE|CREATE|ALTER|DROP|TRUNCATE|SELECT|DELETE|EXEC|INSERT)\b|\b(OR|AND|[-]{2,}|UPDATE|CREATE|ALTER|DROP|TRUNCATE|SELECT|DELETE|EXEC|INSERT)\b.*?[\']+?";
//        private static Regex Regex0 = new Regex(Rule0, RegexOptions.IgnoreCase | RegexOptions.Compiled);
//        private static Regex Regex1 = new Regex(Rule1, RegexOptions.IgnoreCase | RegexOptions.Compiled);

//        private const string ImgRule = @"<img\b[^<>]*?\bsrc[\s\t\r\n]*=[\s\t\r\n]*[""']?[\s\t\r\n]*(?<src>[^\s\t\r\n""'<>]*)[^<>]*?/?[\s\t\r\n]*[/]*>";
//        private static Regex RegexImg = new Regex(ImgRule, RegexOptions.IgnoreCase | RegexOptions.Compiled);

//        #endregion

//        /// <summary>
//        /// Global.Application_BeginRequest 方法中，请在首行调用这个方法，能确保阻止 SQL 注入和 XSS 脚本攻击。
//        /// </summary>
//        /// <returns></returns>
//        public static void Run()
//        {
//            if ((HttpContext.Current.Request == null) ||
//                ((HttpContext.Current.Request.Cookies == null) && (HttpContext.Current.Request.UrlReferrer == null) && ((HttpContext.Current.Request.Form == null) || (HttpContext.Current.Request.Form.Count == 0)) && (HttpContext.Current.Request.QueryString.Count == 0)))
//            {
//                return;
//            }

//            #region Init Rules

//            if (!__SYS_SHOVE_FLAG_IsUsed_InjectionInterceptor)
//            {
//                lock (ValidImgExtName)
//                {
//                    if (!__SYS_SHOVE_FLAG_IsUsed_InjectionInterceptor)
//                    {
//                        __SYS_SHOVE_FLAG_IsUsed_InjectionInterceptor = true;
//                        ExceptionRules = ((List<InjectionInterceptorSettingItem>)System.Configuration.ConfigurationManager.GetSection("injectionInterceptorSettings"));

//                        ValidImgExtName.Clear();
//                        ValidImgExtName.Add(".jpg");
//                        ValidImgExtName.Add(".jpeg");
//                        ValidImgExtName.Add(".png");
//                        ValidImgExtName.Add(".bmp");
//                        ValidImgExtName.Add(".gif");
//                        ValidImgExtName.Add(".tif");
//                        ValidImgExtName.Add(".tiff");
//                    }
//                }
//            }

//            #endregion

//            #region 分析规则，符合则进行拦截

//            string pagePath = HttpContext.Current.Request.Url.AbsoluteUri.Substring(_Web.Utility.GetUrl().Length);

//            foreach (InjectionInterceptorSettingItem iisi in ExceptionRules)
//            {
//                if (iisi.RegexRule.IsMatch(pagePath))
//                {
//                    if (iisi.ExceptionLevel == 2)
//                    {
//                        return;
//                    }

//                    Intercept(Regex1, true);

//                    return;
//                }
//            }

//            #endregion

//            #region 不在规则内，进行完整拦截

//            Intercept(Regex0, false);

//            #endregion
//        }

//        #region 拦截

//        /// <summary>
//        /// 拦截器
//        /// </summary>
//        /// <param name="regex">根据传入的正则规则进行拦截， 分析 Cookie, Referrer, Post, Get 数据</param>
//        /// <param name="IsCheckImg">是否对 Img 进行分析或转码</param>
//        /// <returns></returns>
//        private static bool Intercept(Regex regex, bool IsCheckImg)
//        {
//            if (CheckCookieData(regex, IsCheckImg))
//            {
//                WriteResponse("Cookie");

//                return true;
//            }

//            if (CheckReferer(regex, IsCheckImg))
//            {
//                WriteResponse("Referer");

//                return true;
//            }

//            /*
//            if ((HttpContext.Current.Request.RequestType.ToUpper() == "GET") && CheckGetData(regex, IsCheckImg))
//            {
//                WriteResponse("GET");

//                return true;
//            }
//            else if (CheckPostData(regex, IsCheckImg)) //(HttpContext.Current.Request.RequestType.ToUpper() == "POST") && 
//            {
//                WriteResponse("POST");

//                return true;
//            }
//            */

//            if (CheckGetData(regex, IsCheckImg))
//            {
//                WriteResponse("GET");

//                return true;
//            }
            
//            if (CheckPostData(regex, IsCheckImg))
//            {
//                WriteResponse("POST");

//                return true;
//            } 

//            return false;
//        }

//        #endregion

//        #region 分析数据的详细方法

//        private static bool CheckCookieData(Regex regex, bool IsCheckImg)
//        {
//            if ((HttpContext.Current.Request.Cookies == null) || (HttpContext.Current.Request.Cookies.Count == 0))
//            {
//                return false;
//            }

//            for (int i = 0; i < HttpContext.Current.Request.Cookies.Count; i++)
//            {
//                if (_checkData(regex, HttpContext.Current.Request.Cookies[i].Value, IsCheckImg))
//                {
//                    return true;
//                }
//            }

//            return false;
//        }

//        private static bool CheckReferer(Regex regex, bool IsCheckImg)
//        {
//            if (HttpContext.Current.Request.UrlReferrer == null)
//            {
//                return false;
//            }

//            if (_checkData(regex, HttpContext.Current.Request.UrlReferrer.ToString(), IsCheckImg))
//            {
//                return true;
//            }

//            return false;
//        }

//        private static bool CheckPostData(Regex regex, bool IsCheckImg)
//        {
//            if (HttpContext.Current.Request.Form == null)
//            {
//                return false;
//            }

//            for (int i = 0; i < HttpContext.Current.Request.Form.Count; i++)
//            {
//                if (_checkData(regex, HttpContext.Current.Request.Form[i].ToString(), IsCheckImg))
//                {
//                    return true;
//                }
//            }

//            return false;
//        }

//        private static bool CheckGetData(Regex regex, bool IsCheckImg)
//        {
//            for (int i = 0; i < HttpContext.Current.Request.QueryString.Count; i++)
//            {
//                if (_checkData(regex, HttpContext.Current.Request.QueryString[i].ToString(), IsCheckImg))
//                {
//                    return true;
//                }
//            }

//            return false;
//        }

//        private static bool _checkData(Regex regex, string value, bool IsCheckImg)
//        {
//            if (string.IsNullOrEmpty(value))
//            {
//                return false;
//            }

//            if (regex.IsMatch(value))
//            {
//                return true;
//            }

//            if (!IsCheckImg)
//            {
//                return false;
//            }

//            // 检查 IMG 的 src 属性文件扩展名是否合法
//            MatchCollection mc = RegexImg.Matches(value);

//            foreach (Match m in mc)
//            {
//                string fileName = m.Groups["src"].Value;
//                string extName = System.IO.Path.GetExtension(fileName).ToLower();

//                if (!ValidImgExtName.Contains(extName))
//                {
//                    return true;
//                }
//            }

//            return false;
//        }

//        private static void WriteResponse(string checkType)
//        {
//            string result = "系统检测到您提交的数据中存在恶意的注入型攻击数据(或 img 标签的 src 文件不合法)，请检查你的 " + checkType + " 数据，如果是系统误报，请联系我们处理，谢谢。给您带来了不便，十分抱歉！【技术支持：深圳英迈思文化科技有限公司·EIMS 研究院·云计算实验室与晓风系列产品支撑中心】";
//            log.Write("InjectionInterceptorError: " + result + "\r\n" + HttpContext.Current.Request.Url.AbsoluteUri);

//            // throw new Exception(result);
//            // 本来是应该直接 throw 的，但是应用层面可能会有程序员这么做：
//            // 1、将调用拦截器的代码 try 起来。那就没有作用了。
//            // 2、throw 是页面错误代码 500，有些程序将 500 做了重定向，重定向到专用的错误页面了，这样有些扫描工具会误报。

//            HttpContext.Current.Response.StatusCode = 412;
//            HttpContext.Current.Response.Write(result);
//            HttpContext.Current.Response.End();
//        }

//        #endregion
//    }
//}