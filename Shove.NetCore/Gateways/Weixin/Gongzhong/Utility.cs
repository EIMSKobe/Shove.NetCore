using System;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using LitJson;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 通用类
    /// </summary>
    public class Utility
    {
        /// <summary>
        /// 令牌
        /// </summary>
        public static string access_token;
        private static DateTime last_token_datetime = DateTime.Now;

        /// <summary>
        /// 通过参数AppID,AppSecret,获取到的微信菜单token
        /// </summary>
        public static string Access_token
        {
            get
            {

                //if (!string.IsNullOrEmpty(access_token) && ((DateTime.Now - last_token_datetime).TotalMinutes < 115))
                if (!string.IsNullOrEmpty(access_token) && (DateTime.Now.Subtract(last_token_datetime).Duration().TotalMinutes < 115))
                {
                    return access_token;
                }

                //重置令牌
                ResetAccessToken(AppID, AppSecret);

                if (!string.IsNullOrEmpty(access_token))
                {
                    return access_token;
                }

                return "";
            }
        }

        /// <summary>
        /// 凭证
        /// </summary>
        public static string AppID = string.Empty;

        /// <summary>
        /// 密钥
        /// </summary>
        public static string AppSecret = string.Empty;

        /// <summary>
        /// 初始化AccessToken
        /// </summary>
        /// <param name="_AppID"></param>
        /// <param name="_AppSecret"></param>
        public static void InitializeAccessToken(string _AppID, string _AppSecret)
        {
            AppID = _AppID;
            AppSecret = _AppSecret;
        }

        /// <summary>
        /// 获取access_token(同时验证APPID)
        /// </summary>
        /// <param name="_AppID">第三方用户唯一凭证</param>
        /// <param name="_AppSecret">第三方用户唯一凭证密钥</param>
        /// <returns>返回菜单ACCESS_TOKEN</returns>
        public static string ResetAccessToken(string _AppID, string _AppSecret)
        {
            Shove.IO.Log log = new IO.Log("WeixinGongzhong");

            if (string.IsNullOrEmpty(_AppID) || string.IsNullOrEmpty(_AppSecret))
            {
                log.Write("从微信服务器获取 access_token 发生错误：未提供有效的 AppID、AppSecret，请通过 Utility.InitializeAccessToken(AppID， AppSecret) 方法提供参数后再进行接口访问。");

                return "";
            }

            WebClient webClient = new WebClient();
            byte[] bytes = null;

            try
            {
                bytes = webClient.DownloadData(
                    string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}",
                    _AppID, _AppSecret));
            }
            catch (Exception e)
            {
                log.Write("从微信服务器获取 access_token 发生错误：" + e.Message);

                return "";
            }
            finally
            {
                webClient.Dispose();
            }

            if (bytes == null || bytes.Length == 0)
            {
                log.Write("从微信服务器获取 access_token 发生错误");

                return "";
            }

            string result = Encoding.UTF8.GetString(bytes);

            if (result.IndexOf("errcode", StringComparison.Ordinal) >= 0)
            {
                log.Write("从微信服务器获取 access_token 发生错误：" + result);

                return "";
            }

            string[] res = result.Split('\"');

            //获取的access_token
            access_token = res[3].ToString();

            //更新access_token过期时间
            last_token_datetime = DateTime.Now;

            return access_token;

        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="filePath">要上传的本地文件路径</param>
        /// <param name="type">媒体文件类型，分别有图片（image）、语音（voice）、视频（video）和缩略图（thumb)</param>
        /// <param name="media_id">上传成功后,返回媒体文件ID</param>
        /// <param name="created_at">媒体文件上传的时间 文件上传三天系统会自动删除</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns></returns>
        public static bool UploadFile(string filePath, string type, ref string media_id, ref string created_at, ref string errorDescription)
        {
            errorDescription = string.Empty;
            created_at = string.Empty;
            string types = "image,voice,video,thumb";

            if (types.IndexOf(type, StringComparison.Ordinal) < 0)
            {
                errorDescription = "媒体文件类型错误";
                return false;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                errorDescription = "错误：媒体文件为空";
                return false;
            }

            if (string.IsNullOrEmpty(Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";
                return false;
            }

            string errorCode = string.Empty;

            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                try
                {
                    byte[] by = client.UploadFile(string.Format("http://file.api.weixin.qq.com/cgi-bin/media/upload?access_token={0}&type={1}", Access_token, type), filePath);
                    errorCode = System.Text.Encoding.UTF8.GetString(by);
                }
                catch (Exception ex)
                {
                    errorDescription = ex.Message;

                    return false;
                }
                finally
                {
                    client.Dispose();
                }

                if (string.IsNullOrEmpty(errorCode))
                {
                    errorDescription = "接口调用异常";

                    return false;
                }

                if (errorCode.IndexOf("errcode", StringComparison.Ordinal) < 0)
                {

                    //媒体文件ID;
                    media_id = Utility.AnalysisJson(errorCode, type == "thumb" ? "thumb_media_id" : "media_id");// errorCode.Split('\"')[7];

                    //媒体文件上传时间
                    created_at = Utility.FromUnixTime(Utility.AnalysisJson(errorCode, "created_at")).ToString();

                    return true;
                }

                //根据错误代码获取错误代码对应的解释
                errorDescription = ErrorInformation.GetErrorCode(errorCode, "errcode");
            }

            return false;
        }

        /// <summary>
        /// 下载媒体文件
        /// </summary>
        /// <param name="context"></param>
        /// <param name="media_id">文件的ID</param>
        /// <param name="saveFilePath">保存文件的地址 下载成功返回文件的地址</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns></returns>
        public static bool DownloadFile(HttpContext context, string media_id, ref string saveFilePath, ref string errorDescription)
        {
            errorDescription = string.Empty;
            //得到文件名称,byte
            string pathName = string.Empty;

            if (string.IsNullOrEmpty(media_id))
            {
                errorDescription = "发生错误,media_id为空";

                return false;
            }

            if (string.IsNullOrEmpty(Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";

                return false;
            }

            Uri downUri = new Uri(
                       string.Format("http://file.api.weixin.qq.com/cgi-bin/media/get?access_token={0}&media_id={1}",
                       Access_token, media_id));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(downUri);
            //设置接收对象大小为0-2097152字节(2MB)。
            request.AddRange(0, 2097152);

            if (string.IsNullOrEmpty(saveFilePath))
            {
                saveFilePath = "~/Downloads/Weixin/";
            }

            byte[] bytes = new byte[102400];
            //int n = 1;

            try
            {
                #region 如果返回的信息小于0，获取微信服务器返回的错误编码

                if (request.GetResponse().ContentLength <= 0)
                {
                    using (System.Net.WebClient client = new System.Net.WebClient())
                    {

                        byte[] by = client.DownloadData(string.Format("http://file.api.weixin.qq.com/cgi-bin/media/get?access_token={0}&media_id={1}", Access_token, media_id));

                        string errorCode = System.Text.Encoding.UTF8.GetString(by);

                        //获取返回错误代码
                        errorDescription = ErrorInformation.GetErrorCode(errorCode, "errcode");
                    }

                    return false;
                }
                #endregion


                pathName = request.GetResponse().Headers.GetValues(1)[0].Split('\"')[1];

                //媒体文件类型
                // string type = request.GetResponse().ContentType;
                using (Stream stream = request.GetResponse().GetResponseStream())
                {
                    //[shove]
                    //if (!Directory.Exists(context.Server.MapPath(saveFilePath)))
                    //{
                    //    Directory.CreateDirectory(context.Server.MapPath(saveFilePath));
                    //}

                    //using (FileStream fs = File.Create(context.Server.MapPath(saveFilePath) + pathName))
                    //{
                    //    while (n > 0)
                    //    {
                    //        //一次从流中读多少字节，
                    //        n = stream.Read(bytes, 0, 10240);

                    //        //将指定字节的流信息写入文件流中
                    //        fs.Write(bytes, 0, n);
                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                errorDescription = ex.Message;

                return false;
            }

            saveFilePath = saveFilePath + pathName;

            return true;
        }

        /// <summary>
        /// 下载二维码图片
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ticket">创建二维码时的ticket</param>
        /// <param name="saveFilePath">文件保存的路径 下载成功返回的值获取</param>
        /// <param name="errorDescription"></param>
        public static bool DownloadTwo_Dimension_Code(HttpContext context, string ticket, ref string saveFilePath, ref string errorDescription)
        {
            //系统时间做为二维码图片名称
            string dataTime = DateTime.Now.ToString("yyyyMMddHHmmss");

            if (string.IsNullOrEmpty(ticket))
            {
                errorDescription = "发生错误,ticket为空";

                return false;
            }

            Uri downUri = new Uri(string.Format("https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket={0}", System.Web.HttpUtility.UrlEncode(ticket, System.Text.Encoding.UTF8)));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(downUri);
            request.Method = "get";



            //设置接收对象大小为0-2097152字节(2MB)。
            request.AddRange(0, 2097152);

            if (string.IsNullOrEmpty(saveFilePath))
            {
                saveFilePath = "~/Downloads/Weixin/";
            }


            byte[] bytes = new byte[102400];
            //int n = 1;

            try
            {

                if (request.GetResponse().ContentLength <= 0)
                {
                    errorDescription = "下载失败";

                    return false;
                }

                //媒体文件类型
                //string type = request.GetResponse().ContentType;

                using (Stream stream = request.GetResponse().GetResponseStream())
                {
                    //[shove]
                    //if (!Directory.Exists(context.Server.MapPath(saveFilePath)))
                    //{
                    //    Directory.CreateDirectory(context.Server.MapPath(saveFilePath));
                    //}

                    //using (FileStream fs = File.Create(context.Server.MapPath(saveFilePath) + dataTime + ".jpg"))
                    //{
                    //    while (n > 0)
                    //    {
                    //        //一次从流中读多少字节，
                    //        n = stream.Read(bytes, 0, 10240);

                    //        //将指定字节的流信息写入文件流中
                    //        fs.Write(bytes, 0, n);
                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                errorDescription = ex.Message;

                return false;
            }

            saveFilePath = saveFilePath + dataTime + ".jpg";

            return true;
        }

        /// <summary>
        /// 获取json中的Key获取value值
        /// </summary>
        /// <param name="json">json结构</param>
        /// <param name="Key">json结构中的Key</param>
        /// <returns>value</returns>
        public static string AnalysisJson(string json, string Key)
        {

            string value = string.Empty;
            try
            {

                JsonData jsonData = JsonMapper.ToObject(json);
                value = jsonData[Key].ToString();

            }
            catch (Exception ex)
            { return ex.Message; }

            return value;
        }

        /// <summary>
        /// Unix时间戳转成时间 转换失败，返回系统时间
        /// </summary>
        /// <param name="timeStamp">要转换的时间戳 返回时间</param>
        public static DateTime FromUnixTime(string timeStamp)
        {
            try
            {
                return DateTime.Parse("1970-01-01 08:00:00").AddSeconds(long.Parse(timeStamp));
            }
            catch { return DateTime.Now; }
        }

        /// <summary>
        /// 时间转成Unix时间戳 转换失败返回系统时间戳
        /// </summary>
        /// <param name="dateTime">要转换的时间 返回时间戳</param>
        public static string UnixTimeStamp(DateTime dateTime)
        {
            try
            {
                return ((dateTime.Ticks - DateTime.Parse("1970-01-01 08:00:00").Ticks) / 10000000).ToString();
            }
            catch { return ((DateTime.Now.Ticks - DateTime.Parse("1970-01-01 08:00:00").Ticks) / 10000000).ToString(); }
        }

        /// <summary>
        /// 调用百度地图API,通过请求的经度纬度获取所在地址
        /// </summary>
        /// <param name="latitude">纬度</param>
        /// <param name="longitude">经度</param>
        /// <returns></returns>
        public static string GetDetailedAddress_API(string latitude, string longitude)
        {
            long intger = 0;

            if (long.TryParse(latitude, out intger))
            {
                return "地理纬度不正确";
            }

            if (long.TryParse(longitude, out intger))
            {
                return "地理经度不正确";
            }

            XmlDocument doc = new XmlDocument();

            try
            {
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    byte[] by = client.DownloadData(string.Format("http://api.map.baidu.com/geocoder/v2/?ak=F66f81409fc98ce4397c98f02ebc2020&callback=renderReverse&location={0},{1}&output=xml&pois=1", latitude, longitude));

                    string result = System.Text.Encoding.UTF8.GetString(by);

                    doc.LoadXml(result);
                    XmlNodeList list = doc.GetElementsByTagName("GeocoderSearchResponse");
                    XmlNode xn = list[0];

                    //状态
                    string status = xn.SelectSingleNode("//status").InnerText;

                    if (!status.Equals("0"))
                    {
                        return "请求百度地图API发生错误。";
                    }

                    string name = xn.SelectSingleNode("//formatted_address").InnerText;

                    return name;
                }
            }
            catch (Exception ex)
            {
                return ex.Message; ;
            }
        }

        #region 上传指定群发消息素材

        /// <summary>
        /// 上传群图文素材(此方法上传素材只能用作于群发)
        /// </summary>
        /// <param name="massImageTextMessage">上传素材的集合(支持1到10条图文信息)</param>
        /// <param name="media_id">上传素材后获取的唯一标识(用于群发接口中使用)</param>
        /// <param name="errorDescription"></param>
        /// <returns>成功: true | 失败：false</returns>
        public static bool UploadMassImageTextMessage(List<MassImageTextMessage> massImageTextMessage, ref string media_id, ref string errorDescription)
        {
            errorDescription = string.Empty;
            media_id = string.Empty;

            if (massImageTextMessage == null || massImageTextMessage.Count <= 0)
            {
                errorDescription = "-10000,消息集合不能为空";

                return false;
            }

            StringBuilder json = new StringBuilder();
            json.Append("{\"articles\": [");

            for (int i = 0; i < massImageTextMessage.Count; i++)
            {
                json.Append(massImageTextMessage[i].Tojson());
            }

            json.Remove(json.Length - 1, 1);
            json.Append("]}");


            System.Net.WebClient client = new System.Net.WebClient();
            client.Headers["Content-Type"] = "application/json";
            client.Encoding = System.Text.Encoding.UTF8;

            string strResult = string.Empty;

            try
            {
                strResult = client.UploadString(string.Format("https://api.weixin.qq.com/cgi-bin/media/uploadnews?access_token={0}", Access_token), "post", json.ToString());
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return false;
            }
            finally
            {
                client.Dispose();
            }

            if (string.IsNullOrEmpty(strResult))
            {
                errorDescription = "接口调用异常";

                return false;
            }

            if (strResult.IndexOf("errcode") >= 0)
            {
                errorDescription = ErrorInformation.GetErrorCode(strResult, "errcode");
                media_id = "";

                return false;
            }

            string type = Utility.AnalysisJson(strResult, "type");
            media_id = Utility.AnalysisJson(strResult, "media_id");

            return true;
        }

        #endregion

        #region  微信永久素材操作
        /// <summary>上传永久文件类型素材
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="type">文件类型</param>
        /// <param name="videoTitle">视频标题（不是视频填空即可）</param>
        /// <param name="videoIntroduction">视频简介（不是视频填空即可）</param>
        /// <param name="media_id">返回媒体文件ID</param>
        /// <param name="url">媒体文件路径（仅新增图片素材时会返回该字段）</param>
        /// <param name="errorDescription">返回消息</param>
        /// <returns></returns>
        public static bool UploadPermanenceFileMaterial(string filePath, string type, string videoTitle, string videoIntroduction, ref string media_id, ref string url, ref string errorDescription)
        {
            errorDescription = string.Empty;
            string types = "image,voice,video,thumb";

            if (types.IndexOf(type) < 0)
            {
                errorDescription = "媒体文件类型错误";

                return false;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                errorDescription = "错误：媒体文件为空";

                return false;
            }

            if (string.IsNullOrEmpty(Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";

                return false;
            }

            HttpWebRequest request = null;

            if (type == "video")
            {
                if (string.IsNullOrEmpty(videoTitle))
                {
                    errorDescription = "上传视频的标题不能为空";

                    return false;
                }

                string videoStr = "{ \"title\":\"" + videoTitle + "\", \"introduction\":\"" + videoIntroduction + "\"}";
                request = (HttpWebRequest)WebRequest.Create(string.Format("https://api.weixin.qq.com/cgi-bin/material/add_material?access_token={0}&type={1}&description={2}", Access_token, type, videoStr));
            }
            else
            {
                request = (HttpWebRequest)WebRequest.Create(string.Format("https://api.weixin.qq.com/cgi-bin/material/add_material?access_token={0}&type={1}", Access_token, type));
            }

            MemoryStream postStream = new MemoryStream();
            string boundary = "----" + DateTime.Now.Ticks.ToString("x");
            FileInfo files = new FileInfo(filePath);

            string formdataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
            string formdata = string.Format(formdataTemplate, "media", files.Name);
            byte[] footer = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");

            request.Method = "POST";
            request.Timeout = 300000;
            request.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.KeepAlive = true;
            request.ServicePoint.ConnectionLimit = 1000;
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";

            byte[] buffer = new byte[1024];
            int bytesRead = 0;
            string strResult = string.Empty;

            FileStream fileStream = null;
            byte[] formdataBytes = null;

            try
            {
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                formdataBytes = System.Text.Encoding.UTF8.GetBytes(postStream.Length == 0 ? formdata.Substring(2, formdata.Length - 2) : formdata);//第一行不需要换行
                postStream.Write(formdataBytes, 0, formdataBytes.Length);

                //写入文件
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    postStream.Write(buffer, 0, bytesRead);
                }
                //结尾
                postStream.Write(footer, 0, footer.Length);

            }
            catch (Exception ex)
            {
                errorDescription = ex.Message;

                return false;
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }

            if (postStream == null)
            {
                errorDescription = "读写流异常";

                return false;
            }

            bytesRead = 0;
            request.ContentLength = postStream.Length;
            postStream.Position = 0;
            Stream requestStream = null;
            HttpWebResponse response = null;
            Stream responseStream = null;
            StreamReader myStreamReader = null;

            try
            {
                requestStream = request.GetRequestStream();
                while ((bytesRead = postStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    requestStream.Write(buffer, 0, bytesRead);
                }

                response = (HttpWebResponse)request.GetResponse();
                responseStream = response.GetResponseStream();


            }
            catch (Exception ex)
            {
                errorDescription = ex.Message;

                return false;
            }
            finally
            {
                if (responseStream != null)
                {
                    myStreamReader = new StreamReader(responseStream, System.Text.Encoding.GetEncoding("utf-8"));
                    strResult = myStreamReader.ReadToEnd();

                    if (myStreamReader != null)
                    {
                        myStreamReader.Close();
                        myStreamReader.Dispose();
                    }

                    responseStream.Close();
                    responseStream.Dispose();
                }

                if (postStream != null)
                {
                    postStream.Close();//关闭文件访问
                    postStream.Dispose();//释放资源
                }

                if (requestStream != null)
                {
                    requestStream.Close();
                    requestStream.Dispose();
                }

                if (response != null)
                {
                    response.Close();
                }
            }

            if (string.IsNullOrEmpty(strResult))
            {
                errorDescription = "接口调用异常";

                return false;
            }

            if (strResult.IndexOf("errcode") < 0)
            {
                //媒体文件ID;
                media_id = Utility.AnalysisJson(strResult, "media_id");

                //媒体文件路径
                if (strResult.Contains("url"))
                    url = Utility.AnalysisJson(strResult, "url");
                else
                    url = string.Empty;

                return true;
            }
            else
            {
                //根据错误代码获取错误代码对应的解释
                errorDescription = ErrorInformation.GetErrorCode(strResult, "errcode");

                return false;
            }

        }

        /// <summary>上传永久图文素材
        /// </summary>
        /// <param name="massImageTextMessage">上传素材的集合(支持1到10条图文信息)</param>
        /// <param name="media_id">上传素材后获取的唯一标识(用于群发接口中使用)</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns>成功: true | 失败：false</returns>
        public static bool UploadPermanenceImageTextMaterial(List<MassImageTextMessage> massImageTextMessage, ref string media_id, ref string errorDescription)
        {
            errorDescription = string.Empty;
            media_id = string.Empty;

            if (massImageTextMessage == null || massImageTextMessage.Count <= 0)
            {
                errorDescription = "-10000,消息集合不能为空";

                return false;
            }

            if (string.IsNullOrEmpty(Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";

                return false;
            }

            StringBuilder json = new StringBuilder();
            json.Append("{\"articles\": [");

            for (int i = 0; i < massImageTextMessage.Count; i++)
            {
                json.Append(massImageTextMessage[i].Tojson());
            }

            json.Remove(json.Length - 1, 1);
            json.Append("]}");

            System.Net.WebClient client = new System.Net.WebClient();
            client.Headers["Content-Type"] = "application/json";
            client.Encoding = System.Text.Encoding.UTF8;
            string strResult = string.Empty;

            try
            {
                strResult = client.UploadString(string.Format("https://api.weixin.qq.com/cgi-bin/material/add_news?access_token={0}", Access_token), "post", json.ToString());
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return false;
            }
            finally
            {
                client.Dispose();
            }

            if (string.IsNullOrEmpty(strResult))
            {
                errorDescription = "接口调用异常";

                return false;
            }

            if (strResult.IndexOf("errcode") >= 0)
            {
                errorDescription = ErrorInformation.GetErrorCode(strResult, "errcode");
                media_id = string.Empty;

                return false;
            }

            media_id = Utility.AnalysisJson(strResult, "media_id");

            return true;
        }

        /// <summary>修改永久图文素材
        /// </summary>
        /// <param name="massImageTextMessage">图文信息集合</param>
        /// <param name="media_id">上传素材后获取的唯一标识(用于群发接口中使用)</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns>成功: true | 失败：false</returns>
        public static bool UpdatePermanenceImageTextMaterial(List<MassImageTextMessage> massImageTextMessage, string media_id, ref string errorDescription)
        {
            errorDescription = string.Empty;

            if (massImageTextMessage == null || massImageTextMessage.Count <= 0)
            {
                errorDescription = "-10000,消息集合不能为空";

                return false;
            }

            if (string.IsNullOrEmpty(Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";

                return false;
            }

            StringBuilder json = new StringBuilder();
            json.Append("{ \"media_id\":\"" + media_id + "\",\"index\":\"0\",\"articles\": ");

            for (int i = 0; i < massImageTextMessage.Count; i++)
            {
                json.Append(massImageTextMessage[i].Tojson());
            }

            json.Remove(json.Length - 1, 1);
            json.Append("}");

            System.Net.WebClient client = new System.Net.WebClient();
            client.Headers["Content-Type"] = "application/json";
            client.Encoding = System.Text.Encoding.UTF8;
            string strResult = string.Empty;

            try
            {
                strResult = client.UploadString(string.Format("https://api.weixin.qq.com/cgi-bin/material/update_news?access_token={0}", Access_token), "post", json.ToString());
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return false;
            }
            finally
            {
                client.Dispose();
            }

            if (string.IsNullOrEmpty(strResult))
            {
                errorDescription = "接口调用异常";

                return false;
            }

            if (strResult.IndexOf("errcode") >= 0)
            {
                //根据错误代码获取错误代码对应的解释
                errorDescription = ErrorInformation.GetErrorCode(strResult, "errcode");

                if (Utility.AnalysisJson(strResult, "errcode") == "0")
                    return true;
                else
                    return false;
            }

            return true;
        }

        /// <summary>删除永久素材
        /// </summary>
        /// <param name="media_id">素材标识id</param>
        /// <param name="errorDescription">返回错误信息</param>
        /// <returns></returns>
        public static bool DeletePermanenceMaterial(string media_id, ref string errorDescription)
        {
            if (string.IsNullOrEmpty(Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";

                return false;
            }

            string param = "{\"media_id\":\"" + media_id + "\"}";
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)WebRequest.Create(string.Format("https://api.weixin.qq.com/cgi-bin/material/del_material?access_token={0}", Access_token));
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            byte[] payload = System.Text.Encoding.UTF8.GetBytes(param);
            request.ContentLength = payload.Length;
            string strResult = string.Empty;

            Stream requestStream = null;
            HttpWebResponse response = null;
            Stream responseStream = null;
            StreamReader myStreamReader = null;

            try
            {
                requestStream = request.GetRequestStream();
                requestStream.Write(payload, 0, payload.Length);

                response = (HttpWebResponse)request.GetResponse();
                responseStream = response.GetResponseStream();

            }
            catch (Exception ex)
            {
                errorDescription = ex.Message;

                return false;
            }
            finally
            {
                if (responseStream != null)
                {
                    myStreamReader = new StreamReader(responseStream, System.Text.Encoding.GetEncoding("utf-8"));
                    strResult = myStreamReader.ReadToEnd();

                    if (myStreamReader != null)
                    {
                        myStreamReader.Close();
                        myStreamReader.Dispose();
                    }

                    responseStream.Close();
                    responseStream.Dispose();
                }

                if (requestStream != null)
                {
                    requestStream.Close();
                }

                if (response != null)
                {
                    response.Close();
                }
            }

            if (string.IsNullOrEmpty(strResult))
            {
                errorDescription = "接口调用异常";

                return false;
            }

            if (strResult.IndexOf("errcode") >= 0)
            {
                //根据错误代码获取错误代码对应的解释
                errorDescription = ErrorInformation.GetErrorCode(strResult, "errcode");

                if (Utility.AnalysisJson(strResult, "errcode") == "0")
                    return true;
                else
                    return false;
            }

            return true;
        }

        #endregion
    }
}