using System;
using System.Collections.Generic;
using System.Web;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 客服聊天记录
    /// </summary>
    public class CustomerChatRecord
    {
        private string worker;
        /// <summary>
        /// 客服账号
        /// </summary>
        public string Worker
        {
            get { return worker; }
        }

        private string openid;
        /// <summary>
        /// 用户的标识，对当前公众号唯一
        /// </summary>
        public string Openid
        {
            get { return openid; }
        }

        private string opercode;
        /// <summary>
        /// 会话状态
        /// </summary>
        public string Opercode
        {
            get { return opercode; }
        }

        private string time;
        /// <summary>
        /// 操作时间
        /// </summary>
        public string Time
        {
            get { return time; }
        }

        private string text;
        /// <summary>
        /// 聊天记录
        /// </summary>
        public string Text
        {
            get { return text; }
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        /// <param name="worker">客服账号</param>
        /// <param name="openid">用户的标识，对当前公众号唯一</param>
        /// <param name="opercode">会话状态</param>
        /// <param name="time">操作时间</param>
        /// <param name="text">聊天记录</param>
        public CustomerChatRecord(string worker, string openid, string opercode, string time, string text)
        {
            this.worker = worker;
            this.openid = openid;
            this.opercode = OperateId(opercode);
            this.time = time;
            this.text = text;
        }


        /// <summary>
        /// 操作ID(会化状态）定义
        /// </summary>
        /// <param name="opercode">操作id</param>
        /// <returns></returns>
        public static string OperateId(string opercode)
        {
            switch (Shove.Convert.StrToInt(opercode, -1))
            {
                case 1000: return "创建未接入会话";
                case 1001: return "接入会话";
                case 1002: return "主动发起会话";
                case 1004: return "关闭会话";
                case 1005: return "抢接会话";
                case 2001: return "公众号收到消息";
                case 2002: return "客服发送消息";
                case 2003: return "客服收到消息";
                default: return opercode + ",未知";
            }
        }

    }
}