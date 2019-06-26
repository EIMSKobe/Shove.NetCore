using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.DatabaseFactory.Convert.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class View
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name;
        /// <summary>
        /// 
        /// </summary>
        public string Body;

        /// <summary>
        /// 关联视图
        /// </summary>
        public List<string> Relyon = new List<string>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public View(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="body"></param>
        public View(string name, string body)
        {
            Name = name;
            Body = body;
        }
    }
}
