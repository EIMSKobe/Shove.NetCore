using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data;

namespace Shove.Database.Manage
{
    /// <summary>
    /// 对 MSSQL 数据库的管理类
    /// </summary>
    public class MSSQL : IDatabaseManage
    {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="ConnectionString">管理数据库的连接串</param>
        public MSSQL(string ConnectionString)
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new Exception("The ConnectionString is Empty.");
            }

            this.ConnectionString = ConnectionString;
            this.conn = new SqlConnection(this.ConnectionString);
        }

        /// <summary>
        /// 创建一个新的数据库
        /// </summary>
        /// <param name="DatabaseName"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public override bool CreateDatabase(string DatabaseName, ref string ReturnDescription)
        {
            ReturnDescription = "";
            if (!VaildStringParameters(ref ReturnDescription, DatabaseName))
            {
                return false;
            }

            DatabaseName = DatabaseName.Trim();
            if (!DatabaseName.StartsWith("[") || !DatabaseName.EndsWith("]"))
            {
                DatabaseName = "[" + DatabaseName + "]";
            }

            if (!Open(ref ReturnDescription))
            {
                ReturnDescription = "数据库打开出错！";
                return false;
            }

            Shove.DatabaseFactory.MSSQL mssql = new Shove.DatabaseFactory.MSSQL(this.ConnectionString);
            int result = mssql.ExecuteNonQuery("create database " + DatabaseName + ";");

            Close();

            //测试证明-1已经创建成功了
            if (result < -1)
            {
                ReturnDescription = "数据库指令执行发生错误1，返回值是 " + result.ToString() + "|" + "create database " + DatabaseName + "|" + this.ConnectionString;

                return false;
            }

            return true;
        }

        /// <summary>
        /// 创建一个数据库用户
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="Password"></param>
        /// <param name="GrantOwnerDatabaseName"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public override bool CreateUser(string UserName, string Password, string GrantOwnerDatabaseName, ref string ReturnDescription)
        {
            ReturnDescription = "";
            if (!VaildStringParameters(ref ReturnDescription, UserName, Password, GrantOwnerDatabaseName))
            {
                return false;
            }

            GrantOwnerDatabaseName = GrantOwnerDatabaseName.Trim();
            if (!GrantOwnerDatabaseName.StartsWith("[") || !GrantOwnerDatabaseName.EndsWith("]"))
            {
                GrantOwnerDatabaseName = "[" + GrantOwnerDatabaseName + "]";
            }

            if (!Open(ref ReturnDescription))
            {
                ReturnDescription = "数据库打开出错！";
                return false;
            }
            string sql = string.Format("use {0};\r\n create login {1} with password='{2}', default_database={3},CHECK_EXPIRATION = OFF,CHECK_POLICY = OFF;\r\n create user {4} for login {5} with default_schema=dbo;\r\n exec sp_addrolemember 'db_owner', '{6}';",
                GrantOwnerDatabaseName, UserName, Password, GrantOwnerDatabaseName, UserName, UserName, UserName);
            int result = Shove.Database.MSSQL.ExecuteNonQuery((SqlConnection)this.conn, sql);
            Close();

            if (result < 0)
            {
                ReturnDescription = "数据库指令执行发生错误2,创建用户时出错，返回值是 " + result.ToString() + "||" + sql;

                return false;
            }

            return true;
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="OldPassword"></param>
        /// <param name="NewPassword"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public override bool EditUserPassword(string UserName, string OldPassword, string NewPassword, ref string ReturnDescription)
        {
            ReturnDescription = "";
            if (!VaildStringParameters(ref ReturnDescription, UserName, OldPassword, NewPassword))
            {
                return false;
            }

            if (!Open(ref ReturnDescription))
            {
                return false;
            }
            string sql = string.Format("exec sp_password '{0}', '{1}', '{2}';",
                (string.IsNullOrEmpty(OldPassword) ? "NULL" : OldPassword), NewPassword, UserName);
            Shove.DatabaseFactory.MSSQL mssql = new Shove.DatabaseFactory.MSSQL(this.ConnectionString);

            //int result =Shove . Database.MSSQL.ExecuteNonQuery((SqlConnection)this.conn,
            //    string.Format("exec sp_password '{0}', '{1}', '{2}'",
            //    (string.IsNullOrEmpty(OldPassword) ? "NULL" : OldPassword), NewPassword, UserName));

            int result = mssql.ExecuteNonQuery(sql);


            if (result < -1)//-1时已经成功
            {
                ReturnDescription = "数据库指令执行发生错误3，返回值是 " + result.ToString() + "||" + sql;

                return false;
            }
            return true;
        }

        /// <summary>
        /// 查询数据库使用的空间大小
        /// </summary>
        /// <param name="DatabaseName"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public override float QueryUsedSpaceSize(string DatabaseName, ref string ReturnDescription)
        {
            ReturnDescription = "";
            if (!VaildStringParameters(ref ReturnDescription, DatabaseName))
            {
                return -1;
            }

            DatabaseName = DatabaseName.Trim();
            if (!DatabaseName.StartsWith("[") || !DatabaseName.EndsWith("]"))
            {
                DatabaseName = "[" + DatabaseName + "]";
            }

            if (!Open(ref ReturnDescription))
            {
                return -2;
            }

            DataTable dt = Shove.Database.MSSQL.Select((SqlConnection)this.conn,
                string.Format("use {0};\r\n exec sp_spaceused;",
                DatabaseName));
            Close();

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                ReturnDescription = "数据库指令执行发生错误，返回值是 NULL";

                return -3;
            }

            string str = dt.Rows[0]["database_size"].ToString();
            string[] strs = str.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if ((strs == null) || (strs.Length != 2))
            {
                ReturnDescription = "数据库指令执行发生错误，返回值是 " + str;

                return -4;
            }

            float result = Shove.Convert.StrToFloat(strs[0], -4);
            if (result < 0)
            {
                ReturnDescription = "数据库指令执行发生错误，返回值是 " + result.ToString();

                return -5;
            }

            switch (strs[1])
            {
                case "TB":
                    result *= (long)1024 * 1024 * 1024 * 1024;
                    break;
                case "GB":
                    result *= (long)1024 * 1024 * 1024;
                    break;
                case "MB":
                    result *= (long)1024 * 1024;
                    break;
                case "KB":
                    result *= (long)1024;
                    break;
            }

            return result;
        }

        /// <summary>
        /// 物理移除数据库
        /// </summary>
        /// <param name="DatabaseName"></param>
        /// <param name="UserName"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public override bool RemoveDatabase(string DatabaseName, string UserName, ref string ReturnDescription)
        {
            ReturnDescription = "";
            if (!VaildStringParameters(ref ReturnDescription, DatabaseName, UserName))
            {
                return false;
            }

            DatabaseName = DatabaseName.Trim();
            if (!DatabaseName.StartsWith("[") || !DatabaseName.EndsWith("]"))
            {
                DatabaseName = "[" + DatabaseName + "]";
            }

            if (!Open(ref ReturnDescription))
            {
                return false;
            }

            string sql = string.Format(@"use {0} 
                                            EXEC sp_revokedbaccess N'{1}'
                                            EXEC sp_droplogin N'{2}' 
                                         use master
                                            ALTER DATABASE {3} SET SINGLE_USER with ROLLBACK IMMEDIATE
                                            DROP DATABASE {4}", DatabaseName, UserName, UserName, DatabaseName, DatabaseName);
            Shove.DatabaseFactory.MSSQL mssql = new Shove.DatabaseFactory.MSSQL(this.ConnectionString);
            int result = mssql.ExecuteNonQuery(sql);
            Close();

            if (result < 0)
            {
                ReturnDescription = "移除数据库出现错误，返回值是 " + result.ToString();

                return false;
            }

            return true;
        }
    }
}
