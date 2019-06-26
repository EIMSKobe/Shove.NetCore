using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 消息基类
    /// </summary>
    public class Message
    {
        private string _OpenID;
        /// <summary>
        /// 接收方帐号（收到的OpenID）
        /// </summary>
        public string OpenID
        {
            get { return _OpenID; }
        }

        private string _DeveloperId;
        /// <summary>
        /// 开发者微信号
        /// </summary>
        public string DeveloperId
        {
            get { return _DeveloperId; }
        }

        private DateTime _CreateTime;
        /// <summary>
        /// 消息创建时间
        /// </summary>
        public DateTime CreateTime
        {
            get { return _CreateTime; }
        }

        private string _MsgType;
        /// <summary>
        /// 消息类型
        /// </summary>
        public string MsgType
        {
            get { return _MsgType; }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="_OpenID"></param>
        /// <param name="_DeveloperId"></param>
        /// <param name="createTime"></param>
        /// <param name="msgType"></param>
        public Message(string _OpenID, string _DeveloperId, DateTime createTime, string msgType)
        {
            this._OpenID = _OpenID;
            this._MsgType = msgType;
            this._DeveloperId = _DeveloperId;
            this._CreateTime = createTime;
        }

        /// <summary>
        /// 构造函数,初始化回复类型
        /// </summary>
        public Message(string msgType)
        {
            this._MsgType = msgType;
        }

        /// <summary>
        /// 
        /// </summary>
        public Message()
        {

        }
    }
}
