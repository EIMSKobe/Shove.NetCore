using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.DatabaseFactory.Convert.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class Field
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name;
        /// <summary>
        /// 
        /// </summary>
        public string DbType;
        /// <summary>
        /// 
        /// </summary>
        public int Length;
        /// <summary>
        /// 
        /// </summary>
        public bool IsAUTO_INCREMENT;
        /// <summary>
        /// 
        /// </summary>
        public bool IsPRIMARY_KEY;
        /// <summary>
        /// 
        /// </summary>
        public bool IsNOT_NULL;
        /// <summary>
        /// 
        /// </summary>
        public string DefaultValue;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dbtype"></param>
        /// <param name="length"></param>
        /// <param name="isauto_increment"></param>
        /// <param name="isprimary_key"></param>
        /// <param name="isnot_null"></param>
        /// <param name="defaultValue"></param>
        public Field(string name, string dbtype, int length, bool isauto_increment, bool isprimary_key, bool isnot_null, string defaultValue)
        {
            Name = name;
            DbType = dbtype;
            Length = length;
            IsAUTO_INCREMENT = isauto_increment;
            IsPRIMARY_KEY = isprimary_key;
            IsNOT_NULL = isnot_null;
            DefaultValue = defaultValue;

            if (IsPRIMARY_KEY)
            {
                IsNOT_NULL = true;
            }
        }
    }
}
