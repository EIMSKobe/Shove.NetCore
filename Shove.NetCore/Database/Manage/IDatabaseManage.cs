using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace Shove.Database.Manage
{
    /// <summary>
    /// 对数据库的管理接口
    /// </summary>
    public class IDatabaseManage
    {
        /// <summary>
        /// 连接串，要求用 sa, root 等超级用户连接数据库
        /// </summary>
        protected string ConnectionString;

        /// <summary>
        /// 数据库连接
        /// </summary>
        protected DbConnection conn;

        /// <summary>
        /// 打开连接
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        protected bool Open(ref string description)
        {
            description = "";

            try
            {
                conn.Open();

                return true;
            }
            catch (Exception e)
            {
                description = e.Message;

                return false;
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        protected void Close()
        {
            conn.Close();
        }

        /// <summary>
        /// 校验必须要有值的字符串类型的参数
        /// </summary>
        /// <param name="description"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected bool VaildStringParameters(ref string description, params string[] parameters)
        {
            description = "";

            if ((parameters == null) || (parameters.Length == 0))
            {
                return true;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (string.IsNullOrEmpty(parameters[i]))
                {
                    description = "第 " + (i + 1).ToString() + " 个参数不能为空";

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 创建一个新的数据库
        /// </summary>
        /// <param name="database"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual bool CreateDatabase(string database, ref string description) { return false; }

        /// <summary>
        /// 创建一个数据库用户
        /// </summary>
        /// <param name="user"></param>
        /// <param name="Password"></param>
        /// <param name="grantOwnerDatabase"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual bool CreateUser(string user, string Password, string grantOwnerDatabase, ref string description) { return false; }

        /*
        /// <summary>
        /// 给数据库授权
        /// </summary>
        /// <param name="database"></param>
        /// <param name="user"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual bool Grant(string database, string user, ref string description) { return false; }
        */

        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="user"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual bool EditUserPassword(string user, string oldPassword, string newPassword, ref string description) { return false; }

        /// <summary>
        /// 查询数据库使用的空间大小
        /// </summary>
        /// <param name="database"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual float QueryUsedSpaceSize(string database, ref string description) { return 0; }

        /// <summary>
        /// 物理移除数据库
        /// </summary>
        /// <param name="database"></param>
        /// <param name="user"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual bool RemoveDatabase(string database, string user, ref string description) { return false; }

        /// <summary>
        /// 检测数据库是否存在
        /// </summary>
        /// <param name="database"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual bool DatabaseExists(string database, ref string description) { return false; }

        /// <summary>
        /// 检测数据库用户是否存在
        /// </summary>
        /// <param name="user"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual bool userExists(string user, ref string description) { return false; }
    }
}
