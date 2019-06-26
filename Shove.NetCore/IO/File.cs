using System;
using System.IO;
using System.Collections;

using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Checksum;

using Shove.CharsetDetector;
using System.Collections.Generic;

namespace Shove.IO
{
    /// <summary>
    /// File 的摘要说明。
    /// </summary>
    public class File
    {
        #region 获取磁盘目录下的文件列表

        /// <summary>
        /// 取服务器上 Path 目录下的文件列表
        /// </summary>
        /// <param name="Path">服务器上的绝对路径，调用前用 Server.MapPath 取到完整路径再传入</param>
        /// <returns></returns>
        public static string[] GetFileList(string Path)
        {
            DirectoryInfo di = new DirectoryInfo(Path);
            if (!di.Exists)
                return null;
            FileInfo[] files = di.GetFiles();
            if (files.Length == 0)
                return null;
            string[] FileList = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                FileList[i] = files[i].Name;
            return FileList;
        }

        /// <summary>
        /// 取服务器上 StartDirName 目录下的文件列表，包括所有子目录下的文件
        /// </summary>
        /// <param name="StartDirName">服务器上的绝对路径，调用前用 Server.MapPath 取到完整路径再传入</param>
        /// <returns></returns>
        public static string[] GetFileListWithSubDir(string StartDirName)
        {
            ArrayList al = new ArrayList();
            GetFile(StartDirName, al);

            if (al.Count < 1)
                return null;

            string[] strs = new string[al.Count];
            for (int i = 0; i < al.Count; i++)
                strs[i] = al[i].ToString();

            return strs;
        }

        /// <summary>
        /// GetFileListWithSubDir 方法的递归子方法
        /// </summary>
        /// <param name="Dir">目录</param>
        /// <param name="al">存放文件的集合</param>
        private static void GetFile(string Dir, ArrayList al)
        {
            string[] Files = Directory.GetFiles(Dir);
            string[] Dirs = Directory.GetDirectories(Dir);

            for (int i = 0; i < Files.Length; i++)
                al.Add(Files[i]);
            for (int i = 0; i < Dirs.Length; i++)
                GetFile(Dirs[i], al);
        }

        #endregion

        #region 读写文件

        /// <summary>
        /// 读文件
        /// </summary>
        /// <param name="FileName">文件名</param>
        /// <returns>文件内容字符串</returns>
        public static string ReadFile(string FileName)
        {
            return System.IO.File.ReadAllText(FileName, System.Text.Encoding.Default);
        }

        /// <summary>
        /// 写文件，如果文件不存在，创建该文件，否则改写该文件
        /// </summary>
        /// <param name="FileName">文件名</param>
        /// <param name="Content">写入的内容</param>
        /// <returns>true 为成功</returns>
        public static bool WriteFile(string FileName, string Content)
        {
            return WriteFile(FileName, Content, System.Text.Encoding.Default);
        }

        /// <summary>
        /// 写文件，如果文件不存在，创建该文件，否则改写该文件(根据制定的字符编码)
        /// </summary>
        /// <param name="FileName">文件名</param>
        /// <param name="Content">写入的内容</param>
        /// <param name="encoding">字符编码</param>
        /// <returns>true 为成功</returns>
        public static bool WriteFile(string FileName, string Content, System.Text.Encoding encoding)
        {
            bool OK = true;

            try
            {
                System.IO.File.WriteAllText(FileName, Content, encoding);
            }
            catch
            {
                OK = false;
            }

            return OK;
        }

        #endregion

        #region Copy File/Directory

        /// <summary>
        /// Copy File, 自动创建目标文件夹
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="overwrite"></param>
        public static void CopyFile(string src, string dest, bool overwrite)
        {
            if (!System.IO.File.Exists(src))
            {
                throw new Exception("源文件 " + src + " 不存在。");
            }

            FileInfo fi = new FileInfo(dest);

            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            System.IO.File.Copy(src, dest, overwrite);
        }

        /// <summary>
        /// 整个目录一起复制(递归实现)
        /// </summary>
        /// <param name="src">源目录</param>
        /// <param name="dest">目标目录</param>
        public static void CopyDirectory(string src, string dest)
        {
            if (!Directory.Exists(src))
            {
                return;
            }

            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }

            DirectoryInfo di = new DirectoryInfo(src);

