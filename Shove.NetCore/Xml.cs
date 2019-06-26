using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Xml.Serialization;
using System.Data;

namespace Shove
{
    /// <summary>
    /// Xml 相关
    /// </summary>
    public class Xml
    {
        /// <summary>
        /// 将 Xml 转换为 Json 格式
        /// </summary>
        /// <param name="xml">输入的 xml 字符串</param>
        /// <returns></returns>
        public static string XmlToJson(string xml)
        {
            Stream input = new MemoryStream(Encoding.Default.GetBytes(xml));  

            XmlDocument doc = new XmlDocument();

            input.Position = 0;
            doc.Load(input);

            XmlElement root = doc.DocumentElement;
            XmlNodeList nl;

            nl = root.ChildNodes;
            
            XmlDocument srcdoc = new XmlDocument();
            
            string temp = null;
            
            foreach (XmlNode xn in nl)
            {
                temp += xn.OuterXml;
            }

            temp = "<Shove_Xml_XmlToJson_Result>" + temp + "</Shove_Xml_XmlToJson_Result>";
            srcdoc.InnerXml = temp;

            XPathNavigator nav = srcdoc.CreateNavigator();

            XmlDocument xdStyleSheet = new XmlDocument();

            xdStyleSheet.LoadXml(new Properties.Settings().Xml2JsonXslt);

            XslCompiledTransform xt = new XslCompiledTransform();

            xt.Load(xdStyleSheet);

            Stream w = new MemoryStream();
            XmlTextWriter wr = new XmlTextWriter(w, Encoding.GetEncoding("UTF-8"));
            xt.Transform(nav, null, w);
            w.Position = 0;
            StreamReader tr = new StreamReader(w);

            string result = tr.ReadToEnd().Replace("\"Shove_Xml_XmlToJson_Result\":  ", "");

            w.Close();
            tr.Close();
            wr.Dispose();

            return result;
        }
    }
}
