using System;
using System.Collections.Generic;
using System.Web;
using System.Collections;
using System.Text.RegularExpressions;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 错误信息
    /// </summary>
    public class ErrorInformation
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetErrorCode(string json, string key)
        {
            int code = 0;

            if (!int.TryParse(Utility.AnalysisJson(json, key), out code))
            {
                return "errorCode无效，无法解析返回结果。";
            }

            switch (code)
            {
                case -1: return "-1,系统繁忙。";
                case 0: return "0,请求成功。";
                case 40001: return code + ",获取access_token时AppSecret错误、或者access_token无效。";
                case 40002: return code + ",不合法的凭证类型。";
                case 40003: return code + ",不合法的OpenID。";
                case 40004: return code + ",不合法的媒体文件类型。";
                case 40005: return code + ",不合法的文件类型。";
                case 40006: return code + ",不合法的文件大小。";
                case 40007: return code + ",不合法的媒体文件id。";
                case 40008: return code + ",不合法的消息类型。";
                case 40009: return code + ",不合法的图片文件大小。";
                case 40010: return code + ",不合法的语音文件大小。";
                case 40011: return code + ",不合法的视频文件大小。";
                case 40012: return code + ",不合法的缩略图文件大小。";
                case 40013: return code + ",不合法的APPID。";
                case 40014: return code + ",不合法的access_token。";
                case 40015: return code + ",不合法的菜单类型。";
                case 40016: return code + ",不合法的按钮个数。";
                case 40017: return code + ",不合法的按钮个数。";
                case 40018: return code + ",不合法的按钮名字长度。";
                case 40019: return code + ",不合法的按钮KEY长度。";
                case 40020: return code + ",不合法的按钮URL长度。";
                case 40021: return code + ",不合法的菜单版本号。";
                case 40022: return code + ",不合法的子菜单级数。";
                case 40023: return code + ",不合法的子菜单按钮个数。";
                case 40024: return code + ",不合法的子菜单按钮类型。";
                case 40025: return code + ",不合法的子菜单按钮名字长度。";
                case 40026: return code + ",不合法的子菜单按钮KEY长度。";
                case 40027: return code + ",不合法的子菜单按钮URL长度。";
                case 40028: return code + ",不合法的自定义菜单使用用户。";
                case 40029: return code + ",不合法的oauth_code。";
                case 40030: return code + ",不合法的refresh_token。";
                case 40031: return code + ",不合法的openid列表。";
                case 40032: return code + ",不合法的openid列表长度。";
                case 40033: return code + ",不合法的请求字符，不能包含\\uxxxx格式的字符。";
                case 40035: return code + ",不合法的参数。";
                case 40038: return code + ",不合法的请求格式。";
                case 40039: return code + ",不合法的URL长度。";
                case 40050: return code + ",不合法的分组id。";
                case 40051: return code + ",分组名字不合法。";
                case 40130: return code + ",群发至少需要同时发送两个用户。";
                case 41001: return code + ",缺少access_token参数。";
                case 41002: return code + ",缺少appid参数。";
                case 41003: return code + ",缺少refresh_token参数。";
                case 41004: return code + ",缺少secret参数。";
                case 41005: return code + ",缺少多媒体文件数据。";
                case 41006: return code + ",缺少media_id参数。";
                case 41007: return code + ",缺少子菜单数据。";
                case 41008: return code + ",缺少oauth code。";
                case 41009: return code + ",缺少openid。";
                case 42001: return code + ",access_token超时。";
                case 42002: return code + ",refresh_token超时。";
                case 42003: return code + ",oauth_code超时。";
                case 43001: return code + ",需要GET请求。";
                case 43002: return code + ",需要POST请求。";
                case 43003: return code + ",需要HTTPS请求。";
                case 43004: return code + ",需要接收者关注。";
                case 43005: return code + ",需要好友关系。";
                case 44001: return code + ",多媒体文件为空。";
                case 44002: return code + ",POST的数据包为空。";
                case 44003: return code + ",图文消息内容为空。";
                case 44004: return code + ",文本消息内容为空。";
                case 45001: return code + ",多媒体文件大小超过限制。";
                case 45002: return code + ",消息内容超过限制。";
                case 45003: return code + ",标题字段超过限制。";
                case 45004: return code + ",描述字段超过限制。";
                case 45005: return code + ",链接字段超过限制。";
                case 45006: return code + ",图片链接字段超过限制。";
                case 45007: return code + ",语音播放时间超过限制。";
                case 45008: return code + ",图文消息超过限制。";
                case 45009: return code + ",接口调用超过限制。";
                case 45010: return code + ",创建菜单个数超过限制。";
                case 45015: return code + ",回复时间超过限制。";
                case 45016: return code + ",系统分组、不允许修改。";
                case 45017: return code + ",分组名字过长。";
                case 45018: return code + ",分组数量超过上限。";
                case 46001: return code + ",不存在媒体数据。";
                case 46002: return code + ",不存在的菜单版本。";
                case 46003: return code + ",不存在的菜单数据。";
                case 46004: return code + ",不存在的用户。";
                case 47001: return code + ",解析JSON/XML内容错误。";
                case 48001: return code + ",api功能未授权。";
                case 50001: return code + ",用户未授权该api。";
                default: return code + ",未知错误。";
            }
        }
    }
}