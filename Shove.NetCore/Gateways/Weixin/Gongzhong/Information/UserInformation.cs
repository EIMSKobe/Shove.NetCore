using System;
using System.Collections.Generic;
using System.Web;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    ///用户基本信息
    /// </summary>
    public class UserInformation
    {
        private string subscribe;
        /// <summary>
        ///用户是否订阅该公众号标识，值为0时，代表此用户没有关注该公众号，拉取不到其余信息。  
        /// </summary>
        public string Subscribe
        {
            get { return subscribe; }
        }

        private string openid;
        /// <summary>
        /// 用户的标识，对当前公众号唯一  
        /// </summary>
        public string Openid
        {
            get { return openid; }
        }

        private string nickname;
        /// <summary>
        ///用户的昵称  
        /// </summary>
        public string Nickname
        {
            get { return nickname; }
        }

        private string sex;
        /// <summary>
        ///  用户的性别，值为1时是男性，值为2时是女性，值为0时是未知  
        /// </summary>
        public string Sex
        {
            get { return sex; }
        }

        private string city;
        /// <summary>
        /// 用户所在城市 
        /// </summary>
        public string City
        {
            get { return city; }
        }

        private string country;
        /// <summary>
        /// 用户所在国家  
        /// </summary>
        public string Country
        {
            get { return country; }
        }

        private string province;
        /// <summary>
        /// 用户所在省份  
        /// </summary>
        public string Province
        {
            get { return province; }
        }

        private string language;
        /// <summary>
        ///用户的语言，简体中文为zh_CN  
        /// </summary>
        public string Language
        {
            get { return language; }
        }

        private string headimgurl;
        /// <summary>
        /// 用户头像，最后一个数值代表正方形头像大小（有0、46、64、96、132数值可选，0代表640*640正方形头像）,用户没有头像时该项为空
        /// </summary>
        public string Headimgurl
        {
            get { return headimgurl; }
        }

        private string subscribe_time;
        /// <summary>
        /// 用户关注时间，为时间戳。如果用户曾多次关注，则取最后关注时间  
        /// </summary>
        public string Subscribe_time
        {
            get { return subscribe_time; }
        }

        /// <summary>
        /// 用户所在分组Id
        /// </summary>
        public string GroupId
        {
            get
            {
                if (!string.IsNullOrEmpty(openid))
                {
                    return Information.GetGroupId(openid); ;
                }

                return "";
            }
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        /// <param name="subscribe">用户是否订阅该公众号标识</param>
        /// <param name="openid"> 用户的标识，对当前公众号唯一 </param>
        /// <param name="nickname">用户的昵称 </param>
        /// <param name="sex">用户的性别，值为1时是男性，值为2时是女性，值为0时是未知 </param>
        /// <param name="city">用户所在城市 </param>
        /// <param name="country">用户所在国家  </param>
        /// <param name="province">用户所在省份  </param>
        /// <param name="language">用户的语言，简体中文为zh_CN  </param>
        /// <param name="headimgurl">用户头像，</param>
        /// <param name="subscribe_time">用户关注时间，为时间戳。最后关注时间 </param>
        public UserInformation(string subscribe, string openid, string nickname, string sex,
            string city, string country, string province, string language, string headimgurl, string subscribe_time)
        {
            this.subscribe = subscribe;
            this.province = province;
            this.nickname = nickname;
            this.openid = openid;
            this.sex = sex;
            this.city = city;
            this.country = country;
            this.language = language;
            this.headimgurl = headimgurl;
            this.subscribe_time = subscribe_time;
        }
    }
}