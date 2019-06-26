using System;
using System.Collections.Generic;
using System.Web;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    ///RequestTextMessage 的摘要说明
    /// </summary>
    public class RequestTextMessage : Message
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
        /// 
        /// </summary>
        /// <param name="content">消息内容</param>
        ///<param name="_DeveloperId">开发者微信号</param>
        /// <param name="_OpenID">发送方微信号OpenID</param>
        /// <param name="createTime">创建时间</param>
        /// <param name="msgType">消息类型</param>
        public RequestTextMessage(string content, string _OpenID, string _DeveloperId, DateTime createTime, string msgType)
            : base(_OpenID, _DeveloperId, createTime, msgType)
        {
            _Content = content;
        }
    }
}