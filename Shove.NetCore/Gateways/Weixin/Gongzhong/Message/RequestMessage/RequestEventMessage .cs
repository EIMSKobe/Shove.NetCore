using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 从微信发来的请求事件消息
    /// </summary>
    public class RequestEventMessage : Message
    {
        private string _Event;
        /// <summary>
        /// 事件类型，subscribe(订阅)、unsubscribe(取消订阅)、CLICK(自定义菜单点击事件)
        /// </summary>
        public string Event
        {
            get { return _Event; }
        }

        private string _EventKey;
        /// <summary>
        /// 事件KEY值，与自定义菜单接口中KEY值对应 
        /// </summary>
        public string EventKey
        {
            get { return _EventKey; }
        }

        private string _Ticket;
        /// <summary>
        /// 二维码_Ticket
        /// </summary>
        public string Ticket
        {
            get { return _Ticket; }
            set { _Ticket = value; }
        }

        /// <summary>
        /// 构造函数，初始化信息
        /// </summary>
        /// <param name="_event">事件类型</param>
        /// <param name="eventKey">事件KEY值</param>
        /// <param name="_Ticket">二维码_Ticket</param>
        /// <param name="_DeveloperId">开发者账号</param>
        /// <param name="_OpenID">发送方微信号openid</param>
        /// <param name="createTime">创建时间</param>
        /// <param name="msgType">消息类型</param>
        public RequestEventMessage(string _event, string eventKey, string _Ticket, string _OpenID, string _DeveloperId, DateTime createTime, string msgType)
            : base(_OpenID, _DeveloperId, createTime, msgType)
        {
            this._Event = _event;
            this._EventKey = eventKey;
           this._Ticket = _Ticket;
        }
    }
}
