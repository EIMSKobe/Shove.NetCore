using System;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.IO;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 用户信息
    /// </summary>
    public class Information
    {
        /// <summary>
        /// 获取用户的基本信息
        /// </summary>
        /// <param name="OpenId">用户账号的唯一标识ID</param>
        /// <param name="lang">返回国家地区语言版本，zh_CN 简体，zh_TW 繁体，en 英语 为空默认中文简体</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns>返回用户基本信息实体UserInformation</returns>
        public static UserInformation GetUserInformation(string OpenId, string lang, ref string errorDescription)
        {
            Shove.IO.Log log = new IO.Log("WeixinGongzhong");

            errorDescription = "";
            lang = string.IsNullOrEmpty(lang) ? "zh_CN" : lang.Trim();

            if (string.IsNullOrEmpty(OpenId))
            {
                errorDescription = "OpenId不正确,null";
                return null;
            }

            if (string.IsNullOrEmpty(Utility.Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";
                return null;
            }

            string errorCode = "";

            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                try
                {
                    byte[] by = client.DownloadData(string.Format("https://api.weixin.qq.com/cgi-bin/user/info?access_token={0}&openid={1}&lang={2}", Utility.Access_token, OpenId, lang));

                    errorCode = System.Text.Encoding.UTF8.GetString(by);
                }
                catch (Exception ex)
                {
                    errorDescription = ex.Message;
                    return null;
                }
            }

            if (errorCode.IndexOf("errcode", StringComparison.Ordinal) >= 0)
            {
                errorDescription = ErrorInformation.GetErrorCode(errorCode, "errcode");
                log.Write(errorDescription + "\t" + Utility.Access_token + "\t" + Utility.AppID + "\t" + Utility.AppSecret);
                return null;
            }

            return JsonToObject(errorCode, ref errorDescription);

        }

        /// <summary>
        /// json转用户实体对象
        /// </summary>
        /// <param name="json">json结构</param>
        /// <param name="errorDescription"></param>
        /// <returns></returns>
        private static UserInformation JsonToObject(string json, ref string errorDescription)
        {
            try
            {
                string subscribe = Utility.AnalysisJson(json, "subscribe");

                if (!subscribe.Equals("1"))
                {
                    errorDescription = "50002,用户已经取消关注!无法获取用户基本信息";
                    return null;
                }

                string openid = Utility.AnalysisJson(json, "openid");
                string nickname = Utility.AnalysisJson(json, "nickname");
                string sex = Utility.AnalysisJson(json, "sex");
                string language = Utility.AnalysisJson(json, "language");
                string city = Utility.AnalysisJson(json, "city");
                string province = Utility.AnalysisJson(json, "province");
                string country = Utility.AnalysisJson(json, "country");
                string headimgurl = Utility.AnalysisJson(json, "headimgurl");
                string subscribe_time = Utility.AnalysisJson(json, "subscribe_time");//时间戳

                UserInformation user = new UserInformation(subscribe, openid, nickname, sex, city, country, province, language, headimgurl, Utility.FromUnixTime(subscribe_time).ToString());

                return user;
            }
            catch (Exception ex)
            {
                errorDescription = "json转用户实体对象,初始化对象时发生错误：" + ex.Message;
                return null;
            }
        }

        /// <summary>
        /// 获取粉丝列表
        /// </summary>
        /// <param name="next_openid">第一个拉取的openID,如果不填默认从头开始拉取</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns>返回FansListInformation实体对象</returns>
        public static FansListInformation GetFansList(string next_openid, ref string errorDescription)
        {
            errorDescription = "";

            if (string.IsNullOrEmpty(Utility.Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";
                return null;
            }

            bool IsBool = true;

            FansListInformation FansList = new FansListInformation();
            FansList.Next_openid = next_openid;

            //循环读取粉丝列表,每次只拉取1000个, 通过每次返回粉丝列表最后一个Next_openid作为下一次Next_openid参数，
            //直到把粉丝列表取完Next_openid=""
            while (IsBool)
            {
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    byte[] by;

                    try
                    {
                        by = client.DownloadData(
                                         string.Format("https://api.weixin.qq.com/cgi-bin/user/get?access_token={0}&next_openid={1}",
                                       Utility.Access_token, FansList.Next_openid));
                    }
                    catch (Exception ex)
                    {
                        errorDescription = ex.Message;
                        return null;
                    }

                    if (by.Length <= 0)
                    {
                        errorDescription = "拉取用户列表发生错误!";
                        return null;
                    }

                    string values = System.Text.Encoding.UTF8.GetString(by).Replace("count", "usercount");

                    if (values.IndexOf("errcode", StringComparison.Ordinal) >= 0)
                    {
                        errorDescription = ErrorInformation.GetErrorCode(values, "errcode");
                        return null;
                    }

                    FansList.Next_openid = Utility.AnalysisJson(values, "next_openid");//每次获取用户列表最后一个openid
                    FansList.Total = Utility.AnalysisJson(values, "total");//总用户数
                    FansList.Count = Utility.AnalysisJson(values, "usercount");//每次获取的用户数
                    if (string.IsNullOrEmpty(FansList.Next_openid))
                    {
                        IsBool = false;

                        break;
                    }
                    //分解列表数据
                    string[] _openid = values.Replace("[", "]").Split(']')[1].Split(',');

                    for (int i = 0; i < _openid.Length; i++)
                    {

                        if (!string.IsNullOrEmpty(_openid[i]))
                        {
                            //将拉取到的_openid存入 FansList.Data集合中
                            FansList.Data.Add(_openid[i].Trim('\"'));
                        }

                    }
                }
            }

            if (FansList.Data.Count <= 0)
            {
                errorDescription = "该公众账号没有粉丝关注。";
                return null;
            }

            return FansList;
        }

        /// <summary>
        /// 获取分组列表
        /// </summary>
        /// <param name="errorDescription">错误描述</param>
        /// <returns></returns>
        public static List<GroupsInfromation> AllGroupsInfromation(ref string errorDescription)
        {
            errorDescription = "";

            if (string.IsNullOrEmpty(Utility.Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";
                return null;
            }

            string Url = string.Format("https://api.weixin.qq.com/cgi-bin/groups/get?access_token={0}", Utility.Access_token);
            string JsonResult = "";

            if (!GetRepuestResult(Url, ref errorDescription, ref JsonResult))
            {
                return null;
            }

            if (string.IsNullOrEmpty(JsonResult))
            {
                errorDescription = "获取用户分组时创建JSON错误";
                return null;
            }

            //截取json,获取分组信息
            string[] UserLIst = JsonResult.Replace("[", "]").Replace("},", "}.").Split(']')[1].Split('.');

            List<GroupsInfromation> GroupsList = new List<GroupsInfromation>();

            for (int i = 0; i < UserLIst.Length; i++)
            {

                GroupsInfromation Groups = new GroupsInfromation();
                Groups.Count = Utility.AnalysisJson(UserLIst[i], "count");
                Groups.Id = Utility.AnalysisJson(UserLIst[i], "id");
                Groups.Name = Utility.AnalysisJson(UserLIst[i], "name");

                GroupsList.Add(Groups);
            }

            return GroupsList;
        }

        /// <summary>
        /// 修改分组名称
        /// </summary>
        /// <param name="GroupsId">分组ID</param>
        /// <param name="GroupsName">分组名称</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns></returns>
        public static bool UpdateGroupsName(string GroupsId, string GroupsName, ref string errorDescription)
        {
            if (string.IsNullOrEmpty(GroupsId))
            {
                errorDescription = "发生错误!分组ID为空，";
                return false;
            }

            if (string.IsNullOrEmpty(GroupsName))
            {
                errorDescription = "发生错误!分组名称为空，";
                return false;
            }

            if (string.IsNullOrEmpty(Utility.Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";
                return false;
            }

            string json = "{\"group\":{\"id\":" + GroupsId + ",\"name\":\"" + GroupsName + "\"}}";
            string url = string.Format("https://api.weixin.qq.com/cgi-bin/groups/update?access_token={0}", Utility.Access_token);
            string Result = "";

            return UploadString(url, json, ref errorDescription, ref Result);
        }

        /// <summary>
        /// 移动用户分组
        /// </summary>
        /// <param name="openid">用户的Openid</param>
        /// <param name="to_groupid">要移动的分组Id</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns></returns>
        public static bool ShiftUserGroups(string openid, string to_groupid, ref string errorDescription)
        {
            if (string.IsNullOrEmpty(Utility.Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";
                return false;
            }

            if (string.IsNullOrEmpty(openid))
            {
                errorDescription = "发生错误!用户Openid为空，";
                return false;
            }

            if (string.IsNullOrEmpty(to_groupid))
            {
                errorDescription = "发生错误!to_groupid为空，";
                return false;
            }

            string data = "{\"openid\":\"" + openid + "\",\"to_groupid\":" + to_groupid + "}";
            string JsonResult = "";
            string url = string.Format("https://api.weixin.qq.com/cgi-bin/groups/members/update?access_token={0}", Utility.Access_token);

            return UploadString(url, data, ref errorDescription, ref JsonResult);
        }

        /// <summary>
        /// 创建分组
        /// </summary>
        /// <param name="GroupsName">分组名称</param>
        /// <param name="errorDescription">错误描述</param>
        /// <param name="GroupsId"> 创建成功后的分组Id</param>
        /// <returns></returns>
        public static bool CreateGroups(string GroupsName, ref string errorDescription, ref int GroupsId)
        {
            if (string.IsNullOrEmpty(Utility.Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";
                return false;
            }

            if (string.IsNullOrEmpty(GroupsName))
            {
                errorDescription = "发生错误!分组名称为空，";
                return false;
            }

            string url = string.Format("https://api.weixin.qq.com/cgi-bin/groups/create?access_token={0}", Utility.Access_token);
            string data = "{\"group\":{\"name\":\"" + GroupsName + "\"}}";
            string JsonResult = "";

            if (!UploadString(url, data, ref errorDescription, ref JsonResult))
            {
                return false;
            }

            //返回json去掉头部
            JsonResult = JsonResult.Replace("{\"group\":", "");

            //获取创建成功后返回的GroupsId
            GroupsId = int.Parse(Utility.AnalysisJson(JsonResult, "id"));

            return true;
        }

        /// <summary>
        /// 创建临时二维码 临时二维码默认有效时间1800秒
        /// </summary>
        /// <param name="scene_id">场景ID，</param>
        /// <param name="ticket">二维码ticket（可以通过ticket下载二维码图片）</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns></returns>
        public static bool CreateTemporary_Two_Dimension_Code(string scene_id, ref string ticket, ref string errorDescription)
        {
            if (string.IsNullOrEmpty(Utility.Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";
                return false;
            }

            int Number = 0;

            if (!int.TryParse(scene_id, out Number))
            {
                errorDescription = "scene_id参数必须为Int32位整数";
                return false;
            }

            string JsonResult = "";
            string data = "{\"expire_seconds\": 1800, \"action_name\": \"QR_SCENE\", \"action_info\": {\"scene\": {\"scene_id\": " + scene_id + "}}}";
            string url = string.Format("https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token={0}", Utility.Access_token);
            if (!UploadString(url, data, ref errorDescription, ref JsonResult))
            {
                return false;
            }

            ticket = Utility.AnalysisJson(JsonResult, "ticket");
            return true;

        }

        /// <summary>
        /// 创建永久二维码
        /// </summary>
        /// <param name="scene_id">场景ID，</param>
        /// <param name="ticket">二维码ticket（可以通过ticket下载二维码图片）</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns></returns>
        public static bool CreatePerpetual_Two_Dimension_Code(string scene_id, ref string ticket, ref string errorDescription)
        {
            if (string.IsNullOrEmpty(Utility.Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";
                return false;
            }

            int Number = 0;

            if (!int.TryParse(scene_id, out Number))
            {
                errorDescription = "scene_id参数必须为Int32位整数";
                return false;
            }

            if (System.Convert.ToInt32(scene_id) > 1000)
            {
                errorDescription = "scene_id最大值不能大于1000";
                return false;
            }

            Encoding encode = Encoding.GetEncoding("utf-8");

            string JsonResult = "";
            string data = "{\"action_name\": \"QR_LIMIT_SCENE\", \"action_info\": {\"scene\": {\"scene_id\": " + scene_id + "}}}";
            string url = string.Format("https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token={0}", Utility.Access_token);

            if (!UploadString(url, data, ref errorDescription, ref JsonResult))
            {
                return false;
            }

            ticket = Utility.AnalysisJson(JsonResult, "ticket");

            return true;
        }

        /// <summary>
        /// 带Json参数的请求
        /// </summary>
        /// <param name="url">请求服务器路径</param>
        /// <param name="data">json数据</param>
        /// <param name="errorDescription">错误描述</param>
        /// <param name="JsonResult">json结果</param>
        /// <returns></returns>
        private static bool UploadString(string url, string data, ref string errorDescription, ref string JsonResult)
        {
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                client.Headers["Content-Type"] = "application/json";
                client.Encoding = System.Text.Encoding.UTF8;
                string by = "";

                try
                {
                    by = client.UploadString(url, "post", data);
                }
                catch (Exception ex)
                {
                    errorDescription = ex.Message;

                    return false;
                }

                if (by.IndexOf("errcode", StringComparison.Ordinal) >= 0)
                {
                    if (!Utility.AnalysisJson(by, "errcode").Equals("0"))
                    {
                        errorDescription = ErrorInformation.GetErrorCode(by, "errcode");

                        return false;
                    }
                }

                JsonResult = by;//json结果

                return true;
            }


        }

        /// <summary>
        /// 获取请求返回的Json结果
        /// </summary>
        /// <param name="url">请求微信服务器路径</param>
        /// <param name="errorDescription">错误描述</param>
        /// <param name="JsonResult">json结果</param>
        /// <returns></returns>
        private static bool GetRepuestResult(string url, ref string errorDescription, ref string JsonResult)
        {
            errorDescription = "";

            if (string.IsNullOrEmpty(url))
            {
                errorDescription = "发生错误!请求路径为空";
                return false;
            }

            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                string result = "";

                try
                {
                    byte[] by = client.DownloadData(url);
                    result = System.Text.Encoding.UTF8.GetString(by);
                }
                catch (Exception ex)
                {
                    errorDescription = ex.Message;

                    return false;
                }

                if (result.IndexOf("errcode", StringComparison.Ordinal) >= 0)
                {
                    if (!Utility.AnalysisJson(result, "errcode").Equals("0"))
                    {
                        errorDescription = ErrorInformation.GetErrorCode(result, "errcode");

                        return false;
                    }
                }

                JsonResult = result;

                return true;
            }
        }

        /// <summary>
        /// 根据用户Openid获取分组
        /// </summary>
        /// <param name="OpenID"></param>
        /// <returns></returns>
        public static string GetGroupId(string OpenID)
        {
            if (string.IsNullOrEmpty(OpenID))
            {
                return "OpenId为null";
            }
            string errorDescription = "";

            string json = "{\"openid\":\"" + OpenID + "\"}";
            string url = string.Format("https://api.weixin.qq.com/cgi-bin/groups/getid?access_token={0}", Utility.Access_token);

            string JsonResult = "";

            if (!UploadString(url, json, ref errorDescription, ref JsonResult))
            {
                return errorDescription;
            }

            return Utility.AnalysisJson(JsonResult, "groupid");

        }

        /// <summary>
        /// 获取客服聊天记录 (错误返回null)
        /// </summary>
        /// <param name="openId">普通用户的标识，对当前公众号唯一 为空查询所有</param>
        /// <param name="starttime">查询开始时间</param>
        /// <param name="endtime">查询结束时间，每次查询不能跨日查询</param>
        /// <param name="pagesize">每页大小，每页最多拉取1000条</param>
        /// <param name="pageindex">查询第几页，从1开始</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns></returns>
        public static List<CustomerChatRecord> GetCustomerChatRecord(string openId, DateTime starttime, DateTime endtime, int pagesize, int pageindex, ref string errorDescription)
        {

            errorDescription = "";

            if (starttime >= endtime || starttime.Date != endtime.Date)
            {
                errorDescription = "endtime不正确,不能跨日查询";
                return null;
            }

            if (pagesize < 0 || pagesize > 1000)
            {
                errorDescription = "endtime不正确,大于0";
                return null;
            }

            if (pageindex < 0)
            {
                errorDescription = "pageindex不正确,大于0";
                return null;
            }

            string strData = "{\"openid\":\"" + openId + "\","
             + "\"starttime\":" + Utility.UnixTimeStamp(starttime) + ","
             + "\"endtime\":" + Utility.UnixTimeStamp(endtime) + ","
             + "\"pagesize\":" + pagesize + ","
             + "\"pageindex\":" + pageindex + "}";//post json数据

            string JsonResult = "";//返回的json数据

            if (string.IsNullOrEmpty(Utility.Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";
                return null;
            }

            List<CustomerChatRecord> chatRecordList = new List<CustomerChatRecord>();

            string strUrl = string.Format("https://api.weixin.qq.com/cgi-bin/customservice/getrecord?access_token={0}", Utility.Access_token);

            if (UploadString(strUrl, strData, ref errorDescription, ref JsonResult))
            {

                if (JsonResult.Contains("recordlist"))
                {

                    string[] jsonArry = JsonResult.Split('[')[1].Split('}');
                    foreach (string json in jsonArry)
                    {

                        if (json.Contains("worker"))
                        {
                            string strJson = json.TrimStart(',') + "}";
                            CustomerChatRecord chatRecord = new CustomerChatRecord(Utility.AnalysisJson(strJson, "worker").Trim(),
                                                                                Utility.AnalysisJson(strJson, "openid").Trim(),
                                                                                Utility.AnalysisJson(strJson, "opercode").Trim(),
                                                                                Utility.FromUnixTime(Utility.AnalysisJson(strJson, "time").Trim()).ToString(),
                                                                                Utility.AnalysisJson(strJson, "text").Trim());
                            chatRecordList.Add(chatRecord);
                        }
                    }
                }
            }
            else
            {
                return null;
            }

            return chatRecordList;
        }


    }
}