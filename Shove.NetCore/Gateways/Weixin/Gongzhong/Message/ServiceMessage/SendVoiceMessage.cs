using System;
using System.Collections.Generic;
using System.Web;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 发送声音消息,
    /// </summary>
    public class VoiceServiceMessage : ServiceMessage
    {
        private string media_id;
        /// <summary>
        /// 发送的语音的媒体ID  
        /// </summary>
        public string Media_id
        {
            get { return media_id; }
        }

        /// <summary>
        /// 构造函数,初始化数据
        /// </summary>
        /// <param name="_Media_id">发送的语音的媒体ID</param>
        /// <param name="_touser">普通用户的openid</param>
        public VoiceServiceMessage(string _Media_id, string _touser)
            : base(_touser, "voice")
        {
            this.media_id = _Media_id;
        }

        /// <summary>
        /// 转json格式
        /// </summary>
        /// <returns></returns>
        public override string ToJson()
        {
            StringBuilder Voice = new StringBuilder();

            Voice.Append("{");
            Voice.Append("\"touser\"" + ":" + "\"" + base.Touser + "\",");
            Voice.Append("\"msgtype\":\"voice\",");
            Voice.Append("\"voice\":");
            Voice.Append("{");
            Voice.Append("\"media_id\":\"" + this.Media_id + "\"");
            Voice.Append("}");
            Voice.Append("}");

            return Voice.ToString();
        }
    }
}