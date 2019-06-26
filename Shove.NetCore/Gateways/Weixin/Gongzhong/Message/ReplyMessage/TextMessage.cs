using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 文本类消息
    /// </summary>
    public class TextMessage : Message
    {
        private string _Content;

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content
        {
            get { return _Content; }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="content">消息内容</param>
        public TextMessage(string content)
            : base("text")
        {
            _Content = content;
        }
    }
}
