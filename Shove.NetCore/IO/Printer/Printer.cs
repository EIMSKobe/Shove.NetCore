using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Shove.IO
{
    /// <summary>
    /// Printer 的摘要说明
    /// </summary>
    public class Printer
    {
        /// <summary>
        /// 打印机状态
        /// </summary>
        [FlagsAttribute]
        public enum PrinterEnumFlags
        {
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_DEFAULT = 0x00000001,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_LOCAL = 0x00000002,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_CONNECTIONS = 0x00000004,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_FAVORITE = 0x00000004,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_NAME = 0x00000008,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_REMOTE = 0x00000010,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_SHARED = 0x00000020,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_NETWORK = 0x00000040,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_EXPAND = 0x00004000,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_CONTAINER = 0x00008000,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_ICONMASK = 0x00ff0000,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_ICON1 = 0x00010000,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_ICON2 = 0x00020000,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_ICON3 = 0x00040000,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_ICON4 = 0x00080000,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_ICON5 = 0x00100000,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_ICON6 = 0x00200000,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_ICON7 = 0x00400000,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_ICON8 = 0x00800000,
            /// <summary>
            /// 
            /// </summary>
            PRINTER_ENUM_HIDE = 0x01000000
        }

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct PRINTER_INFO_2
        {
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pServerName;
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pPrinterName;
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pShareName;
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pPortName;
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDriverName;
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pComment;
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pLocation;
            /// <summary>
            /// 
            /// </summary>
            public IntPtr pDevMode;
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pSepFile;
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pPrintProcessor;
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDatatype;
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pParameters;
            /// <summary>
            /// 
            /// </summary>
            public IntPtr pSecurityDescriptor;
            /// <summary>
            /// 
            /// </summary>
            public uint Attributes;
            /// <summary>
            /// 
            /// </summary>
            public uint Priority;
            /// <summary>
            /// 
            /// </summary>
            public uint DefaultPriority;
            /// <summary>
            /// 
            /// </summary>
            public uint StartTime;
            /// <summary>
            /// 
            /// </summary>
            public uint UntilTime;
            /// <summary>
            /// 
            /// </summary>
            public uint Status;
            /// <summary>
            /// 
            /// </summary>
            public uint cJobs;
            /// <summary>
            /// 
            /// </summary>
            public uint AveragePPM;
        }

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumPrinters(PrinterEnumFlags flags, string name, uint level,
                                        IntPtr pPrinterEnum, uint cbBuf, ref uint pcbNeeded,
                                        ref uint pcReturned);

        /// <summary>
        /// 获取打印机列表
        /// </summary>
        /// <returns></returns>
        public static string[] GetPrinter()
        {
            string[] Result = null;
            PRINTER_INFO_2[] printInfo;
            printInfo = EnumPrinters(PrinterEnumFlags.PRINTER_ENUM_LOCAL);

            if (printInfo != null && printInfo.Length >= 0)
            {
                Result = new string[printInfo.Length];
                for (int i = 0; i < printInfo.Length; i++)
                {
                    Result[i] = printInfo[i].pPrinterName;
                }
            }
            return Result;

        }

        /// <summary>
        /// 遍历打印机
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static PRINTER_INFO_2[] EnumPrinters(PrinterEnumFlags flags)
        {
            PRINTER_INFO_2[] Info2 = null;
            uint cbNeeded = 0;
            uint cReturned = 0;
            bool ret = EnumPrinters(flags, null, 2, IntPtr.Zero, 0, ref cbNeeded, ref cReturned);
            IntPtr pAddr = Marshal.AllocHGlobal((int)cbNeeded);
            ret = EnumPrinters(flags, null, 2, pAddr, cbNeeded, ref cbNeeded, ref cReturned);
            if (ret)
            {
                Info2 = new PRINTER_INFO_2[cReturned];
                int offset = pAddr.ToInt32();
                for (int i = 0; i < cReturned; i++)
                {
                    Info2[i] = (PRINTER_INFO_2)Marshal.PtrToStructure(new IntPtr(offset), typeof(PRINTER_INFO_2));
                    offset += Marshal.SizeOf(typeof(PRINTER_INFO_2));
                }
                Marshal.FreeHGlobal(pAddr);
            }
            return Info2;
        }
    }
}
