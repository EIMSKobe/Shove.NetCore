using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 菜单基类
    /// </summary>
    public class Menu
    {
        private string name;
        /// <summary>
        /// 菜单名称
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string type;
        /// <summary>
        /// 菜单类型
        /// </summary>
        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        private string key;
        /// <summary>
        /// 对应的key值
        /// </summary>
        public string Key
        {
            get { return key; }
            set { key = value; }
        }
    }
}
