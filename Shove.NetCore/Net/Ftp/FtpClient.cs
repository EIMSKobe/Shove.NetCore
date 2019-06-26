using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

using EnterpriseDT.Net.Ftp;

namespace Shove.Net.Ftp
{
    /// <summary>
    /// FTP�ϴ�����
    /// </summary>
    public class FtpClient
    {
        #region �����б� Properties

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
        private long TargetOriginalFileSize = 0;    // Ŀ���ļ��Ѿ����ڵ�ʱ���ԭʼ��С

        // �첽ʱʹ�õ���ת����
        private string Asynchronous_localFileName;
        private string Asynchronous_remoteFileName;
        private OverrideMode Asynchronous_mode;

        #endregion

        #region ί�С��¼�

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

            #region ���� edtFtp ��������״̬�ж��� BUG�������У��һ��

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

            #region ���� edtFtp ��������״̬�ж��� BUG�������У��һ��

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

        #region ���졢��������

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="Server">������ IP ��ַ������</param>
        /// <param name="UserName">�û���</param>
        /// <param name="Password">����</param>
        public FtpClient(string Server, string UserName, string Password)
            : this(Server, 21, UserName, Password, true, true, false, 4096)
        { }

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="Server">������ IP ��ַ������</param>
        /// <param name="Port">FTP �˿�</param>
        /// <param name="UserName">�û���</param>
        /// <param name="Password">����</param>
        /// <param name="UseBinary">ʹ�ö����ƴ���</param>
        /// <param name="UsePassive">ʹ�ñ���ģʽ</param>
        /// <param name="EnableSsl">ʹ��SSL</param>
        public FtpClient(string Server, int Port, string UserName, string Password, bool UseBinary, bool UsePassive, bool EnableSsl)
            : this(Server, 21, UserName, Password, UseBinary, UsePassive, EnableSsl, 4096)
        { }

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="Server">������ IP ��ַ������</param>
        /// <param name="Port">FTP �˿�</param>
        /// <param name="UserName">�û���</param>
        /// <param name="Password">����</param>
        /// <param name="UseBinary">ʹ�ö����ƴ���</param>
        /// <param name="UsePassive">ʹ�ñ���ģʽ</param>
        /// <param name="EnableSsl">ʹ��SSL</param>
        /// <param name="TransferBufferSize">ÿ�δ���Ļ�������С</param>
        public FtpClient(string Server, int Port, string UserName, string Password, bool UseBinary, bool UsePassive, bool EnableSsl, int TransferBufferSize)
        {
            this.Server = Server;
            this.Port = Port > 0 ? Port : 21;
            this.UserName = UserName;
            this.Password = Password;
            this.UseBinary = UseBinary;
            this.UsePassive = UsePassive;
            this.EnableSsl = EnableSsl;

            m_pFtp = new FTPConnection();

            m_pFtp.ServerAddress = Server;
            m_pFtp.ServerPort = Port;
            m_pFtp.UserName = UserName;
            m_pFtp.Password = Password;
            m_pFtp.TransferType = UseBinary ? FTPTransferType.BINARY : FTPTransferType.ASCII;
            m_pFtp.ConnectMode = UsePassive ? FTPConnectMode.PASV : FTPConnectMode.ACTIVE;

            m_pFtp.CommandEncoding = Encoding.UTF8; // Encoding.GetEncoding("GB2312");
            m_pFtp.ShowHiddenFiles = true;
            m_pFtp.TransferBufferSize = TransferBufferSize;
            m_pFtp.TransferNotifyInterval = TransferBufferSize;
            m_pFtp.EventsEnabled = true;
            m_pFtp.Timeout = 30000;//20000;
            m_pFtp.AutoFeatures = true;
            m_pFtp.AutoLogin = true;
            m_pFtp.DeleteOnFailure = true;
            m_pFtp.ParsingCulture = new System.Globalization.CultureInfo("");
            m_pFtp.StrictReturnCodes = true;
            m_pFtp.TransferNotifyInterval = (long)TransferBufferSize;

            m_pFtp.BytesTransferred += new BytesTransferredHandler(OnBytesTransferred);
            m_pFtp.Uploading += new FTPFileTransferEventHandler(OnUploading);
            m_pFtp.Uploaded += new FTPFileTransferEventHandler(OnUploaded);
            m_pFtp.Downloading += new FTPFileTransferEventHandler(OnDownloading);
            m_pFtp.Downloaded += new FTPFileTransferEventHandler(OnDownloaded);
        }

