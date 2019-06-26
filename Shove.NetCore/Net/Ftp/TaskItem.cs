using System;
using System.Collections.Generic;
using System.Text;

using EnterpriseDT.Net.Ftp;

namespace Shove.Net.Ftp
{
    /// <summary>
    /// FTP 任务，每个文件一个任务
    /// </summary>
    public class TaskItem
    {
        #region Fields

        /// <summary>
        /// 任务标识符，用于应用程序做标记用
        /// </summary>
        public string Identifiers
        {
            get
            {
                return _Identifiers;
            }
        }
        private string _Identifiers;

        /// <summary>
        /// 父类，属于哪个 Task 任务集合
        /// </summary>
        public Task Parent
        {
            get
            {
                return _Parent;
            }
            set
            {
                _Parent = value;
            }
        }
        private Task _Parent;

        /// <summary>
        /// 本地文件名
        /// </summary>
        public string LocalFileName
        {
            get
            {
                return _LocalFileName;
            }
        }
        private string _LocalFileName;

        /// <summary>
        /// FTP 服务器文件名
        /// </summary>
        public string RemoteFileName
        {
            get
            {
                return _RemoteFileName;
            }
        }
        private string _RemoteFileName;

        /// <summary>
        /// 传输方向
        /// </summary>
        public Direction TransferDirection
        {
            get
            {
                return _TransferDirection;
            }
        }
        private Direction _TransferDirection;

        /// <summary>
        /// 传输指令
        /// </summary>
        public Instruct TransferInstruct
        {
            get
            {
                return _TransferInstruct;
            }
            set
            {
                if ((value == Instruct.Start || value == Instruct.Override || value == Instruct.Resume) && (_TransferStatus == Status.Connecting || _TransferStatus == Status.Transfering))
                {
                    return;// throw new Exception("任务正在传输，不能设置新传输指令。");
                }

                _TransferInstruct = value;
                InstructExcuted = false;

                if (value == Instruct.Cancel)
                {
                    InstructExcuted = true;

                    Cancel();
                }
            }
        }
        private Instruct _TransferInstruct = Instruct.Start;
        internal bool InstructExcuted = false;

        /// <summary>
        /// 传输状态
        /// </summary>
        public Status TransferStatus
        {
            get
            {
                return _TransferStatus;
            }
        }
        private Status _TransferStatus = Status.Waiting;

        /// <summary>
        /// 原文件长度
        /// </summary>
        public long FileSize
        {
            get
            {
                return _FileSize;
            }
        }
        private long _FileSize = 0;

        /// <summary>
        /// 已传输长度
        /// </summary>
        public long TransferredSize
        {
            get
            {
                return _TransferredSize;
            }
        }
        private long _TransferredSize = 0;

        /// <summary>
        /// 当状态为 Failed 时，此异常指示了具体原因
        /// </summary>
        public Exception Exception
        {
            get
            {
                return _Exception;
            }
        }
        private Exception _Exception = null;

        private FtpClient ftp = null;

        #endregion

