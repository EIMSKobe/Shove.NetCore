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
    /// File ��ժҪ˵����
    /// </summary>
    public class File
    {
        #region ��ȡ����Ŀ¼�µ��ļ��б�

        /// <summary>
        /// ȡ�������� Path Ŀ¼�µ��ļ��б�
        /// </summary>
        /// <param name="Path">�������ϵľ���·��������ǰ�� Server.MapPath ȡ������·���ٴ���</param>
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
        /// ȡ�������� StartDirName Ŀ¼�µ��ļ��б�����������Ŀ¼�µ��ļ�
        /// </summary>
        /// <param name="StartDirName">�������ϵľ���·��������ǰ�� Server.MapPath ȡ������·���ٴ���</param>
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
        /// GetFileListWithSubDir �����ĵݹ��ӷ���
        /// </summary>
        /// <param name="Dir">Ŀ¼</param>
        /// <param name="al">����ļ��ļ���</param>
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

        #region ��д�ļ�

        /// <summary>
        /// ���ļ�
        /// </summary>
        /// <param name="FileName">�ļ���</param>
        /// <returns>�ļ������ַ���</returns>
        public static string ReadFile(string FileName)
        {
            return System.IO.File.ReadAllText(FileName, System.Text.Encoding.Default);
        }

        /// <summary>
        /// д�ļ�������ļ������ڣ��������ļ��������д���ļ�
        /// </summary>
        /// <param name="FileName">�ļ���</param>
        /// <param name="Content">д�������</param>
        /// <returns>true Ϊ�ɹ�</returns>
        public static bool WriteFile(string FileName, string Content)
        {
            return WriteFile(FileName, Content, System.Text.Encoding.Default);
        }

        /// <summary>
        /// д�ļ�������ļ������ڣ��������ļ��������д���ļ�(�����ƶ����ַ�����)
        /// </summary>
        /// <param name="FileName">�ļ���</param>
        /// <param name="Content">д�������</param>
        /// <param name="encoding">�ַ�����</param>
        /// <returns>true Ϊ�ɹ�</returns>
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
        /// Copy File, �Զ�����Ŀ���ļ���
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="overwrite"></param>
        public static void CopyFile(string src, string dest, bool overwrite)
        {
            if (!System.IO.File.Exists(src))
            {
                throw new Exception("Դ�ļ� " + src + " �����ڡ�");
            }

            FileInfo fi = new FileInfo(dest);

            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            System.IO.File.Copy(src, dest, overwrite);
        }

        /// <summary>
        /// ����Ŀ¼һ����(�ݹ�ʵ��)
        /// </summary>
        /// <param name="src">ԴĿ¼</param>
        /// <param name="dest">Ŀ��Ŀ¼</param>
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
        /// ��ȡָ���ļ���ռ�õĿռ��С
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

        #region ѹ���ļ�

        /// <summary>
        /// ѹ��һ���ļ���Ŀ���ļ����Զ���Դ�ļ�������� .zip
        /// </summary>
        /// <param name="FileName">Դ�ļ���</param>
        /// <returns>true Ϊ�ɹ�</returns>
        public static bool Compress(string FileName)
        {
            return Compress(FileName, "");
        }

        /// <summary>
        /// ѹ��һ���ļ�
        /// </summary>
        /// <param name="FileName">Դ�ļ���</param>
        /// <param name="ZipFileName">Ŀ���ļ���(.zip)</param>
        /// <returns>true Ϊ�ɹ�</returns>
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

            //��ѹ���ļ�
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
        /// ��ѹ��һ���ļ���Ŀ���ļ����Զ���Դ�ļ�������ȥ������� .zip
        /// </summary>
        /// <param name="ZipFileName">Դ�ļ���</param>
        /// <returns>true Ϊ�ɹ�</returns>
        public static bool Decompress(string ZipFileName)
        {
            return Decompress(ZipFileName, "");
        }

        /// <summary>
        /// ��ѹ��һ���ļ�
        /// </summary>
        /// <param name="ZipFileName">Դ�ļ���(.zip)</param>
        /// <param name="FileName">Ŀ���ļ���</param>
        /// <returns>true Ϊ�ɹ�</returns>
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

            //��ѹ�ļ���ָ����Ŀ¼
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
        /// ѹ������ļ�
        /// </summary>
        /// <param name="CompressLevel">ѹ������0-9��9�����ѹ����</param>
        /// <param name="isWithoutFilePathInfo">�ļ��Ƿ���Ҫ����������ϸ��·����Ϣ��true ����������ļ���������Ϣ</param>
        /// <param name="FileNames">����ļ���</param>
        /// <returns>���ض������� byte[] ���ͣ���һ�������� zip �ļ���������ֱ��д���ļ�</returns>
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

        #region ѹ���ļ���

        /// <summary>
        /// �ļ�(��)ѹ������ѹ��
        /// </summary>
        public class CompressDirectory
        {
            /// <summary>  
            /// ѹ���ļ�  
            /// </summary>  
            /// <param name="FileNames">Ҫ������ļ��б�</param>  
            /// <param name="GzipFileName">Ŀ���ļ���</param>  
            /// <param name="CompressionLevel">ѹ��Ʒ�ʼ���0~9��</param>  
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

                        //  �����������ļ��������뻺����  
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
            /// ѹ���ļ���
            /// </summary>
            /// <param name="DirectoryName">Ҫ������ļ���</param>
            /// <param name="GzipFileName">Ŀ���ļ���</param>
            /// <param name="CompressionLevel">ѹ��Ʒ�ʼ���0~9��</param>
            /// <param name="IsWithDirectory">�Ƿ� DirectoryName ��Ϊ��Ը�Ŀ¼ѹ������ѹ����</param>
            public static void Compress(string DirectoryName, string GzipFileName, int CompressionLevel = 6, bool IsWithDirectory = true)
            {
                DirectoryInfo di = new DirectoryInfo(DirectoryName);

                if (!di.Exists)
                {
                    throw new Exception(DirectoryName + "·�������ڡ�");
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
                        throw new Exception("ѹ��������������Ŀ¼����Ҫָ��һ��Ŀ�� zip �ļ����������浽�����Ĵ����������ϡ�");
                    }

                    GzipFileName = Path.Combine(di.Parent.FullName, di.Name + ".zip");
                }

                FileInfo fi = new FileInfo(GzipFileName);

                if (di.Parent == null)
                {
                    if (di.Root.Name == fi.Directory.Root.Name)
                    {
                        throw new Exception("ѹ��������������Ŀ¼����Ҫָ��һ��Ŀ�� zip �ļ����������浽�����Ĵ����������ϡ�");
                    }
                }

                if (fi.Directory.FullName.StartsWith(di.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Ŀ���ļ����ܱ�����Ҫ��ѹ�����ļ���֮�ڡ�");
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
            /// ��ȡ�����ļ�
            /// </summary>
            /// <param name="DirectoryName"></param>
            /// <returns></returns>
            private static Dictionary<string, DateTime> GetAllFies(string DirectoryName)
            {
                Dictionary<string, DateTime> FilesList = new Dictionary<string, DateTime>();
                DirectoryInfo fileDire = new DirectoryInfo(DirectoryName);

                if (!fileDire.Exists)
                {
                    throw new System.IO.FileNotFoundException("Ŀ¼:" + fileDire.FullName + "û���ҵ�!");
                }

                GetAllDirFiles(fileDire, FilesList);
                GetAllDirsFiles(fileDire.GetDirectories(), FilesList);

                return FilesList;
            }

            /// <summary>  
            /// ��ȡһ���ļ����µ������ļ�������ļ�  
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
            /// ��ȡһ���ļ����µ��ļ�  
            /// </summary>  
            /// <param name="dir">Ŀ¼����</param>  
            /// <param name="filesList">�ļ��б�HastTable</param>  
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
            /// ��ѹ���ļ�
            /// </summary>
            /// <param name="GzipFile">ѹ�����ļ���</param>
            /// <param name="targetPath">��ѹ��Ŀ��·��</param>
            /// <param name="IsOutputDirectory">�Ƿ��ѹ����zip�ļ���Ϊ����Ը�Ŀ¼֮��</param>
            public static void Decompress(string GzipFile, string targetPath, bool IsOutputDirectory = false)
            {
                FileInfo fi = new FileInfo(GzipFile);

                if (!fi.Exists)
                {
                    throw new Exception("�ļ� " + GzipFile + " �����ڡ�");
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
                            //��ѹ�ļ���ָ����Ŀ¼  
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
            /// �Ƿ���Ӧ�ñ����Ե����ļ���
            /// </summary>
            /// <param name="fileName"></param>
            /// <returns></returns>
            private static bool isIgnoredFile(string fileName)
            {
                FileInfo fi = new FileInfo(fileName);

                return isIgnoredFile(fi);
            }

            /// <summary>
            /// �Ƿ���Ӧ�ñ����Ե����ļ���
            /// </summary>
            /// <param name="file"></param>
            /// <returns></returns>
            private static bool isIgnoredFile(FileInfo file)
            {
                return ((string.Compare(file.Name, "Thumbs.db", true) == 0) || (string.Compare(file.Name, "desktop.ini", true) == 0) || (file.Name.StartsWith(".", StringComparison.Ordinal)));
            }
        }

        #endregion

        #region ��ȡ�ļ����ַ���

        private class CharsetDetectionObserver : ICharsetDetectionObserver
        {
            public string Charset = null;

            public void Notify(string charset)
            {
                Charset = charset;
            }
        }

        /// <summary>
        /// ��ȡ�ļ����ַ���
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