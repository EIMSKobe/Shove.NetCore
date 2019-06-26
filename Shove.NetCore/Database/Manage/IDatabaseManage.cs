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
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        protected bool Open(ref string ReturnDescription)
        {
            ReturnDescription = "";

            try
            {
                conn.Open();

                return true;
            }
            catch (Exception e)
            {
                ReturnDescription = e.Message;

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
        /// <param name="ReturnDescription"></param>
        /// <param name="Parameters"></param>
        /// <returns></returns>
        protected bool VaildStringParameters(ref string ReturnDescription, params string[] Parameters)
        {
            ReturnDescription = "";

            if ((Parameters == null) || (Parameters.Length == 0))
            {
                return true;
            }

            for (int i = 0; i < Parameters.Length; i++)
            {
                if (string.IsNullOrEmpty(Parameters[i]))
                {
                    ReturnDescription = "第 " + (i + 1).ToString() + " 个参数不能为空";

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 创建一个新的数据库
        /// </summary>
        /// <param name="DatabaseName"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public virtual bool CreateDatabase(string DatabaseName, ref string ReturnDescription) { return false; }

        /// <summary>
        /// 创建一个数据库用户
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="Password"></param>
        /// <param name="GrantOwnerDatabaseName"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public virtual bool CreateUser(string UserName, string Password, string GrantOwnerDatabaseName, ref string ReturnDescription) { return false; }

        /*
        /// <summary>
        /// 给数据库授权
        /// </summary>
        /// <param name="DatabaseName"></param>
        /// <param name="UserName"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public virtual bool Grant(string DatabaseName, string UserName, ref string ReturnDescription) { return false; }
        */

        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="OldPassword"></param>
        /// <param name="NewPassword"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public virtual bool EditUserPassword(string UserName, string OldPassword, string NewPassword, ref string ReturnDescription) { return false; }

        /// <summary>
        /// 查询数据库使用的空间大小
        /// </summary>
        /// <param name="DatabaseName"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public virtual float QueryUsedSpaceSize(string DatabaseName, ref string ReturnDescription) { return 0; }

        /// <summary>
        /// 物理移除数据库
        /// </summary>
        /// <param name="DatabaseName"></param>
        /// <param name="UserName"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public virtual bool RemoveDatabase(string DatabaseName, string UserName, ref string ReturnDescription) { return false; }

        /// <summary>
        /// 检测数据库是否存在
        /// </summary>
        /// <param name="DatabaseName"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public virtual bool DatabaseExists(string DatabaseName, ref string ReturnDescription) { return false; }

        /// <summary>
        /// 检测数据库用户是否存在
        /// </summary>
        /// <param name="DatabaseUser"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public virtual bool DatabaseUserExists(string DatabaseUser, ref string ReturnDescription) { return false; }
    }
}
