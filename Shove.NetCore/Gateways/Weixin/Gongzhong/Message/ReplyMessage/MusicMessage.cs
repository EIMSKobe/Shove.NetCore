using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 音乐类消息
    /// </summary>
    public class MusicMessage : Message
    {
        private string _Title;
        /// <summary>
        /// 音乐标题
        /// </summary>
        public string Title
        {
            get { return _Title; }
        }

        private string _Description; //>
        /// <summary>
        /// 音乐描述
        /// </summary>
        public string Description
        {
            get { return _Description; }
        }

        private string _MusicUrl;   //>
        /// <summary>
        /// 音乐链接
        /// </summary>
        public string MusicUrl
        {
            get { return _MusicUrl; }
        }

        private string _HQMusicUrl;//><![CDATA[HQ_MUSIC_Url]]></HQMusicUrl>
        /// <summary>
        /// wifi优先链接,
        /// </summary>
        public string HQMusicUrl
        {
            get { return _HQMusicUrl; }
        }

        /// <summary>
        /// 构造函数，初始化属性
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="description">描述</param>
        /// <param name="musicUrl">音乐链接</param>
        /// <param name="hQMusicUrl">音乐wifi链接,可以等于""</param>
        public MusicMessage(string title, string description, string musicUrl, string hQMusicUrl)
            : base("music")
        {
            this._Description = description;
            this._HQMusicUrl = hQMusicUrl;
            this._MusicUrl = musicUrl;
            this._Title = title;
        }
    }
}
