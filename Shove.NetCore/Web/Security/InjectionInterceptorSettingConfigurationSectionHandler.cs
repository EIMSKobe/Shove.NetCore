//using System;
//using System.Data;
//using System.Configuration;
//using System.Web;
//using System.Web.Security;
//using System.Web.UI;
//using System.Web.UI.HtmlControls;
//using System.Web.UI.WebControls;
//using System.Web.UI.WebControls.WebParts;
//using System.Xml;
//using System.Xml.XPath;
//using System.Collections;
//using System.Collections.Generic;

//namespace Shove.Web.Security
//{
//    /// <summary>
//    /// InjectionInterceptorSettingConfigurationSectionHandler 的摘要说明
//    /// </summary>
//    public class InjectionInterceptorSettingConfigurationSectionHandler : IConfigurationSectionHandler
//    {
//        #region IConfigurationSectionHandler 成员

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="parent"></param>
//        /// <param name="configContext"></param>
//        /// <param name="section"></param>
//        /// <returns></returns>
//        public object Create(object parent, object configContext, XmlNode section)
//        {
//            List<InjectionInterceptorSettingItem> result = new List<InjectionInterceptorSettingItem>();

//            foreach (XmlNode node in section.ChildNodes)
//            {
//                InjectionInterceptorSettingItem iisi = new InjectionInterceptorSettingItem(
//                    node.Attributes["key"].Value,
//                    int.Parse(node.Attributes["exceptionLevel"].Value),
//                    node.Attributes["rule"].Value);

//                result.Add(iisi);
//            }

//            return result;
//        }

//        #endregion
//    }
//}