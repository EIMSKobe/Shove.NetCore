using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 图片，图文类消息
    /// </summary>
    public class ImageTextMessage : Message
    {
        private string _ArticleCount;
        /// <summary>
        /// 图文消息个数，限制为10条以内
        /// </summary>
        public string ArticleCount
        {
            get { return _ArticleCount; }
        }

        private string _Title;
        /// <summary>
        /// 图文标题
        /// </summary>
        public string Title
        {
            get { return _Title; }
        }

        private string _Description;
        /// <summary>
        /// 图文描述
        /// </summary>
        public string Description
        {
            get { return _Description; }
        }

        private string _PicUrl;
        /// <summary>
        /// 图片链接，支持JPG、PNG格式，较好的效果为大图640*320，小图80*80。
        /// </summary>
        public string PicUrl
        {
            get { return _PicUrl; }
        }

        private string _Url;
        /// <summary>
        /// 点击图片跳转页面
        /// </summary>
        public string Url
        {
            get { return _Url; }
        }

        /// <summary>
        /// 构造函数，初始化属性
        /// </summary>
        /// <param name="articleCount">图文消息个数，限制为10条以内</param>
        /// <param name="title">图文消息标题</param>
        /// <param name="description">图文描述</param>
        /// <param name="picUrl">图片链接</param>
        /// <param name="url">点击图片跳转页面</param>
        public ImageTextMessage(string articleCount, string title, string description, string picUrl, string url)
            : base("news")
        {
            this._ArticleCount = articleCount;
            this._Description = description;
            this._PicUrl = picUrl;
            this._Title = title;
            this._Url = url;
        }
    }
}
