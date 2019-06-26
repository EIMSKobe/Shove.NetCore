using System;
using System.Collections.Generic;
using System.Web;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 声音类信息
    /// </summary>
    public class VoiceMessage:Message
    {
        private string _MediaId;
        /// <summary>
        /// 声音消息媒体id，可以调用多媒体文件下载接口拉取数据。  
        /// </summary>
        public string MediaId
        {
            get { return _MediaId; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_MediaId">声音消息媒体id</param>
        public VoiceMessage(string _MediaId)
            : base("voice")
        {
            this._MediaId = _MediaId;
        }
    }
}