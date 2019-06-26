using System;
using System.Collections.Generic;
using System.Web;
using System.Text;
namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 发送图片消息类
    /// </summary>
    public class ImageServiceMessage : ServiceMessage
    {
        private string media_id;
       
        /// <summary>
        /// 发送的图片的媒体ID   
        /// </summary>
        public string Media_id
        {
            get { return media_id; }
        }

        /// <summary>
        /// 构造函数,初始化数据
        /// </summary>
        /// <param name="_media_id">发送的图片的媒体ID   </param>
        /// <param name="_touser">普通用户的openid</param>
        public ImageServiceMessage(string _media_id, string _touser)
            : base(_touser, "image")
        {
            this.media_id = _media_id;
        }

        #region 换json格式
        /// <summary>
        /// 将对象转换成json
        /// </summary>
        /// <returns></returns>
        public override string ToJson()
        {
            StringBuilder Image = new StringBuilder();

            Image.Append("{");
            Image.Append("\"touser\"" + ":" + "\"" + base.Touser + "\",");
            Image.Append("\"msgtype\":\"" + base.Msgtype + "\",");
            Image.Append("\"image\":{");
            Image.Append("\"media_id\"" + ":" + "\"" + this.Media_id + "\"");
            Image.Append("}");
            Image.Append("}");

            return Image.ToString();
        }

        #endregion
    }
}