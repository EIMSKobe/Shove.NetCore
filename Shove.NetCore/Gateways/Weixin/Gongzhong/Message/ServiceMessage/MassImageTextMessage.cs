using System;
using System.Collections.Generic;
using System.Web;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    ///MassImageTextMessage 的摘要说明
    /// </summary>
    public class MassImageTextMessage
    {
        private string thumb_media_id;

        /// <summary>
        /// 图文消息缩略图的media_id，可以在基础支持-上传多媒体文件接口中获得 
        /// </summary>
        public string Thumb_media_id
        {
            get { return thumb_media_id; }
        }

        private string author;

        /// <summary>
        /// 图文消息的作者 
        /// </summary>
        public string Author
        {
            get { return author; }
        }

        private string title;

        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get { return title; }
        }

        private string content_source_url;

        /// <summary>
        /// 在图文消息页面点击“阅读原文”后的页面 
        /// </summary>
        public string Content_source_url
        {
            get { return content_source_url; }
        }

        private string content;

        /// <summary>
        /// 图文消息页面的内容，支持HTML标签 
        /// </summary>
        public string Content
        {
            get { return content; }
        }

        private string digest;

        /// <summary>
        /// 图文消息的描述
        /// </summary>
        public string Digest
        {
            get { return digest; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_thumb_media_id"> 图文消息缩略图的media_id，可以在基础支持-上传多媒体文件接口中获得</param>
        /// <param name="_author">图文消息的作者 </param>
        /// <param name="_title">标题</param>
        /// <param name="_content_source_url">在图文消息页面点击“阅读原文”后的页面 </param>
        /// <param name="_content">图文消息页面的内容，支持HTML标签 </param>
        /// <param name="_digest">图文消息的描述</param>
        public MassImageTextMessage(string _thumb_media_id, string _author, string _title, string _content_source_url, string _content, string _digest)
        {
            this.thumb_media_id = _thumb_media_id;
            this.author = _author;
            this.title = _title;
            this.content_source_url = _content_source_url;
            this.content = _content;
            this.digest = _digest;
        }

        /// <summary>
        /// 实体集合转json
        /// </summary>
        /// <returns></returns>
        public string Tojson()
        {
            StringBuilder dr = new StringBuilder();

            dr.Append("{");

            dr.Append("\"thumb_media_id\":" + "\"" + this.Thumb_media_id + "\",");
            dr.Append("\"author\":" + "\"" + this.Author + "\",");
            dr.Append("\"title\":" + "\"" + this.Title + "\",");
            dr.Append("\"content_source_url\":" + "\"" + this.Content_source_url + "\",");
            dr.Append("\"content\":" + "\"" + this.Content + "\",");
            dr.Append("\"digest\":" + "\"" + this.Digest + "\"");

            dr.Append("},");

            return dr.ToString();
        }
    }
}