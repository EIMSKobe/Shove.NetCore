using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Shove.IO
{
    /// <summary>
    /// PrintDirect2 的摘要说明
    /// </summary>
    public class PrintDirect2
    {
        #region API

        [StructLayout(LayoutKind.Sequential)]
        private struct OVERLAPPED
        {
            int Internal;
            int InternalHigh;
            int Offset;
            int OffSetHigh;
            int hEvent;
        }

        [DllImport("kernel32.dll")]
        private static extern int CreateFile(string lpFileName, uint dwDesiredAccess, int dwShareMode, int lpSecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, int hTemplateFile);

        [DllImport("kernel32.dll")]
        private static extern bool WriteFile(int hFile, byte[] lpBuffer, int nNumberOfBytesToWrite, out int lpNumberOfBytesWritten, out OVERLAPPED lpOverlapped);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(int hObject);

        #endregion

        /// <summary>
        /// 打印输出到打印机
        /// </summary>
        /// <param name="portName">打印机端口名称</param>
        /// <param name="content">要打印的内容，这里输入字符串，含打印机的控制码</param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static bool Print(string portName, string content, ref string description)
        {
            description = "";
            System.ComponentModel.Win32Exception we = null;

            int iHandle = CreateFile(portName, 0x40000000, 0, 0, 3, 0, 0);

            if (iHandle == -1)
            {
                we = new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                description = we.Message;
                return false;
            }

            int i;
            OVERLAPPED x;
            byte[] bData = System.Text.Encoding.Default.GetBytes(content);
            bool Result = WriteFile(iHandle, bData, bData.Length, out i, out x);

            CloseHandle(iHandle);

            if (Result)
            {
                return true;
            }

            we = new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
            description = we.Message;
            return false;
        }
    }
}
