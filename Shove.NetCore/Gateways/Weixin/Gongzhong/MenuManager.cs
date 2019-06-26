using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Net;
using System.Data;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 创建导航条菜单类
    /// </summary>
    public class MenuManager
    {
        /// <summary>
        /// 通过参数AppID,AppSecret,获取微信菜单token,通过token来创建微信菜单导航
        /// </summary>
        /// <param name="Menus">菜单按钮条集合</param>
        /// <param name="errorDescription">错误信息</param>
        public static bool Create(List<MenuView> Menus, ref string errorDescription)
        {
            errorDescription = "";

            if (!ValidMenuRule(Menus, ref errorDescription))
            {
                return false;
            }

            string json = ToJson(Menus);

            if (!PostToWeixinServer("https://api.weixin.qq.com/cgi-bin/menu/create", json, ref errorDescription))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 删除菜单
        /// </summary>
        /// <param name="errorDescription">错误描述</param>
        /// <returns></returns>
        public static bool Delete(ref string errorDescription)
        {
            errorDescription = "";

            if (!PostToWeixinServer("https://api.weixin.qq.com/cgi-bin/menu/delete", "", ref errorDescription))
            {
                return false;
            }

            return true;
        }

        ///// <summary>
        ///// 获取access_token
        ///// </summary>
        ///// <param name="AppID">第三方用户唯一凭证</param>
        ///// <param name="AppSecret">第三方用户唯一凭证密钥</param>
        ///// <returns>返回菜单ACCESS_TOKEN</returns>
        //public static string GetAccessToken(string AppID, string AppSecret)
        //{
        //    WebClient webClient = new WebClient();

        //    try
        //    {
        //        Byte[] bytes = webClient.DownloadData(string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}", AppID, AppSecret));

        //        string result = System.Text.Encoding.UTF8.GetString(bytes);
        //        string[] res = result.Split('\"');
        //        Utility.Access_token  = res[3].ToString();

        //        return access_token;

        //    }
        //    catch (Exception e)
        //    {
        //        log.Write("从微信服务器获取 access_token 发生错误：" + e.Message);

        //        return "";
        //    }
        //}

        /// <summary>
        /// 验证菜单按钮是否符合规则
        /// </summary>
        /// <param name="Menus">菜单按钮集合</param>
        /// <param name="errorDescription"></param>
        /// <returns>合格返回 true,不合格返回false</returns>
        private static bool ValidMenuRule(List<MenuView> Menus, ref string errorDescription)
        {
            errorDescription = "";

            if ((Menus == null) || (Menus.Count <= 0))
            {
                errorDescription = "一级菜单按钮个数不能少于1个。";

                return false;
            }

            if ((Menus == null) || (Menus.Count > 3))
            {
                errorDescription = "一级菜单按钮个数超出,不能超过3个。";

                return false;
            }

            for (int i = 0; i < Menus.Count; i++)
            {
                if (Shove.String.GetLength(Menus[i].Name) > 16)
                {
                    errorDescription = "菜单名称字节长度过长，只限于 16 个字符。";

                    return false;
                }

                if (Menus[i].SubMenus.Count > 5)
                {
                    errorDescription = "子菜单数量超出限制，最多只能为 5 个。";

                    return false;
                }

                if (Menus[i].Key == null && Menus[i].SubMenus.Count > 0)
                {
                    continue;
                }

                if (Shove.String.GetLength(Menus[i].Key) > 128)
                {
                    errorDescription = "key 值节长度超出,只限于 128 个字符。";

                    return false;
                }

                for (int j = 0; j < Menus[i].SubMenus.Count; j++)
                {
                    if (Shove.String.GetLength(Menus[i].SubMenus[j].Name) > 40)
                    {
                        errorDescription = "子菜单名称字节过长，只限于在 40 个字符。";

                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 解析成json
        /// </summary>
        /// <param name="Menus"></param>
        /// <returns></returns>
        private static string ToJson(List<MenuView> Menus)
        {
            StringBuilder dr = new StringBuilder("{\"button\":[");

            for (int i = 0; i < Menus.Count; i++)
            {
                if (Menus[i].SubMenus.Count > 0)
                {
                    dr.Append("{\"name\":\"" + Menus[i].Name + "\",");
                    dr.Append("\"sub_button\":[");

                    for (int j = 0; j < Menus[i].SubMenus.Count; j++)
                    {
                        string json = "";

                        if (Menus[i].SubMenus[j].Type == "click")
                        {
                            json = json + "{\"key\":\"" + Menus[i].SubMenus[j].Key + "\",\"type\":\"" + Menus[i].SubMenus[j].Type + "\",\"name\":\"" + Menus[i].SubMenus[j].Name + "\"},";
                        }
                        else
                        {
                            json = json + "{\"url\":\"" + Menus[i].SubMenus[j].Key + "\",\"type\":\"" + Menus[i].SubMenus[j].Type + "\",\"name\":\"" + Menus[i].SubMenus[j].Name + "\"},";
                        }

                        string jsong = "";

                        if (j + 1 == Menus[i].SubMenus.Count)
                        {
                            jsong = jsong + "]},";
                            json = json.Substring(0, json.Length - 1);
                        }

                        dr.Append(json);

                        if (i + 1 == Menus.Count && j + 1 == Menus[i].SubMenus.Count)
                        {
                            jsong = jsong.Substring(0, jsong.Length - 1);
                        }

                        dr.Append(jsong);
                    }
                }
                else
                {
                    string json = string.Empty;

                    if (Menus[i].Type == "click")
                    {
                        json = json + "{\"key\":\"" + Menus[i].Key + "\",\"type\":\"" + Menus[i].Type + "\",\"name\":\"" + Menus[i].Name + "\"},";
                    }
                    else
                    {
                        json = json + "{\"url\":\"" + Menus[i].Key + "\",\"type\":\"" + Menus[i].Type + "\",\"name\":\"" + Menus[i].Name + "\"},";
                    }

                    if (i + 1 == Menus.Count)
                    {
                        json = json.Substring(0, json.Length - 1);
                    }

                    dr.Append(json);
                }
            }

            dr.Append("]}");

            return dr.ToString();
        }

        /// <summary>
        /// 请求向微信服务器通讯，并取得结果
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="errorDescription"></param>
        /// <returns></returns>
        private static bool PostToWeixinServer(string url, string data, ref string errorDescription)
        {
            // 通过 AppID，AppSecret 得到菜单token
            //access_token = GetAccessToken(AppID, AppSecret);

            if (string.IsNullOrEmpty(Utility.Access_token))
            {
                errorDescription = "发生错误:access_token为null,请查看页面的Load事件是否调用了Utility.GetAccessToken()方法";
                return false;
            }

            System.Net.WebClient client = new System.Net.WebClient();
            client.Headers["Content-Type"] = "application/json";
            client.Encoding = System.Text.Encoding.UTF8;

            try
            {
                string strResult = "";

                if (string.IsNullOrEmpty(data))
                {
                    strResult = client.UploadString(string.Format("{0}?access_token={1}", url, Utility.Access_token), (string.IsNullOrEmpty(data) ? "get" : "post"));
                }
                else
                {
                    strResult = client.UploadString(string.Format("{0}?access_token={1}", url, Utility.Access_token), (string.IsNullOrEmpty(data) ? "get" : "post"), data);
                }

                if (strResult.IndexOf("ok") <= 0)
                {
                    errorDescription = ErrorInformation.GetErrorCode(strResult, "errcode");

                    return false;
                }
            }
            catch (Exception e)
            {
                errorDescription = e.Message;

                return false;
            }

            return true;
        }
    }
}
