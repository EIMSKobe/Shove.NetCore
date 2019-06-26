using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 菜单条
    /// </summary>
    public class MenuView : Menu
    {
        private List<Menu> subMenus = new List<Menu>();
        /// <summary>
        /// 子栏目
        /// </summary>
        public List<Menu> SubMenus
        {
            get { return subMenus; }
            set { subMenus = value; }
        }
    }
}
