using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.DatabaseFactory.Convert.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class Table
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name;

        /// <summary>
        /// 
        /// </summary>
        public IList<Field> Fields = new List<Field>();

        /// <summary>
        /// 
        /// </summary>
        public IList<Index> Indexs = new List<Index>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public Table(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        public void AddField(Field field)
        {
            Fields.Add(field);
        }

        /// <summary>
        /// 
        /// </summary>
        public int FieldCount
        {
            get
            {
                return Fields.Count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        public void AddIndex(Index index)
        {
            Indexs.Add(index);
        }

        /// <summary>
        /// 
        /// </summary>
        public int IndexCount
        {
            get
            {
                return Indexs.Count;
            }
        }
    }
}
