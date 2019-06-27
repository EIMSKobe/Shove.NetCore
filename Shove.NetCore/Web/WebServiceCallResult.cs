using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Shove.Web
{
    /// <summary>
    ///WebServiceCallResult 的摘要说明
    /// </summary>
    [Serializable]
    public class WebServiceCallResult
    {
        /// <summary>
        /// Result
        /// </summary>
        public long Result;
        /// <summary>
        /// Description
        /// </summary>
        public string Description;
        /// <summary>
        /// Additional
        /// </summary>
        public object[] Additional;

        /// <summary>
        /// 构造器
        /// </summary>
        public WebServiceCallResult()
        {
            Result = -1;
            Description = "尚未初始化 WebServiceCallResult 的实例";
            Additional = null;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="result"></param>
        public WebServiceCallResult(long result)
        {
            Result = result;
            Description = "";
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="result"></param>
        /// <param name="description"></param>
        public WebServiceCallResult(long result, string description)
        {
            Result = result;
            Description = description;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="result"></param>
        /// <param name="description"></param>
        /// <param name="additional"></param>
        public WebServiceCallResult(long result, string description, params object[] additional)
        {
            Result = result;
            Description = description;

            Additional = additional;
        }

        /// <summary>
        /// 将 WebServiceCallResult 类序列化
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            MemoryStream ms = new MemoryStream();

            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Serialize(ms, this);

            return ms.GetBuffer();
        }

        /// <summary>
        /// 从 WebService 返回的二进制值转换到 WebServiceCallResult 类。
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public WebServiceCallResult Deserialize(byte[] buffer)
        {
            if (buffer == null)
            {
                return null;
            }

            MemoryStream ms = new MemoryStream(buffer);
            BinaryFormatter serializer = new BinaryFormatter();

            WebServiceCallResult wscr = (WebServiceCallResult)serializer.Deserialize(ms);

            return wscr;
        }
    }
}