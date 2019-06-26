using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Net.Ftp
{
    /// <summary>
    /// 传输过程回调用的参数
    /// </summary>
    public class TransferEventArgs : EventArgs
    {
        /// <summary>
        /// 当前实时状态
        /// </summary>
        public Status TransferStatus
        {
            get
            {
                return _TransferStatus;
            }
            set
            {
                _TransferStatus = value;
            }
        }
        private Status _TransferStatus;

        /// <summary>
        /// 原文件长度
        /// </summary>
        public long FileSize
        {
            get
            {
                return _FileSize;
            }
            set
            {
                _FileSize = value;
            }
        }
        private long _FileSize;

        /// <summary>
        /// 已传输长度
        /// </summary>
        public long TransferredSize
        {
            get
            {
                return _TransferredSize;
            }
            set
            {
                _TransferredSize = value;
            }
        }
        private long _TransferredSize;

        /// <summary>
        /// FTP 异常
        /// </summary>
        public Exception Exception
        {
            get
            {
                return _Exception;
            }
            set
            {
                _Exception = value;
            }
        }
        private Exception _Exception;
    }
}
