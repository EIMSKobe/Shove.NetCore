using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

using EnterpriseDT.Net.Ftp;

namespace Shove.Net.Ftp
{
    /// <summary>
    /// FTP上传下载
    /// </summary>
    public class FtpClient
    {
        #region 属性列表 Properties

        private string Server;
        private int Port = 21;
        private string UserName;
        private string Password;
        private bool UseBinary = true;
        private bool UsePassive = false;
        private bool EnableSsl = false;

        private FTPConnection m_pFtp = null;

        private long FileSize = 0;
        private long TransferredSize = 0;
        private long TargetOriginalFileSize = 0;    // 目标文件已经存在的时候的原始大小

        // 异步时使用的中转变量
        private string Asynchronous_localFileName;
        private string Asynchronous_remoteFileName;
        private OverrideMode Asynchronous_mode;

        #endregion

        #region 委托、事件

        /// <summary>
        /// 
        /// </summary>
        public event TransferEventHandler OnTransfering;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void TransferEventHandler(object sender, TransferEventArgs e);

        private void OnBytesTransferred(object sender, BytesTransferredEventArgs e)
        {
            if (OnTransfering == null)
            {
                return;
            }

            this.TransferredSize = this.TargetOriginalFileSize + e.ByteCount;

            TransferEventArgs arg = new TransferEventArgs();
            arg.TransferStatus = Status.Transfering;
            arg.FileSize = this.FileSize;
            arg.TransferredSize = this.TransferredSize;
            arg.Exception = null;

            OnTransfering(sender, arg);
        }

        private void OnUploading(object sender, FTPFileTransferEventArgs e)
        {
            if (OnTransfering == null)
            {
                return;
            }

            this.FileSize = e.LocalFileSize;
            this.TargetOriginalFileSize = e.Append ? e.RemoteFileSize : 0;

            TransferEventArgs arg = new TransferEventArgs();
            arg.TransferStatus = Status.Transfering;
            arg.FileSize = this.FileSize;
            arg.TransferredSize = this.TransferredSize;
            arg.Exception = e.Exception;

            OnTransfering(sender, arg);
        }

        private void OnUploaded(object sender, FTPFileTransferEventArgs e)
        {
            if (OnTransfering == null)
            {
                return;
            }

            Status status = e.Cancel ? Status.Canceled : (e.Succeeded ? Status.Finished : Status.Failed);

            #region 怀疑 edtFtp 组件的完成状态判断有 BUG，这里多校验一次

            if (status == Status.Finished)
            {
                if (e.RemoteFileSize != e.LocalFileSize)
                {
                    status = Status.Failed;
                }
            }

            #endregion

            TransferEventArgs arg = new TransferEventArgs();
            arg.TransferStatus = status;
            arg.FileSize = this.FileSize;
            arg.TransferredSize = this.TransferredSize;
            arg.Exception = e.Exception;

            OnTransfering(sender, arg);
        }

        private void OnDownloading(object sender, FTPFileTransferEventArgs e)
        {
            if (OnTransfering == null)
            {
                return;
            }

            this.FileSize = e.RemoteFileSize;
            this.TargetOriginalFileSize = e.Append ? e.LocalFileSize : 0;

            TransferEventArgs arg = new TransferEventArgs();
            arg.TransferStatus = Status.Transfering;
            arg.FileSize = this.FileSize;
            arg.TransferredSize = this.TransferredSize;
            arg.Exception = e.Exception;

            OnTransfering(sender, arg);
        }

        private void OnDownloaded(object sender, FTPFileTransferEventArgs e)
        {
            if (OnTransfering == null)
            {
                return;
            }

            Status status = e.Cancel ? Status.Canceled : (e.Succeeded ? Status.Finished : Status.Failed);

            #region 怀疑 edtFtp 组件的完成状态判断有 BUG，这里多校验一次

            if (status == Status.Finished)
            {
                if (e.LocalFileSize != e.RemoteFileSize)
                {
                    status = Status.Failed;
                }
            }

            #endregion

            TransferEventArgs arg = new TransferEventArgs();
            arg.TransferStatus = status;
            arg.FileSize = this.FileSize;
            arg.TransferredSize = this.TransferredSize;
            arg.Exception = e.Exception;

            OnTransfering(sender, arg);
        }

