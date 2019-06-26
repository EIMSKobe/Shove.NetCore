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
        /// <param name="Name"></param>
        public Index(string Name)
        {
            this.Name = Name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Body"></param>
        public Index(string Name, string Body)
        {
            this.Name = Name;
            this.Body = Body;
        }
    }
}
