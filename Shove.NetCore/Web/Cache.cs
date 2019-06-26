using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Shove.Web
{
    /// <summary>
    /// Cache Read Write Operate. Note: Add ¡°SystemPreFix¡±¡°CacheSeconds¡± keys in Web.Config file.
    /// </summary>
    public static class Cache
    {
        private static IMemoryCache _cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetCache(string key, object value)
        {
            int CacheSeconds = AppConfigurtaionServices.GetAppSettingsInt("CacheSeconds", 0);

            SetCache(key, value, CacheSeconds);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cacheSeconds"></param>
        public static void SetCache(string key, object value, int cacheSeconds)
        {
            key = AppConfigurtaionServices.GetAppSettingsString("SystemPreFix") + key;

            RemoveCache(key);

            if (cacheSeconds <= 0)
            {
                return;
            }

            var entry = _cache.CreateEntry(key);
            entry.Value = value;
            entry.SetAbsoluteExpiration(DateTime.Now.AddSeconds(cacheSeconds));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object GetCache(string key)
        {
            key = AppConfigurtaionServices.GetAppSettingsString("SystemPreFix") + key;

            var exists = _cache.TryGetValue(key, out object value);
            if (!exists)
            {
                return null;
            }

            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int GetCacheAsInt(string key, int defaultValue)
        {
            object value = GetCache(key);

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
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static long GetCacheAsLong(string key, long defaultValue)
        {
            object value = GetCache(key);

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
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetCacheAsString(string key, string defaultValue)
        {
            object value = GetCache(key);

            try
            {
                return value.ToString();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool GetCacheAsBoolean(string key, bool defaultValue)
        {
            object value = GetCache(key);

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
        /// 
        /// </summary>
        /// <param name="key"></param>
        public static void RemoveCache(string key)
        {
            key = AppConfigurtaionServices.GetAppSettingsString("SystemPreFix") + key;

            _cache.Remove(key);
        }
    }
}
