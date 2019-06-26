using System;
using System.Collections.Generic;
using System.Web;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 发送视频消息
    /// </summary>
    public class VideoServiceMessage:ServiceMessage
    {

        private string media_id;
        /// <summary>
        /// 发送的视频的媒体ID  
        /// </summary>
        public string Media_id
        {
            get { return media_id; }
            set { media_id = value; }
        }

        private string thumb_media_id; 
        /// <summary>
        /// 视频缩略图的媒体ID 
        /// </summary>
        public  string Thumb_media_id
        {
            get { return thumb_media_id; }
            set { thumb_media_id = value; }
        }

        /// <summary>
        /// 构造函数,初始化数据
        /// </summary>
        /// <param name="_media_id">发送的视频的媒体ID</param>
        /// <param name="_thumb_media_id">视频缩略图的媒体ID </param>
        /// <param name="_touser">普通用户的openid</param>
        public VideoServiceMessage(string _media_id, string _thumb_media_id, string _touser)
            : base(_touser, "video")
        {
            this.media_id = _media_id;
            this.thumb_media_id = _thumb_media_id;
        }

        /// <summary>
        /// 转json格式
        /// </summary>
        /// <returns></returns>
        public override string ToJson()
        {
            StringBuilder Video = new StringBuilder();

            Video.Append("{");
            Video.Append("\"touser\"" + ":" + "\"" + base.Touser + "\",");
            Video.Append("\"msgtype\":\"video\",");
            Video.Append("\"video\":");
            Video.Append("{");
            Video.Append("\"media_id\":\"" + this.Media_id + "\",");
            Video.Append("\"thumb_media_id\":\"" + this.Thumb_media_id + "\"");
            Video.Append("}");
            Video.Append("}");

            return Video.ToString();
        }
    }
}