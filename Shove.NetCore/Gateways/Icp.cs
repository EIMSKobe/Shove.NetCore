using System.ServiceModel;

namespace Shove.Gateways
{
    /// <summary>
    /// 
    /// </summary>
    public class Icp
    {
        /// <summary>
        /// 工信部实时域名备案状态查询(通过英迈思的备案系统接口)
        /// </summary>
        /// <param name="domainName"></param>
        /// <returns></returns>
        public static string IcpBeianQueryRealTime(string domainName)
        {
            var binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = 2147483647;
            var endpoint = new EndpointAddress(AppConfigurtaionServices.GetAppSettingsString("Icp_GatewayServiceUrl"));

            var service = new GatewaySoapClient(binding, endpoint);
            var v = service.IcpBeianQueryRealTimeAsync("eimslab", domainName);
            v.Wait();

            return v.Result.Body.IcpBeianQueryRealTimeResult;
        }
    }
}
