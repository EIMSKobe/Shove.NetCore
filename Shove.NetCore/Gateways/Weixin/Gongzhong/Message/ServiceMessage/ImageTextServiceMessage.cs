using System;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    ///发送图文客服消息
    /// </summary>
    public class ImageTextServiceMessage : ServiceMessage
    {
        private string title;
        /// <summary>
        ///标题 
        /// </summary>
        public string Title
        {
            get { return title; }
        }

        private string description;
        /// <summary>
        /// 描述  
        /// </summary>
        public string Description
        {
            get { return description; }
        }

        private string url;
        /// <summary>
        /// 点击后跳转的链接  
        /// </summary>
        public string Url
        {
            get { return url; }
        }

        private string picurl;
        /// <summary>
        ///图文消息的图片链接，支持JPG、PNG格式，较好的效果为大图640*320，小图80*80  
        /// </summary>
        public string Picurl
        {
            get { return picurl; }
        }

        /// <summary>
        /// 保存图文集合
        /// </summary>
        public static List<ImageTextServiceMessage> ImageTextList = new List<ImageTextServiceMessage>();

        /// <summary>
        /// 构造函数,初始化数据
        /// </summary>
        /// <param name="_title">标题</param>
        /// <param name="_description">描述</param>
        /// <param name="_url">点击后跳转的链接</param>
        /// <param name="_picurl">图文消息的图片链接</param>
        /// <param name="_touser">普通用户的openid</param>
        public ImageTextServiceMessage(string _title, string _description, string _url, string _picurl, string _touser)
            : base(_touser, "news")
        {
            this.title = _title;
            this.description = _description;
            this.url = _url;
            this.picurl = _picurl;

            ImageTextList.Add(this);
        }

        /// <summary>
        /// 将对象转换成json
        /// </summary>
        /// <returns></returns>
        public override string ToJson()
        {
            StringBuilder Text = new StringBuilder();

            Text.Append("{");
            Text.Append("\"touser\"" + ":" + "\"" + base.Touser + "\",");
            Text.Append("\"msgtype\":\"news\",");
            Text.Append("\"news\":{");
            Text.Append("\"articles\":");
            Text.Append("[");

            for (int i = 0; i < ImageTextList.Count; i++)
            {
                Text.Append("{");
                Text.Append("\"title\":\"" + ImageTextList[i].Title + "\",");
                Text.Append("\"description\":\"" + ImageTextList[i].Description + "\",");
                Text.Append("\"url\":\"" + ImageTextList[i].Url + "\",");
                Text.Append("\"picurl\":\"" + ImageTextList[i].Picurl + "\"");
                Text.Append("},");
            }

            Text.Remove(Text.Length - 1, 1);
            Text.Append("]");
            Text.Append("}");
            Text.Append("}");

            //每次调用之后清除集合所有元素
            ImageTextList.Clear();

            return Text.ToString();
        }
    }
}