            foreach (FileSystemInfo fsi in di.GetFileSystemInfos())
            {
                string destName = Path.Combine(dest, fsi.Name);

                if (fsi is FileInfo)
                {
                    System.IO.File.Copy(fsi.FullName, destName, true);
                }
                else
                {
                    Directory.CreateDirectory(destName);
                    CopyDirectory(fsi.FullName, destName);
                }
            }
        }

        /// <summary>
        /// 获取指定文件夹占用的空间大小
        /// </summary>
        /// <param name="DirectoryName"></param>
        public static long GetDirectorySize(string DirectoryName)
        {
            long Size = 0;
            DirectoryInfo di = new DirectoryInfo(DirectoryName);

            FileInfo[] fis = di.GetFiles();
            foreach (FileInfo fi in fis)
            {
                Size += fi.Length;
            }

            DirectoryInfo[] dis = di.GetDirectories();
            foreach (DirectoryInfo di2 in dis)
            {
                Size += GetDirectorySize(di2.FullName);
            }

            return Size;
        }

        #endregion

        #region 压缩文件

        /// <summary>
        /// 压缩一个文件，目标文件名自动在源文件后面加上 .zip
        /// </summary>
        /// <param name="FileName">源文件名</param>
        /// <returns>true 为成功</returns>
        public static bool Compress(string FileName)
        {
            return Compress(FileName, "");
        }

        /// <summary>
        /// 压缩一个文件
        /// </summary>
        /// <param name="FileName">源文件名</param>
        /// <param name="ZipFileName">目标文件名(.zip)</param>
        /// <returns>true 为成功</returns>
        public static bool Compress(string FileName, string ZipFileName)
        {
            if (ZipFileName == "")
            {
                ZipFileName = FileName + ".zip";
            }

            Crc32 crc = new Crc32();
            ZipOutputStream s;

            try
            {
                s = new ZipOutputStream(System.IO.File.Create(ZipFileName));
            }
            catch
            {
                return false;
            }

            s.SetLevel(6); // 0 - store only to 9 - means best compression

            //打开压缩文件
            FileStream fs;
            try
            {
                fs = System.IO.File.OpenRead(FileName);
            }
            catch
            {
                s.Finish();
                s.Close();
                System.IO.File.Delete(ZipFileName);
                return false;
            }

            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            ZipEntry entry = new ZipEntry(FileName.Split('\\')[FileName.Split('\\').Length - 1]); //FileName);
            entry.DateTime = DateTime.Now;
            entry.Size = fs.Length;

            fs.Close();

            crc.Reset();
            crc.Update(buffer);

            entry.Crc = crc.Value;
            s.PutNextEntry(entry);
            s.Write(buffer, 0, buffer.Length);

            s.Finish();
            s.Close();

            return true;
        }

        /// <summary>
        /// 解压缩一个文件，目标文件名自动在源文件基础上去掉后面的 .zip
        /// </summary>
        /// <param name="ZipFileName">源文件名</param>
        /// <returns>true 为成功</returns>
        public static bool Decompress(string ZipFileName)
        {
            return Decompress(ZipFileName, "");
        }

        /// <summary>
        /// 解压缩一个文件
        /// </summary>
        /// <param name="ZipFileName">源文件名(.zip)</param>
        /// <param name="FileName">目标文件名</param>
        /// <returns>true 为成功</returns>
        public static bool Decompress(string ZipFileName, string FileName)
        {
            FileName = FileName.Trim();

            ZipInputStream s;

            try
            {
                s = new ZipInputStream(System.IO.File.OpenRead(ZipFileName));
            }
            catch
            {
                return false;
            }

            ZipEntry theEntry = s.GetNextEntry();
            if (theEntry == null)
            {
                s.Close();
                return false;
            }

            string DirectoryName = Path.GetDirectoryName((FileName == "") ? ZipFileName : FileName);
            if (FileName == "")
            {
                FileName = Path.Combine(DirectoryName, Path.GetFileName(theEntry.Name));
            }

            if (!Directory.Exists(DirectoryName))
            {
                Directory.CreateDirectory(DirectoryName);
            }

            //解压文件到指定的目录
            FileStream streamWriter = System.IO.File.Create(FileName);
            int size = 2048;
            byte[] data = new byte[size];

            while (true)
            {
                size = s.Read(data, 0, data.Length);
                if (size > 0)
                {
                    streamWriter.Write(data, 0, size);
                }
                else
                {
                    break;
                }
            }

            streamWriter.Close();

            s.Close();

            return true;
        }