        /// <summary>
        /// �ر��� FTP ������������
        /// </summary>
        ~FtpClient()
        {
            Disconnect();
        }

        #endregion

        #region ���ӡ��Ͽ�

        /// <summary>
        /// ���� FTP ������
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
        /// �Ͽ�����
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
        /// �첽�Ͽ�����
        /// </summary>
        public void AsynchronousDisconnect()
        {
            System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ThreadStart(this.Disconnect));
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// ȡ���������ڽ��е�����
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

        #region ��ȡ FTP ���������ļ������ļ��б�

        /// <summary>
        /// ��ȡ FTP ���������ļ��б�
        /// </summary>
        /// <param name="path">���������·��</param>
        /// <returns></returns>
        public IList<FtpFileInfo> GetFiles(string path)
        {
            IList<FtpFileInfo> files = new List<FtpFileInfo>();

            GetFiles(path, files);

            return files;
        }

        /// <summary>
        /// ��ȡ FTP ���������ļ��б�ĵݹ鷽��
        /// </summary>
        /// <param name="path">���������·��</param>
        /// <param name="files">����ļ����ı���</param>
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

        #region ��ȡ FTP ��������Ŀ¼�б�

        /// <summary>
        /// ��ȡ FTP ��������Ŀ¼�б�
        /// </summary>
        /// <param name="path">���������·��</param>
        /// <returns></returns>
        public IList<FtpFileInfo> GetDirectorys(string path)
        {
            IList<FtpFileInfo> directorys = new List<FtpFileInfo>();

            GetDirectorys(path, directorys);

            return directorys;
        }

        /// <summary>
        /// ��ȡ FTP ��������Ŀ¼�б�ĵݹ鷽��
        /// </summary>
        /// <param name="path">���������·��</param>
        /// <param name="directorys">���Ŀ¼���ı���</param>
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

        #region �� FTP �������ϴ���һ��Ŀ¼

        /// <summary>
        /// �� FTP �������ϴ���һ��Ŀ¼
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

        #region ɾ���ļ���Ŀ¼

        /// <summary>
        /// �� FTP ��������ɾ��һ��Ŀ¼���ݹ�ɾ���������Ŀ¼���ļ�
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
        /// �� FTP ��������ɾ��һ���ļ�
        /// </summary>
        /// <param name="path"></param>
        public void DeleteFile(string path)
        {
            m_pFtp.DeleteFile("/" + path.TrimStart('/'));
        }

        #endregion

        #region �������ļ���Ŀ¼

        /// <summary>
        /// �� FTP �������ϸ�Ŀ¼���ļ�������
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public bool Rename(string from, string to)
        {
            return m_pFtp.RenameFile(from, to);
        }

        #endregion

        #region �ж� FTP ���������ļ���Ŀ¼�Ƿ���ڡ���ȡ�ļ�������Ϣ

        /// <summary>
        /// �ж� FTP ��������ָ�����ļ��Ƿ����
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool FileExists(string filename)
        {
            return m_pFtp.Exists("/" + filename.TrimStart('/'));
        }

        /// <summary>
        /// ��ȡ FTP ��ָ���ļ��ĳ���
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public long GetFileSize(string filename)
        {
            return m_pFtp.GetSize("/" + filename.TrimStart('/'));
        }

        /// <summary>
        /// �ж� FTP ��������ָ����Ŀ¼�Ƿ����
        /// </summary>
        /// <param name="directoryName"></param>
        /// <returns></returns>
        public bool DirectoryExists(string directoryName)
        {
            return m_pFtp.DirectoryExists("/" + directoryName.TrimStart('/'));
        }

        #endregion

        #region �ϴ��ļ�

