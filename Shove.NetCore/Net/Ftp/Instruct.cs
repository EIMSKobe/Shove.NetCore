using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Net.Ftp
{
    /// <summary>
    /// 传输指令
    /// </summary>
    public enum Instruct
    {
        /// <summary>
        /// 开始
        /// </summary>
        Start,
        
        /// <summary>
        /// 覆盖
        /// </summary>
        Override,

        /// <summary>
        /// 续传
        /// </summary>
        Resume,

        /// <summary>
        /// 取消
        /// </summary>
        Cancel
    }
}