        /// <summary>
        /// 压缩多个文件
        /// </summary>
        /// <param name="CompressLevel">压缩级别，0-9，9是最高压缩率</param>
        /// <param name="isWithoutFilePathInfo">文件是否不需要包含进入详细的路径信息，true 则仅仅包含文件名本身信息</param>
        /// <param name="FileNames">多个文件名</param>
        /// <returns>返回二进制流 byte[] 类型，是一个完整的 zip 文件流，可以直接写入文件</returns>
        public static byte[] ZipMultiFiles(int CompressLevel, bool isWithoutFilePathInfo, params string[] FileNames)
        {
            ZipOutputStream zipStream = null;
            FileStream streamWriter = null;
            MemoryStream ms = new MemoryStream();

            bool success = false;

            try
            {
                Crc32 crc32 = new Crc32();

                zipStream = new ZipOutputStream(ms);
                zipStream.SetLevel(CompressLevel);

                foreach (string FileName in FileNames)
                {
                    if (!System.IO.File.Exists(FileName))
                    {
                        continue;
                    }

                    //Read the file to stream
                    streamWriter = System.IO.File.OpenRead(FileName);
                    byte[] buffer = new byte[streamWriter.Length];
                    streamWriter.Read(buffer, 0, buffer.Length);
                    streamWriter.Close();

                    //Specify ZipEntry
                    crc32.Reset();
                    crc32.Update(buffer);
                    ZipEntry zipEntry = new ZipEntry(isWithoutFilePathInfo ? Path.GetFileName(FileName) : FileName);
                    zipEntry.DateTime = DateTime.Now;
                    zipEntry.Size = buffer.Length;
                    zipEntry.Crc = crc32.Value;

                    //Put file info into zip stream
                    zipStream.PutNextEntry(zipEntry);

                    //Put file data into zip stream
                    zipStream.Write(buffer, 0, buffer.Length);
                }

                success = true;
            }
            catch
            {
            }
            finally
            {
                //Clear Resource
                if (streamWriter != null)
                {
                    streamWriter.Close();
                }
                if (zipStream != null)
                {
                    zipStream.Finish();
                    zipStream.Close();
                }
            }

            byte[] Result = null;

            if (success)
            {
                Result = ms.GetBuffer();
            }

            return Result;
        }

        #endregion

        #region 压缩文件夹

        /// <summary>
        /// 文件(夹)压缩、解压缩
        /// </summary>
        public class CompressDirectory
        {
            /// <summary>  
            /// 压缩文件  
            /// </summary>  
            /// <param name="FileNames">要打包的文件列表</param>  
            /// <param name="GzipFileName">目标文件名</param>  
            /// <param name="CompressionLevel">压缩品质级别（0~9）</param>  
            private static void CompressFile(List<FileInfo> FileNames, string GzipFileName, int CompressionLevel)
            {
                ZipOutputStream s = new ZipOutputStream(System.IO.File.Create(GzipFileName));

                try
                {
                    s.SetLevel(CompressionLevel);

                    foreach (FileInfo file in FileNames)
                    {
                        FileStream fs = null;

                        try
                        {
                            fs = file.Open(FileMode.Open, FileAccess.ReadWrite);
                        }
                        catch
                        {
                            continue;
                        }

                        //  方法二，将文件分批读入缓冲区  
                        byte[] data = new byte[2048];
                        int size = 2048;
                        ZipEntry entry = new ZipEntry(Path.GetFileName(file.Name));
                        entry.DateTime = (file.CreationTime > file.LastWriteTime ? file.LastWriteTime : file.CreationTime);
                        s.PutNextEntry(entry);

                        while (true)
                        {
                            size = fs.Read(data, 0, size);

                            if (size <= 0)
                            {
                                break;
                            }

                            s.Write(data, 0, size);
                        }

                        fs.Close();
                    }
                }
                finally
                {
                    s.Finish();
                    s.Close();
                }
            }

