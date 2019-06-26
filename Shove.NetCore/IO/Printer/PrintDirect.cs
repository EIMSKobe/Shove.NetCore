using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Shove.IO
{
    /// <summary>
    /// Printer 是摘要说明
    /// </summary>
    public class PrintDirect
    {
        /// <summary>
        /// DOCINFO
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct DOCINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDataType;
        }

        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        private static extern long OpenPrinter(string pPrinterName, ref IntPtr phPrinter, int pDefault);
        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        private static extern long StartDocPrinter(IntPtr hPrinter, int Level, ref DOCINFO pDocInfo);

        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern long StartPagePrinter(IntPtr hPrinter);
        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern long WritePrinter(IntPtr hPrinter, string data, int buf, ref int pcWritten);

        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern long EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern long EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern long ClosePrinter(IntPtr hPrinter);

        /// <summary>
        /// 打印输出到打印机
        /// </summary>
        /// <param name="PrinterName">打印机的名称</param>
        /// <param name="DocumentName"></param>
        /// <param name="Content">要打印的内容，这里输入字符串，含打印机的控制码</param>
        /// <param name="pcWritten">实际打印输出的长度</param>
        /// <param name="ErrorDescription"></param>
        /// <returns></returns>
        public static bool Print(string PrinterName, string DocumentName, string Content, ref int pcWritten, ref string ErrorDescription)
        {
            System.IntPtr lhPrinter = new System.IntPtr();
            DOCINFO di = new DOCINFO();
            long Result = 0;

            System.ComponentModel.Win32Exception we = null;
            pcWritten = 0;
            ErrorDescription = "";

            di.pDocName = DocumentName;
            di.pDataType = "RAW";


            Result = OpenPrinter(PrinterName, ref lhPrinter, 0);
            if (Result == 0)
            {
                we = new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                ErrorDescription = we.Message;
                return false;
            }

            Result = StartDocPrinter(lhPrinter, 1, ref di);
            if (Result == 0)
            {
                we = new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                ErrorDescription = we.Message;
                return false;
            }

            Result = StartPagePrinter(lhPrinter);
            if (Result == 0)
            {
                we = new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                ErrorDescription = we.Message;
                return false;
            }

            Result = WritePrinter(lhPrinter, Content, Content.Length, ref pcWritten);
            if (Result == 0)
            {
                we = new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                ErrorDescription = we.Message;
                return false;
            }

            Result = EndPagePrinter(lhPrinter);
            if (Result == 0)
            {
                we = new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                ErrorDescription = we.Message;
                return false;
            } 
            
            Result = EndDocPrinter(lhPrinter);
            if (Result == 0)
            {
                we = new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                ErrorDescription = we.Message;
                return false;
            }
            
            Result = ClosePrinter(lhPrinter);
            if (Result == 0)
            {
                we = new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                ErrorDescription = we.Message;
                return false;
            }

            return true;
        }
    }
}
