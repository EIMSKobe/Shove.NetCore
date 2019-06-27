using System;

namespace Shove.DatabaseFactory
{
    /// <summary>
    /// 数据仓库管理器
    /// </summary>
    public class FactoryManager
    {
        /// <summary>
        /// 计数器，用于记录当前使用本数据工厂打开了多少个活动连接。这个没有考虑线程安全，统计是有误差的，属于正常情况。（不能考虑同步，否则会引起性能阻塞）
        /// </summary>
        public static int currentConnections = 0;

        /// <summary>
        /// 数据仓库实例
        /// </summary>
        public Factory factory;

        /// <summary>
        /// 数据库提供者，可选：MySQL, MSSQL, SQLite
        /// </summary>
        private string DataProvider;

        /// <summary>
        /// 构造
        /// </summary>
        public FactoryManager() : this(string.Empty)
        {
        }

        /// <summary>
        /// 构造，带连接串，而不从配置文件读取
        /// </summary>
        public FactoryManager(string connectionString)
        {
            DataProvider = System.Configuration.ConfigurationManager.AppSettings["DataProvider"];

            if (string.IsNullOrEmpty(DataProvider))
            {
                throw new Exception("Must be configured in the Web.Config or App.Config file DataProvider, And DataProvider can in “MySQL”, “MSSQL”, “SQLite” only.");
            }

            DataProvider = DataProvider.Trim(new char[] { ' ', '　', '\t', '\r', '\n', '\v', '\f' }).ToLower();

            if (DataProvider == "mysql")
            {
                DataProvider = "MySQL";
            }
            else if (DataProvider == "mssql")
            {
                DataProvider = "MSSQL";
            }
            else if (DataProvider == "sqlite")
            {
                DataProvider = "SQLite";
            }
            else
            {
                throw new Exception("DataProvider can in “MySQL”, “MSSQL”, “SQLite” only.");
            }

            if (!string.IsNullOrEmpty(connectionString))
            {
                factory = (Factory)Activator.CreateInstance(Type.GetType("Shove.DatabaseFactory." + DataProvider), new object[] { connectionString });
            }
            else
            {
                factory = (Factory)Activator.CreateInstance(Type.GetType("Shove.DatabaseFactory." + DataProvider));
            }
        }
    }
}