            /// <summary>  
            /// 压缩文件夹
            /// </summary>
            /// <param name="DirectoryName">要打包的文件夹</param>
            /// <param name="GzipFileName">目标文件名</param>
            /// <param name="CompressionLevel">压缩品质级别（0~9）</param>
            /// <param name="IsWithDirectory">是否将 DirectoryName 作为相对根目录压缩进入压缩包</param>
            public static void Compress(string DirectoryName, string GzipFileName, int CompressionLevel = 6, bool IsWithDirectory = true)
            {
                DirectoryInfo di = new DirectoryInfo(DirectoryName);

                if (!di.Exists)
                {
                    throw new Exception(DirectoryName + "路径不存在。");
                }

                string entryRoot = "";

                if (di.Parent != null)
                {
                    entryRoot = di.Name + "\\";
                }
                else
                {
                    IsWithDirectory = false;
                }

                if (GzipFileName == string.Empty)
                {
                    if (di.Parent == null)
                    {
                        throw new Exception("压缩整个驱动器根目录，需要指定一个目标 zip 文件名，并保存到其他的磁盘驱动器上。");
                    }

                    GzipFileName = Path.Combine(di.Parent.FullName, di.Name + ".zip");
                }

                FileInfo fi = new FileInfo(GzipFileName);

                if (di.Parent == null)
                {
                    if (di.Root.Name == fi.Directory.Root.Name)
                    {
                        throw new Exception("压缩整个驱动器根目录，需要指定一个目标 zip 文件名，并保存到其他的磁盘驱动器上。");
                    }
                }

                if (fi.Directory.FullName.StartsWith(di.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("目标文件不能保存在要被压缩的文件夹之内。");
                }

                if ((CompressionLevel < 0) || (CompressionLevel > 9))
                {
                    CompressionLevel = 6;
                }

                using (ZipOutputStream zipoutputstream = new ZipOutputStream(System.IO.File.Create(GzipFileName)))
                {
                    zipoutputstream.SetLevel(CompressionLevel);
                    Crc32 crc = new Crc32();
                    Dictionary<string, DateTime> fileList = GetAllFies(DirectoryName);

                    foreach (KeyValuePair<string, DateTime> item in fileList)
                    {
                        FileStream fs = System.IO.File.OpenRead(item.Key.ToString());
                        byte[] buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, buffer.Length);
                        ZipEntry entry = new ZipEntry((IsWithDirectory ? entryRoot : "") + item.Key.Substring(DirectoryName.Length + 1));
                        entry.DateTime = item.Value;
                        entry.Size = fs.Length;
                        fs.Close();
                        crc.Reset();
                        crc.Update(buffer);
                        entry.Crc = crc.Value;
                        zipoutputstream.PutNextEntry(entry);
                        zipoutputstream.Write(buffer, 0, buffer.Length);
                    }
                }
            }

            /// <summary>
            /// 获取所有文件
            /// </summary>
            /// <param name="DirectoryName"></param>
            /// <returns></returns>
            private static Dictionary<string, DateTime> GetAllFies(string DirectoryName)
            {
                Dictionary<string, DateTime> FilesList = new Dictionary<string, DateTime>();
                DirectoryInfo fileDire = new DirectoryInfo(DirectoryName);

                if (!fileDire.Exists)
                {
                    throw new System.IO.FileNotFoundException("目录:" + fileDire.FullName + "没有找到!");
                }

                GetAllDirFiles(fileDire, FilesList);
                GetAllDirsFiles(fileDire.GetDirectories(), FilesList);

                return FilesList;
            }

            /// <summary>  
            /// 获取一个文件夹下的所有文件夹里的文件  
            /// </summary>  
            /// <param name="dirs"></param>  
            /// <param name="filesList"></param>  
            private static void GetAllDirsFiles(DirectoryInfo[] dirs, Dictionary<string, DateTime> filesList)
            {
                foreach (DirectoryInfo dir in dirs)
                {
                    foreach (FileInfo file in dir.GetFiles("*.*"))
                    {
                        if (isIgnoredFile(file))
                        {
                            continue;
                        }

                        filesList.Add(file.FullName, file.LastWriteTime);
                    }

                    GetAllDirsFiles(dir.GetDirectories(), filesList);
                }
            }

            /// <summary>  
            /// 获取一个文件夹下的文件  
            /// </summary>  
            /// <param name="dir">目录名称</param>  
            /// <param name="filesList">文件列表HastTable</param>  
            private static void GetAllDirFiles(DirectoryInfo dir, Dictionary<string, DateTime> filesList)
            {
                foreach (FileInfo file in dir.GetFiles("*.*"))
                {
                    if (isIgnoredFile(file))
                    {
                        continue;
                    }

                    filesList.Add(file.FullName, file.LastWriteTime);
                }
            }

