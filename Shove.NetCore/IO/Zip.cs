using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace Shove.IO
{
    /// <summary>
    /// Zip 压缩、解压，对流、文件进行 Zip 处理的各种方法
    /// </summary>
    public class Zip
    {
        /// <summary>
        /// 压缩为 Zip 流
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public static byte[] Compress(Stream Source)
        {
            Source.Seek(0, SeekOrigin.Begin);
            MemoryStream mMemory = new MemoryStream();
            ZipOutputStream mStream = new ZipOutputStream(mMemory);

            Int32 mSize;

            const int BUFFER_SIZE = 1024 * 10;
            byte[] mWriteData = new byte[BUFFER_SIZE];

            do
            {
                mSize = Source.Read(mWriteData, 0, BUFFER_SIZE);
                if (mSize > 0)
                {
                    mStream.Write(mWriteData, 0, mSize);
                }
            } while (mSize > 0);

            mStream.Finish();

            byte[] arrResult = mMemory.ToArray();

            mStream.Close();
            mStream = null;
            mMemory.Close();
            mMemory = null;

            return arrResult;
        }

        /// <summary>
        /// 将 Zip 压缩流解压
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public static byte[] Decompress(Stream Source)
        {
            Source.Seek(0, SeekOrigin.Begin);
            MemoryStream mMemory = new MemoryStream();
            ZipInputStream mStream = new ZipInputStream(Source);

            Int32 mSize;

            const int BUFFER_SIZE = 1024 * 10;
            byte[] mWriteData = new byte[BUFFER_SIZE];

            do
            {
                mSize = mStream.Read(mWriteData, 0, BUFFER_SIZE);
                if (mSize > 0)
                {
                    mMemory.Write(mWriteData, 0, mSize);
                }
            } while (mSize > 0);

            byte[] arrResult = mMemory.ToArray();

            mStream.Close();
            mMemory.Close();

            return arrResult;
        }
    }
}
