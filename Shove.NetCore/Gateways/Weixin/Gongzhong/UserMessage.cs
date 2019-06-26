using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Microsoft.AspNetCore.Http;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 发送微信信息类
    /// </summary>
    public class UserMessage
    {
        /// <summary>
        /// 声明委托，回调取数据
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="MsgType">消息类型</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns>返回消息集合</returns>
        public delegate List<Message> DelegateGetData(Message message, string MsgType, ref string errorDescription);

        /// <summary>
        /// 发送微信信息,回复用户
        /// </summary>
        /// <param name="context"></param>
        /// <param name="toKen">微信账号toKen</param>
        /// <param name="GetData">回调函数,取回\回复给用户的消息内容</param>
        /// <param name="errorDescription"></param>
        public static bool Handle(HttpContext context, string toKen, DelegateGetData GetData, ref string errorDescription)
        {
            errorDescription = "";
            #region 验证请求的信息是否来自微信服务器

            bool sucessed = ValidSignature(context, toKen, ref errorDescription);

            if (!sucessed)
            {
                //context.Response.End(); //[shove]

                return false;
            }

            #endregion

            #region 获取微信请求的内容

            Stream stream = null;//[shove] context.Request.InputStream;

            if (stream.Length < 1)
            {
                //errorDescription = "请求内容为空，无法解析。";
                //HttpContext.Current.Response.End();

                //return false;

                context.Response.WriteAsync(context.Request.Query["echoStr"]);
                //[shove] context.Response.End();

                return true;
            }

            string requestContent = "";
            byte[] b = new byte[stream.Length];
            stream.Read(b, 0, (int)stream.Length);
            requestContent = Encoding.UTF8.GetString(b);

            if (string.IsNullOrEmpty(requestContent))
            {
                //errorDescription = "请求内容为空，无法解析。";
                //HttpContext.Current.Response.End();

                //return false;

                context.Response.WriteAsync(context.Request.Query["echoStr"]);
                //[shove] context.Response.End();

                return true;
            }
            #endregion

            #region 分析微信请求的内容

            string FromUserName = "", ToUserName = "", Conten = "", MsgType = "";

            DateTime createTime;
            Message requestMessage = new Message();

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.LoadXml(requestContent);
                XmlNodeList list = doc.GetElementsByTagName("xml");
                XmlNode xn = list[0];

                FromUserName = xn.SelectSingleNode("//FromUserName").InnerText;
                ToUserName = xn.SelectSingleNode("//ToUserName").InnerText;

                MsgType = xn.SelectSingleNode("//MsgType").InnerText;
                string Time = xn.SelectSingleNode("//CreateTime").InnerText;

                DateTime dtStart = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);
                long lTime = long.Parse(xn.SelectSingleNode("//CreateTime").InnerText + "0000000");
                TimeSpan toNow = new TimeSpan(lTime);
                createTime = dtStart.Add(toNow);

                // 判断用户点击了菜单栏还是自己发送的信息
                if (MsgType.ToLower().Equals("event")) //用户点击菜单
                {
                    string Event = xn.SelectSingleNode("//Event").InnerText.ToLower();
                    string Ticket = "";//获取二维码Ticket;

                    //上报地理位置事件
                    if (Event.Equals("location"))
                    {
                        string _Latitude = xn.SelectSingleNode("//Latitude").InnerText;
                        string _Longitude = xn.SelectSingleNode("//Longitude").InnerText;
                        string _Precision = xn.SelectSingleNode("//Precision").InnerText;

                        requestMessage = new RepuestGeographicalPositionmMessage
                            (Event, _Latitude, _Longitude, _Precision, FromUserName, ToUserName, createTime, MsgType);

                    }
                    else if (Event.ToUpper().Equals("MASSSENDJOBFINISH"))//群发失败信息返回
                    {
                        string _MsgID = xn.SelectSingleNode("//MsgID").InnerText;
                        string _Status = xn.SelectSingleNode("//Status").InnerText;
                        string _TotalCount = xn.SelectSingleNode("//TotalCount").InnerText;
                        string _FilterCount = xn.SelectSingleNode("//FilterCount ").InnerText;
                        long _SentCount = Shove.Convert.StrToLong(xn.SelectSingleNode("//SentCount").InnerText, -1);
                        long _ErrorCount = Shove.Convert.StrToLong(xn.SelectSingleNode("//ErrorCount").InnerText, -1);

                        requestMessage = new RequestSendFinishMessage(_MsgID, _Status, _TotalCount, _FilterCount, _SentCount, _ErrorCount);

                    }
                    else//事假推送
                    {
                        string EventKey = xn.SelectSingleNode("//EventKey").InnerText;

                        //是否是从二维码扫描关注
                        if (EventKey.IndexOf("qrscene") >= 0)
                        {
                            Ticket = EventKey.Split('_')[1];
                        }

                        requestMessage = new RequestEventMessage(Event, EventKey, Ticket, FromUserName, ToUserName, createTime, MsgType);
                    }
                }
                else if (MsgType.ToLower().Equals("image")) //微信用户发来的图片
                {
                    string Url = xn.SelectSingleNode("//PicUrl").InnerText;
                    string MsgId = xn.SelectSingleNode("//MsgId").InnerText;
                    string MediaId = xn.SelectSingleNode("//MediaId").InnerText;//图片ID

                    requestMessage = new RequestImageMessage(Url, MsgId, MediaId, FromUserName, ToUserName, createTime, MsgType);
                }
                else if (MsgType.ToLower().Equals("link")) // 微信用户发来的链接
                {
                    string title = xn.SelectSingleNode("//Title").InnerText;
                    string Description = xn.SelectSingleNode("//Description").InnerText;
                    string Url = xn.SelectSingleNode("//Url ").InnerText;
                    string MsgId = xn.SelectSingleNode("//MsgId").InnerText;

                    requestMessage = new RequestLinkMessage(title, Description, Url, MsgId, FromUserName, ToUserName, createTime, MsgType);
                }
                else if (MsgType.ToLower().Equals("text")) // 消息文本
                {
                    Conten = xn.SelectSingleNode("//Content").InnerText;//文本

                    requestMessage = new RequestTextMessage(Conten, FromUserName, ToUserName, createTime, MsgType);
                }
                else if (MsgType.ToLower().Equals("location")) // 地理位置
                {
                    string Location_X = xn.SelectSingleNode("//Location_X").InnerText; // 纬度
                    string Location_Y = xn.SelectSingleNode("//Location_Y").InnerText; // 经度
                    string Scale = xn.SelectSingleNode("//Scale").InnerText; // 地图缩放大小
                    string Label = xn.SelectSingleNode("//Label").InnerText; // 地理消息位置 
                    string MsgId = xn.SelectSingleNode("//MsgId").InnerText;

                    requestMessage = new RequestLocationMessage(Location_X, Location_Y, Scale, Label, MsgId, FromUserName, ToUserName, createTime, MsgType);
                }
                else if (MsgType.ToLower().Equals("video"))//视频类型
                {
                    string MediaId = xn.SelectSingleNode("//MediaId").InnerText; // 视频消息媒体id，可以调用多媒体文件下载接口拉取数据。 
                    string ThumbMediaId = xn.SelectSingleNode("//ThumbMediaId").InnerText; // 视频消息缩略图的媒体id，可以调用多媒体文件下载接口拉取数据。 
                    string MsgId = xn.SelectSingleNode("//MsgId").InnerText; // 

                    requestMessage = new RequestVideoMessage(MediaId, ThumbMediaId, MsgId, FromUserName, ToUserName, createTime, MsgType);
                }
                else if (MsgType.ToLower().Equals("voice"))//声音类型
                {
                    string MediaId = xn.SelectSingleNode("//MediaId").InnerText; // 语音消息媒体id，可以调用多媒体文件下载接口拉取数据。  
                    string Format = xn.SelectSingleNode("//Format").InnerText; // 语音格式，如amr，speex等 
                    string MsgId = xn.SelectSingleNode("//MsgId").InnerText; // 消息ID;
                    string Recognition = xn.SelectSingleNode("//Recognition").InnerText.ToString();//语音识别结果,utf-8编码

                    requestMessage = new RequestVoiceMessage(MediaId, Format, MsgId, Recognition, FromUserName, ToUserName, createTime, MsgType);
                }
                else
                {
                    errorDescription = "请求内容无法解析类型。";
                    //[shove] context.Response.End();

                    return false;
                }
            }
            catch (Exception e)
            {
                errorDescription = "请求内容格式错误，无法解析：" + e.Message;
                //[shove] context.Response.End();

                return false;
            }

            #endregion

            #region 由应用程序返回消息内容

            List<Message> replyMessages = GetData(requestMessage, MsgType, ref errorDescription);

            if ((replyMessages == null) || (replyMessages.Count < 1))
            {
                //[shove] context.Response.End();

                return true;
            }

            #endregion

            #region 构建回复消息，并返回给微信服务器

            StringBuilder reply = new StringBuilder();

            reply.Append("<xml>");
            reply.Append("<ToUserName><![CDATA[" + FromUserName + "]]></ToUserName>");
            reply.Append("<FromUserName><![CDATA[" + ToUserName + "]]></FromUserName>");
            reply.Append("<CreateTime><![CDATA[" + DateTime.Now.Ticks.ToString() + "]]></CreateTime>");
            reply.Append("<MsgType><![CDATA[" + replyMessages[0].MsgType + "]]></MsgType>");

            for (int i = 0; i < replyMessages.Count; i++)
            {
                if (replyMessages[i] is TextMessage)
                {
                    TextMessage message = replyMessages[i] as TextMessage;
                    int len = Shove.String.GetLength(message.Content);

                    if (len > 2048)
                    {
                        errorDescription = "文本内容字节长度不能超过 2048。";
                        //[shove] context.Response.End();

                        return false;
                    }

                    reply.Append("<Content><![CDATA[" + message.Content + "]]></Content>");
                }
                else if (replyMessages[i] is ImageTextMessage)
                {
                    ImageTextMessage message = replyMessages[i] as ImageTextMessage;
                    string Ext = System.IO.Path.GetExtension(message.PicUrl).ToLower();

                    if (!Ext.Equals(".png") && !Ext.Equals(".jpg"))
                    {
                        errorDescription = "图片格式不正确,只能是：jpg、png。";
                        //[shove] context.Response.End();

                        return false;
                    }

                    if (System.Convert.ToInt32(message.ArticleCount) > 10 || replyMessages.Count > 10)
                    {
                        errorDescription = "图文信息超出 10 条限制。";
                        //[shove] context.Response.End();

                        return false;
                    }

                    if (i == 0)
                    {
                        reply.Append("<ArticleCount>" + message.ArticleCount + "</ArticleCount>");
                        reply.Append("<Articles>");
                    }

                    reply.Append("<item>");
                    reply.Append("<Title><![CDATA[" + message.Title + "]]></Title>");
                    reply.Append("<Description><![CDATA[" + message.Description + "]]></Description>");
                    reply.Append("<PicUrl><![CDATA[" + message.PicUrl + "]]></PicUrl>");
                    reply.Append("<Url><![CDATA[" + message.Url + "]]></Url>");
                    reply.Append("</item>");

                    if (i == replyMessages.Count - 1)
                    {
                        reply.Append("</Articles>");
                    }
                }
                else if (replyMessages[i] is MusicMessage)
                {
                    MusicMessage message = replyMessages[i] as MusicMessage;

                    reply.Append("<Music>");
                    reply.Append("<Title><![CDATA[" + message.Title + "]]></Title>");
                    reply.Append("<Description><![CDATA[" + message.Description + "]]></Description>");
                    reply.Append("<MusicUrl><![CDATA[" + message.MusicUrl + "]]></MusicUrl>");
                    reply.Append("<HQMusicUrl><![CDATA[" + message.HQMusicUrl + "]]></HQMusicUrl>");
                    reply.Append("</Music>");
                }
                else if (replyMessages[i] is VoiceMessage)
                {
                    VoiceMessage Voice = replyMessages[i] as VoiceMessage;

                    if (string.IsNullOrEmpty(Voice.MediaId))
                    {
                        errorDescription = "音频媒体ID为空,";
                        //[shove] context.Response.End();

                        return false;
                    }

                    reply.Append("<Voice>");
                    reply.Append("<MediaId><![CDATA[" + Voice.MediaId + "]]></MediaId>");
                    reply.Append("</Voice>");
                }
                else if (replyMessages[i] is VideoMessage)
                {
                    VideoMessage Video = replyMessages[i] as VideoMessage;

                    if (string.IsNullOrEmpty(Video.MediaId))
                    {
                        errorDescription = "视频媒体ID为空,";
                        //[shove] context.Response.End();

                        return false;
                    }

                    reply.Append("<Video>");
                    reply.Append("<MediaId><![CDATA[" + Video.MediaId + "]]></MediaId>");
                    reply.Append("<Title><![CDATA[" + Video.Title + "]]></Title>");
                    reply.Append("<Description><![CDATA[" + Video.Description + "]]></Description>");
                    reply.Append("</Video>");
                }
                else if (replyMessages[i] is CustomerServiceMessage)
                { }
                else
                {
                    errorDescription = "暂不支持回复此类型的消息。";
                    //[shove] context.Response.End();

                    return false;
                }
            }

            reply.Append("</xml>");

            context.Response.WriteAsync(reply.ToString());

            #endregion

            return true;
        }

        /// <summary>
        ///  通过微信服务器在 URL 后传递的 4 个参数,toKen,_signaTure,_timesTamp 进行排序加密后来判断是否是从微信服务器请求的。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="toKen">公众用户的 ToKen</param>
        /// <param name="errorDescription"></param>
        /// <returns>验证请求的信息是否来自微信服务器</returns>
        private static bool ValidSignature(HttpContext context, string toKen, ref string errorDescription)
        {
            errorDescription = "";

            string signature = context.Request.Query["signature"];
            string timestamp = context.Request.Query["timesTamp"];
            string nonce = context.Request.Query["nonce"];
            string echostr = context.Request.Query["echoStr"];

            string[] group = { toKen, timestamp, nonce };
            Array.Sort(group, StringComparer.OrdinalIgnoreCase);

            string str = string.Join("", group);
            var sha1 = System.Security.Cryptography.HashAlgorithm.Create("SHA1");
            str = Encoding.Default.GetString(sha1.ComputeHash(Encoding.Default.GetBytes(str))).ToLower();
            //str = FormsAuthentication.HashPasswordForStoringInConfigFile(str, "SHA1").ToLower();

            if (!str.Equals(signature.ToLower()))
            {
                errorDescription = "数据来源非法，验证失败。";

                return false;
            }

            return true;
        }

        /// <summary>
        /// 发送客服信息，返回 0:发送成功,-1:发送失败,-2:媒体文件过期
        /// </summary>
        /// <param name="message">消息对象</param>
        /// <param name="errorDescription">错误消息描述</param>
        /// <returns>0:发送成功,-1:发送失败,-2:媒体文件过期</returns>
        public static int SendMessage(ServiceMessage message, ref string errorDescription)
        {
            errorDescription = "";

            if (message == null)
            {
                errorDescription = "发生错误:发送的数据为null";
                return -1;
            }

            if (string.IsNullOrEmpty(Utility.Access_token))
            {
                errorDescription = "发生错误:access_token为null,页面的Load事件是否调用了Utility.GetAccessToken()方法";
                return -1;
            }

            string data = message.ToJson();

            if (data.Equals("-1"))
            {
                errorDescription = "URL地址不合法";
                return -1;
            }

            string Url = string.Format("https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token={0}", Utility.Access_token);

            return RequestAndValidOpenID(Url, data, ref errorDescription);
        }

        /// <summary>
        /// 请求向微信服务器通讯发送可客服消息，验证是否成功
        /// </summary>
        /// <param name="url">请求微信服务器的URL</param>
        /// <param name="data">请求的json数据</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns>0:发送成功,-1:发送失败,-2:媒体文件过期</returns>
        private static int RequestAndValidOpenID(string url, string data, ref string errorDescription)
        {
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                client.Headers["Content-Type"] = "application/json";
                client.Encoding = System.Text.Encoding.UTF8;

                int errorId = 0;

                try
                {
                    string strResult = client.UploadString(url, "post", data);
                    string errorCode = Utility.AnalysisJson(strResult, "errcode");

                    if (errorCode.Equals("40007"))
                    {
                        return errorId = -2;
                    }

                    if (!errorCode.Equals("0"))
                    {
                        errorDescription = ErrorInformation.GetErrorCode(strResult, "errcode");
                        return errorId = -1;
                    }
                }
                catch (Exception ex)
                {
                    errorDescription = ex.Message;
                    errorId = -1;
                }

                errorDescription = "发送成功";
                return errorId;
            }
        }

        /// <summary>
        /// 根据分组进行群发 
        /// </summary>
        /// <param name="group_id">用户分组ID</param>
        /// <param name="messagetype">群发的消息类型(目前只支持 1、mpnews(图文) 2、voice(语音) 3、image(图片)  4、text(文本))</param>
        /// <param name="content">群发的消息的内容(如果发送消息是媒体类型,此参数填写媒体media_id，如果消息为文本类型,直接填写文本消息内容就可)</param>
        /// <param name="msg_id">发送成功返回本次的群发的消息ID(可以通过消息ID删除群发信息)</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns>成功返回：true，否则返回true</returns>
        public static bool SendGroupMessage(string group_id, string messagetype, string content, ref string msg_id, ref string errorDescription)
        {
            List<string> typeList = new List<string>();
            typeList.Add("mpnews");
            typeList.Add("voice");
            typeList.Add("image");
            typeList.Add("text");

            if (!typeList.Contains(messagetype))
            {
                errorDescription = "-1,参数\"messagetype\":类型不合法";
                return false;
            }

            errorDescription = "";
            msg_id = "";
            string media_id = "media_id";//类型名称

            if (string.IsNullOrEmpty(content))
            {
                errorDescription = "-10004,media_id不能为空";
                return false;
            }

            if (string.IsNullOrEmpty(group_id))
            {
                errorDescription = "-10003,group_id不能为空";
                return false;
            }

            if (Shove.Convert.StrToLong(group_id, -1) < 0)
            {
                errorDescription = "-10002,不合法的groupId";
                return false;
            }

            if (messagetype == "text")
            {
                media_id = "content";
            }

            StringBuilder dr = new StringBuilder();

            dr.Append("{");
            dr.Append("\"filter\":{");
            dr.Append("\"group_id\":\"" + group_id + "\"");
            dr.Append("},");
            dr.Append("\"" + messagetype + "\":{");
            dr.Append("\"" + media_id + "\":\"" + content + "\"");
            dr.Append("},");
            dr.Append("\"msgtype\":\"" + messagetype + "\"");
            dr.Append("}");

            string result = RequestSend(string.Format("https://api.weixin.qq.com/cgi-bin/message/mass/sendall?access_token={0}", Utility.Access_token), dr.ToString(), ref errorDescription);

            if (string.IsNullOrEmpty(result))
            {
                return false;
            }

            long errcode = Shove.Convert.StrToLong(Utility.AnalysisJson(result, "errcode"), -1);

            if (errcode == 0)
            {
                msg_id = Utility.AnalysisJson(result, "msg_id");
                return true;
            }

            errorDescription = ErrorInformation.GetErrorCode(result, "errcode");

            return false;
        }

        /// <summary>
        /// 群发视频信息(根据分组和Openid进行发送)
        /// </summary>
        /// <param name="to_user"></param>
        /// <param name="Video_media_id"></param>
        /// <param name="type"></param>
        /// <param name="VideoTitle"></param>
        /// <param name="description"></param>
        /// <param name="msg_id"></param>
        /// <param name="errorDescription"></param>
        /// <returns></returns>
        public static bool SendGroupVideoMessage(string to_user, string Video_media_id, int type, string VideoTitle, string description, ref string msg_id, ref string errorDescription)
        {
            #region 参数验证
            if (type <= 0 && type > 2)
            {
                errorDescription = "-1,参数\"type\":类型不合法";
                return false;
            }

            errorDescription = "";
            msg_id = "";

            if (string.IsNullOrEmpty(Video_media_id))
            {
                errorDescription = "-10004,Video_media_id不能为空";
                return false;
            }

            if (string.IsNullOrEmpty(to_user))
            {
                errorDescription = "-10003,group_id不能为空";
                return false;
            }

            if (type == 1)
            {
                if (Shove.Convert.StrToLong(to_user, -1) < 0)
                {
                    errorDescription = "-10002,不合法的groupId";
                    return false;
                }
            }
            else
            {
            }

            #endregion

            #region 微信重新处理视频文件id,返回新的视频文件ID
            string json = "{\"media_id\": \"" + Video_media_id + "\", \"title\": \"" + VideoTitle + "\",\"description\": \"" + description + "\"}";


            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                client.Headers["Content-Type"] = "application/json";
                client.Encoding = System.Text.Encoding.UTF8;
                client.Credentials = CredentialCache.DefaultCredentials;
                try
                {
                    string strResult = "";
                    ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);
                    strResult = client.UploadString(string.Format("https://file.api.weixin.qq.com/cgi-bin/media/uploadvideo?access_token={0}", Utility.Access_token), "post", json);

                    if (strResult.IndexOf("errcode") >= 0)
                    {
                        errorDescription = ErrorInformation.GetErrorCode(strResult, "errcode");
                        Video_media_id = "";
                        return false;
                    }

                    string media_type = Utility.AnalysisJson(strResult, "type");
                    Video_media_id = Utility.AnalysisJson(strResult, "media_id");
                }
                catch (Exception e)
                {
                    errorDescription = e.Message;

                    return false;
                }
            }
            #endregion

            #region 群发处理后的视频文件
            StringBuilder dr = new StringBuilder();

            if (type == 1)
            {
                dr.Append("{");
                dr.Append("\"filter\":{");
                dr.Append("\"group_id\":\"" + to_user + "\"");
                dr.Append("},");
                dr.Append("\"mpvideo\":{");
                dr.Append("\"media_id\":\"" + Video_media_id + "\"");
                dr.Append("},");
                dr.Append("\"msgtype\":\"mpvideo\"");
                dr.Append("}");
            }
            else
            {
                string[] openid = to_user.Split(',');

                dr.Append("{");
                dr.Append("\"touser\":[");

                string str = "";
                for (int i = 0; i < openid.Length; i++)
                {
                    if (i < 10000 && !string.IsNullOrEmpty(openid[i]))
                    {
                        str += "\"" + openid[i] + "\",";
                    }
                }

                dr.Append(str.Substring(0, str.Length - 1));
                dr.Append("],");

                dr.Append("\"mpnews\":{");
                dr.Append("\"media_id\":\"" + Video_media_id + "\"");
                dr.Append("},");
                dr.Append("\"msgtype\":\"mpnews\"");
                dr.Append("}");
            }

            string result = RequestSend(string.Format("https://api.weixin.qq.com/cgi-bin/message/mass/sendall?access_token={0}", Utility.Access_token), dr.ToString(), ref errorDescription);

            if (string.IsNullOrEmpty(result))
            {
                return false;
            }

            long errcode = Shove.Convert.StrToLong(Utility.AnalysisJson(result, "errcode"), -1);

            if (errcode == 0)
            {
                msg_id = Utility.AnalysisJson(result, "msg_id");
                return true;
            }

            errorDescription = ErrorInformation.GetErrorCode(result, "errcode");

            return false;
            #endregion
        }

        /// <summary>
        /// 根据Openid进行群发 
        /// </summary>
        /// <param name="OpenidList">用户openid集合(用 "," 隔开)</param>
        /// <param name="messagetype">群发的消息类型(目前只支持 1、mpnews(图文) 2、voice(语音) 3、image(图片)  4、text(文本)</param>
        /// <param name="content">群发的消息的内容(如果发送信息是媒体类型,此参数填写媒体media_id，如果消息类型为文本类型,直接填写文本内容就可)</param>
        /// <param name="msg_id">发送成功返回本次的群发的消息ID(可以通过消息ID删除群发信息)</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns>成功返回：true，否则返回true</returns>
        public static bool SendFansListMessage(string OpenidList, string messagetype, string content, ref string msg_id, ref string errorDescription)
        {
            errorDescription = "";
            msg_id = "";

            if (string.IsNullOrEmpty(content))
            {
                errorDescription = "-10005,media_id不能为空";
                return false;
            }

            if (string.IsNullOrEmpty(OpenidList))
            {
                errorDescription = "-10004,OpenidList不能为空";
                return false;
            }

            StringBuilder dr = new StringBuilder();

            string media_id = "media_id";//类型名称

            if (string.IsNullOrEmpty(content))
            {
                errorDescription = "-10004,media_id不能为空";
                return false;
            }

            if (messagetype == "text")
            {
                media_id = "content";
            }

            try
            {
                string[] openid = OpenidList.Split(',');

                dr.Append("{");
                dr.Append("\"touser\":[");

                string str = "";
                for (int i = 0; i < openid.Length; i++)
                {
                    if (i < 10000 && !string.IsNullOrEmpty(openid[i]))
                    {
                        str += "\"" + openid[i] + "\",";
                    }
                }

                dr.Append(str.Substring(0, str.Length - 1));
                dr.Append("],");
                dr.Append("\"" + messagetype + "\":{");
                dr.Append("\"" + media_id + "\":\"" + content + "\"");
                dr.Append("},");
                dr.Append("\"msgtype\":\"" + messagetype + "\"");
                dr.Append("}");

            }
            catch (Exception ex)
            {
                errorDescription = "发送失败：" + ex.Message;
                return false;
            }

            string result = RequestSend(string.Format("https://api.weixin.qq.com/cgi-bin/message/mass/send?access_token={0}", Utility.Access_token), dr.ToString(), ref errorDescription);

            if (string.IsNullOrEmpty(result))
            {
                return false;
            }

            long errcode = Shove.Convert.StrToLong(Utility.AnalysisJson(result, "errcode"), -1);

            if (errcode == 0)
            {
                msg_id = Utility.AnalysisJson(result, "msg_id");
                return true;
            }

            errorDescription = ErrorInformation.GetErrorCode(result, "errcode");

            return false;
        }

        /// <summary>
        /// 删除群发消息(删除消息只是将消息的图文详情页失效，已经收到的用户，还是能在其本地看到消息卡片)
        /// </summary>
        /// <param name="msg_id">群发的消息ID</param>
        /// <param name="errorDescription">描述</param>
        /// <returns>返回成功：true 失败：false</returns>
        public static bool DeleteMassSendMessage(string msg_id, ref string errorDescription)
        {
            errorDescription = "";

            if (string.IsNullOrEmpty(msg_id))
            {
                errorDescription = "-10007,消息ID能为空";
                return false;
            }

            if (Shove.Convert.StrToLong(msg_id, -1) < 0)
            {
                errorDescription = "-10008,不合法的msg_id";
                return false;
            }

            string json = "{\"msgid\":" + msg_id + "}";

            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                client.Headers["Content-Type"] = "application/json";
                client.Encoding = System.Text.Encoding.UTF8;

                try
                {
                    string strResult = client.UploadString
                        (string.Format("https://api.weixin.qq.com//cgi-bin/message/mass/delete?access_token={0}", Utility.Access_token), "post", json);
                    string errorCode = Utility.AnalysisJson(strResult, "errcode");

                    if (!errorCode.Equals("0"))
                    {
                        errorDescription = ErrorInformation.GetErrorCode(strResult, "errcode");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    errorDescription = ex.Message;
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="_url"></param>
        /// <param name="data"></param>
        /// <param name="errorDescription"></param>
        /// <returns></returns>
        private static string RequestSend(string _url, string data, ref string errorDescription)
        {
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                client.Headers["Content-Type"] = "application/json";
                client.Encoding = System.Text.Encoding.UTF8;

                try
                {
                    string strResult = client.UploadString(_url, "post", data);

                    return strResult;
                }
                catch (Exception ex)
                {
                    errorDescription = ex.Message;

                    return "";
                }
            }
        }

        /// <summary>
        /// 用户页面授权
        /// </summary>
        /// <param name="context"></param>
        /// <param name="appid">第三方用户唯一凭证APPID</param>
        /// <param name="secret">第三方用户唯一凭证密钥，既appsecret</param>
        /// <param name="state">网页授权,得到开发者指定在菜单中的参数(似于菜单中Key)</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns>返回微信用户Openid</returns>
        public static string WebPageAuthorization(HttpContext context, string appid, string secret, ref string state, ref string errorDescription)
        {
            string code = context.Request.Query["code"].ToString();

            //如果为空表示用户未授权,或者请求不是来自微信服务器
            if (string.IsNullOrEmpty(code))
            {
                errorDescription = "未知请求";
                return null;
            }

            string _state = context.Request.Query["state"].ToString();

            if (string.IsNullOrEmpty(state))
            {
                errorDescription = "未知请求";
                return null;
            }

            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                string result = "";
                string url = string.Format("https://api.weixin.qq.com/sns/oauth2/access_token?appid={0}&secret={1}&code={2}&grant_type=authorization_c", appid, secret, code);

                try
                {
                    byte[] by = client.DownloadData(url);
                    result = System.Text.Encoding.UTF8.GetString(by);
                }
                catch (Exception ex)
                {
                    errorDescription = ex.Message;
                    return null;
                }

                if (result.IndexOf("errcode", StringComparison.Ordinal) >= 0)
                {
                    errorDescription = Utility.AnalysisJson(result, "errcode");
                    return null;
                }

                string access_token = Utility.AnalysisJson(result, "access_token");
                string refresh_token = Utility.AnalysisJson(result, "refresh_token");
                string openid = Utility.AnalysisJson(result, "openid");
                string scope = Utility.AnalysisJson(result, "scope");

                try
                {
                    //调用通过Openid获取用户信息的方法
                    state = _state;
                    return openid;// Information.GetUserInformation(openid, ref errorDescription);
                }
                catch (Exception ex)
                {
                    errorDescription = ex.Message;
                    return null;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }

    }
}