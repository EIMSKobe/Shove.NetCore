using System;
using System.Collections.Generic;
using System.Web;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 视频信息
    /// </summary>
    public class VideoMessage:Message
    {
        private string _MediaId;
        /// <summary>
        /// 视频消息媒体id，可以调用多媒体文件下载接口拉取数据。  
        /// </summary>
        public string MediaId
        {
            get { return _MediaId; }
        }

        private string _Title;
        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get { return _Title; }
        }

        private string _Description;
        /// <summary>
        ///  描述
        /// </summary>
        public string Description
        {
            get { return _Description; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_MediaId">视频消息媒体id</param>
        /// <param name="_Title"></param>
        /// <param name="_Description"></param>
        public VideoMessage(string _MediaId, string _Title, string _Description)
            : base("video")
        {
            this._MediaId = _MediaId;
            this._Title = _Title;
            this._Description = _Description;
        }
    }
}