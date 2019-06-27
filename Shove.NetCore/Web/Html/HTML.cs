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
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetHTML(string url)
        {
            return GetHTML(url, 0);
        }

        /// <summary>
        /// 获取 Url 的相应 html
        /// </summary>
        /// <param name="url"></param>
        /// <param name="timeout">超时毫秒</param>
        /// <returns></returns>
        public static string GetHTML(string url, int timeout)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamReader reader = null;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022)";

                if (timeout > 0)
                {
                    request.Timeout = timeout;
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
        /// <param name="url"></param>
        /// <param name="type"></param>
        /// <param name="lastModifiedTime"></param>
        /// <param name="hostAbsolutePath"></param>
        /// <returns></returns>
        public static string GetHTML(string url, ref int type, ref System.DateTime lastModifiedTime, ref string hostAbsolutePath)
        {
            type = -1;
            lastModifiedTime = System.DateTime.Now;
            hostAbsolutePath = "";

            HttpWebRequest request;
            HttpWebResponse response = null;
            Stream s = null;
            StreamReader sr = null;

            bool ReadOK = false;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022)";
                request.Timeout = 30000;
                request.AllowAutoRedirect = true;

                response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    type = -1;
                    return "";
                }

                lastModifiedTime = response.LastModified;
                hostAbsolutePath = response.ResponseUri.AbsoluteUri;
                string Path = response.ResponseUri.AbsolutePath;
                int iLocate = Path.LastIndexOf("/", StringComparison.Ordinal);
                if (iLocate > 0)
                {
                    Path = Path.Substring(iLocate, Path.Length - iLocate);
                }
                hostAbsolutePath = hostAbsolutePath.Substring(0, hostAbsolutePath.Length - Path.Length).ToLower();

                if (response.ContentType.ToLower().StartsWith("image/", StringComparison.Ordinal))	//是图片
                {
                    type = 2;
                    return "";
                }

                if (response.ContentType.ToLower().StartsWith("audio/", StringComparison.Ordinal))	//是声音
                {
                    type = 3;
                    return "";
                }

                if (!response.ContentType.ToLower().StartsWith("text/", StringComparison.Ordinal))	//不是文本类型的文档
                {
                    type = -1;
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
                type = -1;
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
                type = 1;
                return HTML;
            }
            catch
            {
                type = -1;
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
        /// <param name="url"></param>
        /// <param name="hostAbsolutePath"></param>
        /// <returns></returns>
        private static string ReBuildUrl(string url, string hostAbsolutePath)
        {
            url = url.Trim().ToLower();

            if (url.EndsWith("/", StringComparison.Ordinal))
                url = url.Substring(0, url.Length - 1);

            if (url.StartsWith("http://", StringComparison.Ordinal) || url.StartsWith("https://", StringComparison.Ordinal))
                return url;

            if (url.StartsWith("../", StringComparison.Ordinal) || url.StartsWith("..\\", StringComparison.Ordinal))
                //return hostAbsolutePath + "/" + Url.Substring(3, Url.Length - 3);
                url = url.Substring(3, url.Length - 3);

            if (url.Length > 0)
            {
                if (url.StartsWith("/", StringComparison.Ordinal) || url.StartsWith("\\", StringComparison.Ordinal))
                    url = hostAbsolutePath + url;
                else
                    url = hostAbsolutePath + "/" + url;
            }
            else
                url = hostAbsolutePath;

            return url;
        }

        /// <summary>
        /// 获取 html 包含的所有的链接地址
        /// </summary>
        /// <param name="page"></param>
        /// <param name="hostAbsolutePath"></param>
        /// <param name="maxLen"></param>
        /// <param name="findUrlLevel"></param>
        /// <returns></returns>
        public static string[] GetHTMLUrls(string page, string hostAbsolutePath, int maxLen, int findUrlLevel)
        {
            ArrayList m_Url = new ArrayList();
            HtmlParse.ParseHTML parse = new HtmlParse.ParseHTML();
            HtmlParse.Attribute a;

            parse.Source = page;
            while (!parse.Eof())
            {
                char ch = parse.Parse();
                if (ch == 0)
                {
                    a = parse.GetTag()["HREF"];
                    if (a != null)
                    {
                        string str = a.Value.Trim().ToLower();
                        if ((str != "") && (!str.StartsWith("mailto", StringComparison.Ordinal)) && (!str.StartsWith("#", StringComparison.Ordinal)))
                        {
                            if ((findUrlLevel == 2) || str.StartsWith("http://", StringComparison.Ordinal) || str.StartsWith("https://", StringComparison.Ordinal))
                            {
                                str = ReBuildUrl(str, hostAbsolutePath);
                                if ((maxLen < 1) || (str.Length <= maxLen))
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
                            if ((findUrlLevel == 2) || str.StartsWith("http://", StringComparison.Ordinal) || str.StartsWith("https://", StringComparison.Ordinal))
                            {
                                str = ReBuildUrl(str, hostAbsolutePath);
                                if ((maxLen < 1) || (str.Length <= maxLen))
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
        /// <param name="page"></param>
        /// <param name="hostAbsolutePath"></param>
        /// <param name="hrefMaxLen"></param>
        /// <param name="descriptionMaxLen"></param>
        /// <param name="findUrlLevel"></param>
        /// <returns></returns>
        public static string[,] GetHTMLUrlsWithDescription(string page, string hostAbsolutePath, int hrefMaxLen, int descriptionMaxLen, int findUrlLevel)
        {
            string Title = GetTitle(page, descriptionMaxLen);

            ArrayList m_Url = new ArrayList();
            ArrayList m_UrlDescription = new ArrayList();

            //找普通链接
            Regex regex = new Regex(@"<a[\s\t\r\n]+[\S\s]*?href[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<href>[^""]*)""|'(?<href>[^']*)'|(?<href>[^\s\t\r\n>]*))[^>]*>(?<title>[\S\s]*?)</a[\s\t\r\n]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            MatchCollection mc = regex.Matches(page);
            int i;
            for (i = 0; i < mc.Count; i++)
            {
                string Href = mc[i].Groups["href"].Value.Trim().ToLower();
                string Description = ClearReplace(mc[i].Groups["title"].Value);
                if (Href.StartsWith("mailto", StringComparison.Ordinal) || Href.StartsWith("#", StringComparison.Ordinal)) Href = "";
                bool isUseTitleAsDescription = false;
                if (Description == "")
                {
                    Description = Title;
                    isUseTitleAsDescription = true;
                }
                else if ((descriptionMaxLen > 0) && (Description.Length > descriptionMaxLen))
                    Description = Description.Substring(0, descriptionMaxLen);

                if (Href != "")
                {
                    if ((findUrlLevel == 2) || Href.StartsWith("http://", StringComparison.Ordinal) || Href.StartsWith("https://", StringComparison.Ordinal))
                        Href = ReBuildUrl(Href, hostAbsolutePath);
                    else
                        continue;
                    if ((hrefMaxLen > 0) && (Href.Length > hrefMaxLen))
                        Href = "";
                }
                if (Href == "")
                    continue;

                if ((Description == "") || isUseTitleAsDescription || ((Description.IndexOf("<", StringComparison.Ordinal) < 0) && (Description.IndexOf(">", StringComparison.Ordinal) < 0)))
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
                else if ((descriptionMaxLen > 0) && (Description.Length > descriptionMaxLen))
                    Description = Description.Substring(0, descriptionMaxLen);

                m_Url.Add(Href);
                m_UrlDescription.Add(Description.Trim());
            }

            //找图片链接
            regex = new Regex(@"<img[\s\t\r\n]+([^>]*?alt[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<alt>[^""]*)""|'(?<alt>[^']*)'|(?<alt>[^\s\t\r\n>]*)))?[^>]*?src[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<src>[^""]*)""|'(?<src>[^']*)'|(?<src>[^\s\t\r\n>]*))([^>]*?alt[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<alt>[^""]*)""|'(?<alt>[^']*)'|(?<alt>[^\s\t\r\n>]*)))?[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            mc = regex.Matches(page);
            for (i = 0; i < mc.Count; i++)
            {
                string Href = mc[i].Groups["src"].Value.Trim().ToLower();
                string Description = mc[i].Groups["alt"].Value.Trim();

                if (Description == "")
                    Description = Title;
                else if ((descriptionMaxLen > 0) && (Description.Length > descriptionMaxLen))
                    Description = Description.Substring(0, descriptionMaxLen);

                if (Href != "")
                {
                    if ((findUrlLevel == 2) || Href.StartsWith("http://", StringComparison.Ordinal) || Href.StartsWith("https://", StringComparison.Ordinal))
                        Href = ReBuildUrl(Href, hostAbsolutePath);
                    if ((hrefMaxLen > 0) && (Href.Length > hrefMaxLen))
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

                if (!s.StartsWith("<HTML", StringComparison.Ordinal))
                {
                    int l = s.IndexOf("<HTML", StringComparison.Ordinal);
                    if (l > 0)
                        html = html.Substring(l, s.Length - l);
                    else
                        html = "<HTML>" + html;
                }
                s = html.ToUpper();
                if (!s.EndsWith("</HTML>", StringComparison.Ordinal))
                {
                    int l = s.LastIndexOf("</HTML>", StringComparison.Ordinal);
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
        /// <param name="maxLen"></param>
        /// <returns></returns>
        public static string GetTitle(string html, int maxLen)
        {
            html = html.Trim();
            if (html == "")
                return "";

            Regex regex = new Regex(@"<title[^>]*?>(?<title>[^<]*?)</title[\s\t\r\n]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            MatchCollection mc = regex.Matches(html);
            System.Text.StringBuilder Result = new System.Text.StringBuilder();
            for (int i = 0; i < mc.Count; i++)
                Result.Append(ClearReplace(mc[i].Groups["title"].Value) + " ");

            if ((maxLen < 1) || (Result.Length <= maxLen))
                return Result.ToString().Trim();
            else
                return Result.ToString().Substring(0, maxLen);
        }

        /// <summary>
        /// 获取 Html 文档的 Keyword 部分
        /// </summary>
        /// <param name="html"></param>
        /// <param name="maxLen"></param>
        /// <returns></returns>
        public static string GetKeywords(string html, int maxLen)
        {
            if (html.Trim() == "")
                return "";

            Regex regex = new Regex(@"<meta[\s\t\r\n]+([^>]*?content[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<content>[^""]*)""|'(?<content>[^']*)'|(?<content>[^\s\t\r\n>]*)))?[^>]*?name[\s\t\r\n]*=[\s\t\r\n]*[""']?keywords[""']?([^>]*?content[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<content>[^""]*)""|'(?<content>[^']*)'|(?<content>[^\s\t\r\n>]*)))?[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            MatchCollection mc = regex.Matches(html);
            System.Text.StringBuilder Result = new System.Text.StringBuilder();
            for (int i = 0; i < mc.Count; i++)
                Result.Append(ClearReplace(mc[i].Groups["content"].Value) + " ");

            if ((maxLen < 1) || (Result.Length <= maxLen))
                return Result.ToString().Trim();
            else
                return Result.ToString().Substring(0, maxLen);
        }

        /// <summary>
        /// 获取 Html 文档的 Description 部分
        /// </summary>
        /// <param name="html"></param>
        /// <param name="maxLen"></param>
        /// <returns></returns>
        public static string GetDescription(string html, int maxLen)
        {
            if (html.Trim() == "")
                return "";

            Regex regex = new Regex(@"<meta[\s\t\r\n]+([^>]*?content[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<content>[^""]*)""|'(?<content>[^']*)'|(?<content>[^\s\t\r\n>]*)))?[^>]*?name[\s\t\r\n]*=[\s\t\r\n]*[""']?description[""']?([^>]*?content[\s\t\r\n]*=[\s\t\r\n]*(?:""(?<content>[^""]*)""|'(?<content>[^']*)'|(?<content>[^\s\t\r\n>]*)))?[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            MatchCollection mc = regex.Matches(html);
            System.Text.StringBuilder Result = new System.Text.StringBuilder();
            for (int i = 0; i < mc.Count; i++)
                Result.Append(ClearReplace(mc[i].Groups["content"].Value) + " ");

            if ((maxLen < 1) || (Result.Length <= maxLen))
                return Result.ToString().Trim();
            else
                return Result.ToString().Substring(0, maxLen);
        }

        /// <summary>
        /// 获取 Html 文档的纯文字部分，使用前需要用 StandardizationHTML 规范化
        /// </summary>
        /// <param name="html"></param>
        /// <param name="maxLen"></param>
        /// <returns></returns>
        public static string GetText(string html, int maxLen)
        {
            string Result = GetFromXPath(html, "HTML/BODY");

            if (Result != "")
            {
                if ((maxLen > 0) && (Result.Length > maxLen))
                    return Result.Substring(0, maxLen);
                else
                    return Result;
            }

            int iStart = html.IndexOf("<BODY", StringComparison.Ordinal);
            if (iStart < 0)
                return "";
            int iEnd = html.LastIndexOf("</BODY", StringComparison.Ordinal);
            if (iEnd < 0)
                iEnd = html.Length;
            if (iEnd <= iStart)
                return "";

            html = "<HTML>" + html.Substring(iStart, iEnd - iStart) + "</BODY></HTML>";
            Result = GetFromXPath(html, "HTML/BODY");
            if ((maxLen > 0) && (Result.Length > maxLen))
                return Result.Substring(0, maxLen);
            else
                return Result;
        }

        /// <summary>
        /// 获取 Html 文档的 Body 部分，使用前需要用 StandardizationHTML 规范化
        /// </summary>
        /// <param name="html"></param>
        /// <param name="withBodyTag"></param>
        /// <returns></returns>
        public static string GetBody(string html, bool withBodyTag)
        {
            Regex regex;
            if (withBodyTag)
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
        /// <param name="text"></param>
        /// <param name="keywords"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string SetTextKeywordsHighLight(string text, string[] keywords, string color)
        {
            if (text.Trim() == "")
                return "";

            int Len = keywords.Length;
            if (Len == 0) return text;

            switch (color.Trim().ToLower())
            {
                case "red":
                    color = "FF0000";
                    break;
                case "green":
                    color = "00FF00";
                    break;
                case "blue":
                    color = "0000FF";
                    break;
            }

            string RegexStr = "(?:";
            for (int i = 0; i < Len; i++)
            {
                RegexStr += "(?<Keywords>" + keywords[i] + ")";
                if (i < Len - 1) RegexStr += "|";
            }
            RegexStr += ")";

            return Regex.Replace(text, RegexStr, "<FONT COLOR = \"#" + color + "\">${Keywords}</FONT>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}