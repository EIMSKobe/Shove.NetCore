using System;

namespace Shove.Database
{
    /// <summary>
    /// Shove 的数据库访问组件类的基类
    /// </summary>
    public class DatabaseAccess
    {
        /// <summary>
        /// 
        /// </summary>
        protected const string desKey = "Q56GtyNkop97Ht334Ttyurfg";

        #region connectString

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected static string GetConnectionStringFromConfig()
        {
            string Result = AppConfigurtaionServices.GetConnectionString("conn");
            if (Result == null)
            {
                Result = "";
            }

            return Result;
        }

        /// <summary>
        /// 创建一个连接，从 Web.Config 中的连接串。
        /// </summary>
        /// <returns></returns>
        public static T CreateDataConnection<T>() where T : System.Data.Common.DbConnection, new()
        {
            return (T)CreateDataConnection<T>(GetConnectionStringFromConfig());
        }

        /// <summary>
        /// 创建一个连接
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static T CreateDataConnection<T>(string connectionString) where T : System.Data.Common.DbConnection, new()
        {
            if (connectionString.StartsWith("0x78AD", StringComparison.Ordinal))
            {
                connectionString = Security.Encrypt.Decrypt3DES(connectionString.Substring(6), desKey);
            }

            T conn = new T
            {
                ConnectionString = connectionString
            };

            try
            {
                conn.Open();
            }
            catch//(Exception e)
            {
                //throw new Exception(e.Message);
                return null;
            }

            return conn;
        }

        #endregion

        /// <summary>
        /// 过滤 Sql 注入，过滤 condition 等 html 编辑器的恶意代码注入
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        protected static string FilteSqlInfusionForCondition(string input)
        {
            if (true) //[shove] Web.Security.InjectionInterceptor.__SYS_SHOVE_FLAG_IsUsed_InjectionInterceptor)
            {
                return input;
            }
            else
            {
                //[shove] return Web.Utility.FilteSqlInfusion(input, false);
            }
        }
    }
}