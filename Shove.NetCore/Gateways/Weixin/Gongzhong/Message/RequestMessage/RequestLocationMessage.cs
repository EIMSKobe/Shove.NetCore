using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    /// 从微信发来的地理位置消息
    /// </summary>
    public class RequestLocationMessage : Message
    {
        private string _Location_X;
        /// <summary>
        /// 地理位置纬度
        /// </summary>
        public string Location_X
        {
            get { return _Location_X; }
        }

        private string _Location_Y;
        /// <summary>
        /// 地理位置经度
        /// </summary>
        public string Location_Y
        {
            get { return _Location_Y; }
        }

        private string _Scale;
        /// <summary>
        /// 地图缩放大小
        /// </summary>
        public string Scale
        {
            get { return _Scale; }
        }

        private string _Label;
        /// <summary>
        /// 地理位置信息
        /// </summary>
        public string Label
        {
            get { return _Label; }
        }

        private string _MsgId;
        /// <summary>
        /// 消息id，64位整型
        /// </summary>
        public string MsgId
        {
            get { return _MsgId; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="location_X">地理位置纬度</param>
        /// <param name="location_Y">地理位置经度</param>
        /// <param name="scale">地图缩放大小</param>
        /// <param name="label">地理位置信息</param>
        /// <param name="msgId">消息id，64位整型</param>
        /// <param name="_DeveloperId">开发者微信号</param>
        /// <param name="_OpenID">发送方微信号openid</param>
        /// <param name="createTime">创建时间</param>
        /// <param name="msgType">消息类型</param>
        public RequestLocationMessage(string location_X, string location_Y, string scale, string label,
            string msgId, string _OpenID, string _DeveloperId, DateTime createTime, string msgType)
            : base(_OpenID, _DeveloperId, createTime, msgType)
        {
            this._Label = label;
            this._Location_X = location_X;
            this._Location_Y = location_Y;
            this._MsgId = msgId;
            this._Scale = scale;
        }
    }
}
