using System;
using System.Collections.Generic;
using System.Web;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 发送文本客服消息
    /// </summary>
    public class TextServiceMessage : ServiceMessage
    {
        private string content;
        /// <summary>
        ///  文本消息内容 
        /// </summary>
        public string Content
        {
            get { return content; }
            set { content = value; }
        }

        /// <summary>
        /// 构造函数,初始化数据
        /// </summary>
        /// <param name="_content">文本消息内容</param>
        /// <param name="_touser">普通用户的openid</param>
        public TextServiceMessage(string _content, string _touser)
            : base(_touser, "text")
        {
            this.content = _content;
        }

        /// <summary>
        /// 将对象转换成json
        /// </summary>
        /// <returns></returns>
        public override string  ToJson()
        {
            StringBuilder Text = new StringBuilder();

            Text.Append("{");
            Text.Append("\"touser\"" + ":" + "\"" + base.Touser + "\",");
            Text.Append("\"msgtype\":\"" + base.Msgtype + "\",");
            Text.Append("\"text\":");
            Text.Append("{");
            Text.Append("\"content\":\"" + this.content + "\"");
            Text.Append("}");
            Text.Append("}");

            return Text.ToString();
        }
    }
}