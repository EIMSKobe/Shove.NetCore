using System;
using System.Collections.Generic;
using System.Web;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    ///RequestSendFailureMessage 的摘要说明
    /// </summary>
    public class RequestSendFinishMessage : Message
    {
        private string msgID;

        /// <summary>
        /// 群发的消息ID 
        /// </summary>
        public string MsgID
        {
            get { return msgID; }
        }

        private string status;

        /// <summary>
        /// 群发的结构，为“send success”或“send fail”或“err(num)”
        /// 但send success时，也有可能因用户拒收公众号的消息、系统错误等原因造成少量用户接收失败
        /// </summary>
        public string Status
        {
            get { return status; }
        }

        private string totalCount;

        /// <summary>
        /// group_id下粉丝数；或者openid_list中的粉丝数 
        /// </summary>
        public string TotalCount
        {
            get { return totalCount; }
        }

        private string filterCount;

        /// <summary>
        /// 过滤（过滤是指，有些用户在微信设置不接收该公众号的消息）后，准备发送的粉丝数，原则上，FilterCount = SentCount + ErrorCount 
        /// </summary>
        public string FilterCount
        {
            get { return filterCount; }
        }

        private long sentCount;

        /// <summary>
        /// 发送成功的粉丝数
        /// </summary>
        public long SentCount
        {
            get { return sentCount; }
        }

        private long errorCount;

        /// <summary>
        /// 发送失败的粉丝数 
        /// </summary>
        public long ErrorCount
        {
            get { return errorCount; }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="_msgID">群发的消息ID</param>
        /// <param name="_status"> 群发的结构，为“send success”或“send fail”或“err(num)”但send success时，也有可能因用户拒收公众号的消息、系统错误等原因造成少量用户接收失败</param>
        /// <param name="_totalCount">group_id下粉丝数；或者openid_list中的粉丝数 </param>
        /// <param name="_filterCount">过滤（过滤是指，有些用户在微信设置不接收该公众号的消息）后，准备发送的粉丝数，原则上，FilterCount = SentCount + ErrorCount </param>
        /// <param name="_sentCount">发送成功的粉丝数</param>
        /// <param name="_errorCount">发送失败的粉丝数</param>
        public RequestSendFinishMessage(string _msgID, string _status, string _totalCount, string _filterCount, long _sentCount, long _errorCount)
        {
            msgID = _msgID;
            status = _status;
            totalCount = _totalCount;
            filterCount = _filterCount;
            sentCount = _sentCount;
            errorCount = _errorCount;
        }
    }
}