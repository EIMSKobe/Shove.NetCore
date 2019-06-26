using System.Data;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Shove.Web
{
    /// <summary>
    /// Session Read and Write Operate. Note: Add a ¡°SystemPreFix¡± key in Web.Config file.
    /// </summary>
    public static class Session
    {
        /// <summary>
        /// SetSession
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetSession(HttpContext context, string key, byte[] value)
        {
            key = AppConfigurtaionServices.GetAppSettingsString("SystemPreFix") + key;

            context.Session.Remove(key);
            context.Session.Set(key, value);
        }

        /// <summary>
        /// GetSession
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] GetSession(HttpContext context, string key)
        {
            key = AppConfigurtaionServices.GetAppSettingsString("SystemPreFix") + key;

            if (context.Session.TryGetValue(key, out byte[] r))
            {
                return r;
            }

            return null;
        }

        /// <summary>
        /// GetSessionAsString
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetSessionAsString(HttpContext context, string key, string defaultValue)
        {
            byte[] value = GetSession(context, key);

            if (value == null)
            {
                return defaultValue;
            }

            try
            {
                return Encoding.Default.GetString(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// GetSessionAsInt
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int GetSessionAsInt(HttpContext context, string key, int defaultValue)
        {
            string value = GetSessionAsString(context, key, "");

            try
            {
                return System.Convert.ToInt32(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// GetSessionAsLong
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static long GetSessionAsLong(HttpContext context, string key, long defaultValue)
        {
            string value = GetSessionAsString(context, key, "");

            try
            {
                return System.Convert.ToInt64(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// GetSessionAsBoolean
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool GetSessionAsBoolean(HttpContext context, string key, bool defaultValue)
        {
            string value = GetSessionAsString(context, key, "");

            try
            {
                return System.Convert.ToBoolean(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// ClearSession
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        public static void RemoveSession(HttpContext context, string key)
        {
            key = AppConfigurtaionServices.GetAppSettingsString("SystemPreFix") + key;

            context.Session.Remove(key);
        }

        /// <summary>
        /// Clear session
        /// </summary>
        /// <param name="context"></param>
        public static void ClearSession(HttpContext context)
        {
            foreach (string key in context.Session.Keys)
            {
                context.Session.Remove(key);
            }
        }
    }
}
