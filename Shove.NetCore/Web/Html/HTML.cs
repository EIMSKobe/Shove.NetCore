using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Net;
using System.Collections;
using System.Text.RegularExpressions;

namespace Shove.HTML
{
    /// <summary>
    /// HTML 的摘要说明。
    /// </summary>
    public class HTML
    {
        /// <summary>
        /// 
        /// </summary>
        public HTML()
        {
        }

        /// <summary>
        /// 获取 Url 的相应 html
        /// </summary>
        /// <param name="Url"></param>
        /// <returns></returns>
        public static string GetHTML(string Url)
        {
            return GetHTML(Url, 0);
        }

        /// <summary>
        /// 获取 Url 的相应 html
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="Timeout">超时毫秒</param>
        /// <returns></returns>
        public static string GetHTML(string Url, int Timeout)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamReader reader = null;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(Url);
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022)";

                if (Timeout > 0)
                {
                    request.Timeout = Timeout;
                }

                request.AllowAutoRedirect = true;

                response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string CharSet = response.CharacterSet;

                    #region 获取目标页面的字符集

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

                    #endregion

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

                    reader = new StreamReader(s, System.Text.Encoding.GetEncoding(CharSet));
                    string html = reader.ReadToEnd();
                    return html;
                }
                else
                {
                    return "";
                }
            }
            catch (SystemException)
            {
                return "";
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }

                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>
        /// 获取 Url 的相应 html
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="Type"></param>
        /// <param name="LastModifiedTime"></param>
        /// <param name="HostAbsolutePath"></param>
        /// <returns></returns>
        public static string GetHTML(string Url, ref int Type, ref System.DateTime LastModifiedTime, ref string HostAbsolutePath)
        {
            Type = -1;
            LastModifiedTime = System.DateTime.Now;
            HostAbsolutePath = "";

            HttpWebRequest request;
            HttpWebResponse response = null;
            Stream s = null;
            StreamReader sr = null;

            bool ReadOK = false;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(Url);
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022)";
                request.Timeout = 30000;
                request.AllowAutoRedirect = true;

                response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Type = -1;
                    return "";
                }

                LastModifiedTime = response.LastModified;
                HostAbsolutePath = response.ResponseUri.AbsoluteUri;
                string Path = response.ResponseUri.AbsolutePath;
                int iLocate = Path.LastIndexOf("/");
                if (iLocate > 0)
                {
                    Path = Path.Substring(iLocate, Path.Length - iLocate);
                }
                HostAbsolutePath = HostAbsolutePath.Substring(0, HostAbsolutePath.Length - Path.Length).ToLower();

                if (response.ContentType.ToLower().StartsWith("image/"))	//是图片
                {
                    Type = 2;
                    return "";
                }

                if (response.ContentType.ToLower().StartsWith("audio/"))	//是声音
                {
                    Type = 3;
                    return "";
                }

                if (!response.ContentType.ToLower().StartsWith("text/"))	//不是文本类型的文档
                {
                    Type = -1;
                    return "";
                }

                string CharSet = response.CharacterSet;

                #region 获取目标页面的字符集

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

                #endregion

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

                sr = new StreamReader(s, System.Text.Encoding.GetEncoding(CharSet));

                ReadOK = true;
            }
            catch
            {
                Type = -1;
                return "";
            }
            finally
            {
                if (!ReadOK)
                {
                    if (sr != null)
                    {
                        sr.Close();
                    }

                    if (s != null)
                    {
                        s.Close();
                    }

                    if (response != null)
                    {
                        response.Close();
                    }
                }
            }

            string HTML = "";
            string sLine = "";

            try
            {
                while (sLine != null)
                {
                    sLine = sr.ReadLine();
                    if (sLine != null)
                        HTML += sLine;
                }
                Type = 1;
                return HTML;
            }
            catch
            {
                Type = -1;
                return "";
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }

                if (s != null)
                {
                    s.Close();
                }

                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="HostAbsolutePath"></param>
        /// <returns></returns>
        private static string ReBuildUrl(string Url, string HostAbsolutePath)
        {
            Url = Url.Trim().ToLower();

            if (Url.EndsWith("/"))
                Url = Url.Substring(0, Url.Length - 1);

            if (Url.StartsWith("http://") || Url.StartsWith("https://"))
                return Url;

            if (Url.StartsWith("../") || Url.StartsWith("..\\"))
                //return HostAbsolutePath + "/" + Url.Substring(3, Url.Length - 3);
                Url = Url.Substring(3, Url.Length - 3);

            if (Url.Length > 0)
            {
                if (Url.StartsWith("/") || Url.StartsWith("\\"))
                    Url = HostAbsolutePath + Url;
                else
                    Url = HostAbsolutePath + "/" + Url;
            }
            else
                Url = HostAbsolutePath;

            return Url;
        }

        /// <summary>
        /// 获取 html 包含的所有的链接地址
        /// </summary>
        /// <param name="Page"></param>
        /// <param name="HostAbsolutePath"></param>
        /// <param name="MaxLen"></param>
        /// <param name="FindUrlLevel"></param>
        /// <returns></returns>
        public static string[] GetHTMLUrls(string Page, string HostAbsolutePath, int MaxLen, int FindUrlLevel)
        {
            ArrayList m_Url = new ArrayList();
            HtmlParse.ParseHTML parse = new HtmlParse.ParseHTML();
            HtmlParse.Attribute a;

            parse.Source = Page;
            while (!parse.Eof())
            {
                char ch = parse.Parse();
                if (ch == 0)
                {
                    a = parse.GetTag()["HREF"];
                    if (a != null)
                    {
                        string str = a.Value.Trim().ToLower();
                        if ((str != "") && (!str.StartsWith("mailto")) && (!str.StartsWith("#")))
                        {
                            if ((FindUrlLevel == 2) || str.StartsWith("http://") || str.StartsWith("https://"))
                            {
                                str = ReBuildUrl(str, HostAbsolutePath);
                                if ((MaxLen < 1) || (str.Length <= MaxLen))
                                    m_Url.Add(str);
                            }
                        }
                    }

                    a = parse.GetTag()["SRC"];
                    if (a != null)
                    {
                        string str = a.Value.Trim().ToLower();
                        if (str != "")
                        {
                            if ((FindUrlLevel == 2) || str.StartsWith("http://") || str.StartsWith("https://"))
                            {
                                str = ReBuildUrl(str, HostAbsolutePath);
                                if ((MaxLen < 1) || (str.Length <= MaxLen))
                                    m_Url.Add(str);
                            }
                        }
                    }
                }
            }

            if (m_Url.Count == 0)
                return null;

            string[] strs = new string[m_Url.Count];
            int i;
            for (i = 0; i < m_Url.Count; i++)
                strs[i] = m_Url[i].ToString();
            return strs;
        }

        /// <summary>
        /// 获取 html 包含的所有的链接地址以及链接的 Title
        /// </summary>
        /// <param name="Page"></param>
        /// <param name="HostAbsolutePath"></param>
        /// <param name="HrefMaxLen"></param>
        /// <param name="DescriptionMaxLen"></param>
        /// <param name="FindUrlLevel"></param>
        /// <returns></returns>
        public static string[,] GetHTMLUrlsWithDescription(string Page, string HostAbsolutePath, int HrefMaxLen, int DescriptionMaxLen, int FindUrlLevel)
        {
            string Title = GetTitle(Page, DescriptionMaxLen);

            ArrayList m_Url = new ArrayList();
            ArrayList m_UrlDescription = new ArrayList();

            //找普通链接
            Regex regex = new Regex(@"<a[\s\t\r\n]+[\S\s]*?href[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<href>[^""]*)""|'(?<href>[^']*)'|(?<href>[^\s\t\r\n>]*))[^>]*>(?<title>[\S\s]*?)</a[\s\t\r\n]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            MatchCollection mc = regex.Matches(Page);
            int i;
            for (i = 0; i < mc.Count; i++)
            {
                string Href = mc[i].Groups["href"].Value.Trim().ToLower();
                string Description = ClearReplace(mc[i].Groups["title"].Value);
                if (Href.StartsWith("mailto") || Href.StartsWith("#")) Href = "";
                bool isUseTitleAsDescription = false;
                if (Description == "")
                {
                    Description = Title;
                    isUseTitleAsDescription = true;
                }
                else if ((DescriptionMaxLen > 0) && (Description.Length > DescriptionMaxLen))
                    Description = Description.Substring(0, DescriptionMaxLen);

                if (Href != "")
                {
                    if ((FindUrlLevel == 2) || Href.StartsWith("http://") || Href.StartsWith("https://"))
                        Href = ReBuildUrl(Href, HostAbsolutePath);
                    else
                        continue;
                    if ((HrefMaxLen > 0) && (Href.Length > HrefMaxLen))
                        Href = "";
                }
                if (Href == "")
                    continue;

                if ((Description == "") || isUseTitleAsDescription || ((Description.IndexOf("<") < 0) && (Description.IndexOf(">") < 0)))
                {
                    m_Url.Add(Href);
                    m_UrlDescription.Add(Description);
                    continue;
                }

                string str = StandardizationHTML(Description, true, true, true);
                if (str == "")
                {
                    m_Url.Add(Href);
                    m_UrlDescription.Add(Title);
                    continue;
                }

                System.Xml.XmlDocument XmlDoc = new XmlDocument();
                XmlDoc.Load(new StringReader(str));

                Description = "";
                System.Xml.XmlNodeList nodes = XmlDoc.GetElementsByTagName("*");
                if (nodes == null)
                {
                    m_Url.Add(Href);
                    m_UrlDescription.Add(Title);
                    continue;
                }

                for (int j = 0; j < nodes.Count; j++)
                {
                    if (nodes[j].Name.ToUpper() == "IMG")
                    {
                        try
                        {
                            Description += nodes[j].Attributes["ALT"].Value + " ";
                        }
                        catch { }
                        break;
                    }
                }

                if (Description.Trim() == "")
                {
                    XPathNodeIterator node = XmlDoc.CreateNavigator().Select("*");
                    while (node.MoveNext())
                        Description += node.Current.Value + " ";
                }

                if (Description.Trim() == "")
                    Description = Title;
                else if ((DescriptionMaxLen > 0) && (Description.Length > DescriptionMaxLen))
                    Description = Description.Substring(0, DescriptionMaxLen);

                m_Url.Add(Href);
                m_UrlDescription.Add(Description.Trim());
            }

            //找图片链接
            regex = new Regex(@"<img[\s\t\r\n]+([^>]*?alt[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<alt>[^""]*)""|'(?<alt>[^']*)'|(?<alt>[^\s\t\r\n>]*)))?[^>]*?src[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<src>[^""]*)""|'(?<src>[^']*)'|(?<src>[^\s\t\r\n>]*))([^>]*?alt[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<alt>[^""]*)""|'(?<alt>[^']*)'|(?<alt>[^\s\t\r\n>]*)))?[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            mc = regex.Matches(Page);
            for (i = 0; i < mc.Count; i++)
            {
                string Href = mc[i].Groups["src"].Value.Trim().ToLower();
                string Description = mc[i].Groups["alt"].Value.Trim();

                if (Description == "")
                    Description = Title;
                else if ((DescriptionMaxLen > 0) && (Description.Length > DescriptionMaxLen))
                    Description = Description.Substring(0, DescriptionMaxLen);

                if (Href != "")
                {
                    if ((FindUrlLevel == 2) || Href.StartsWith("http://") || Href.StartsWith("https://"))
                        Href = ReBuildUrl(Href, HostAbsolutePath);
                    if ((HrefMaxLen > 0) && (Href.Length > HrefMaxLen))
                        Href = "";
                }
                if (Href != "")
                {
                    m_Url.Add(Href);
                    m_UrlDescription.Add(Description);
                }
            }

            string[,] strs = new string[2, m_Url.Count];
            for (i = 0; i < m_Url.Count; i++)
            {
                strs[0, i] = m_Url[i].ToString();
                strs[1, i] = m_UrlDescription[i].ToString();
            }
            return strs;
        }

        /// <summary>
        /// 将 html 代码规范化，如果没有 html /html 作为开头、结尾，将自动补齐。
        /// </summary>
        /// <param name="html"></param>
        /// <param name="isClearCommentary"></param>
        /// <param name="isClearScript"></param>
        /// <param name="isClearStyle"></param>
        /// <returns></returns>
        public static string StandardizationHTML(string html, bool isClearCommentary, bool isClearScript, bool isClearStyle)
        {
            return StandardizationHTML(html, isClearCommentary, isClearScript, isClearStyle, true);
        }

        /// <summary>
        /// 将 html 代码规范化
        /// </summary>
        /// <param name="html"></param>
        /// <param name="isClearCommentary"></param>
        /// <param name="isClearScript"></param>
        /// <param name="isClearStyle"></param>
        /// <param name="isReplenishHtmlTag">是否补上首尾的 html /html 标记></param>
        /// <returns></returns>
        public static string StandardizationHTML(string html, bool isClearCommentary, bool isClearScript, bool isClearStyle, bool isReplenishHtmlTag)
        {
            if (html.Trim() == "")
                return "";

            if (isReplenishHtmlTag)
            {
                string s = html.ToUpper();

                if (!s.StartsWith("<HTML"))
                {
                    int l = s.IndexOf("<HTML");
                    if (l > 0)
                        html = html.Substring(l, s.Length - l);
                    else
                        html = "<HTML>" + html;
                }
                s = html.ToUpper();
                if (!s.EndsWith("</HTML>"))
                {
                    int l = s.LastIndexOf("</HTML>");
                    if (l > 0)
                        html = html.Substring(0, l) + "</HTML>";
                    else
                        html += "</HTML>";
                }
            }

            SgmlReader.SgmlReader sr = new SgmlReader.SgmlReader();
            sr.DocType = "HTML";
            sr.InputStream = new StringReader(html);
            sr.CaseFolding = SgmlReader.CaseFolding.ToUpper;
            sr.WhitespaceHandling = WhitespaceHandling.None;

            StringWriter strWriter = new StringWriter();
            XmlTextWriter xmlWriter = new XmlTextWriter(strWriter);
            xmlWriter.Formatting = Formatting.Indented;

            string Result = "";
            try
            {
                while (sr.Read())
                {
                    if (sr.NodeType != XmlNodeType.Whitespace)
                        xmlWriter.WriteNode(sr, true);
                }
            }
            catch
            { }
            Result = strWriter.ToString();

            if (isClearCommentary && (Result != ""))
                Result = ClearCommentary(Result);
            if (isClearScript && (Result != ""))
                Result = ClearScript(Result);
            if (isClearStyle && (Result != ""))
                Result = ClearStyle(Result);
            if (isClearCommentary || isClearScript || isClearStyle)
                return StandardizationHTML(Result, false, false, false, isReplenishHtmlTag);
            else
                return Result;
        }

        /// <summary>
        /// 获取 Html 文档的 Title 部分
        /// </summary>
        /// <param name="html"></param>
        /// <param name="MaxLen"></param>
        /// <returns></returns>
        public static string GetTitle(string html, int MaxLen)
        {
            html = html.Trim();
            if (html == "")
                return "";

            Regex regex = new Regex(@"<title[^>]*?>(?<title>[^<]*?)</title[\s\t\r\n]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            MatchCollection mc = regex.Matches(html);
            System.Text.StringBuilder Result = new System.Text.StringBuilder();
            for (int i = 0; i < mc.Count; i++)
                Result.Append(ClearReplace(mc[i].Groups["title"].Value) + " ");

            if ((MaxLen < 1) || (Result.Length <= MaxLen))
                return Result.ToString().Trim();
            else
                return Result.ToString().Substring(0, MaxLen);
        }

        /// <summary>
        /// 获取 Html 文档的 Keyword 部分
        /// </summary>
        /// <param name="html"></param>
        /// <param name="MaxLen"></param>
        /// <returns></returns>
        public static string GetKeywords(string html, int MaxLen)
        {
            if (html.Trim() == "")
                return "";

            Regex regex = new Regex(@"<meta[\s\t\r\n]+([^>]*?content[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<content>[^""]*)""|'(?<content>[^']*)'|(?<content>[^\s\t\r\n>]*)))?[^>]*?name[\s\t\r\n]*=[\s\t\r\n]*[""']?keywords[""']?([^>]*?content[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<content>[^""]*)""|'(?<content>[^']*)'|(?<content>[^\s\t\r\n>]*)))?[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            MatchCollection mc = regex.Matches(html);
            System.Text.StringBuilder Result = new System.Text.StringBuilder();
            for (int i = 0; i < mc.Count; i++)
                Result.Append(ClearReplace(mc[i].Groups["content"].Value) + " ");

            if ((MaxLen < 1) || (Result.Length <= MaxLen))
                return Result.ToString().Trim();
            else
                return Result.ToString().Substring(0, MaxLen);
        }

        /// <summary>
        /// 获取 Html 文档的 Description 部分
        /// </summary>
        /// <param name="html"></param>
        /// <param name="MaxLen"></param>
        /// <returns></returns>
        public static string GetDescription(string html, int MaxLen)
        {
            if (html.Trim() == "")
                return "";

            Regex regex = new Regex(@"<meta[\s\t\r\n]+([^>]*?content[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<content>[^""]*)""|'(?<content>[^']*)'|(?<content>[^\s\t\r\n>]*)))?[^>]*?name[\s\t\r\n]*=[\s\t\r\n]*[""']?description[""']?([^>]*?content[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<content>[^""]*)""|'(?<content>[^']*)'|(?<content>[^\s\t\r\n>]*)))?[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            MatchCollection mc = regex.Matches(html);
            System.Text.StringBuilder Result = new System.Text.StringBuilder();
            for (int i = 0; i < mc.Count; i++)
                Result.Append(ClearReplace(mc[i].Groups["content"].Value) + " ");

            if ((MaxLen < 1) || (Result.Length <= MaxLen))
                return Result.ToString().Trim();
            else
                return Result.ToString().Substring(0, MaxLen);
        }

        /// <summary>
        /// 获取 Html 文档的纯文字部分，使用前需要用 StandardizationHTML 规范化
        /// </summary>
        /// <param name="html"></param>
        /// <param name="MaxLen"></param>
        /// <returns></returns>
        public static string GetText(string html, int MaxLen)
        {
            string Result = GetFromXPath(html, "HTML/BODY");

            if (Result != "")
            {
                if ((MaxLen > 0) && (Result.Length > MaxLen))
                    return Result.Substring(0, MaxLen);
                else
                    return Result;
            }

            int iStart = html.IndexOf("<BODY");
            if (iStart < 0)
                return "";
            int iEnd = html.LastIndexOf("</BODY");
            if (iEnd < 0)
                iEnd = html.Length;
            if (iEnd <= iStart)
                return "";

            html = "<HTML>" + html.Substring(iStart, iEnd - iStart) + "</BODY></HTML>";
            Result = GetFromXPath(html, "HTML/BODY");
            if ((MaxLen > 0) && (Result.Length > MaxLen))
                return Result.Substring(0, MaxLen);
            else
                return Result;
        }

        /// <summary>
        /// 获取 Html 文档的 Body 部分，使用前需要用 StandardizationHTML 规范化
        /// </summary>
        /// <param name="html"></param>
        /// <param name="WithBodyTag"></param>
        /// <returns></returns>
        public static string GetBody(string html, bool WithBodyTag)
        {
            Regex regex;
            if (WithBodyTag)
                regex = new Regex(@"(?<body><Body[\S\s]*?>[\S\s]*?</Body>)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            else
                regex = new Regex(@"<Body[\S\s]*?>[\s\t\r\n]*(?<body>[\S\s]*?)[\s\t\r\n]*</Body>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Match m = regex.Match(html);
            return m.Groups["body"].Value;
        }

        private static string GetFromXPath(string html, string XPath)
        {
            if (html.Trim() == "")
                return "";

            string Result = "";
            try
            {
                XPathDocument doc = new XPathDocument(new StringReader(html));
                XPathNavigator nav = doc.CreateNavigator();
                XPathNodeIterator nodes = nav.Select(XPath);
                while (nodes.MoveNext())
                    Result += ClearReplace(nodes.Current.Value);
            }
            catch//(Exception ee)
            {
                return "对该文档的解析失败，原因是该文档的格式错误或没有遵循 HTML 规范。";
                //return ee.Message;
            }

            return Result;
        }

        /// <summary>
        /// 清除 Html 文档中的某部分
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ClearReplace(string s)
        {
            s = Regex.Replace(s, @"&(\s*\w+;)+", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            s = Regex.Replace(s, @"[\t\r\n]+", " ", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            s = Regex.Replace(s, @"\s+", " ", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return s;
        }

        /// <summary>
        /// 清除 Html 文档中的 Script 部分
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string ClearScript(string html)
        {
            return Regex.Replace(html, @"<script[\S\s]*?>[\S\s]*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// 清除 Html 文档中的 Style 部分
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string ClearStyle(string html)
        {
            return Regex.Replace(html, @"<style[\S\s]*?>[\S\s]*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// 清除 Html 文档中的 Commentary 部分
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string ClearCommentary(string html)
        {
            return Regex.Replace(html, @"<!--[\S\s]*?-->", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// 将 Html 文档中的关键词用 Color 进行高亮
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Keywords"></param>
        /// <param name="Color"></param>
        /// <returns></returns>
        public static string SetTextKeywordsHighLight(string Text, string[] Keywords, string Color)
        {
            if (Text.Trim() == "")
                return "";

            int Len = Keywords.Length;
            if (Len == 0) return Text;

            switch (Color.Trim().ToLower())
            {
                case "red":
                    Color = "FF0000";
                    break;
                case "green":
                    Color = "00FF00";
                    break;
                case "blue":
                    Color = "0000FF";
                    break;
            }

            string RegexStr = "(?:";
            for (int i = 0; i < Len; i++)
            {
                RegexStr += "(?<Keywords>" + Keywords[i] + ")";
                if (i < Len - 1) RegexStr += "|";
            }
            RegexStr += ")";

            return Regex.Replace(Text, RegexStr, "<FONT COLOR = \"#" + Color + "\">${Keywords}</FONT>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}