            /// <summary>
            /// 解压缩文件
            /// </summary>
            /// <param name="GzipFile">压缩包文件名</param>
            /// <param name="targetPath">解压缩目标路径</param>
            /// <param name="IsOutputDirectory">是否解压到以zip文件名为的相对根目录之内</param>
            public static void Decompress(string GzipFile, string targetPath, bool IsOutputDirectory = false)
            {
                FileInfo fi = new FileInfo(GzipFile);

                if (!fi.Exists)
                {
                    throw new Exception("文件 " + GzipFile + " 不存在。");
                }

                if (string.IsNullOrEmpty(targetPath))
                {
                    targetPath = fi.Directory.FullName;
                }

                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                if (IsOutputDirectory)
                {
                    targetPath = System.IO.Path.Combine(targetPath, System.IO.Path.GetFileNameWithoutExtension(fi.Name));
                }

                byte[] data = new byte[2048];
                int size = 2048;
                ZipEntry theEntry = null;

                using (ZipInputStream s = new ZipInputStream(System.IO.File.OpenRead(GzipFile)))
                {
                    while ((theEntry = s.GetNextEntry()) != null)
                    {
                        string fileName = System.IO.Path.Combine(targetPath, theEntry.Name);
                        string dirName = System.IO.Path.GetDirectoryName(fileName);

                        if (theEntry.IsFile && isIgnoredFile(fileName))
                        {
                            continue;
                        }

                        if (!Directory.Exists(dirName))
                        {
                            Directory.CreateDirectory(dirName);
                        }

                        if (theEntry.IsDirectory)
                        {
                            continue;
                        }

                        if (theEntry.Name != string.Empty)
                        {
                            //解压文件到指定的目录  
                            using (FileStream streamWriter = System.IO.File.Create(fileName))
                            {
                                while (true)
                                {
                                    size = s.Read(data, 0, data.Length);

                                    if (size <= 0)
                                    {
                                        break;
                                    }

                                    streamWriter.Write(data, 0, size);
                                }

                                streamWriter.Close();
                            }
                        }
                    }

                    s.Close();
                }
            }

            /// <summary>
            /// 是否是应该被忽略掉的文件名
            /// </summary>
            /// <param name="fileName"></param>
            /// <returns></returns>
            private static bool isIgnoredFile(string fileName)
            {
                FileInfo fi = new FileInfo(fileName);

                return isIgnoredFile(fi);
            }

            /// <summary>
            /// 是否是应该被忽略掉的文件名
            /// </summary>
            /// <param name="file"></param>
            /// <returns></returns>
            private static bool isIgnoredFile(FileInfo file)
            {
                return ((string.Compare(file.Name, "Thumbs.db", true) == 0) || (string.Compare(file.Name, "desktop.ini", true) == 0) || (file.Name.StartsWith(".", StringComparison.Ordinal)));
            }
        }

        #endregion

        #region 获取文件的字符集

        private class CharsetDetectionObserver : ICharsetDetectionObserver
        {
            public string Charset = null;

            public void Notify(string charset)
            {
                Charset = charset;
            }
        }

        /// <summary>
        /// 获取文件的字符集
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static System.Text.Encoding GetEncodingOfFile(string fileName)
        {
            int count = 0;
            byte[] buf;

            using (System.IO.FileStream fs = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                buf = new byte[fs.Length];
                count = fs.Read(buf, 0, buf.Length);
            }

            if (count < 1)
            {
                return System.Text.Encoding.Default;
            }

            Detector detect = new Detector();
            CharsetDetectionObserver cdo = new CharsetDetectionObserver();
            detect.Init(cdo);

            if (detect.isAscii(buf, count))
            {
                return System.Text.Encoding.ASCII;
            }
            else
            {
                detect.DoIt(buf, count, true);
                detect.DataEnd();

                if (string.IsNullOrEmpty(cdo.Charset))
                {
                    return System.Text.Encoding.Default;
                }
                else
                {
                    return System.Text.Encoding.GetEncoding(cdo.Charset);
                }
            }
        }

        #endregion
    }
}