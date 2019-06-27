using System;
using System.Data.SqlClient;
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
        /// <param name="database"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public override bool CreateDatabase(string database, ref string description)
        {
            description = "";
            if (!VaildStringParameters(ref description, database))
            {
                return false;
            }

            database = database.Trim();
            if (!database.StartsWith("[", StringComparison.Ordinal) || !database.EndsWith("]", StringComparison.Ordinal))
            {
                database = "[" + database + "]";
            }

            if (!Open(ref description))
            {
                description = "数据库打开出错！";
                return false;
            }

            DatabaseFactory.MSSQL mssql = new DatabaseFactory.MSSQL(this.ConnectionString);
            int result = mssql.ExecuteNonQuery("create database " + database + ";");

            Close();

            //测试证明-1已经创建成功了
            if (result < -1)
            {
                description = "数据库指令执行发生错误1，返回值是 " + result.ToString() + "|" + "create database " + database + "|" + this.ConnectionString;

                return false;
            }

            return true;
        }

        /// <summary>
        /// 创建一个数据库用户
        /// </summary>
        /// <param name="user"></param>
        /// <param name="Password"></param>
        /// <param name="grantOwnerDatabase"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public override bool CreateUser(string user, string Password, string grantOwnerDatabase, ref string description)
        {
            description = "";
            if (!VaildStringParameters(ref description, user, Password, grantOwnerDatabase))
            {
                return false;
            }

            grantOwnerDatabase = grantOwnerDatabase.Trim();
            if (!grantOwnerDatabase.StartsWith("[", StringComparison.Ordinal) || !grantOwnerDatabase.EndsWith("]", StringComparison.Ordinal))
            {
                grantOwnerDatabase = "[" + grantOwnerDatabase + "]";
            }

            if (!Open(ref description))
            {
                description = "数据库打开出错！";
                return false;
            }
            string sql = string.Format("use {0};\r\n create login {1} with password='{2}', default_database={3},CHECK_EXPIRATION = OFF,CHECK_POLICY = OFF;\r\n create user {4} for login {5} with default_schema=dbo;\r\n exec sp_addrolemember 'db_owner', '{6}';",
                grantOwnerDatabase, user, Password, grantOwnerDatabase, user, user, user);
            int result = Database.MSSQL.ExecuteNonQuery((SqlConnection)this.conn, sql);
            Close();

            if (result < 0)
            {
                description = "数据库指令执行发生错误2,创建用户时出错，返回值是 " + result.ToString() + "||" + sql;

                return false;
            }

            return true;
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="user"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public override bool EditUserPassword(string user, string oldPassword, string newPassword, ref string description)
        {
            description = "";
            if (!VaildStringParameters(ref description, user, oldPassword, newPassword))
            {
                return false;
            }

            if (!Open(ref description))
            {
                return false;
            }
            string sql = string.Format("exec sp_password '{0}', '{1}', '{2}';",
                (string.IsNullOrEmpty(oldPassword) ? "NULL" : oldPassword), newPassword, user);
            DatabaseFactory.MSSQL mssql = new DatabaseFactory.MSSQL(this.ConnectionString);

            //int result =Shove . Database.MSSQL.ExecuteNonQuery((SqlConnection)this.conn,
            //    string.Format("exec sp_password '{0}', '{1}', '{2}'",
            //    (string.IsNullOrEmpty(oldPassword) ? "NULL" : oldPassword), newPassword, user));

            int result = mssql.ExecuteNonQuery(sql);

            if (result < -1)//-1时已经成功
            {
                description = "数据库指令执行发生错误3，返回值是 " + result.ToString() + "||" + sql;

                return false;
            }
            return true;
        }

        /// <summary>
        /// 查询数据库使用的空间大小
        /// </summary>
        /// <param name="database"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public override float QueryUsedSpaceSize(string database, ref string description)
        {
            description = "";
            if (!VaildStringParameters(ref description, database))
            {
                return -1;
            }

            database = database.Trim();
            if (!database.StartsWith("[", StringComparison.Ordinal) || !database.EndsWith("]", StringComparison.Ordinal))
            {
                database = "[" + database + "]";
            }

            if (!Open(ref description))
            {
                return -2;
            }

            DataTable dt = Database.MSSQL.Select((SqlConnection)this.conn,
                string.Format("use {0};\r\n exec sp_spaceused;",
                database));
            Close();

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                description = "数据库指令执行发生错误，返回值是 NULL";

                return -3;
            }

            string str = dt.Rows[0]["database_size"].ToString();
            string[] strs = str.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if ((strs == null) || (strs.Length != 2))
            {
                description = "数据库指令执行发生错误，返回值是 " + str;

                return -4;
            }

            float result = Convert.StrToFloat(strs[0], -4);
            if (result < 0)
            {
                description = "数据库指令执行发生错误，返回值是 " + result.ToString();

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
        /// <param name="database"></param>
        /// <param name="user"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public override bool RemoveDatabase(string database, string user, ref string description)
        {
            description = "";
            if (!VaildStringParameters(ref description, database, user))
            {
                return false;
            }

            database = database.Trim();
            if (!database.StartsWith("[", StringComparison.Ordinal) || !database.EndsWith("]", StringComparison.Ordinal))
            {
                database = "[" + database + "]";
            }

            if (!Open(ref description))
            {
                return false;
            }

            string sql = string.Format(@"use {0} 
                                            EXEC sp_revokedbaccess N'{1}'
                                            EXEC sp_droplogin N'{2}' 
                                         use master
                                            ALTER DATABASE {3} SET SINGLE_USER with ROLLBACK IMMEDIATE
                                            DROP DATABASE {4}", database, user, user, database, database);
            DatabaseFactory.MSSQL mssql = new DatabaseFactory.MSSQL(this.ConnectionString);
            int result = mssql.ExecuteNonQuery(sql);
            Close();

            if (result < 0)
            {
                description = "移除数据库出现错误，返回值是 " + result.ToString();

                return false;
            }

            return true;
        }
    }
}
