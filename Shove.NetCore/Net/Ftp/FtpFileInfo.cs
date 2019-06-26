using System;
using System.Collections.Generic;
using System.Text;

using EnterpriseDT.Net.Ftp;

namespace Shove.Net.Ftp
{
    /// <summary>
    /// 
    /// </summary>
    public class FtpFileInfo
    {
        /// <summary>
        /// 是否是目录
        /// </summary>
        public bool IsDirectory = false;
        /// <summary>
        /// 是否是文件
        /// </summary>
        public bool IsFile = false;
        /// <summary>
        /// 大小
        /// </summary>
        public long Size = 0;
        /// <summary>
        /// 文件名
        /// </summary>
        public string Name = "";
        /// <summary>
        /// 文件带完整路径名
        /// </summary>
        public string FullName = "";
        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModifiedTime;

        //public FtpFileInfo(EnterpriseDT.Net.Ftp.FTPFile file)
        //{
        //    this.IsDirectory = file.isDir;
        //    this.IsFile = !this.IsDirectory;
        //    this.Size = file.Size;
        //    this.Name = file.Name;
        //    this.FullName = file.Path.TrimStart('/');
        //    this.LastModifiedTime = file.LastModified;
        //}

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="file"></param>
        /// <param name="path"></param>
        public FtpFileInfo(EnterpriseDT.Net.Ftp.FTPFile file, string path)
        {
            this.IsDirectory = file.Dir;
            this.IsFile = !this.IsDirectory;
            this.Size = file.Size;
            this.Name = file.Name;
            this.FullName = (path.TrimEnd('/') + "/" + file.Name).TrimStart('/');//file.Path.TrimStart('/');
            this.LastModifiedTime = file.LastModified;
        }

        //public FtpFileInfo(string listItem)
        //{
        //    //-rw-r--r-- 1 ftp ftp         604038 Jan 09 01:08 aaaa.rar
        //    //drw-r--r-- 1 ftp ftp              0 Jan 09 02:08 dirName
        //    this.IsDirectory = false;
        //    this.IsFile = false;
        //    this.Size = 0;
        //    this.Name = "";

        //    if (listItem.Trim().Length < 49)
        //    {
        //        return;
        //    }

        //    this.IsDirectory = (listItem[0] == 'd');
        //    this.IsFile = !this.IsDirectory;
        //    this.Size = long.Parse(listItem.Substring(20, 15));
        //    this.Name = listItem.Substring(49);
        //}
    }
}