        #endregion

        #region 构造、析构函数

        /// <summary>
        /// 乖胩
        /// </summary>
        /// <param name="server">服务器 IP 地址或域名</param>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        public FtpClient(string server, string userName, string password)
            : this(server, 21, userName, password, true, true, false, 4096)
        { }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="server">服务器 IP 地址或域名</param>
        /// <param name="Port">FTP 端口</param>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="useBinary">使用二进制传输</param>
        /// <param name="usePassive">使用被动模式</param>
        /// <param name="enableSsl">使用SSL</param>
        public FtpClient(string server, int Port, string userName, string password, bool useBinary, bool usePassive, bool enableSsl)
            : this(server, 21, userName, password, useBinary, usePassive, enableSsl, 4096)
        { }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="server">服务器 IP 地址或域名</param>
        /// <param name="Port">FTP 端口</param>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="useBinary">使用二进制传输</param>
        /// <param name="usePassive">使用被动模式</param>
        /// <param name="enableSsl">使用SSL</param>
        /// <param name="transferBufferSize">每次传输的缓冲区大小</param>
        public FtpClient(string server, int Port, string userName, string password, bool useBinary, bool usePassive, bool enableSsl, int transferBufferSize)
        {
            this.Server = server;
            this.Port = Port > 0 ? Port : 21;
            this.UserName = userName;
            this.Password = password;
            this.UseBinary = useBinary;
            this.UsePassive = usePassive;
            this.EnableSsl = enableSsl;

            m_pFtp = new FTPConnection();

            m_pFtp.ServerAddress = server;
            m_pFtp.ServerPort = Port;
            m_pFtp.UserName = userName;
            m_pFtp.Password = password;
            m_pFtp.TransferType = UseBinary ? FTPTransferType.BINARY : FTPTransferType.ASCII;
            m_pFtp.ConnectMode = UsePassive ? FTPConnectMode.PASV : FTPConnectMode.ACTIVE;

            m_pFtp.CommandEncoding = Encoding.UTF8; // Encoding.GetEncoding("GB2312");
            m_pFtp.ShowHiddenFiles = true;
            m_pFtp.TransferBufferSize = transferBufferSize;
            m_pFtp.TransferNotifyInterval = transferBufferSize;
            m_pFtp.EventsEnabled = true;
            m_pFtp.Timeout = 30000;//20000;
            m_pFtp.AutoFeatures = true;
            m_pFtp.AutoLogin = true;
            m_pFtp.DeleteOnFailure = true;
            m_pFtp.ParsingCulture = new System.Globalization.CultureInfo("");
            m_pFtp.StrictReturnCodes = true;
            m_pFtp.TransferNotifyInterval = (long)transferBufferSize;

            m_pFtp.BytesTransferred += new BytesTransferredHandler(OnBytesTransferred);
            m_pFtp.Uploading += new FTPFileTransferEventHandler(OnUploading);
            m_pFtp.Uploaded += new FTPFileTransferEventHandler(OnUploaded);
            m_pFtp.Downloading += new FTPFileTransferEventHandler(OnDownloading);
            m_pFtp.Downloaded += new FTPFileTransferEventHandler(OnDownloaded);
        }

        /// <summary>
        /// 关闭与 FTP 服务器的链接
        /// </summary>
        ~FtpClient()
        {
            Disconnect();
        }

        #endregion

        #region 连接、断开

        /// <summary>
        /// 连接 FTP 服务器
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            this.FileSize = 0;
            this.TransferredSize = 0;
            this.TargetOriginalFileSize = 0;

            TransferEventArgs arg = new TransferEventArgs();

            if (OnTransfering != null)
            {
                arg.TransferStatus = Status.Connecting;
                arg.FileSize = 0;
                arg.TransferredSize = 0;
                arg.Exception = null;

                OnTransfering(this.m_pFtp, arg);
            }

            try
            {
                m_pFtp.Connect();
            }
            catch (Exception e)
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Failed;
                    arg.FileSize = 0;
                    arg.TransferredSize = 0;
                    arg.Exception = e;

