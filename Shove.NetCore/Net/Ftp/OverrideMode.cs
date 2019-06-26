using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Net.Ftp
{
    /// <summary>
    /// 覆盖模式
    /// </summary>
    public enum OverrideMode
    {
        /// <summary>
        /// 警告文件已经存在，终止任务
        /// </summary>
        Wanring,

        /// <summary>
        /// 不警告文件已经存在，直接覆盖
        /// </summary>
        Override,

        /// <summary>
        /// 不警告文件已经存在，直接续传
        /// </summary>
        Resume
    }
}
