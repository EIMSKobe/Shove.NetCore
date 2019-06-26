using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 从微信发来的图片请求消息
    /// </summary>
    public class RequestImageMessage : Message
    {
        private string _PicUrl;
        /// <summary>
        ///  图片链接 
        /// </summary>
        public string PicUrl
        {
            get { return _PicUrl; }
        }

        private string _MsgId;
        /// <summary>
        /// 消息id，64位整型  
        /// </summary>
        public string MsgId
        {
            get { return _MsgId; }
        }

        private string _MediaId;
        /// <summary>
        /// 图片Id
        /// </summary>
        public string MediaId
        {
            get { return _MediaId; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="picUrl">图片链接</param>
        /// <param name="msgId">消息ID，64位整型</param>
        /// <param name="MediaId">文件Id</param> 
        /// <param name="_DeveloperId">开发者微信号</param>
        /// <param name="_OpenID">发送方微信号OpenID</param>
        /// <param name="createTime">创建时间</param>
        /// <param name="msgType">消息类型</param>
        public RequestImageMessage(string picUrl, string msgId, string MediaId, string _OpenID, string _DeveloperId, DateTime createTime, string msgType)
            : base(_OpenID, _DeveloperId, createTime, msgType)
        {
            this._MsgId = msgId;
            this._PicUrl = picUrl;
            this._MediaId = MediaId;
        }
    }
}
