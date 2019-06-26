using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
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
            if (!DatabaseName.StartsWith("`") || !DatabaseName.EndsWith("`"))
            {
                DatabaseName = "`" + DatabaseName + "`";
            }

            Shove.DatabaseFactory.MySQL mysql = new Shove.DatabaseFactory.MySQL(this.ConnectionString);
            try
            {
                int result = mysql.ExecuteNonQuery("create database " + DatabaseName + " DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;");
                Close();

                if (result < 0)
                {
                    ReturnDescription = "数据库指令执行发生错误，返回值是 " + result.ToString() + "|" + "create database " + DatabaseName;

                    return false;
                }
            }
            catch (Exception e)
            {
                ReturnDescription = "创建数据库出错！" + e.ToString() + "|" + "create database " + DatabaseName;
                return false;
            }

            return true;
        }
        #endregion

        #region 创建一个数据库用户
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
            if (!GrantOwnerDatabaseName.StartsWith("`") || !GrantOwnerDatabaseName.EndsWith("`"))
            {
                GrantOwnerDatabaseName = "`" + GrantOwnerDatabaseName + "`";
            }

            if (!Open(ref ReturnDescription))
            {
                return false;
            }
            string comm = string.Format("CREATE USER '{0}'@'%' IDENTIFIED BY '{1}';\r\n GRANT ALL ON {2}.* TO '{3}'@'%';\r\n flush privileges;",
                UserName, Password, GrantOwnerDatabaseName, UserName);

            Shove.DatabaseFactory.MySQL mysql = new Shove.DatabaseFactory.MySQL(this.ConnectionString);
            int result = mysql.ExecuteNonQuery(comm);
            Close();
            if (result < 0)
            {
                ReturnDescription = "数据库指令执行发生错误，返回值是 " + result.ToString() + "||" + comm;

                return false;
            }

            return true;
        }
        #endregion

        #region 修改用户密码
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

            int result = Shove.Database.MySQL.ExecuteNonQuery((MySqlConnection)this.conn,
                string.Format("use mysql;\r\n update user set password=password('{0}') where User='{1}';\r\n flush privileges;",
                NewPassword, UserName));
            Close();

            if (result < 0)
            {
                ReturnDescription = "数据库指令执行发生错误，返回值是 " + result.ToString();

                return false;
            }
            return true;
        }
        #endregion

        #region 查询数据库使用的空间大小
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
            if (!DatabaseName.StartsWith("`") || !DatabaseName.EndsWith("`"))
            {
                DatabaseName = "`" + DatabaseName + "`";
            }

            if (!Open(ref ReturnDescription))
            {
                return -2;
            }

            DataTable dt = Shove.Database.MySQL.Select((MySqlConnection)this.conn,
                string.Format("use information_schema;\r\n select sum(DATA_LENGTH) as database_size from TABLES where table_schema = '{0}'; ",
                DatabaseName));
            Close();

            if ((dt == null) || (dt.Rows.Count < 1))
            {
                ReturnDescription = "数据库指令执行发生错误，返回值是 NULL";

                return -3;
            }

            string str = dt.Rows[0]["database_size"].ToString();
            if (string.IsNullOrEmpty(str))
            {
                return 0;
            }

            float result = Shove.Convert.StrToFloat(str, -4);
            if (result < 0)
            {
                ReturnDescription = "数据库指令执行发生错误，返回值是 " + result.ToString();

                return -4;
            }

            return result;
        }
        #endregion

        #region 物理移除数据库
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
            if (!DatabaseName.StartsWith("`") || !DatabaseName.EndsWith("`"))
            {
                DatabaseName = "`" + DatabaseName + "`";
            }

            if (!Open(ref ReturnDescription))
            {
                return false;
            }

            int result = Shove.Database.MySQL.ExecuteNonQuery((MySqlConnection)this.conn,
                string.Format("use mysql;\r\n delete from user where user='{0}';\r\n flush privileges;\r\n drop database {1};\r\n flush privileges;",
                UserName, DatabaseName));
            Close();

            if (result < 0)
            {
                ReturnDescription = "数据库指令执行发生错误，返回值是 " + result.ToString();

                return false;
            }

            return true;
        }
        #endregion

        #region 检测数据库是否存在
        /// <summary>
        /// 检测数据库是否存在
        /// </summary>
        /// <param name="DatabaseName">数据库名称</param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public override bool DatabaseExists(string DatabaseName, ref string ReturnDescription)
        {
            ReturnDescription = "";
            if (!VaildStringParameters(ref ReturnDescription, DatabaseName))
            {
                return false;
            }

            DatabaseName = DatabaseName.Trim();
            //if (!DatabaseName.StartsWith("`") || !DatabaseName.EndsWith("`"))
            //{
            //    DatabaseName = "`" + DatabaseName + "`";
            //}

            if (!Open(ref ReturnDescription))
            {
                return false;
            }

            int result = Shove.Convert.StrToInt(Shove.Database.MySQL.ExecuteScalar((MySqlConnection)this.conn,
                string.Format("use mysql;\r\n select count(0) from information_schema.schemata where schema_name='{0}';",
                DatabaseName)) + "", -1);
            Close();

            if (result < 0)
            {
                ReturnDescription = "数据库指令执行发生错误，返回值是 " + result.ToString();

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
        /// <param name="DatabaseUser"></param>
        /// <param name="ReturnDescription"></param>
        /// <returns></returns>
        public override bool DatabaseUserExists(string DatabaseUser, ref string ReturnDescription)
        {
            ReturnDescription = "";
            if (!VaildStringParameters(ref ReturnDescription, DatabaseUser))
            {
                return false;
            }

            if (!Open(ref ReturnDescription))
            {
                return false;
            }

            int result = Shove.Convert.StrToInt(Shove.Database.MySQL.ExecuteScalar((MySqlConnection)this.conn,
                string.Format("use mysql;\r\n SELECT count(0) from mysql.`user` WHERE `User`='{0}';",
                DatabaseUser)) + "", -1);
            Close();

            if (result < 0)
            {
                ReturnDescription = "数据库指令执行发生错误，返回值是 " + result.ToString();

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
