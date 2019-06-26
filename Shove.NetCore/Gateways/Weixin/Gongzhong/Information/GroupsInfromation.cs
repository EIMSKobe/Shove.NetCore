using System;
using System.Collections.Generic;
using System.Web;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    ///GroupsInfromation 的摘要说明
    /// </summary>
    public class GroupsInfromation
    {
        private string id;
        /// <summary>
        /// 分组Id
        /// </summary>
        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        private string name;
        /// <summary>
        /// 分组名称
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string count;
        /// <summary>
        /// 分组人数
        /// </summary>
        public string Count
        {
            get { return count; }
            set { count = value; }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="id">分组Id</param>
        /// <param name="name">分组名称  </param>
        /// <param name="count">组人数</param>
        public GroupsInfromation(string id, string name, string count)
        {
            this.id = id;
            this.name = name;
            this.count = count;
        }

        /// <summary>
        /// 
        /// </summary>
        public GroupsInfromation()
        {
        }
    }
}