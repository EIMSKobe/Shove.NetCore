using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Shove
{
    /// <summary>
    /// json config file (appsettings.json) 操作类
    /// </summary>
    public class AppConfigurtaionServices
    {
        public static IConfiguration Configuration { get; set; }

        static AppConfigurtaionServices()
        {       
            Configuration = new ConfigurationBuilder()
                .Add(new JsonConfigurationSource { Path = "appsettings.json", ReloadOnChange = true })
                .Build();
        }

        /// <summary>
        /// GetConnectionString
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetConnectionString(string key)
        {
            string result;
            try
            {
                result = AppConfigurtaionServices.Configuration["ConnectionStrings:" + key].Trim();
            }
            catch { result = ""; }

            return result;
        }

        /// <summary>
        /// GetAppSettingsString
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetAppSettingsString(string key)
        {
            string result;
            try
            {
                result = AppConfigurtaionServices.Configuration["Appsettings:" + key].Trim();
            }
            catch { result = ""; }

            return result;
        }

        /// <summary>
        /// GetAppSettingsBool
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool GetAppSettingsBool(string key)
        {
            return GetAppSettingsBool(key, false);
        }

        /// <summary>
        /// GetAppSettingsBool
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool GetAppSettingsBool(string key, bool defaultValue)
        {
            return Shove.Convert.StrToBool(GetAppSettingsString(key), defaultValue);
        }

        /// <summary>
        /// GetAppSettingsInt
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static int GetAppSettingsInt(string Key)
        {
            return GetAppSettingsInt(Key, 0);
        }

        /// <summary>
        /// GetAppSettingsInt
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int GetAppSettingsInt(string key, int defaultValue)
        {
            return Shove.Convert.StrToInt(GetAppSettingsString(key), defaultValue);
        }

        /// <summary>
        /// GetAppSettingsDouble
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static double GetAppSettingsDouble(string key)
        {
            return GetAppSettingsDouble(key, 0);
        }

        /// <summary>
        /// GetAppSettingsDouble
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double GetAppSettingsDouble(string key, double defaultValue)
        {
            return Shove.Convert.StrToDouble(GetAppSettingsString(key), defaultValue);
        }

        /// <summary>
        /// 得到配置文件中的配置decimal信息
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static decimal GetAppSettingsDecimal(string key)
        {
            return GetAppSettingsDecimal(key, 0);
        }

        /// <summary>
        /// 得到配置文件中的配置decimal信息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal GetAppSettingsDecimal(string key, decimal defaultValue)
        {
            return Shove.Convert.StrToDecimal(GetAppSettingsString(key), defaultValue);
        }

        /// <summary>
        /// GetConfigString
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetConfigString(string key)
        {
            string result;
            try
            {
                result = AppConfigurtaionServices.Configuration[key].Trim();
            }
            catch { result = ""; }

            return result;
        }

        /// <summary>
        /// GetAppSettingsBool
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool GetConfigBool(string key)
        {
            return GetConfigBool(key, false);
        }

        /// <summary>
        /// GetAppSettingsBool
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool GetConfigBool(string key, bool defaultValue)
        {
            return Shove.Convert.StrToBool(GetConfigString(key), defaultValue);
        }

        /// <summary>
        /// GetAppSettingsInt
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static int GetConfigInt(string Key)
        {
            return GetConfigInt(Key, 0);
        }

        /// <summary>
        /// GetAppSettingsInt
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int GetConfigInt(string key, int defaultValue)
        {
            return Shove.Convert.StrToInt(GetConfigString(key), defaultValue);
        }

        /// <summary>
        /// GetAppSettingsDouble
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static double GetConfigDouble(string key)
        {
            return GetConfigDouble(key, 0);
        }

        /// <summary>
        /// GetAppSettingsDouble
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double GetConfigDouble(string key, double defaultValue)
        {
            return Shove.Convert.StrToDouble(GetConfigString(key), defaultValue);
        }

        /// <summary>
        /// 得到配置文件中的配置decimal信息
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static decimal GetConfigDecimal(string key)
        {
            return GetConfigDecimal(key, 0);
        }

        /// <summary>
        /// 得到配置文件中的配置decimal信息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal GetConfigDecimal(string key, decimal defaultValue)
        {
            return Shove.Convert.StrToDecimal(GetConfigString(key), defaultValue);
        }
    }
}

/*
{
  "ConnectionStrings": {
    "conn": "Server=localhost;port=3306;database=mysql;uid=root;pwd=123456;"
  },
  "AppSettings": {
    "item1": "111111",
    "item2": "22222",
    "item3": "3333333"
  },
  "Logging": {
    "IncludeScopes": false,
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "serviceUrl": "https://api.baidu.com"
}

AppConfigurtaionServices.Configuration.GetConnectionString("conn");
AppConfigurtaionServices.Configuration["serviceUrl"];
AppConfigurtaionServices.Configuration["Appsettings:item1"];
注意，如果AppConfigurtaionServices类中抛出FileNotFoundException异常，说明目录下未找到appsettings.json文件，这时请在项目appsettings.json文件上右键——属性——将“复制到输出目录”项的值改为“始终复制”即可。
*/