                    OnTransfering(this.m_pFtp, arg);
                }
                else
                {
                    throw e;
                }
            }

            return m_pFtp.IsConnected;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (m_pFtp != null && m_pFtp.IsConnected)
            {
                Cancel();

                try
                {
                    m_pFtp.Close();
                }
                catch { }
            }
        }

        /// <summary>
        /// 异步断开连接
        /// </summary>
        public void AsynchronousDisconnect()
        {
            System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ThreadStart(this.Disconnect));
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// 取消连接正在进行的任务
        /// </summary>
        public void Cancel()
        {
            if (m_pFtp != null && m_pFtp.IsConnected && m_pFtp.IsTransferring)
            {
                try
                {
                    m_pFtp.CancelTransfer();
                }
                catch { }
            }
        }

        #endregion

        #region 获取 FTP 服务器的文件、或文件列表

        /// <summary>
        /// 获取 FTP 服务器的文件列表
        /// </summary>
        /// <param name="path">服务器相对路径</param>
        /// <returns></returns>
        public IList<FtpFileInfo> GetFiles(string path)
        {
            IList<FtpFileInfo> files = new List<FtpFileInfo>();

            GetFiles(path, files);

            return files;
        }

        /// <summary>
        /// 获取 FTP 服务器的文件列表的递归方法
        /// </summary>
        /// <param name="path">服务器相对路径</param>
        /// <param name="files">填充文件名的变量</param>
        /// <returns></returns>
        private void GetFiles(string path, IList<FtpFileInfo> files)
        {
            if (files == null)
            {
                files = new List<FtpFileInfo>();
            }

            m_pFtp.ChangeWorkingDirectory("/" + path.TrimStart('/'));
            FTPFile[] listItems = m_pFtp.GetFileInfos("/" + path.TrimStart('/'));

            foreach (FTPFile listItem in listItems)
            {
                if (!listItem.Dir)
                {
                    files.Add(new FtpFileInfo(listItem, path));//(path.TrimEnd('/') + "/").TrimStart('/') + listItem.Name);
                }
                else
                {
                    GetFiles(path.TrimEnd('/') + "/" + listItem.Name, files);
                }
            }
        }

        #endregion

        #region 获取 FTP 服务器的目录列表

        /// <summary>
        /// 获取 FTP 服务器的目录列表
        /// </summary>
        /// <param name="path">服务器相对路径</param>
        /// <returns></returns>
        public IList<FtpFileInfo> GetDirectorys(string path)
        {
            IList<FtpFileInfo> directorys = new List<FtpFileInfo>();

            GetDirectorys(path, directorys);

            return directorys;
        }

        /// <summary>
        /// 获取 FTP 服务器的目录列表的递归方法
        /// </summary>
        /// <param name="path">服务器相对路径</param>
        /// <param name="directorys">填充目录名的变量</param>
        /// <returns></returns>
        private void GetDirectorys(string path, IList<FtpFileInfo> directorys)
        {
            if (directorys == null)
            {
                directorys = new List<FtpFileInfo>();
            }

            m_pFtp.ChangeWorkingDirectory("/" + path.TrimStart('/'));
            FTPFile[] listItems = m_pFtp.GetFileInfos("/" + path.TrimStart('/'));

            foreach (FTPFile listItem in listItems)
            {
                if (listItem.Dir)
                {
                    directorys.Add(new FtpFileInfo(listItem, path));//(path.TrimEnd('/') + "/").TrimStart('/') + listItem.Name);
                    GetDirectorys(path.TrimEnd('/') + "/" + listItem.Name, directorys);
                }
            }
        }

        #endregion

        #region 在 FTP 服务器上创建一个目录

        /// <summary>
        /// 在 FTP 服务器上创建一个目录
        /// </summary>
        /// <param name="path"></param>
        public void CreateDirectory(string path)
        {
            if (string.IsNullOrEmpty(path.Trim()))
            {
                return;
            }

            m_pFtp.CreateDirectory("/" + path.TrimStart('/'));
        }

        #endregion

        #region 删除文件、目录

        /// <summary>
        /// 在 FTP 服务器上删除一个目录，递归删除里面的子目录及文件
        /// </summary>
        /// <param name="path"></param>
        public void DeleteDirectory(string path)
        {
            IList<FtpFileInfo> files = GetFiles(path);
            foreach (FtpFileInfo file in files)
            {
                DeleteFile(file.FullName);
            }

            IList<FtpFileInfo> directorys = GetDirectorys(path);
            for (int i = directorys.Count - 1; i >= 0; i--)
            {
                m_pFtp.DeleteDirectory("/" + directorys[i].FullName.TrimStart('/'));
            }

            m_pFtp.DeleteDirectory("/" + path.TrimStart('/'));
        }

        /// <summary>
        /// 在 FTP 服务器上删除一个文件
        /// </summary>
        /// <param name="path"></param>
        public void DeleteFile(string path)
        {
            m_pFtp.DeleteFile("/" + path.TrimStart('/'));
        }

        #endregion

        #region 重命名文件、目录

        /// <summary>
        /// 在 FTP 服务器上给目录、文件重命名
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public bool Rename(string from, string to)
        {
            return m_pFtp.RenameFile(from, to);
        }

        #endregion

        #region 判断 FTP 服务器上文件、目录是否存在、获取文件长度信息

        /// <summary>
        /// 判断 FTP 服务器上指定的文件是否存在
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool FileExists(string filename)
        {
            return m_pFtp.Exists("/" + filename.TrimStart('/'));
        }

        /// <summary>
        /// 获取 FTP 上指定文件的长度
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public long GetFileSize(string filename)
        {
            return m_pFtp.GetSize("/" + filename.TrimStart('/'));
        }

        /// <summary>
        /// 判断 FTP 服务器上指定的目录是否存在
        /// </summary>
        /// <param name="directoryName"></param>
        /// <returns></returns>
        public bool DirectoryExists(string directoryName)
        {
            return m_pFtp.DirectoryExists("/" + directoryName.TrimStart('/'));
        }

        #endregion

        #region 上传文件

        /// <summary>
        /// 同步上传文件
        /// </summary>
        /// <param name="localFileName"></param>
        /// <param name="remoteFileName"></param>
        /// <param name="resume">如果远程文件存在，是否续传</param>
        public void Update(string localFileName, string remoteFileName, bool resume)
        {
            string path = Path.GetDirectoryName(remoteFileName).Replace('\\', '/').TrimStart('/');

            if (!DirectoryExists(path))
            {
                CreateDirectory(path);
            }

            if (resume)
            {
                long RemoteFileSize = GetFileSize(remoteFileName);
                long localFileSize = new FileInfo(localFileName).Length;

                if (RemoteFileSize >= localFileSize)
                {
                    return;
                }
            }

            if (resume)
            {
                m_pFtp.ResumeNextTransfer();
                m_pFtp.UploadFile(localFileName, remoteFileName);
                m_pFtp.CancelResume();
            }
            else
            {
                m_pFtp.CancelResume();
                m_pFtp.UploadFile(localFileName, remoteFileName);
            }
        }

        /// <summary>
        /// 异步上传文件
        /// </summary>
        /// <param name="localFileName"></param>
        /// <param name="remoteFileName"></param>
        /// <param name="mode"></param>
        public void AsynchronousUpdate(string localFileName, string remoteFileName, OverrideMode mode)
        {
            Asynchronous_localFileName = localFileName;
            Asynchronous_remoteFileName = remoteFileName;
            Asynchronous_mode = mode;

            System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ThreadStart(this.AsynchronousUpdate));
            thread.IsBackground = true;
            thread.Start();
        }

        private void AsynchronousUpdate()
        {
            _AsynchronousUpdate(Asynchronous_localFileName, Asynchronous_remoteFileName, Asynchronous_mode);
        }

        /// <summary>
        /// 异步上传文件
        /// </summary>
        /// <param name="localFileName"></param>
        /// <param name="remoteFileName"></param>
        /// <param name="mode"></param>
        public void _AsynchronousUpdate(string localFileName, string remoteFileName, OverrideMode mode)
        {
            TransferEventArgs arg = new TransferEventArgs();
            long LocalFileSize = 0;
            long RemoteFileSize = 0;
            bool localExists = false;
            bool remoteExists = false;

            #region Get local file information.

            FileInfo fi = new FileInfo(localFileName);
            localExists = fi.Exists;

            if (!localExists)
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Failed;
                    arg.FileSize = 0;
                    arg.TransferredSize = 0;
                    arg.Exception = new Exception("本地文件 " + localFileName + " 不存在。");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("本地文件 " + localFileName + " 不存在。");
            }

            LocalFileSize = fi.Length;

            #endregion

            #region Get remote file is exists.

            try
            {
                remoteExists = FileExists(remoteFileName);
            }
            catch (Exception e)
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Failed;
                    arg.FileSize = 0;
                    arg.TransferredSize = 0;
                    arg.Exception = e;

                    OnTransfering(this, arg);

                    return;
                }

                throw e;
            }

            #endregion

            #region If remote file is exists and wanring then return.

            if ((remoteExists) && (mode == OverrideMode.Wanring))
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Exists;
                    arg.FileSize = 0;
                    arg.TransferredSize = 0;
                    arg.Exception = new Exception("远程文件 " + remoteFileName + " 已存在。");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("远程文件 " + remoteFileName + " 已存在。");
            }

            #endregion

            #region Get remote file size.

            if (remoteExists)
            {
                try
                {
                    RemoteFileSize = GetFileSize(remoteFileName);
                }
                catch (Exception e)
                {
                    if (OnTransfering != null)
                    {
                        arg.TransferStatus = Status.Failed;
                        arg.FileSize = 0;
                        arg.TransferredSize = 0;
                        arg.Exception = e;

                        OnTransfering(this, arg);

                        return;
                    }

                    throw e;
                }
            }

            #endregion

            #region Resume but remote file is not exists.

            if ((mode == OverrideMode.Resume) && (!remoteExists))
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Failed;
                    arg.FileSize = 0;
                    arg.TransferredSize = 0;
                    arg.Exception = new Exception("远程文件 " + remoteFileName + " 不存在。");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("远程文件 " + remoteFileName + " 不存在。");
            }

            #endregion

            #region Resume

            if (mode == OverrideMode.Resume)
            {
                if (RemoteFileSize == LocalFileSize)
                {
                    if (OnTransfering != null)
                    {
                        arg.TransferStatus = Status.Finished;
                        arg.FileSize = LocalFileSize;
                        arg.TransferredSize = LocalFileSize;
                        arg.Exception = null;

                        OnTransfering(this, arg);
                    }

                    return;
                }
                else if (RemoteFileSize > LocalFileSize)
                {
                    if (OnTransfering != null)
                    {
                        arg.TransferStatus = Status.Failed;
                        arg.FileSize = 0;
                        arg.TransferredSize = 0;
                        arg.Exception = new Exception("远程文件 " + remoteFileName + " 大小异常。");

                        OnTransfering(this, arg);

                        return;
                    }

                    throw new Exception("远程文件 " + remoteFileName + " 大小异常。");
                }

                try
                {
                    m_pFtp.ResumeNextTransfer();
                    m_pFtp.UploadFile(localFileName, remoteFileName);
                    m_pFtp.CancelResume();
                }
                catch (Exception e)
                {
                    if (OnTransfering != null)
                    {
                        arg.TransferStatus = Status.Failed;
                        arg.FileSize = 0;
                        arg.TransferredSize = 0;
                        arg.Exception = e;

                        OnTransfering(this, arg);

                        return;
                    }

                    throw e;
                }

                return;
            }

            #endregion

            #region Delete remote file when remote is exists.

            if (remoteExists)
            {
                try
                {
                    DeleteFile(remoteFileName);
                }
                catch (Exception e)
                {
                    if (OnTransfering != null)
                    {
                        arg.TransferStatus = Status.Failed;
                        arg.FileSize = this.FileSize;
                        arg.TransferredSize = this.TransferredSize;
                        arg.Exception = e;

                        OnTransfering(this, arg);

                        return;
                    }

                    throw e;
                }
            }

            #endregion

            #region Create remote dir

            if (!remoteExists)
            {
                string path = Path.GetDirectoryName(remoteFileName).Replace('\\', '/').TrimStart('/');

                try
                {
                    if (!DirectoryExists(path))
                    {
                        CreateDirectory(path);
                    }
                }
                catch (Exception e)
                {
                    if (OnTransfering != null)
                    {
                        arg.TransferStatus = Status.Failed;
                        arg.FileSize = this.FileSize;
                        arg.TransferredSize = this.TransferredSize;
                        arg.Exception = e;

                        OnTransfering(this, arg);

                        return;
                    }

                    throw e;
                }
            }

            #endregion

            #region Upload file.

            try
            {
                m_pFtp.CancelResume();
                m_pFtp.UploadFile(localFileName, remoteFileName);
            }
            catch (Exception e)
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Failed;
                    arg.FileSize = this.FileSize;
                    arg.TransferredSize = this.TransferredSize;
                    arg.Exception = e;

                    OnTransfering(this, arg);

                    return;
                }

                throw e;
            }

            #endregion
        }

        #endregion

        #region 下载文件

        /// <summary>
        /// 同步下载文件
        /// </summary>
        /// <param name="localFileName"></param>
        /// <param name="remoteFileName"></param>
        /// <param name="resume">如果本地文件存在，是否续传</param>
        public void Download(string localFileName, string remoteFileName, bool resume)
        {
            string path = Path.GetDirectoryName(localFileName);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (File.Exists(localFileName) && FileUsing(localFileName))
            {
                throw new Exception("本地文件 " + localFileName + " 被占用。");
            }

            if (resume)
            {
                m_pFtp.ResumeNextTransfer();
                m_pFtp.DownloadFile(localFileName, remoteFileName);
                m_pFtp.CancelResume();
            }
            else
            {
                m_pFtp.CancelResume();
                m_pFtp.DownloadFile(localFileName, remoteFileName);
            }
        }

        /// <summary>
        /// 异步下载文件
        /// </summary>
        /// <param name="localFileName"></param>
        /// <param name="remoteFileName"></param>
        /// <param name="mode"></param>
        public void AsynchronousDownload(string localFileName, string remoteFileName, OverrideMode mode)
        {
            Asynchronous_localFileName = localFileName;
            Asynchronous_remoteFileName = remoteFileName;
            Asynchronous_mode = mode;

            System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ThreadStart(this.AsynchronousDownload));
            thread.IsBackground = true;
            thread.Start();
        }

        private void AsynchronousDownload()
        {
            _AsynchronousDownload(Asynchronous_localFileName, Asynchronous_remoteFileName, Asynchronous_mode);
        }

        /// <summary>
        /// 异步下载文件
        /// </summary>
        /// <param name="localFileName"></param>
        /// <param name="remoteFileName"></param>
        /// <param name="mode"></param>
        public void _AsynchronousDownload(string localFileName, string remoteFileName, OverrideMode mode)
        {
            TransferEventArgs arg = new TransferEventArgs();
            long localFileSize = 0;
            long remoteFileSize = 0;
            bool localExists = false;
            bool remoteExists = false;

            #region Get remote file information.

            try
            {
                remoteExists = FileExists(remoteFileName);
            }
            catch (Exception e)
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Failed;
                    arg.FileSize = 0;
                    arg.TransferredSize = 0;
                    arg.Exception = e;

                    OnTransfering(this, arg);

                    return;
                }

                throw e;
            }

            if (!remoteExists)
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Failed;
                    arg.FileSize = 0;
                    arg.TransferredSize = 0;
                    arg.Exception = new Exception("远程文件 " + remoteFileName + " 不存在。");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("远程文件 " + remoteFileName + " 不存在。");
            }

            try
            {
                remoteFileSize = GetFileSize(remoteFileName);
            }
            catch (Exception e)
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Failed;
                    arg.FileSize = 0;
                    arg.TransferredSize = 0;
                    arg.Exception = e;

                    OnTransfering(this, arg);

                    return;
                }

                throw e;
            }

            #endregion

            #region Get local file information.

            FileInfo fi = new FileInfo(localFileName);
            localExists = fi.Exists;

            if (localExists)
            {
                localFileSize = fi.Length;
            }

            #endregion

            #region If local file is exists and wanring then return.

            if (localExists && (mode == OverrideMode.Wanring))
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Exists;
                    arg.FileSize = 0;
                    arg.TransferredSize = 0;
                    arg.Exception = new Exception("本地文件 " + localFileName + " 已存在。");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("本地文件 " + localFileName + " 已存在。");
            }

            #endregion

            #region Resume but local file is not exists.

            if ((mode == OverrideMode.Resume) && (!localExists))
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Failed;
                    arg.FileSize = 0;
                    arg.TransferredSize = 0;
                    arg.Exception = new Exception("本地文件 " + localFileName + " 不存在。");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("本地文件 " + localFileName + " 不存在。");
            }

            #endregion

            #region Local file be using.

            if (FileUsing(localFileName))
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Using;
                    arg.FileSize = 0;
                    arg.TransferredSize = 0;
                    arg.Exception = new Exception("本地文件 " + localFileName + " 被占用。");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("本地文件 " + localFileName + " 被占用。");
            }

            #endregion

            #region Resume

            if (mode == OverrideMode.Resume)
            {
                if (localFileSize == remoteFileSize)
                {
                    if (OnTransfering != null)
                    {
                        arg.TransferStatus = Status.Finished;
                        arg.FileSize = remoteFileSize;
                        arg.TransferredSize = remoteFileSize;
                        arg.Exception = null;

                        OnTransfering(this, arg);
                    }

                    return;
                }
                else if (localFileSize > remoteFileSize)
                {
                    if (OnTransfering != null)
                    {
                        arg.TransferStatus = Status.Failed;
                        arg.FileSize = 0;
                        arg.TransferredSize = 0;
                        arg.Exception = new Exception("本地文件 " + localFileName + " 大小异常。");

                        OnTransfering(this, arg);

                        return;
                    }

                    throw new Exception("本地文件 " + localFileName + " 大小异常。");
                }

                try
                {
                    m_pFtp.ResumeNextTransfer();
                    m_pFtp.DownloadFile(localFileName, remoteFileName);
                    m_pFtp.CancelResume();
                }
                catch (Exception e)
                {
                    if (OnTransfering != null)
                    {
                        arg.TransferStatus = Status.Failed;
                        arg.FileSize = this.FileSize;
                        arg.TransferredSize = this.TransferredSize;
                        arg.Exception = e;

                        OnTransfering(this, arg);

                        return;
                    }

                    throw e;
                }

                return;
            }

            #endregion

            #region Delete local file when local is exists.

            if (localExists)
            {
                try
                {
                    System.IO.File.Delete(localFileName);
                }
                catch (Exception e)
                {
                    if (OnTransfering != null)
                    {
                        arg.TransferStatus = Status.Failed;
                        arg.FileSize = this.FileSize;
                        arg.TransferredSize = this.TransferredSize;
                        arg.Exception = e;

                        OnTransfering(this, arg);

                        return;
                    }

                    throw e;
                }
            }

            #endregion

            #region Create local dir

            if (!localExists)
            {
                string path = Path.GetDirectoryName(localFileName);

                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch (Exception e)
                    {
                        if (OnTransfering != null)
                        {
                            arg.TransferStatus = Status.Failed;
                            arg.FileSize = 0;
                            arg.TransferredSize = 0;
                            arg.Exception = e;

                            OnTransfering(this, arg);

                            return;
                        }

                        throw e;
                    }
                }
            }

            #endregion

            #region Download file.

            try
            {
                m_pFtp.CancelResume();
                m_pFtp.DownloadFile(localFileName, remoteFileName);
            }
            catch (Exception e)
            {
                if (OnTransfering != null)
                {
                    arg.TransferStatus = Status.Failed;
                    arg.FileSize = this.FileSize;
                    arg.TransferredSize = this.TransferredSize;
                    arg.Exception = e;

                    OnTransfering(this, arg);

                    return;
                }

                throw e;
            }

            #endregion
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 文件被占用
        /// </summary>
        /// <param name="localFileName"></param>
        /// <returns></returns>
        private bool FileUsing(string localFileName)
        {
            if (!File.Exists(localFileName))
            {
                return false;
            }

            try
            {
                File.Move(localFileName, localFileName);

                return false;
            }
            catch
            {
                return true;
            }
        }

        #endregion
    }
}