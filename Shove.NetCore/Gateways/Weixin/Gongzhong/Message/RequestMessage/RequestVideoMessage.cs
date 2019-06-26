using System;
using System.Collections.Generic;
using System.Web;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    ///微信用户请求的视频信息
    /// </summary>
    public class RequestVideoMessage:Message
    {
        private string _MediaId;
        /// <summary>
        /// 视频消息媒体id,可以调用多媒体文件下载接口拉取数据。  
        /// </summary>
        public string MediaId
        {
            get { return _MediaId; }
        }

        private string _ThumbMediaId;
        /// <summary>
        /// 视频消息缩略图的媒体id,可以调用多媒体文件下载接口拉取数据。  
        /// </summary>
        public string ThumbMediaId
        {
            get { return _ThumbMediaId; }
        }

        private string _MsgId;
        /// <summary>
        ///消息id,64位整型  
        /// </summary>
        public string MsgId
        {
            get { return _MsgId; }
        }

         /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_MediaId">视频媒体id</param>
        /// <param name="_ThumbMediaId">视频缩略图id</param>
        /// <param name="_MsgID">消息ID</param>
        ///<param name="_DeveloperId">开发者微信号</param>
        /// <param name="_OpenID">发送方微信号OPenid</param>
        /// <param name="createTime">创建时间</param>
        /// <param name="msgType">消息类型</param>
        public RequestVideoMessage(string _MediaId, string _ThumbMediaId, string _MsgID, string _OpenID, string _DeveloperId, DateTime createTime, string msgType)
            : base(_OpenID, _DeveloperId, createTime, msgType)
        {
            this._MediaId = _MediaId;
            this._ThumbMediaId = _ThumbMediaId;
            this._MsgId = _MsgID;
        }
      
         
    }
}