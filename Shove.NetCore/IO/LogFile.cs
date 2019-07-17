using System;
using System.IO;
using System.Threading;

namespace Shove.IO
{
    /// <summary>
    /// 日志文件分割的颗粒度，可选 year, month, day, hour
    /// </summary>
    public enum FileGranularity
    {
        year, month, day, hour
    }

    /// <summary>
    /// Log 的摘要说明
    /// </summary>
    public class Log
    {
        private string pathName;
        private FileGranularity granularity;
        private bool writeTimeInfo;
        private static ReaderWriterLockSlim logWriteLock = new ReaderWriterLockSlim();

        /// <summary>
        /// 构造 Log
        /// </summary>
        /// <param name="pathname">相对于网站、应用程序根目录 App_Log 目录的相对路径，如： System， 就相当于 ~/App_Log/System/、 应用程序根\App_Log\System\</param>
        /// <param name="granularity">日志文件分割的颗粒度，可选 year, month, day, hour</param>
        /// <param name="writeTimeInfo">日志内容是否附加时间刻度信息</param>
        public Log(string pathname, FileGranularity granularity = FileGranularity.day, bool writeTimeInfo = true)
        {
            if (string.IsNullOrEmpty(pathname))
            {
                throw new Exception("没有初始化 Log 类的 PathName 变量");
            }

            this.pathName = System.AppDomain.CurrentDomain.BaseDirectory + "App_Log/" + pathname;
            this.granularity = granularity;
            this.writeTimeInfo = writeTimeInfo;

            if (!Directory.Exists(this.pathName))
            {
                try
                {
                    Directory.CreateDirectory(this.pathName);
                }
                catch { }
            }
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <param name="message"></param>
        public void Write(string message)
        {
            string fileName;
            switch (granularity)
            {
                case FileGranularity.year:
                    fileName = pathName + "/" + DateTime.Now.ToString("yyyy") + ".log";
                    break;
                case FileGranularity.month:
                    fileName = pathName + "/" + DateTime.Now.ToString("yyyy-MM") + ".log";
                    break;
                case FileGranularity.day:
                    fileName = pathName + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                    break;
                default:
                    fileName = pathName + "/" + DateTime.Now.ToString("yyyy-MM-dd_HH") + ".log";
                    break;
            }

            if (writeTimeInfo)
            {
                message = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":" + System.DateTime.Now.Millisecond.ToString() + "\t\t" + message;
            }
            message += "\r\n";

            FileStream fs = null;
            StreamWriter writer = null;

            try
            {
                logWriteLock.EnterWriteLock();

                fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Write);
                writer = new StreamWriter(fs, System.Text.Encoding.GetEncoding("GBK"));
                writer.WriteLine(message);
            }
            catch { }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                    writer.Dispose();
                }
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }

                logWriteLock.ExitWriteLock();
            }
        }
    }
}
