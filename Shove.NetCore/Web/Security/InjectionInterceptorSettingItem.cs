//using System;
//using System.Data;
//using System.Configuration;
//using System.Web;
//using System.Web.Security;
//using System.Web.UI;
//using System.Web.UI.HtmlControls;
//using System.Web.UI.WebControls;
//using System.Web.UI.WebControls.WebParts;
//using System.Text.RegularExpressions;

//namespace Shove.Web.Security
//{
//    /// <summary>
//    /// 
//    /// </summary>
//    internal class InjectionInterceptorSettingItem
//    {
//        /// <summary>
//        /// 
//        /// </summary>
//        internal string Key;
//        /// <summary>
//        /// 
//        /// </summary>
//        internal int ExceptionLevel;
//        /// <summary>
//        /// 
//        /// </summary>
//        internal string Rule;
//        /// <summary>
//        /// 
//        /// </summary>
//        internal Regex RegexRule;

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="key"></param>
//        /// <param name="exceptionLevel"></param>
//        /// <param name="rule"></param>
//        internal InjectionInterceptorSettingItem(string key, int exceptionLevel, string rule)
//        {
//            Key = key;
//            ExceptionLevel = exceptionLevel;
//            Rule = rule;

//            if (string.IsNullOrEmpty(Key) || string.IsNullOrEmpty(Rule))
//            {
//                throw new Exception("Web.config 配置文件 injectionSettings 节中 key、Rule 属性不能为空值。");
//            }

//            if ((ExceptionLevel < 1) || (ExceptionLevel > 2))
//            {
//                throw new Exception("Web.config 配置文件 injectionSettings 节中 exceptionLevel 属性存在未知的属性值：“" + ExceptionLevel.ToString() + "”。");
//            }

//            RegexRule = new Regex(Rule, RegexOptions.Compiled | RegexOptions.IgnoreCase);
//        }
//    }
//}