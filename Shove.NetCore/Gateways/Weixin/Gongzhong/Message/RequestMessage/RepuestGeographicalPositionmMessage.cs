using System;
using System.Collections.Generic;
using System.Web;

namespace Shove.Gateways.Weixin.Gongzhong
{
    /// <summary>
    ///上报地理位置
    /// </summary>
    public class RepuestGeographicalPositionmMessage : Message
    {
        private string _Latitude;
        /// <summary>
        ///地理位置纬度  
        /// </summary>
        public string Latitude
        {
            get { return _Latitude; }
        }

        private string _Longitude;
        /// <summary>
        ///地理位置经度  
        /// </summary>
        public string Longitude
        {
            get { return _Longitude; }
        }

        private string _Precision;
        /// <summary>
        ///地理位置精度  
        /// </summary>
        public string Precision
        {
            get { return _Precision; }
        }

        private string _Event;
        /// <summary>
        /// 事件类型，subscribe(订阅)、unsubscribe(取消订阅)、CLICK(自定义菜单点击事件) LOCATION （上报地理位置）
        /// </summary>
        public string Event
        {
            get { return _Event; }
        }

        private string location;
        /// <summary>
        /// 详细地址
        /// </summary>
        public string Location
        {
            get
            {
                return location;
            }
            set { location = value; }
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        /// <param name="_Latitude">地理位置纬度 </param>
        /// <param name="_Longitude">地理位置经度 </param>
        /// <param name="_Precision">地理位置精度  </param>
        /// <param name="_DeveloperId">开发者微信号</param>
        /// <param name="_OpenID">发送方微信号openid</param>
        /// <param name="createTime">创建时间</param>
        /// <param name="msgType">消息类型</param>
        /// <param name="_Event">事件类型</param>
        public RepuestGeographicalPositionmMessage(string _Event, string _Latitude, string _Longitude, string _Precision, string _OpenID, string _DeveloperId, DateTime createTime, string msgType)
            : base(_OpenID, _DeveloperId, createTime, msgType)
        {
            this._Latitude = _Latitude;
            this._Longitude = _Longitude;
            this._Precision = _Precision;
            this._Event = _Event;
            this.location = Utility.GetDetailedAddress_API(_Latitude, _Longitude);
        }
    }
}