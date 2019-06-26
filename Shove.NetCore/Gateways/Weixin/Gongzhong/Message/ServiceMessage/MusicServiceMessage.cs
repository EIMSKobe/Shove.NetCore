using System;
using System.Collections.Generic;
using System.Web;
using System.Text;
namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    ///SendMusicMessage 的摘要说明
    /// </summary>
    public class MusicServiceMessage : ServiceMessage
    {
        private string title;
        /// <summary>
        ///音乐标题  
        /// </summary>
        public string Title
        {
            get { return title; }
        }

        private string description;
        /// <summary>
        ///音乐描述
        /// </summary>
        public string Description
        {
            get { return description; }
        }

        private string musicurl;
        /// <summary>
        /// 音乐链接
        /// </summary>
        public string Musicurl
        {
            get { return musicurl; }
        }

        private string hqmusicurl;
        /// <summary>
        ///高品质音乐链接，wifi环境优先使用该链接播放音乐  
        /// </summary>
        public string Hqmusicurl
        {
            get { return hqmusicurl; }
        }

        private string thumb_media_id;
        /// <summary>
        ///视频缩略图的媒体ID  
        /// </summary>
        public string Thumb_media_id
        {
            get { return thumb_media_id; }
        }

        /// <summary>
        /// 构造函数,初始化函数
        /// </summary>
        /// <param name="_title">音乐标题</param>
        /// <param name="_description">音乐描述</param>
        /// <param name="_musicurl">音乐链接</param>
        /// <param name="_hqmusicurl">高品质音乐链接</param>
        /// <param name="_thumb_media_id">视频缩略图的媒体ID</param>
        /// <param name="_touser">普通用户的openid</param>
        public MusicServiceMessage(string _title, string _description, string _musicurl,
            string _hqmusicurl, string _thumb_media_id, string _touser)
            : base(_touser, "music")
        {
            this.title = _title;
            this.description = _description;
            this.musicurl = _musicurl;
            this.hqmusicurl = _hqmusicurl;
            this.thumb_media_id = _thumb_media_id;
        }

        /// <summary>
        /// 将对象转换成json
        /// </summary>
        /// <returns></returns>
        public override string ToJson()
        {
            StringBuilder Music = new StringBuilder();

            Music.Append("{");
            Music.Append("\"touser\"" + ":" + "\"" + base.Touser + "\",");
            Music.Append("\"msgtype\":\"" + base.Msgtype + "\",");
            Music.Append("\"music\":\"");
            Music.Append("{");
            Music.Append("\"title\":\"" + this.Title + "\"");
            Music.Append("\"description\":\"" + this.Description + "\",");
            Music.Append("\"musicurl\":\"" + this.Hqmusicurl + "\",");
            Music.Append("\"thumb_media_id\":\"" + this.Thumb_media_id + "\",");
            Music.Append("}");
            Music.Append("}");

            return Music.ToString();
        }
    }
}