using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.DatabaseFactory.Convert.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class Index
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
        /// 
        /// </summary>
        /// <param name="name"></param>
        public Index(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="body"></param>
        public Index(string name, string body)
        {
            this.Name = name;
            this.Body = body;
        }
    }
}
