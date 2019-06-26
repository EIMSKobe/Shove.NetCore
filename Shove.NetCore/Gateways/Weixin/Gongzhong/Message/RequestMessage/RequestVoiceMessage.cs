using System;
using System.Collections.Generic;
using System.Web;


namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    ///用户请求的声音信息
    /// </summary>
    public class RequestVoiceMessage:Message
    {
        private string _MediaId;
        /// <summary>
        /// 语音消息媒体id，可以调用多媒体文件下载接口拉取数据。  
        /// </summary>
        public string MediaId
        {
            get { return _MediaId; }
        }

        private string _Format;
        /// <summary>
        /// 语音格式，如amr，speex等  
        /// </summary>
        public string Format
        {
            get { return _Format; }
        }

        private string _MsgID;
        /// <summary>
        /// 消息id
        /// </summary>
        public string MsgID
        {
            get { return _MsgID; }
        }

        private string _Recognition;
        /// <summary>
        /// 语音识别文字结果，UTF-8编码
        /// </summary>
        public string Recognition
        {
            get { return _Recognition; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_MediaId">语音媒体id</param>
        /// <param name="_Format">语音格式</param>
        /// <param name="_OpenID">用户Openid</param>
        /// <param name="_MsgID">消息ID</param>
        ///<param name="_recognition">语音识别结果</param>
        /// <param name="_DeveloperId">发送方微信号</param>
        /// <param name="createTime">创建时间</param>
        /// <param name="msgType">消息类型</param>
        public RequestVoiceMessage(string _MediaId, string _Format, string _MsgID, string _recognition, string _OpenID, string _DeveloperId, DateTime createTime, string msgType)
            : base(_OpenID, _DeveloperId, createTime, msgType)
        {
            this._MediaId = _MediaId;
            this._Format = _Format;
            this._MsgID = MsgID;
            this._Recognition = _recognition;
        }
    }
}