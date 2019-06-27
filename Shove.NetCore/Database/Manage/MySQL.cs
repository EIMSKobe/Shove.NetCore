using System;
using MySql.Data.MySqlClient;
using System.Data;

namespace Shove.Database.Manage
{
    /// <summary>
    /// 对 MySQL 数据库的管理类
    /// </summary>
    public class MySQL : IDatabaseManage
    {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="ConnectionString">管理数据库的连接串</param>
        public MySQL(string ConnectionString)
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new Exception("The ConnectionString is Empty.");
            }

            this.ConnectionString = ConnectionString;
            this.conn = new MySqlConnection(this.ConnectionString);
        }

        #region 创建一个新的数据库
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
            if (!database.StartsWith("`", StringComparison.Ordinal) || !database.EndsWith("`", StringComparison.Ordinal))
            {
                database = "`" + database + "`";
            }

            DatabaseFactory.MySQL mysql = new DatabaseFactory.MySQL(this.ConnectionString);
            try
            {
                int result = mysql.ExecuteNonQuery("create database " + database + " DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;");
                Close();

                if (result < 0)
                {
                    description = "数据库指令执行发生错误，返回值是 " + result.ToString() + "|" + "create database " + database;

                    return false;
                }
            }
            catch (Exception e)
            {
                description = "创建数据库出错！" + e.ToString() + "|" + "create database " + database;
                return false;
            }

            return true;
        }
        #endregion

        #region 创建一个数据库用户
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
            if (!grantOwnerDatabase.StartsWith("`", StringComparison.Ordinal) || !grantOwnerDatabase.EndsWith("`", StringComparison.Ordinal))
            {
                grantOwnerDatabase = "`" + grantOwnerDatabase + "`";
            }

            if (!Open(ref description))
            {
                return false;
            }
            string comm = string.Format("CREATE USER '{0}'@'%' IDENTIFIED BY '{1}';\r\n GRANT ALL ON {2}.* TO '{3}'@'%';\r\n flush privileges;",
                user, Password, grantOwnerDatabase, user);

            DatabaseFactory.MySQL mysql = new DatabaseFactory.MySQL(this.ConnectionString);
            int result = mysql.ExecuteNonQuery(comm);
            Close();
            if (result < 0)
            {
                description = "数据库指令执行发生错误，返回值是 " + result.ToString() + "||" + comm;

                return false;
            }

            return true;
        }
        #endregion

        #region 修改用户密码
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

            int result = Database.MySQL.ExecuteNonQuery((MySqlConnection)this.conn,
                string.Format("use mysql;\r\n update user set password=password('{0}') where User='{1}';\r\n flush privileges;",
                newPassword, user));
            Close();

            if (result < 0)
            {
                description = "数据库指令执行发生错误，返回值是 " + result.ToString();

                return false;
            }
            return true;
        }
        #endregion

        #region 查询数据库使用的空间大小
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
            if (!database.StartsWith("`", StringComparison.Ordinal) || !database.EndsWith("`", StringComparison.Ordinal))
            {
                database = "`" + database + "`";
            }

            if (!Open(ref description))
            {
                return -2;
            }

            DataTable dt = Database.MySQL.Select((MySqlConnection)this.conn,
                string.Format("use information_schema;\r\n select sum(DATA_LENGTH) as database_size from TABLES where table_schema = '{0}'; ",
                database));
            Close();

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                description = "数据库指令执行发生错误，返回值是 NULL";

                return -3;
            }

            string str = dt.Rows[0]["database_size"].ToString();
            if (string.IsNullOrEmpty(str))
            {
                return 0;
            }

            float result = Convert.StrToFloat(str, -4);
            if (result < 0)
            {
                description = "数据库指令执行发生错误，返回值是 " + result.ToString();

                return -4;
            }

            return result;
        }
        #endregion

        #region 物理移除数据库
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
            if (!database.StartsWith("`", StringComparison.Ordinal) || !database.EndsWith("`", StringComparison.Ordinal))
            {
                database = "`" + database + "`";
            }

            if (!Open(ref description))
            {
                return false;
            }

            int result = Database.MySQL.ExecuteNonQuery((MySqlConnection)this.conn,
                string.Format("use mysql;\r\n delete from user where user='{0}';\r\n flush privileges;\r\n drop database {1};\r\n flush privileges;",
                user, database));
            Close();

            if (result < 0)
            {
                description = "数据库指令执行发生错误，返回值是 " + result.ToString();

                return false;
            }

            return true;
        }
        #endregion

        #region 检测数据库是否存在
        /// <summary>
        /// 检测数据库是否存在
        /// </summary>
        /// <param name="database">数据库名称</param>
        /// <param name="description"></param>
        /// <returns></returns>
        public override bool DatabaseExists(string database, ref string description)
        {
            description = "";
            if (!VaildStringParameters(ref description, database))
            {
                return false;
            }

            database = database.Trim();
            //if (!database.StartsWith("`") || !database.EndsWith("`"))
            //{
            //    database = "`" + database + "`";
            //}

            if (!Open(ref description))
            {
                return false;
            }

            int result = Convert.StrToInt(Database.MySQL.ExecuteScalar((MySqlConnection)this.conn,
                string.Format("use mysql;\r\n select count(0) from information_schema.schemata where schema_name='{0}';",
                database)) + "", -1);
            Close();

            if (result < 0)
            {
                description = "数据库指令执行发生错误，返回值是 " + result.ToString();

                return false;
            }

            if (result == 0)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region 检测数据库用户是否存在
        /// <summary>
        /// 检测数据库用户是否存在
        /// </summary>
        /// <param name="user"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public override bool userExists(string user, ref string description)
        {
            description = "";
            if (!VaildStringParameters(ref description, user))
            {
                return false;
            }

            if (!Open(ref description))
            {
                return false;
            }

            int result = Convert.StrToInt(Database.MySQL.ExecuteScalar((MySqlConnection)this.conn,
                string.Format("use mysql;\r\n SELECT count(0) from mysql.`user` WHERE `User`='{0}';",
                user)) + "", -1);
            Close();

            if (result < 0)
            {
                description = "数据库指令执行发生错误，返回值是 " + result.ToString();

                return false;
            }

            if (result == 0)
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}
