using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 从微信发来的链接消息
    /// </summary>
    public class RequestLinkMessage : Message
    {
        private string _Title;
        /// <summary>
        /// 消息标题  
        /// </summary>
        public string Title
        {
            get { return _Title; }
        }

        private string _Description;
        /// <summary>
        /// 消息描述  
        /// </summary>
        public string Description
        {
            get { return _Description; }
        }

        private string _Url;
        /// <summary>
        /// 消息链接  
        /// </summary>
        public string Url
        {
            get { return _Url; }
        }

        private string _MsgId;
        /// <summary>
        /// 消息id，64位整型  
        /// </summary>
        public string MsgId
        {
            get { return _MsgId; }
        }

        /// <summary>
        /// 初始化实体类信息,父类信息
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="descriptiuon">消息描述</param>
        /// <param name="url">消息链接</param>
        /// <param name="msgId">消息ID</param>
        /// <param name="_DeveloperId">开发者微信号</param>
        /// <param name="_OpenID">发送方微信号OpenID</param>
        /// <param name="createTime">创建时间</param>
        /// <param name="msgType">消息类型</param>
        public RequestLinkMessage(string title, string descriptiuon, string url, string msgId, string _OpenID, string _DeveloperId, DateTime createTime, string msgType)
            : base(_OpenID, _DeveloperId, createTime, msgType)
        {
            this._Description = descriptiuon;
            this._MsgId = msgId;
            this._Title = title;
            this._Url = url;
        }
    }
}
