using System;
using System.Text;
using System.Data;
using System.Security.Cryptography;
using System.Collections;
using System.IO;
using System.Net;
using System.Web;
using System.Xml;

using Shove.Database;
using Shove.Web;

namespace Shove.Alipay
{
    /// <summary>
    /// Alipay 各种接口需要的公共方法
    /// </summary>
    public class Alipay
    {
        private string PartnerID = "";
        private string PartnerKey = "";
        private string ServicesAccount = ""; //对应的分润账号
        private double FormalitiesFees = 0.01;

        #region 读写选项相关 MSSQL

        /// <summary>
        /// 
        /// </summary>
        /// <param name="optionValue"></param>
        public void SetOption(string optionValue)
        {
            if (!CheckOptonValue(optionValue))
            {
                throw new Exception("OptionValue is error.");
            }

            if (!IsExistsOptionTable())
            {
                if (MSSQL.ExecuteNonQuery("create table T_Options (ID smallint, [Key] varchar(100), [Value] varchar(2000))") < 0)
                {
                    throw new Exception("Create table T_Options fail.");
                }
            }

            if (!IsExistsOptionTableField("Key"))
            {
                if (MSSQL.ExecuteNonQuery("alter table T_Options add [Key] varchar(100)") < 0)
                {
                    throw new Exception("Add column [Key] for T_Options fail.");
                }
            }

            if (!IsExistsOptionTableField("Value"))
            {
                if (MSSQL.ExecuteNonQuery("alter table T_Options add [Value] varchar(2000)") < 0)
                {
                    throw new Exception("Add column [Value] for T_Options fail.");
                }
            }


            if (MSSQL.ExecuteNonQuery("if exists (select 1 from T_Options where [Key] = 'AlipayOnlinePaySetting') update T_Options set [Value] = @Value where [Key] = 'AlipayOnlinePaySetting' else insert into T_Options ([Key], [Value]) values ('AlipayOnlinePaySetting', @Value2)",
                new MSSQL.Parameter("Value", SqlDbType.VarChar, 0, ParameterDirection.Input, optionValue),
                new MSSQL.Parameter("Value2", SqlDbType.VarChar, 0, ParameterDirection.Input, optionValue)) < 0)
            {
                throw new Exception("Set fail.");
            }

            try
            {
                string[] strs = Security.Encrypt.Decrypt3DES(optionValue.Substring(32, optionValue.Length - 32), "key").Split(',');

                PartnerID = strs[0];
                PartnerKey = strs[1];
                ServicesAccount = strs[2];
                FormalitiesFees = Convert.StrToDouble(strs[3], 0.01);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void GetOption()
        {
            object obj = MSSQL.ExecuteScalar("select [Value] from T_Options where [Key] = 'AlipayOnlinePaySetting'");

            if (obj == null)
            {
                return;
            }

            string OptionValue = obj.ToString().Trim();

            if (OptionValue == "")
            {
                return;
            }

            if (!CheckOptonValue(OptionValue))
            {
                throw new Exception("OptionValue is error.");
            }

            try
            {
                string[] strs = Security.Encrypt.Decrypt3DES(OptionValue.Substring(32, OptionValue.Length - 32), "key").Split(',');

                PartnerID = strs[0];
                PartnerKey = strs[1];
                ServicesAccount = strs[2];
                FormalitiesFees = Convert.StrToDouble(strs[3], 0.01);
            }
            catch
            {
            }
        }

        private bool CheckOptonValue(string optionValue)
        {
            if (optionValue.Length < 32)
            {
                return false;
            }

            string MD5Key = optionValue.Substring(0, 32);
            optionValue = optionValue.Substring(32, optionValue.Length - 32);

            if (MD5Key != Security.Encrypt.MD5(optionValue))
            {
                return false;
            }

            try
            {
                optionValue = Security.Encrypt.Decrypt3DES(optionValue, "key");
            }
            catch
            {
                return false;
            }

            string[] strs = optionValue.Split(',');

            if (strs.Length < 4)
            {
                return false;
            }

            return true;
        }

        private bool IsExistsOptionTable()
        {
            DataTable dt = MSSQL.Select("Select 1 from sysobjects where OBJECTPROPERTY(id, N'IsUserTable') = 1 and OBJECTPROPERTY(id,N'IsMSShipped')=0 and [Name]='T_Options'");

            if (dt == null)
            {
                throw new Exception("Database Connect Fail.");
            }

            return (dt.Rows.Count > 0);
        }

        private bool IsExistsOptionTableField(string columnName)
        {
            DataTable dt = MSSQL.Select("select * from T_Options where 1 = 2");

            if (dt == null)
            {
                throw new Exception("T_Options not found.");
            }

            foreach (DataColumn dc in dt.Columns)
            {
                if (dc.ColumnName.ToLower() == columnName.ToLower())
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region 读写选项相关 MySQL

        /// <summary>
        /// 
        /// </summary>
        /// <param name="optionValue"></param>
        public void SetOptionForMySQL(string optionValue)
        {
            if (!CheckOptonValue(optionValue))
            {
                throw new Exception("OptionValue is error.");
            }

            if (!IsExistsOptionTableForMySQL())
            {
                if (MySQL.ExecuteNonQuery("create table T_Options (ID smallint, `Key` varchar(100), `Value` varchar(2000))") < 0)
                {
                    throw new Exception("Create table T_Options fail.");
                }
            }

            if (!IsExistsOptionTableFieldForMySQL("Key"))
            {
                if (MySQL.ExecuteNonQuery("alter table T_Options add `Key` varchar(100)") < 0)
                {
                    throw new Exception("Add column `Key` for T_Options fail.");
                }
            }

            if (!IsExistsOptionTableFieldForMySQL("Value"))
            {
                if (MySQL.ExecuteNonQuery("alter table T_Options add `Value` varchar(2000)") < 0)
                {
                    throw new Exception("Add column `Value` for T_Options fail.");
                }
            }

            string sql = @"drop procedure  if exists ShoveTmpP_options;
                            create procedure ShoveTmpP_options
                            (
	                            in _value1 varchar(50),
	                            in _value2 varchar(50)
                            )
                            begin
                            declare c int(10);
                            set c = 0;
                            select count(*) into c from T_Options where `Key` = 'AlipayOnlinePaySetting';
                            if c > 0 then
	                            update T_Options set `Value` = _value1 where `Key` = 'AlipayOnlinePaySetting' ;
                            else 
	                            insert into T_Options (`Key`, `Value`) values ('AlipayOnlinePaySetting', _value2);
                            end if;
                            end;
                            call ShoveTmpP_options (@Value,@Value2);
                            drop procedure  if exists ShoveTmpP_options;";
            if (MySQL.ExecuteNonQuery(sql,
                new MySQL.Parameter("@Value", MySql.Data.MySqlClient.MySqlDbType.VarChar, 0, ParameterDirection.Input, optionValue),
                new MySQL.Parameter("@Value2", MySql.Data.MySqlClient.MySqlDbType.VarChar, 0, ParameterDirection.Input, optionValue)) < 0)
            {
                throw new Exception("Set fail.");
            }

            try
            {
                string[] strs = Security.Encrypt.Decrypt3DES(optionValue.Substring(32, optionValue.Length - 32), "key").Split(',');

                PartnerID = strs[0];
                PartnerKey = strs[1];
                ServicesAccount = strs[2];
                FormalitiesFees = Convert.StrToDouble(strs[3], 0.01);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void GetOptionForMySQL()
        {
            object obj = MySQL.ExecuteScalar("select `Value` from T_Options where `Key` = 'AlipayOnlinePaySetting'");

            if (obj == null)
            {
                return;
            }

            string OptionValue = obj.ToString().Trim();

            if (OptionValue == "")
            {
                return;
            }

            if (!CheckOptonValue(OptionValue))
            {
                throw new Exception("OptionValue is error.");
            }

            try
            {
                string[] strs = Security.Encrypt.Decrypt3DES(OptionValue.Substring(32, OptionValue.Length - 32), "key").Split(',');

                PartnerID = strs[0];
                PartnerKey = strs[1];
                ServicesAccount = strs[2];
                FormalitiesFees = Convert.StrToDouble(strs[3], 0.01);
            }
            catch
            {
            }
        }

        private bool IsExistsOptionTableForMySQL()
        {
            DataTable dt = MySQL.Select("select `TABLE_NAME` from `INFORMATION_SCHEMA`.`TABLES` where `TABLE_SCHEMA`=@database and `TABLE_NAME`='T_Options' ",
                                 new MySQL.Parameter("@database", MySql.Data.MySqlClient.MySqlDbType.VarChar, 0, ParameterDirection.Input, DatabaseAccess.CreateDataConnection<MySql.Data.MySqlClient.MySqlConnection>().Database));

            if (dt == null)
            {
                throw new Exception("Database Connect Fail.");
            }

            return (dt.Rows.Count > 0);
        }

        private bool IsExistsOptionTableFieldForMySQL(string columnName)
        {
            DataTable dt = MySQL.Select("select * from T_Options where 1 = 2");

            if (dt == null)
            {
                throw new Exception("T_Options not found.");
            }

            foreach (DataColumn dc in dt.Columns)
            {
                if (dc.ColumnName.ToLower() == columnName.ToLower())
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        /// <summary>
        /// 仅适应 MSSQL 数据库
        /// </summary>
        /// <param name="notifyService"></param>
        /// <param name="notifyID"></param>
        /// <param name="sellerEmail"></param>
        /// <param name="charset"></param>
        /// <param name="notifyType"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public string Get_Http(string notifyService, string notifyID, string sellerEmail, string charset, int notifyType, int timeOut)
        {
            GetOption();

            return Get_HttpPublic(notifyService, notifyID, notifyType, timeOut);
        }

        /// <summary>
        /// 如果使用的数据库不是 MSSQL，请使用此方法
        /// </summary>
        /// <param name="notifyService"></param>
        /// <param name="notifyID"></param>
        /// <param name="sellerEmail"></param>
        /// <param name="charset"></param>
        /// <param name="notifyType"></param>
        /// <param name="timeOut"></param>
        /// <param name="type">type 表示数据库类型 1 MSSQL 2 MySQL</param>
        /// <returns></returns>
        public string Get_Http(string notifyService, string notifyID, string sellerEmail, string charset, int notifyType, int timeOut, int type)
        {
            if (type == 1)
            {
                GetOption();
            }
            else if (type == 2)
            {
                GetOptionForMySQL();
            }
            return Get_HttpPublic(notifyService, notifyID, notifyType, timeOut);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="notifyService"></param>
        /// <param name="notifyID"></param>
        /// <param name="notifyType"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public string Get_HttpPublic(string notifyService, string notifyID, int notifyType, int timeOut)
        {
            string partner = PartnerID;
            string Url = "";

            if (notifyType == 1)
            {
                Url = "https://www.alipay.com/cooperate/gateway.do?service=" + notifyService + "&partner=" + partner + "&notify_id=" + notifyID;	        //支付接口
            }
            else
            {
                Url = "http://notify.alipay.com/trade/notify_query.do?partner=" + partner + "&notify_id=" + notifyID;
            }

            string strResult;
            try
            {
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(Url);
                myReq.Timeout = timeOut;
                HttpWebResponse HttpWResp = (HttpWebResponse)myReq.GetResponse();
                Stream myStream = HttpWResp.GetResponseStream();
                StreamReader sr = new StreamReader(myStream, Encoding.Default);
                StringBuilder strBuilder = new StringBuilder();
                while (-1 != sr.Peek())
                {
                    strBuilder.Append(sr.ReadLine());
                }

                strResult = strBuilder.ToString();
            }
            catch (Exception exp)
            {

                strResult = "错误：" + exp.Message;
            }

            return strResult;
        }

        #region     MD5加密

        /// <summary>
        /// 与ASP兼容的MD5加密算法
        /// </summary>
        public static string GetMD5(string s, string charset)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] t = md5.ComputeHash(Encoding.GetEncoding(charset).GetBytes(s));
            StringBuilder sb = new StringBuilder(32);
            for (int i = 0; i < t.Length; i++)
            {
                sb.Append(t[i].ToString("x").PadLeft(2, '0'));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 与ASP兼容的MD5加密算法（仅适应 MSSQL 数据库）
        /// </summary>
        public string GetMD5(string s, string sellerEmail, string charset)
        {
            string key = "";

            GetOption();

            key = PartnerKey;

            s += key;

            return GetMD5(s, charset);
        }

        /// <summary>
        /// 与ASP兼容的MD5加密算法（如果使用的数据库不是 MSSQL，请使用此方法）
        /// </summary>
        /// <param name="s"></param>
        /// <param name="sellerEmail"></param>
        /// <param name="charset"></param>
        /// <param name="type">type 表示数据库类型 1 MSSQL 2 MySQL</param>
        /// <returns></returns>
        public string GetMD5(string s, string sellerEmail, string charset, int type)
        {
            string key = "";

            if (type == 2)
            {
                GetOptionForMySQL();
            }
            else
            {
                GetOption();
            }

            key = PartnerKey;

            s += key;

            return GetMD5(s, charset);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Date"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static string GetMD5(byte[] Date, string charset)
        {
            MD5 md5 = new MD5CryptoServiceProvider();

            byte[] t = md5.ComputeHash(Date);
            StringBuilder sb = new StringBuilder(32);
            for (int i = 0; i < t.Length; i++)
            {
                sb.Append(t[i].ToString("x").PadLeft(2, '0'));
            }

            md5.Dispose();

            return sb.ToString();
        }

        #endregion

        /// <summary>
        /// 冒泡排序法
        /// </summary>
        public static string[] BubbleSort(string[] r)
        {
            int i, j; //交换标志 
            string temp;

            bool exchange;

            for (i = 0; i < r.Length; i++) //最多做R.Length-1趟排序 
            {
                exchange = false; //本趟排序开始前，交换标志应为假

                for (j = r.Length - 2; j >= i; j--)
                {
                    if (string.CompareOrdinal(r[j + 1], r[j]) < 0)　//交换条件
                    {
                        temp = r[j + 1];
                        r[j + 1] = r[j];
                        r[j] = temp;

                        exchange = true; //发生了交换，故将交换标志置为真 
                    }
                }

                if (!exchange) //本趟排序未发生交换，提前终止算法 
                {
                    break;
                }
            }

            return r;
        }

        #region 支付结果查询

        /// <summary>
        /// 支付结果查询（仅适用 MSSQL 数据库）
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="paymentNumber"></param>
        /// <param name="alipaypaymentNumber"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public int Query(string gateway, string paymentNumber, ref string alipaypaymentNumber, ref string description)
        {
            GetOption();

            return Query_Public(gateway, paymentNumber, ref alipaypaymentNumber, ref description);
        }

        /// <summary>
        /// 支付结果查询（如果使用的数据库不是 MSSQL，请使用此方法）
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="paymentNumber"></param>
        /// <param name="type">type 表示数据库类型 1 MSSQL 2 MySQL</param>
        /// <param name="alipaypaymentNumber"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public int Query(string gateway, string paymentNumber, int type, ref string alipaypaymentNumber, ref string description)
        {
            if (type == 2)
            {
                GetOptionForMySQL();
            }
            else
            {
                GetOption();
            }
            return Query_Public(gateway, paymentNumber, ref alipaypaymentNumber, ref description);
        }

        private int Query_Public(string gateway, string paymentNumber, ref string alipaypaymentNumber, ref string description)
        {
            string service = "single_trade_query";
            string partner = PartnerID;  //卖家商户号
            string key = PartnerKey;

            string _input_charset = "utf-8";
            string sign_type = "MD5";

            if ((gateway == "") || (partner == "") || (key == ""))
            {
                description = "系统设置信息错误";

                return -1;
            }

            string aliay_url = Creaturl(gateway, service, partner, key, sign_type, _input_charset, "out_trade_no", paymentNumber);

            string AlipayResult = "";

            try
            {
                AlipayResult = Utility.Post(aliay_url, "utf-8", 120);
            }
            catch
            {
                description = "数据获取异常，请重新审核";

                return -2;
            }

            if (string.IsNullOrEmpty(AlipayResult))
            {
                description = "数据获取异常，请重新审核";

                return -3;
            }

            XmlDocument XmlDoc = new XmlDocument();

            try
            {
                XmlDoc.Load(new StringReader(AlipayResult));
            }
            catch
            {
                description = "数据获取异常，请重新审核";

                return -4;
            }

            System.Xml.XmlNodeList nodesIs_success = XmlDoc.GetElementsByTagName("is_success");

            if ((nodesIs_success == null) || (nodesIs_success.Count < 1))
            {
                description = "查询信息获取异常，请重新查询";

                return -5;
            }

            string is_success = nodesIs_success[0].InnerText;

            if (is_success.ToUpper() != "T")
            {
                description = "该支付记录未支付成功";

                return -6;
            }

            System.Xml.XmlNodeList nodesTrade_no = XmlDoc.GetElementsByTagName("trade_no");

            if ((nodesTrade_no == null) || (nodesTrade_no.Count < 1))
            {
                description = "没有对应的支付信息";

                return -7;
            }

            alipaypaymentNumber = nodesTrade_no[0].InnerText;

            System.Xml.XmlNodeList nodesTrade_Status = XmlDoc.GetElementsByTagName("trade_status");

            if ((nodesTrade_Status == null) || (nodesTrade_Status.Count < 1))
            {
                description = "没有对应的支付信息";

                return -8;
            }

            string Trade_Status = nodesTrade_Status[0].InnerText.ToUpper();

            if (Trade_Status == "WAIT_BUYER_PAY")
            {
                description = "等待买家付款";

                return -9;
            }

            if (Trade_Status == "WAIT_SELLER_SEND_GOODS")
            {
                description = "买家付款成功(担保交易，未确定支付给商家)";

                return -10;
            }

            if (Trade_Status == "WAIT_BUYER_CONFIRM_GOODS")
            {
                description = "卖家发货成功(未确定支付给商家)";

                return -11;
            }

            if (Trade_Status == "TRADE_CLOSED")
            {
                description = "交易被关闭，未成功付款";

                return -12;
            }

            if (Trade_Status != "TRADE_FINISHED")
            {
                description = "其他未成功支付的错误";

                return -9999;
            }

            return 0;
        }
        #endregion

        #region     地址构建

        /// <summary>
        /// 支付宝地址构建 （仅适应 MSSQL 数据库）
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="service"></param>
        /// <param name="partner"></param>
        /// <param name="return_url"></param>
        /// <param name="notify_url"></param>
        /// <param name="out_trade_no"></param>
        /// <param name="subject"></param>
        /// <param name="payment_type"></param>
        /// <param name="total_fee"></param>
        /// <param name="seller_email"></param>
        /// <param name="key"></param>
        /// <param name="charset"></param>
        /// <param name="signType"></param>
        /// <param name="paramsAndValue"></param>
        /// <returns></returns>
        public string CreatUrl(
        string gateway,
        string service,
        string partner,
        string return_url,
        string notify_url,
        string out_trade_no,
        string subject,
        string payment_type,
        string total_fee,
        string seller_email,
        string key,
        string charset,
        string signType,
        params string[] paramsAndValue)
        {
            GetOption();

            return CreateUrl_Public(gateway, service, partner, return_url, notify_url, out_trade_no, subject, payment_type, total_fee, seller_email, key, charset, signType, paramsAndValue);
        }

        /// <summary>
        /// 支付宝地址构建 2010-7-7（如果使用的数据库不是 MSSQL，请使用此方法）
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="service"></param>
        /// <param name="partner"></param>
        /// <param name="return_url"></param>
        /// <param name="notify_url"></param>
        /// <param name="out_trade_no"></param>
        /// <param name="subject"></param>
        /// <param name="payment_type"></param>
        /// <param name="total_fee"></param>
        /// <param name="seller_email"></param>
        /// <param name="key"></param>
        /// <param name="charset"></param>
        /// <param name="signType"></param>
        /// <param name="type">type 表示数据库类型 1 MSSQL 2 MySQL</param>
        /// <param name="paramsAndValue"></param>
        /// <returns></returns>
        public string CreatUrl(
        string gateway,
        string service,
        string partner,
        string return_url,
        string notify_url,
        string out_trade_no,
        string subject,
        string payment_type,
        string total_fee,
        string seller_email,
        string key,
        string charset,
        string signType,
        int type,
        params string[] paramsAndValue)
        {
            if (type == 2)
            {
                GetOptionForMySQL();
            }
            else
            {
                GetOption();
            }

            return CreateUrl_Public(gateway, service, partner, return_url, notify_url, out_trade_no, subject, payment_type, total_fee, seller_email, key, charset, signType, paramsAndValue);

        }

        private string CreateUrl_Public(
        string gateway,
        string service,
        string partner,
        string return_url,
        string notify_url,
        string out_trade_no,
        string subject,
        string payment_type,
        string total_fee,
        string seller_email,
        string key,
        string charset,
        string signType,
        params string[] paramsAndValue)
        {
            string Royalty = "10";
            string ServicesUser = "";

            double _FormalitiesFees = 0.01;

            partner = PartnerID;
            key = PartnerKey;
            ServicesUser = ServicesAccount;
            _FormalitiesFees = FormalitiesFees;

            if (!gateway.EndsWith("?", StringComparison.Ordinal))
            {
                gateway += "?";
            }

            ArrayList al = new ArrayList();

            ////////////////////////////计算服务费/////////////////////////////////////
            double Tmptotal_fee = 0;

            try
            {
                Tmptotal_fee = System.Convert.ToDouble(total_fee);
            }
            catch
            {
                return "";
            }

            _FormalitiesFees = Math.Round(Tmptotal_fee * _FormalitiesFees, 2);

            string RoyaltyPparameters = "";
            if (_FormalitiesFees > 0)
            {
                RoyaltyPparameters = ServicesUser + "^" + _FormalitiesFees.ToString() + "^alipay_service";
            }
            //////////////////////////计算服务费结束///////////////////////////////////

            al.Add("seller_email=" + seller_email);
            al.Add("subject=" + subject);
            al.Add("out_trade_no=" + out_trade_no);
            al.Add("total_fee=" + total_fee);
            al.Add("return_url=" + return_url);
            al.Add("notify_url=" + notify_url);
            ///////////////////代理商相关信息//////////////////////////////////
            al.Add("partner=" + partner);

            for (int i = 0; i < paramsAndValue.Length / 2; i++)
            {
                if ((paramsAndValue[i * 2] != "") && (paramsAndValue[i * 2 + 1]) != "")
                {
                    if (paramsAndValue[i * 2].ToLower().IndexOf("royalty_parameters", StringComparison.Ordinal) < 0)
                    {
                        al.Add(paramsAndValue[i * 2].ToLower() + "=" + paramsAndValue[i * 2 + 1]);
                    }
                    else
                    {
                        if (RoyaltyPparameters != "")
                        {
                            RoyaltyPparameters += "|" + paramsAndValue[i * 2 + 1]; //paramsAndValue[i * 2 + 1]的格式必须是alipay_services_cn@yahoo.com^10.0^alipay_service（分账账户^金额^描述）
                        }
                        else
                        {
                            RoyaltyPparameters += paramsAndValue[i * 2 + 1];
                        }
                    }
                }
            }

            string[] RoyaltyPparameterList = RoyaltyPparameters.Split('|');

            if (RoyaltyPparameterList.Length > 4)
            {
                RoyaltyPparameters = RoyaltyPparameterList[0] + "|" + RoyaltyPparameterList[1] + "|" + RoyaltyPparameterList[2] + "|" + RoyaltyPparameterList[3] + "|" + RoyaltyPparameterList[4];
            }

            if (RoyaltyPparameters != "")
            {
                al.Add("royalty_type=" + Royalty);
                al.Add("royalty_parameters=" + RoyaltyPparameters);
            }
            ///////////////////////////////////////////////////////////////////
            al.Add("_input_charset=" + charset);
            al.Add("payment_type=" + payment_type);
            al.Add("service=" + service);

            //初始数组
            string[] InitialOristr = new string[al.Count];
            string[] Oristr = new string[al.Count];

            for (int i = 0; i < al.Count; i++)
            {
                Oristr[i] = al[i].ToString();
                InitialOristr[i] = al[i].ToString();
            }

            //进行排序
            string[] Sortedstr = BubbleSort(Oristr);

            //构造待md5摘要字符串

            StringBuilder prestr = new StringBuilder();

            for (int i = 0; i < Sortedstr.Length; i++)
            {
                if (i == Sortedstr.Length - 1)
                {
                    prestr.Append(Sortedstr[i]);

                }
                else
                {

                    prestr.Append(Sortedstr[i] + "&");
                }
            }

            prestr.Append(key);

            //生成Md5摘要；
            string sign = GetMD5(prestr.ToString(), charset);

            //构造支付Url；
            char[] delimiterChars = { '=' };
            StringBuilder parameter = new StringBuilder();
            parameter.Append(gateway);

            for (int i = 0; i < InitialOristr.Length; i++)
            {
                parameter.Append(InitialOristr[i].Split(delimiterChars)[0] + "=" + HttpUtility.UrlEncode(InitialOristr[i].Split(delimiterChars)[1]) + "&");
                //parameter.Append(Sortedstr[i].Split(delimiterChars)[0] + "=" + Sortedstr[i].Split(delimiterChars)[1] + "&");
            }

            parameter.Append("sign=" + sign + "&sign_type=" + signType);

            //返回支付Url；
            return parameter.ToString();
        }

        /// <summary>
        /// 支付结果查询
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="service"></param>
        /// <param name="partner"></param>
        /// <param name="key"></param>
        /// <param name="sign_type"></param>
        /// <param name="charset"></param>
        /// <param name="paramsAndValue"></param>
        /// <returns></returns>
        public string Creaturl(string gateway, string service, string partner, string key, string sign_type, string charset, params string[] paramsAndValue)
        {
            if (!gateway.EndsWith("?", StringComparison.Ordinal))
            {
                gateway += "?";
            }

            ArrayList al = new ArrayList();

            //////////////////固定参数///////////

            al.Add("_input_charset=" + charset);
            al.Add("partner=" + partner);
            al.Add("service=" + service);

            ////////可变参数/////////////////
            for (int i = 0; i < paramsAndValue.Length / 2; i++)
            {
                if ((paramsAndValue[i * 2] != "") && (paramsAndValue[i * 2 + 1]) != "")
                {
                    al.Add(paramsAndValue[i * 2].ToLower() + "=" + paramsAndValue[i * 2 + 1]);
                }
            }
            ///////////////////////////////////////////////////////////////////

            //初始数组
            string[] InitialOristr = new string[al.Count];
            string[] Oristr = new string[al.Count];

            for (int i = 0; i < al.Count; i++)
            {
                Oristr[i] = al[i].ToString();
                InitialOristr[i] = al[i].ToString();
            }

            //进行排序
            string[] Sortedstr = BubbleSort(Oristr);

            //构造待md5摘要字符串

            StringBuilder prestr = new StringBuilder();

            for (int i = 0; i < Sortedstr.Length; i++)
            {
                if (i == Sortedstr.Length - 1)
                {
                    prestr.Append(Sortedstr[i]);

                }
                else
                {

                    prestr.Append(Sortedstr[i] + "&");
                }
            }

            prestr.Append(key);

            //生成Md5摘要；
            string sign = GetMD5(prestr.ToString(), charset);

            //构造支付Url；
            char[] delimiterChars = { '=' };
            StringBuilder parameter = new StringBuilder();
            parameter.Append(gateway);

            for (int i = 0; i < InitialOristr.Length; i++)
            {
                parameter.Append(InitialOristr[i].Split(delimiterChars)[0] + "=" + HttpUtility.UrlEncode(InitialOristr[i].Split(delimiterChars)[1]) + "&");
                //parameter.Append(Sortedstr[i].Split(delimiterChars)[0] + "=" + Sortedstr[i].Split(delimiterChars)[1] + "&");
            }

            parameter.Append("sign=" + sign + "&sign_type=" + sign_type);

            //返回支付Url；
            return parameter.ToString();
        }

        /// <summary>
        /// 会员共享
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="service"></param>
        /// <param name="partner"></param>
        /// <param name="sign_type"></param>
        /// <param name="key"></param>
        /// <param name="return_url"></param>
        /// <param name="_input_charset"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public string CreatUrl(
            string gateway,
            string service,
            string partner,
            string sign_type,
            string key,
            string return_url,
            string _input_charset,
            string returnUrl
            )
        {
            // created by sunzhizhi 2006.5.21,sunzhizhi@msn.com。

            if (!gateway.EndsWith("?", StringComparison.Ordinal))
            {
                gateway += "?";
            }

            int i;

            //构造数组；
            string[] Oristr ={ 
                "service="+service, 
                "partner=" + partner, 
                "_input_charset="+_input_charset,          
                "return_url=" + return_url,
                "returnurl=" + returnUrl
                };

            //进行排序；
            string[] Sortedstr = BubbleSort(Oristr);

            //构造待md5摘要字符串 ；

            StringBuilder prestr = new StringBuilder();

            for (i = 0; i < Sortedstr.Length; i++)
            {
                if (i == Sortedstr.Length - 1)
                {
                    prestr.Append(Sortedstr[i]);

                }
                else
                {
                    prestr.Append(Sortedstr[i] + "&");
                }
            }

            prestr.Append(key);

            //生成Md5摘要；
            string sign = GetMD5(prestr.ToString(), _input_charset);

            //构造支付Url；
            char[] delimiterChars = { '=' };
            StringBuilder parameter = new StringBuilder();
            parameter.Append(gateway);
            for (i = 0; i < Sortedstr.Length; i++)
            {
                parameter.Append(Sortedstr[i].Split(delimiterChars)[0] + "=" + HttpUtility.UrlEncode(Sortedstr[i].Split(delimiterChars)[1]) + "&");
            }

            parameter.Append("sign=" + sign + "&sign_type=" + sign_type);

            //返回支付Url；
            return parameter.ToString();
        }


        /// <summary>
        /// 在线派款----支付宝到支付宝
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="service"></param>
        /// <param name="partner"></param>
        /// <param name="sign_type"></param>
        /// <param name="batch_no"></param>
        /// <param name="account_name"></param>
        /// <param name="batch_fee"></param>
        /// <param name="batch_num"></param>
        /// <param name="email"></param>
        /// <param name="pay_date"></param>
        /// <param name="detail_data"></param>
        /// <param name="key"></param>
        /// <param name="notify_url"></param>
        /// <param name="_input_charset"></param>
        /// <returns></returns>
        public string CreatUrl(
            string gateway,
            string service,
            string partner,
            string sign_type,
            string batch_no,
            string account_name,
            string batch_fee,
            string batch_num,
            string email,
            string pay_date,
            string detail_data,
            string key,
            string notify_url,
            string _input_charset

            )
        {
            // created by sunzhizhi 2006.5.21,sunzhizhi@msn.com。
            int i;

            //构造数组；
            string[] Oristr ={ 
                "service="+service, 
                "partner=" + partner, 
                "batch_no=" + batch_no, 
                "account_name=" + account_name, 
                "batch_fee=" + batch_fee,  
                "batch_num=" + batch_num, 
                "email=" + email, 
                "pay_date=" + pay_date, 
                "detail_data=" + detail_data,
                  "notify_url=" + notify_url,
                "_input_charset="+_input_charset
               
            };

            //进行排序；
            string[] Sortedstr = BubbleSort(Oristr);

            //构造待md5摘要字符串 ；

            StringBuilder prestr = new StringBuilder();

            for (i = 0; i < Sortedstr.Length; i++)
            {
                if (i == Sortedstr.Length - 1)
                {
                    prestr.Append(Sortedstr[i]);

                }
                else
                {

                    prestr.Append(Sortedstr[i] + "&");
                }
            }

            prestr.Append(key);

            //生成Md5摘要；
            string sign = GetMD5(prestr.ToString(), _input_charset);

            //构造支付Url；
            StringBuilder parameter = new StringBuilder();
            parameter.Append(gateway);
            for (i = 0; i < Sortedstr.Length; i++)
            {
                parameter.Append(Sortedstr[i] + "&");
            }

            parameter.Append("sign=" + sign + "&sign_type=" + sign_type);

            //返回支付Url；
            return parameter.ToString();
        }

        #endregion

        /// <summary>
        /// 上传派款文件参数构建
        /// </summary>
        /// <param name="service"></param>
        /// <param name="_input_charset"></param>
        /// <param name="partner"></param>
        /// <param name="file_digest_type"></param>
        /// <param name="biz_type"></param>
        /// <param name="agentID"></param>
        /// <returns></returns>
        public string[] GetUploadParams(
            string service,
            string _input_charset,
            string partner,
            string file_digest_type,
            string biz_type,
            string agentID
        )
        {
            // created by sunzhizhi 2006.5.21,sunzhizhi@msn.com。

            //构造数组；
            string[] Oristr ={ 
                "service="+service,
                "partner=" + partner,
                "biz_type=" + biz_type,
                "file_digest_type=" + file_digest_type,
                "_input_charset=" + _input_charset,                
                "agentID=" + agentID
                };

            //进行排序；

            string[] Sortedstr = BubbleSort(Oristr);

            return Sortedstr;
        }

        /// <summary>
        /// 下载派款处理文件参数构建
        /// </summary>
        /// <param name="service"></param>
        /// <param name="_input_charset"></param>
        /// <param name="partner"></param>
        /// <param name="biz_type"></param>
        /// <returns></returns>
        public string[] GetDownloadParams(
            string service,
            string _input_charset,
            string partner,
            string biz_type
        )
        {
            // created by sunzhizhi 2006.5.21,sunzhizhi@msn.com。

            //构造数组；
            string[] Oristr ={ 
                "service="+service,
                "partner=" + partner,
                "biz_type=" + biz_type,
                "_input_charset=" + _input_charset,                
                };

            //进行排序；

            string[] Sortedstr = BubbleSort(Oristr);

            return Sortedstr;
        }
    }

    /// <summary>
    /// 创建WebClient.UploadData方法所需二进制数组
    /// </summary>
    public class CreateBytes
    {
        Encoding encoding = Encoding.UTF8;

        /**/
        /// <summary>
        /// 拼接所有的二进制数组为一个数组
        /// </summary>
        /// <param name="byteArrays">数组</param>
        /// <returns></returns>
        /// <remarks>加上结束边界</remarks>
        public byte[] JoinBytes(ArrayList byteArrays)
        {
            int length = 0;
            int readLength = 0;

            // 加上结束边界
            string endBoundary = Boundary + "--\r\n"; //结束边界
            byte[] endBoundaryBytes = encoding.GetBytes(endBoundary);

            byteArrays.Add(endBoundaryBytes);

            foreach (byte[] b in byteArrays)
            {
                length += b.Length;
            }

            byte[] bytes = new byte[length];

            // 遍历复制
            foreach (byte[] b in byteArrays)
            {
                b.CopyTo(bytes, readLength);

                readLength += b.Length;
            }

            return bytes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uploadUrl"></param>
        /// <param name="bytes"></param>
        /// <param name="responseBytes"></param>
        /// <returns></returns>
        public bool UploadData(string uploadUrl, byte[] bytes, out byte[] responseBytes)
        {
            WebClient webClient = new WebClient();

            webClient.Headers.Add("Content-Type", ContentType);

            try
            {
                responseBytes = webClient.UploadData(uploadUrl, bytes);

                return true;
            }
            catch (WebException ex)
            {
                Stream resp = ex.Response.GetResponseStream();
                responseBytes = new byte[ex.Response.ContentLength];
                resp.Read(responseBytes, 0, responseBytes.Length);
            }

            return false;
        }

        /**/
        /// <summary>
        /// 获取普通表单区域二进制数组
        /// </summary>
        /// <param name="fieldName">表单名</param>
        /// <param name="fieldValue">表单值</param>
        /// <returns></returns>
        /// <remarks>
        /// -----------------------------7d52ee27210a3c\r\nContent-Disposition: form-data; name=\"表单名\"\r\n\r\n表单值\r\n
        /// </remarks>
        public byte[] CreateFieldData(string fieldName, string fieldValue)
        {
            string textTemplate = Boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}\r\n";
            string text = string.Format(textTemplate, fieldName, fieldValue);
            byte[] bytes = encoding.GetBytes(text);

            return bytes;
        }


        /**/
        /// <summary>
        /// 获取文件上传表单区域二进制数组
        /// </summary>
        /// <param name="fieldName">表单名</param>
        /// <param name="filename">文件名</param>
        /// <param name="contentType">文件类型</param>
        /// <param name="fileBytes">文件流</param>
        /// <returns>二进制数组</returns>
        public byte[] CreateFieldData(string fieldName, string filename, string contentType, byte[] fileBytes)
        {
            string end = "\r\n";
            string textTemplate = Boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";

            // 头数据
            string data = string.Format(textTemplate, fieldName, filename, contentType);
            byte[] bytes = encoding.GetBytes(data);

            // 尾数据
            byte[] endBytes = encoding.GetBytes(end);

            // 合成后的数组
            byte[] fieldData = new byte[bytes.Length + fileBytes.Length + endBytes.Length];

            bytes.CopyTo(fieldData, 0); // 头数据
            fileBytes.CopyTo(fieldData, bytes.Length); // 文件的二进制数据
            endBytes.CopyTo(fieldData, bytes.Length + fileBytes.Length); // \r\n

            return fieldData;
        }


        #region 属性

        /// <summary>
        /// 
        /// </summary>
        public string Boundary
        {
            get
            {
                string[] bArray, ctArray;
                string contentType = ContentType;
                ctArray = contentType.Split(';');
                if (ctArray[0].Trim().ToLower() == "multipart/form-data")
                {
                    bArray = ctArray[1].Split('=');
                    return "--" + bArray[1];
                }

                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ContentType
        {
            get
            {
                return "multipart/form-data; boundary=---------------------------7d5b915500cee";

                //客户端采用下面的方式
                //if (HttpContext.Current == null)
                //{
                //    return "multipart/form-data; boundary=---------------------------7d5b915500cee";
                //}
                //return HttpContext.Current.Request.ContentType;
            }
        }

        #endregion
    }

    /// <summary>
    /// AlipayCommon 的摘要说明。
    /// </summary>
    public class AlipayCommon
    {
        /// <summary>
        /// 
        /// </summary>
        public AlipayCommon()
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
        }


        /// <summary>
        /// 根据输入的原始文件名得到压缩文件名
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        //private static string GetZipFile(string fileName)
        //{
        //    int pos = fileName.LastIndexOf("\\");
        //    string path = fileName.Substring(0, pos);
        //    string shortName = fileName.Substring(pos, fileName.Length - pos - 4);
        //    string value = path + shortName + ".zip";

        //    return value;
        //}

        //public static string FileZip(string fileName)
        //{
        //    C1.C1Zip.C1ZipFile zip = new C1.C1Zip.C1ZipFile();
        //    string zipFileName = GetZipFile(fileName);
        //    if (File.Exists(zipFileName))
        //    {
        //        File.Delete(zipFileName);
        //    }
        //    zip.Create(zipFileName);

        //    zip.Entries.Add(fileName);

        //    return zipFileName;
        //}

        public static string GetFileMD5(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open);
            int len = (int)fs.Length;
            byte[] data = new byte[len];
            fs.Read(data, 0, len);
            fs.Close();
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(data);

            string sResult = " ";
            foreach (byte b in result)
            {
                System.Diagnostics.Debug.WriteLine(b);

                string temp = ("0" + System.Convert.ToString(b, 16));

                temp = temp.Substring(temp.Length - 2);
                sResult += temp;
            }

            return sResult;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static string[] BubbleSort(string[] r)
        {
            // 冒泡排序法

            int i, j; //交换标志 
            string temp;

            bool exchange;

            for (i = 0; i < r.Length; i++) //最多做R.Length-1趟排序 
            {
                exchange = false; //本趟排序开始前，交换标志应为假

                for (j = r.Length - 2; j >= i; j--)
                {
                    if (System.String.CompareOrdinal(r[j + 1], r[j]) < 0)　//交换条件
                    {
                        temp = r[j + 1];
                        r[j + 1] = r[j];
                        r[j] = temp;

                        exchange = true; //发生了交换，故将交换标志置为真 
                    }
                }

                if (!exchange) //本趟排序未发生交换，提前终止算法 
                {
                    break;
                }

            }

            return r;
        }


        /// <summary>
        /// 返回签名，需要排序
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="safeCode"></param>
        /// <returns></returns>
        public static string GetSign(string[] fields, string safeCode)
        {
            string[] v = BubbleSort(fields);
            string value = "";
            foreach (string s in v)
            {
                value += s + "&";
            }

            value = value.TrimEnd('&');
            value += safeCode;

            int len = value.Length;
            byte[] data = new byte[len];
            data = System.Text.Encoding.Default.GetBytes(value);

            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(data);

            string sResult = " ";
            foreach (byte b in result)
            {
                System.Diagnostics.Debug.WriteLine(b);
                string temp = ("0" + System.Convert.ToString(b, 16));
                temp = temp.Substring(temp.Length - 2);
                sResult += temp;
            }

            return sResult;
        }
    }

    /// <summary>
    /// HttpClient 的摘要说明。
    /// </summary>
    public class HttpClient
    {
        /// <summary>
        /// 
        /// </summary>
        public HttpClient()
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
        }

        #region properties

        private string boundary = "";

        /// <summary>
        /// 
        /// </summary>
        public string Boundary
        {
            get { return boundary; }
            set { boundary = value; }
        }


        private Stream writer = null;

        /// <summary>
        /// 
        /// </summary>
        public Stream Writer
        {
            set { writer = value; }
        }

        #endregion

        private byte[] String2Bytes(string content)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);

            return bytes;
        }

        #region public function

        /// <summary>
        /// 添加一个文本框
        /// </summary>
        /// <param name="txtName"></param>
        /// <param name="content"></param>
        public void AppendText(string txtName, string content)
        {
            //			byte[] b1 = String2Bytes(boundary + "\r\n");
            //			writer.Write(b1,0,b1.Length);
            //			byte[] b2 = String2Bytes("Content-Disposition: form-data; name=\"" + txtName + "\"\r\n\r\n");
            //			writer.Write(b2,0,b2.Length);
            //			byte[] b3 = String2Bytes(content + "\r\n");
            //			writer.Write(b3,0,b3.Length);

            string textTemplate = boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}\r\n";
            string text = string.Format(textTemplate, txtName, content);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);

            writer.Write(bytes, 0, bytes.Length);
            //						byte[] b1 = String2Bytes(txtName + "="  + content + "&");
            //						writer.Write(b1,0,b1.Length);

        }


        /// <summary>
        /// 添加一个文件
        /// </summary>
        /// <param name="txtName"></param>
        /// <param name="fileName"></param>
        public void AppendFile(string txtName, string fileName)
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            int length = (int)file.Length;
            byte[] bytes = new byte[length];
            file.Read(bytes, 0, length);
            file.Close();

            byte[] b1 = String2Bytes(boundary + "\r\n");
            writer.Write(b1, 0, b1.Length);
            byte[] b2 = (String2Bytes("Content-Disposition: form-data; name=\"" + txtName + "\"; filename=\"" + fileName + "\"\r\n"));
            writer.Write(b2, 0, b2.Length);
            byte[] b3 = (String2Bytes("Content-Type: application/octet-stream\r\n\r\n"));
            writer.Write(b3, 0, b3.Length);
            writer.Write(bytes, 0, length);
            writer.Write(System.Text.Encoding.Default.GetBytes("\r\n"), 0, 2);
        }

        /// <summary>
        /// 
        /// </summary>
        public void AppendEnd()
        {
            byte[] b4 = String2Bytes(boundary + "--\r\n");

            writer.Write(b4, 0, b4.Length);
        }

        #endregion
    }
}