        #region 构造

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="Identifiers">标识</param>
        /// <param name="Server">服务器 IP 或名称</param>
        /// <param name="UserName">用户名</param>
        /// <param name="Password">密码</param>
        /// <param name="LocalFileName">本地文件名</param>
        /// <param name="RemoteFileName">远程文件名</param>
        /// <param name="TransferDirection">传输方向</param>
        public TaskItem(string Identifiers, string Server, string UserName, string Password, string LocalFileName, string RemoteFileName, Direction TransferDirection)
            : this(Identifiers, Server, 21, UserName, Password, true, true, false, 4096, LocalFileName, RemoteFileName, TransferDirection, Instruct.Start)
        {
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="Identifiers">标识</param>
        /// <param name="Server">服务器 IP 或名称</param>
        /// <param name="UserName">用户名</param>
        /// <param name="Password">密码</param>
        /// <param name="LocalFileName">本地文件名</param>
        /// <param name="RemoteFileName">远程文件名</param>
        /// <param name="TransferDirection">传输方向</param>
        /// <param name="InitializationInstruct">初始化时的默认指令</param>
        public TaskItem(string Identifiers, string Server, string UserName, string Password, string LocalFileName, string RemoteFileName, Direction TransferDirection, Instruct InitializationInstruct)
            : this(Identifiers, Server, 21, UserName, Password, true, true, false, 4096, LocalFileName, RemoteFileName, TransferDirection, InitializationInstruct)
        {
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="Identifiers">标识</param>
        /// <param name="Server">服务器 IP 或名称</param>
        /// <param name="Port">端口</param>
        /// <param name="UserName">用户名</param>
        /// <param name="Password">密码</param>
        /// <param name="LocalFileName">本地文件名</param>
        /// <param name="RemoteFileName">远程文件名</param>
        /// <param name="TransferDirection">传输方向</param>
        public TaskItem(string Identifiers, string Server, int Port, string UserName, string Password, string LocalFileName, string RemoteFileName, Direction TransferDirection)
            : this(Identifiers, Server, Port, UserName, Password, true, true, false, 4096, LocalFileName, RemoteFileName, TransferDirection, Instruct.Start)
        {
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="Identifiers">标识</param>
        /// <param name="Server">服务器 IP 或名称</param>
        /// <param name="Port">端口</param>
        /// <param name="UserName">用户名</param>
        /// <param name="Password">密码</param>
        /// <param name="LocalFileName">本地文件名</param>
        /// <param name="RemoteFileName">远程文件名</param>
        /// <param name="TransferDirection">传输方向</param>
        /// <param name="InitializationInstruct">初始化时的默认指令</param>
        public TaskItem(string Identifiers, string Server, int Port, string UserName, string Password, string LocalFileName, string RemoteFileName, Direction TransferDirection, Instruct InitializationInstruct)
            : this(Identifiers, Server, Port, UserName, Password, true, true, false, 4096, LocalFileName, RemoteFileName, TransferDirection, InitializationInstruct)
        {
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="Identifiers">标识</param>
        /// <param name="Server">服务器 IP 或名称</param>
        /// <param name="Port">端口</param>
        /// <param name="UserName">用户名</param>
        /// <param name="Password">密码</param>
        /// <param name="UseBinary">二进制传输</param>
        /// <param name="UsePassive">使用被动模式，注意：一般此变量设置为 false, 用主动模式</param>
        /// <param name="EnableSsl">使用 SSL 加密传输</param>
        /// <param name="LocalFileName">本地文件名</param>
        /// <param name="RemoteFileName">远程文件名</param>
        /// <param name="TransferDirection">传输方向</param>
        /// <param name="InitializationInstruct">初始化时的默认指令</param>
        public TaskItem(string Identifiers, string Server, int Port, string UserName, string Password, bool UseBinary, bool UsePassive, bool EnableSsl, string LocalFileName, string RemoteFileName, Direction TransferDirection, Instruct InitializationInstruct)
            : this(Identifiers, Server, Port, UserName, Password, UseBinary, UsePassive, EnableSsl, 4096, LocalFileName, RemoteFileName, TransferDirection, InitializationInstruct)
        {
        }

		/// <param name="Identifiers">标识</param>
        /// <param name="Server">服务器 IP 或名称</param>
        /// <param name="Port">端口</param>
        /// <param name="UserName">用户名</param>
        /// <param name="Password">密码</param>
        /// <param name="UseBinary">二进制传输</param>
        /// <param name="UsePassive">使用被动模式，注意：一般此变量设置为 false, 用主动模式</param>
        /// <param name="EnableSsl">使用 SSL 加密传输</param>
		/// <param name="TransferBufferSize">每次传输的缓冲区大小</param>
        /// <param name="LocalFileName">本地文件名</param>
        /// <param name="RemoteFileName">远程文件名</param>
        /// <param name="TransferDirection">传输方向</param>
        /// <param name="InitializationInstruct">初始化时的默认指令</param>
        public TaskItem(string Identifiers, string Server, int Port, string UserName, string Password, bool UseBinary, bool UsePassive, bool EnableSsl, int TransferBufferSize, string LocalFileName, string RemoteFileName, Direction TransferDirection, Instruct InitializationInstruct)
        {
            this._Identifiers = Identifiers;

            this._LocalFileName = LocalFileName;
            this._RemoteFileName = RemoteFileName;
            this._TransferDirection = TransferDirection;
            this._TransferInstruct = InitializationInstruct;

            ftp = new FtpClient(Server, Port, UserName, Password, UseBinary, UsePassive, EnableSsl, TransferBufferSize);
            ftp.OnTransfering += new FtpClient.TransferEventHandler(On_Transfering);
        }

        #endregion

        #region 控制任务

        /// <summary>
        /// 启动任务
        /// </summary>
        internal void Start()
        {
            if (_TransferStatus == Status.Connecting || _TransferStatus == Status.Transfering)
            {
                return;
            }

            if (!ftp.Connect())
            {
                return;
            }

            InstructExcuted = true;

            switch (TransferInstruct)
            {
                case Instruct.Start:
                    if (TransferDirection == Direction.Upload)
                    {
                        ftp.AsynchronousUpdate(LocalFileName, RemoteFileName, OverrideMode.Wanring);
                    }
                    else
                    {
                        ftp.AsynchronousDownload(LocalFileName, RemoteFileName, OverrideMode.Wanring);
                    }
                    break;
                case Instruct.Override:
                    if (TransferDirection == Direction.Upload)
                    {
                        ftp.AsynchronousUpdate(LocalFileName, RemoteFileName, OverrideMode.Override);
                    }
                    else
                    {
                        ftp.AsynchronousDownload(LocalFileName, RemoteFileName, OverrideMode.Override);
                    }
                    break;
                case Instruct.Resume:
                    if (TransferDirection == Direction.Upload)
                    {
                        ftp.AsynchronousUpdate(LocalFileName, RemoteFileName, OverrideMode.Resume);
                    }
                    else
                    {
                        ftp.AsynchronousDownload(LocalFileName, RemoteFileName, OverrideMode.Resume);
                    }
                    break;
            }
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        internal void Cancel()
        {
            if (TransferStatus != Status.Finished)
            {
                _TransferStatus = Status.Canceled;
                InstructExcuted = true;
            }

            ftp.AsynchronousDisconnect();
        }

        #endregion

        #region 实现委托事件

        /// <summary>
        /// 传输过程的委托时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void On_Transfering(object sender, TransferEventArgs e)
        {
            _TransferStatus = e.TransferStatus;
            _FileSize = e.FileSize;
            _TransferredSize = e.TransferredSize;
            _Exception = e.Exception;

            if ((e.TransferStatus != Status.Connecting) && (e.TransferStatus != Status.Transfering))
            {
                ftp.AsynchronousDisconnect();
            }
        }

        #endregion
    }
}
