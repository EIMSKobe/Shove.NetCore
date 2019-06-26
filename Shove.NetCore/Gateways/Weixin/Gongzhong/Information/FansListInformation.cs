using System;
using System.Collections.Generic;
using System.Web;


namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 粉丝列表
    /// </summary>
    public class FansListInformation
    {
        private string total;
        /// <summary>
        /// 关注该公众账号的总用户数  
        /// </summary>
        public string Total
        {
            get { return total; }
            set { total = value; }
        }

        private string count;
        /// <summary>
        /// 拉取的OPENID个数，最大值为10000  
        /// </summary>
        public string Count
        {
            get { return count; }
            set { count = value; }
        }

        private List<string> data = new List<string>();
        /// <summary>
        /// 
        /// </summary>
        public List<string> Data
        {
            get { return data; }
            set { data = value; }
        }

        private string next_openid;
        /// <summary>
        /// 拉取列表的后一个用户的OPENID 
        /// </summary>
        public string Next_openid
        {
            get { return next_openid; }
            set { next_openid = value; }
        }
    }
}