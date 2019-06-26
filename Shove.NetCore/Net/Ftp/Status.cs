using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Net.Ftp
{
    /// <summary>
    /// 传输状态的枚举
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// 尚未开始传输，任务等待中
        /// </summary>
        Waiting,

        /// <summary>
        /// 连接 FTP 服务器中
        /// </summary>
        Connecting,

        /// <summary>
        /// 正在传输中
        /// </summary>
        Transfering,

        /// <summary>
        /// 传输结束，成功了
        /// </summary>
        Finished,

        /// <summary>
        /// 传输失败(网络故障等其他原因)
        /// </summary>
        Failed,

        /// <summary>
        /// 传输失败(文件被占用)
        /// </summary>
        Using,

        /// <summary>
        /// 文件已经存在，等待下一指令
        /// </summary>
        Exists,

        /// <summary>
        /// 操作被取消
        /// </summary>
        Canceled
    }
}
