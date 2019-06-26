using System;
using System.Collections.Generic;
using System.Web;
namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 客户服务 转多客服系统
    /// </summary>
    public class CustomerServiceMessage : Message
    {
        /// <summary>
        /// 构造
        /// </summary>
        public CustomerServiceMessage()
            : base("transfer_customer_service")
        {
            //
            //TODO: 在此处添加构造函数逻辑
            //
        }
    }
}