using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Microsoft.AspNetCore.Http;
using ZXing;

namespace Shove.Gateways.Weixin.Shanghu
{
    /// <summary>
    /// Summary description for Pay
    /// Wechart 支付操作类
    /// </summary>
    public class Pay
    {

        #region Request 

        /// <summary>
        /// 生成一笔支付订单
        /// </summary>
        /// <param name="outTradeNo">商户系统内部的订单号,32个字符内、可包含字母</param>
        /// <param name="totalFeeCent">
        ///     订单总金额，只能为整数(***注意：金额单位为 "分")
        ///     <para>*注意：金额单位为 "分"</para>
        /// </param>
        /// <param name="spbillCreateIp">APP和网页支付提交用户端ip，Native支付填调用微信支付API的机器IP</param>
        /// <param name="body">商品或支付单简要描述</param>
        /// <param name="attach">在查询API和支付通知中原样返回，该字段主要用于商户携带订单的自定义数据</param>
        /// <param name="notifyUrl">通知地址 (接收微信支付异步通知回调地址)</param>
        /// <param name="payQrCodeImagePath">支付二维码图片绝对地址(如：d:\\wwwroot\\site\\images\\qrcode\\order.png)
        ///     <para>默认尺寸 430*430 (wx.qq.com)</para>
        /// </param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns></returns>
        public static bool Payment(string outTradeNo, int totalFeeCent, string spbillCreateIp,  string body, string attach, string notifyUrl, string payQrCodeImagePath, ref string errorDescription)
        {
            #region Check Agraments
            
            if (string.IsNullOrEmpty(payQrCodeImagePath))
            {
                errorDescription = "支付二维码图片路径错误";
                return false;
            }

            if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(payQrCodeImagePath)))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(payQrCodeImagePath));
                }
                catch
                {
                    errorDescription = "支付二维码图片目录错误或权限不足。";
                    return false;
                }
            }

            if (System.IO.File.Exists(payQrCodeImagePath))
            {
                errorDescription = "支付二维码图片已存在。";
                return false;
            }

            #endregion

            string qrCodeUrl = string.Empty;

            if(!Payment(outTradeNo, totalFeeCent, spbillCreateIp, body, attach, notifyUrl, ref qrCodeUrl, ref errorDescription))
            {
                return false;
            }

            #region Build Images File

            try
            {
                System.Drawing.Bitmap bitmap = Shove.InformationCode.QrCode.CreateCode(qrCodeUrl, BarcodeFormat.QR_CODE, 430, 430, System.Drawing.Imaging.ImageFormat.Png);
                bitmap.Save(payQrCodeImagePath);
            }
            catch (Exception ex)
            {
                errorDescription = "微信支付发起成功，但生成二维码失败或文件权限不足。原因：" + ex.ToString();
                return true;
            }

            #endregion

            return true;
        }


        /// <summary>
        /// 生成一笔支付订单
        /// </summary>
        /// <param name="outTradeNo">商户系统内部的订单号,32个字符内、可包含字母</param>
        /// <param name="totalFeeCent">
        ///     订单总金额，只能为整数(***注意：金额单位为 "分")
        ///     <para>*注意：金额单位为 "分"</para>
        /// </param>
        /// <param name="spbillCreateIp">APP和网页支付提交用户端ip，Native支付填调用微信支付API的机器IP</param>
        /// <param name="body">商品或支付单简要描述</param>
        /// <param name="attach">在查询API和支付通知中原样返回，该字段主要用于商户携带订单的自定义数据</param>
        /// <param name="notifyUrl">通知地址 (接收微信支付异步通知回调地址)</param>
        /// <param name="qrCodeUrl">支付二维码链接地址</param>
        /// <param name="errorDescription">错误描述</param>
        /// <returns></returns>
        public static bool Payment(string outTradeNo, int totalFeeCent, string spbillCreateIp, string body, string attach, string notifyUrl, ref string qrCodeUrl, ref string errorDescription)
        {
            #region Check Agraments

            if (string.IsNullOrEmpty(Utility.AppID))
            {
                errorDescription = "发生错误:appid为null,请查看页面的Load事件是否调用了Utility.InitializePayConfig()方法";
                return false;
            }

            if (string.IsNullOrEmpty(Utility.MchID))
            {
                errorDescription = "发生错误:mchid为null,请查看页面的Load事件是否调用了Utility.InitializePayConfig()方法";
                return false;
            }

            if (string.IsNullOrEmpty(Utility.PayKey))
            {
                errorDescription = "发生错误:paykey为null,请查看页面的Load事件是否调用了Utility.InitializePayConfig()方法";
                return false;
            }

            if (string.IsNullOrEmpty(outTradeNo))
            {
                errorDescription = "商户系统内部的订单号错误";
                return false;
            }

            if (totalFeeCent < 1)
            {
                errorDescription = "订单总金额不能小于1分";
                return false;
            }

            if (string.IsNullOrEmpty(spbillCreateIp) || spbillCreateIp.Split('.').Length < 4 || spbillCreateIp.Length < 6)
            {
                errorDescription = "用户客户端Ip错误";
                return false;
            }

            #endregion

            errorDescription = string.Empty;
            Utility utility = new Utility();

            // 商户支付密钥 Key
            string appSecretKey = Utility.PayKey;
            // 微信分配的公众账号ID（企业号corpid即为此appId）
            string appid = Utility.AppID;
            // 微信支付分配的商户号
            string mchId = Utility.MchID;
            // 随机字符串，不长于32位
            string nonceStr = Guid.NewGuid().ToString("N");
            // 交易类型 (取值如下：JSAPI，NATIVE，APP，WAP)
            // JSAPI--公众号支付、NATIVE--原生扫码支付、APP--app支付、WAP--手机浏览器H5支付，
            // 统一下单接口trade_type的传参可参考这里 MICROPAY--刷卡支付，刷卡支付有单独的支付接口，不调用统一下单接口
            string tradeType = "NATIVE";
            // 签名 MD5(agraments + key)
            string sign = string.Empty;

            string xmlResult = string.Empty;
            string _errorDescription = string.Empty;


            SortedDictionary<string, string> sParaTemp = new SortedDictionary<string, string>();
            sParaTemp.Add("appid", appid);
            sParaTemp.Add("attach", attach);
            sParaTemp.Add("body", body);
            sParaTemp.Add("mch_id", mchId);
            sParaTemp.Add("nonce_str", nonceStr);
            sParaTemp.Add("notify_url", notifyUrl);
            sParaTemp.Add("out_trade_no", outTradeNo);
            sParaTemp.Add("spbill_create_ip", spbillCreateIp);
            sParaTemp.Add("total_fee", totalFeeCent.ToString());
            sParaTemp.Add("trade_type", tradeType);

            // 制作签名
            sign = utility.CreateSign(sParaTemp, appSecretKey).ToUpper();

            string data = @"<xml>
                                <appid>{0}</appid>
                                <attach>{1}</attach>
                                <body>{2}</body>
                                <mch_id>{3}</mch_id>
                                <nonce_str>{4}</nonce_str>
                                <notify_url>{5}</notify_url>
                                <out_trade_no>{6}</out_trade_no>
                                <spbill_create_ip>{7}</spbill_create_ip>
                                <total_fee>{8}</total_fee>
                                <trade_type>{9}</trade_type>
                                <sign>{10}</sign>
                            </xml>";

            data = string.Format(data, appid, attach, body, mchId, nonceStr, notifyUrl, outTradeNo, spbillCreateIp, totalFeeCent, tradeType, sign);

            if (!utility.Post(Utility.WeChatPayUrl, data, ref _errorDescription, ref xmlResult))
            {
                errorDescription = "发生错误:请求到微信网关失败。错误源:" + _errorDescription;
                return false;
            }

            if (string.IsNullOrEmpty(xmlResult))
            {
                errorDescription = "发生错误:请求到微信网关返回结果为空。";
                return false;
            }

            #region  Analysis

            System.Xml.XPath.XPathDocument xmldoc = null;

            try
            {
                xmldoc = new System.Xml.XPath.XPathDocument(new System.IO.StringReader(xmlResult));
            }
            catch (Exception ex)
            {
                errorDescription = "发生错误:请求微信网关成功，但返回XML解析失败:" + ex.ToString() + "，解析源：" + xmlResult;
                return false;
            }

            System.Xml.XPath.XPathNavigator nav = xmldoc.CreateNavigator();
            string returnCode, returnMsg, returnAppid, returnMchId, returnNonceStr, returnSign, returnResultCode, returnPrepayId, returnTradeType, returnCodeUrl;

            try
            {
                returnCode = nav.SelectSingleNode("xml/return_code").Value;
                returnMsg = nav.SelectSingleNode("xml/return_msg").Value;
            }
            catch (Exception ex)
            {
                errorDescription = "发生错误:请求微信网关成功，但返回XML解析字段错误：" + ex.ToString() + "，解析源：" + xmlResult;
                return false;
            }

            if (returnCode.ToUpper() != "SUCCESS")
            {
                errorDescription = "发生错误:请求微信网关成功，但微信接口返回错误。错误状态码：" + returnCode + "，描述：" + returnMsg;
                return false;
            }

            try
            {
                returnAppid = nav.SelectSingleNode("xml/appid").Value;
                returnMchId = nav.SelectSingleNode("xml/mch_id").Value;
                returnNonceStr = nav.SelectSingleNode("xml/nonce_str").Value;
                returnResultCode = nav.SelectSingleNode("xml/result_code").Value;
                returnPrepayId = nav.SelectSingleNode("xml/prepay_id").Value;
                returnTradeType = nav.SelectSingleNode("xml/trade_type").Value;
                returnCodeUrl = nav.SelectSingleNode("xml/code_url").Value;

                returnSign = nav.SelectSingleNode("xml/sign").Value;
            }
            catch (Exception ex)
            {
                errorDescription = "发生错误:请求微信网关成功，但返回XML解析字段错误(2)：" + ex.ToString() + "，解析源：" + xmlResult;
                return false;
            }

            #endregion

            #region Sign Check

            if (appid != returnAppid || mchId != returnMchId)
            {
                errorDescription = "发生错误:请求微信网关成功，但返回的商户号或凭据不一致。";
                return false;
            }

            sParaTemp = new SortedDictionary<string, string>();
            sParaTemp.Add("return_code", returnCode);
            sParaTemp.Add("return_msg", returnMsg);
            sParaTemp.Add("appid", returnAppid);
            sParaTemp.Add("mch_id", returnMchId);
            sParaTemp.Add("nonce_str", returnNonceStr);
            sParaTemp.Add("result_code", returnResultCode);
            sParaTemp.Add("prepay_id", returnPrepayId);
            sParaTemp.Add("trade_type", returnTradeType);
            sParaTemp.Add("code_url", returnCodeUrl);

            sign = utility.CreateSign(sParaTemp, appSecretKey).ToUpper();

            if (returnSign.Trim().ToLower() != sign.Trim().ToLower())
            {
                errorDescription = "发生错误:请求微信网关成功，但数据签名不一致。";
                return false;
            }

            #endregion

            qrCodeUrl = returnCodeUrl;
            return true;
        }

        #endregion

        #region CallBack
        
        /// <summary>
        /// 委托回调函数
        /// </summary>
        /// <param name="returnXml">微信接口通知的XML信息</param>
        /// <returns></returns>
        public delegate bool DelegateHandleBusiness(string returnXml);

        /// <summary>
        /// 微信接口通知处理函数
        /// </summary>
        /// <param name="context"></param>
        /// <param name="HandleBusiness">委托函数</param>
        /// <param name="errorDescription">返回的错误描述</param>
        /// <returns></returns>
        public static bool Handle(HttpContext context, DelegateHandleBusiness HandleBusiness, ref string errorDescription)
        {
            errorDescription = string.Empty;
            Shove.IO.Log log = new Shove.IO.Log("WeixinShanghu");

            #region Check Agrament

            Stream stream = null;//[shove] context.Request.InputStream;

            if (stream.Length < 1)
            {
                errorDescription = "数据流为空";
                return false;
            }

            string requestContent = "";
            byte[] b = new byte[stream.Length];
            stream.Read(b, 0, (int)stream.Length);
            requestContent = System.Text.Encoding.UTF8.GetString(b);

            if (string.IsNullOrEmpty(requestContent))
            {
                errorDescription = "数据流为空";
                return false;
            }

            log.Write("微信支付通知数据：" + requestContent);

            if (string.IsNullOrEmpty(Utility.AppID))
            {
                errorDescription = "发生错误:appid为null,请查看页面的Load事件是否调用了Utility.InitializePayConfig()方法";
                return false;
            }

            if (string.IsNullOrEmpty(Utility.MchID))
            {
                errorDescription = "发生错误:mchid为null,请查看页面的Load事件是否调用了Utility.InitializePayConfig()方法";
                return false;
            }

            if (string.IsNullOrEmpty(Utility.PayKey))
            {
                errorDescription = "发生错误:paykey为null,请查看页面的Load事件是否调用了Utility.InitializePayConfig()方法";
                return false;
            }

            /*
             *  返回 XMl 示例
             *<xml>
             *   <appid><![CDATA[wxdf9f01a2ae215175]]></appid>
             *   <attach><![CDATA[Pay_Data]]></attach>
             *   <bank_type><![CDATA[CFT]]></bank_type>
             *   <cash_fee><![CDATA[1]]></cash_fee>
             *   <fee_type><![CDATA[CNY]]></fee_type>
             *   <is_subscribe><![CDATA[Y]]></is_subscribe>
             *   <mch_id><![CDATA[1243322702]]></mch_id>
             *   <nonce_str><![CDATA[bda67e73dbf446498c87717ccaa2d4dc]]></nonce_str>
             *   <openid><![CDATA[obIe-jsl8G_M36uOMV-qBgKSnFGo]]></openid>
             *   <out_trade_no><![CDATA[20150909110449679]]></out_trade_no>
             *   <result_code><![CDATA[SUCCESS]]></result_code>
             *   <return_code><![CDATA[SUCCESS]]></return_code>
             *   <sign><![CDATA[2FBEAF62577AADAC3B9C841BAB2539E6]]></sign>
             *   <time_end><![CDATA[20150909111319]]></time_end>
             *   <total_fee>1</total_fee>
             *   <trade_type><![CDATA[NATIVE]]></trade_type>
             *   <transaction_id><![CDATA[1003250181201509090826253035]]></transaction_id>
             *</xml>
             */

            #endregion

            // 解析
            System.Xml.XPath.XPathDocument xmldoc = null;

            try
            {
                xmldoc = new System.Xml.XPath.XPathDocument(new System.IO.StringReader(requestContent));
            }
            catch (Exception ex)
            {
                errorDescription = "解析通知数据 XML 错误。";
                log.Write("解析通知数据xml错误。" + ex.ToString());

                return false;
            }

            System.Xml.XPath.XPathNavigator nav = xmldoc.CreateNavigator();
            string appid, attach, bankType, cashFee, feeType, isSubscribe, mchId, nonceStr, openid, outTradeＮo, 
                   resultCode, returnCode, timeEnd, TotalFee, TradeType, TransactionId, sign;

            try
            {
                resultCode = nav.SelectSingleNode("xml/result_code").Value;
                returnCode = nav.SelectSingleNode("xml/return_code").Value;
            }
            catch(Exception ex)
            {
                errorDescription = "解析通知数据 XML 异常。";
                log.Write("解析通知数据xml异常。" + ex.ToString());

                return false;
            }

            if(resultCode.ToUpper() != "SUCCESS")
            {
                errorDescription = "发生错误:请求微信网关成功，但微信接口返回错误。错误状态码：" + resultCode + "，描述：" + returnCode;
                log.Write("发生错误:请求微信网关成功，但微信接口返回错误。错误状态码：" + resultCode + "，描述：" + returnCode);

                return false;
            }

            try
            {
                appid = nav.SelectSingleNode("xml/appid").Value;
                bankType = nav.SelectSingleNode("xml/bank_type").Value;
                cashFee = nav.SelectSingleNode("xml/cash_fee").Value;
                feeType = nav.SelectSingleNode("xml/fee_type").Value;
                isSubscribe = nav.SelectSingleNode("xml/is_subscribe").Value;
                mchId = nav.SelectSingleNode("xml/mch_id").Value;
                nonceStr = nav.SelectSingleNode("xml/nonce_str").Value;
                openid = nav.SelectSingleNode("xml/openid").Value;
                outTradeＮo = nav.SelectSingleNode("xml/out_trade_no").Value;
                timeEnd = nav.SelectSingleNode("xml/time_end").Value;
                TotalFee = nav.SelectSingleNode("xml/total_fee").Value;
                TradeType = nav.SelectSingleNode("xml/trade_type").Value;
                TransactionId = nav.SelectSingleNode("xml/transaction_id").Value;

                sign = nav.SelectSingleNode("xml/sign").Value;
            }
            catch (Exception ex)
            {
                errorDescription = "解析通知数据 XML 异常。";
                log.Write("解析通知数据xml异常。" + ex.ToString());

                return false;
            }

            try
            {
                // attach 接口附加返回参数，防止为空
                attach = nav.SelectSingleNode("xml/attach").Value;
            }
            catch { attach = string.Empty; }

            SortedDictionary<string, string> sParaTemp = new SortedDictionary<string, string>();
            sParaTemp.Add("appid", appid);
            sParaTemp.Add("attach", attach);
            sParaTemp.Add("bank_type", bankType);
            sParaTemp.Add("cash_fee", cashFee);
            sParaTemp.Add("fee_type", feeType);
            sParaTemp.Add("is_subscribe", isSubscribe);
            sParaTemp.Add("mch_id", mchId);
            sParaTemp.Add("nonce_str", nonceStr);
            sParaTemp.Add("openid", openid);
            sParaTemp.Add("out_trade_no", outTradeＮo);
            sParaTemp.Add("result_code", resultCode);
            sParaTemp.Add("return_code", returnCode);
            sParaTemp.Add("time_end", timeEnd);
            sParaTemp.Add("total_fee", TotalFee);
            sParaTemp.Add("trade_type", TradeType);
            sParaTemp.Add("transaction_id", TransactionId);

            Utility utility = new Utility();

            string _sign = utility.CreateSign(sParaTemp, Utility.PayKey).ToUpper();

            if (sign.Trim().ToUpper() != _sign)
            {
                errorDescription = "微信响应结果签名错误";
                log.Write("签名错误。微信签名结果：" + sign + "，系统签名：" + _sign);

                return false;
            }

            string returnWeChartData = "<xml><return_code><![CDATA[SUCCESS]]></return_code><return_msg><![CDATA[OK]]></return_msg></xml>";

            if(!HandleBusiness(requestContent))
            {
                log.Write("发生错误:调用HandleBusiness()方法错误。等待微信重新通知。");
                return false;
            }

            // 响应微信支付，订单处理成功
            context.Response.WriteAsync(returnWeChartData);
            return true;
        }
        
        #endregion
    }
}