        /// <summary>
        /// ͬ���ϴ��ļ�
        /// </summary>
        /// <param name="localFileName"></param>
        /// <param name="remoteFileName"></param>
        /// <param name="resume">���Զ���ļ����ڣ��Ƿ�����</param>
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
        /// �첽�ϴ��ļ�
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
        /// �첽�ϴ��ļ�
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
                    arg.Exception = new Exception("�����ļ� " + localFileName + " �����ڡ�");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("�����ļ� " + localFileName + " �����ڡ�");
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
                    arg.Exception = new Exception("Զ���ļ� " + remoteFileName + " �Ѵ��ڡ�");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("Զ���ļ� " + remoteFileName + " �Ѵ��ڡ�");
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
                    arg.Exception = new Exception("Զ���ļ� " + remoteFileName + " �����ڡ�");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("Զ���ļ� " + remoteFileName + " �����ڡ�");
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
                        arg.Exception = new Exception("Զ���ļ� " + remoteFileName + " ��С�쳣��");

                        OnTransfering(this, arg);

                        return;
                    }

                    throw new Exception("Զ���ļ� " + remoteFileName + " ��С�쳣��");
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

        #region �����ļ�

        /// <summary>
        /// ͬ�������ļ�
        /// </summary>
        /// <param name="localFileName"></param>
        /// <param name="remoteFileName"></param>
        /// <param name="resume">��������ļ����ڣ��Ƿ�����</param>
        public void Download(string localFileName, string remoteFileName, bool resume)
        {
            string path = Path.GetDirectoryName(localFileName);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (File.Exists(localFileName) && FileUsing(localFileName))
            {
                throw new Exception("�����ļ� " + localFileName + " ��ռ�á�");
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
        /// �첽�����ļ�
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
        /// �첽�����ļ�
        /// </summary>
        /// <param name="localFileName"></param>
        /// <param name="remoteFileName"></param>
        /// <param name="mode"></param>
        public void _AsynchronousDownload(string localFileName, string remoteFileName, OverrideMode mode)
        {
            TransferEventArgs arg = new TransferEventArgs();
            long LocalFileSize = 0;
            long RemoteFileSize = 0;
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
                    arg.Exception = new Exception("Զ���ļ� " + remoteFileName + " �����ڡ�");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("Զ���ļ� " + remoteFileName + " �����ڡ�");
            }

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

            #endregion

            #region Get local file information.

            FileInfo fi = new FileInfo(localFileName);
            localExists = fi.Exists;

            if (localExists)
            {
                LocalFileSize = fi.Length;
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
                    arg.Exception = new Exception("�����ļ� " + localFileName + " �Ѵ��ڡ�");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("�����ļ� " + localFileName + " �Ѵ��ڡ�");
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
                    arg.Exception = new Exception("�����ļ� " + localFileName + " �����ڡ�");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("�����ļ� " + localFileName + " �����ڡ�");
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
                    arg.Exception = new Exception("�����ļ� " + localFileName + " ��ռ�á�");

                    OnTransfering(this, arg);

                    return;
                }

                throw new Exception("�����ļ� " + localFileName + " ��ռ�á�");
            }

            #endregion

            #region Resume

            if (mode == OverrideMode.Resume)
            {
                if (LocalFileSize == RemoteFileSize)
                {
                    if (OnTransfering != null)
                    {
                        arg.TransferStatus = Status.Finished;
                        arg.FileSize = RemoteFileSize;
                        arg.TransferredSize = RemoteFileSize;
                        arg.Exception = null;

                        OnTransfering(this, arg);
                    }

                    return;
                }
                else if (LocalFileSize > RemoteFileSize)
                {
                    if (OnTransfering != null)
                    {
                        arg.TransferStatus = Status.Failed;
                        arg.FileSize = 0;
                        arg.TransferredSize = 0;
                        arg.Exception = new Exception("�����ļ� " + localFileName + " ��С�쳣��");

                        OnTransfering(this, arg);

                        return;
                    }

                    throw new Exception("�����ļ� " + localFileName + " ��С�쳣��");
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

        #region ��������

        /// <summary>
        /// �ļ���ռ��
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