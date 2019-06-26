using System;
using System.Collections.Generic;
using System.Web;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 发送客服消息基类
    /// </summary>
    public class ServiceMessage
    {
        private string touser; 
        /// <summary>
        /// 是普通用户openid  
        /// </summary>
        public string Touser
        {
            get { return touser; }
            set { touser = value; }
        }

        private string msgtype;
        /// <summary>
        /// 消息类型，text  
        /// </summary>
        public string Msgtype
        {
            get { return msgtype; }
            set { msgtype = value; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_touser">普通用户的openid</param>
        /// <param name="_msgtype">消息类型</param>
        public ServiceMessage(string _touser, string _msgtype)
        {
            this.touser = _touser;
            this.msgtype = _msgtype;
        }

        /// <summary>
        /// 将对象转换json格式
        /// </summary>
        /// <returns></returns>
        public virtual string ToJson()
        {
            return "";
        }
    }
}