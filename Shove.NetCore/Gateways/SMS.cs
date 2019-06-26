using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.ServiceModel;

namespace Shove.Gateways
{
    /// <summary>
    /// 访问 sms.gateway.i3km.com 封装的短信网关
    /// </summary>
    public class SMS
    {
        /// <summary>
        /// 发送短信。如果 App.Config 或者 Web.Config 的 AppSetting 中没有 Key="I3kmSMS_GatewayServiceUrl" value="" 的设置，将使用默认的网关地址。 
        /// </summary>
        /// <param name="regCode"></param>
        /// <param name="regKey"></param>
        /// <param name="content"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static ArrayOfXElement SendSMS(string regCode, string regKey, string content, string to)
        {
            return SendSMS(regCode, regKey, content, to, DateTime.Now);
        }

        /// <summary>
        /// 发送短信。如果 App.Config 或者 Web.Config 的 AppSetting 中没有 Key="I3kmSMS_GatewayServiceUrl" value="" 的设置，将使用默认的网关地址。 
        /// </summary>
        /// <param name="regCode"></param>
        /// <param name="regKey"></param>
        /// <param name="content"></param>
        /// <param name="to"></param>
        /// <param name="sendTime">指定的发送时间，可以实现按指定的发送时间再进行发送的功能</param>
        /// <returns></returns>
        public static ArrayOfXElement SendSMS(string regCode, string regKey, string content, string to, DateTime sendTime)
        {
            var binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = 2147483647;
            var endpoint = new EndpointAddress(AppConfigurtaionServices.GetAppSettingsString("I3kmSMS_GatewayServiceUrl"));

            var service = new sms_gatewaySoapClient(binding, endpoint);
            DateTime timeStamp = DateTime.Now;
            string sign = Shove.Security.Encrypt.MD5(regCode + timeStamp.ToString() + content + to + sendTime.ToString("yyyyMMdd HHmmss") + regKey);
            var v = service.SendSMS_2Async(regCode, timeStamp.ToString(), sign, content, to, sendTime);
            v.Wait();

            return v.Result;
        }

        /// <summary>
        /// 查询短信账户余额。如果 App.Config 或者 Web.Config 的 AppSetting 中没有 Key="I3kmSMS_GatewayServiceUrl" value="" 的设置，将使用默认的网关地址。 
        /// </summary>
        /// <param name="regCode"></param>
        /// <param name="regKey"></param>
        /// <returns></returns>
        public static ArrayOfXElement GetBalance(string regCode, string regKey)
        {
            var binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = 2147483647;
            var endpoint = new EndpointAddress(AppConfigurtaionServices.GetAppSettingsString("I3kmSMS_GatewayServiceUrl"));

            var service = new sms_gatewaySoapClient(binding, endpoint);
            DateTime TimeStamp = DateTime.Now;
            string sign = Shove.Security.Encrypt.MD5(regCode + TimeStamp.ToString() + regKey);
            var v = service.QueryBalanceAsync(regCode, TimeStamp.ToString(), sign);
            v.Wait();

            return v.Result;
        }
    }
}
