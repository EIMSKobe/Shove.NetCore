using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using ICSharpCode.SharpZipLib.Zip.Compression;
using System.Collections.Generic;

namespace Shove
{
    /// <summary>
    /// �ַ�����ء�
    /// </summary>
    public static class String
    {
        /// <summary>
        /// �ַ� ch �� �ַ��� str �г��ֵĴ���
        /// </summary>
        /// <param name="str"></param>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static int StringAt(string str, char ch)
        {
            if (str == null)
                return 0;

            int Result = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == ch)
                    Result++;
            }

            return Result;
        }

        /// <summary>
        /// �滻�ַ����е�ĳ���ַ�
        /// </summary>
        /// <param name="input"></param>
        /// <param name="ch"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static string ReplaceAt(string input, char ch, int pos)
		{
            return input.Substring(0, pos) + ch.ToString() + ((pos < input.Length) ? input.Substring(pos + 1) : "");
		}

        /// <summary>
        /// ��ת�ַ���
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <returns></returns>
        public static string Reverse(string sourceStr)
        {
            StringBuilder reversed = new StringBuilder();
            for (int i = sourceStr.Length - 1; i >= 0; i--)
                reversed.Append(sourceStr[i]);
            return reversed.ToString();
        }

        /// <summary>
        /// �ִ���ȡ(���Ǻ���)
        /// </summary>
        /// <param name="input">Ҫ��ȡ���ַ���</param>
        /// <param name="length">Ҫ��ȡ���ַ�������</param>
        /// <returns></returns>
        public static string Cut(string input, int length)
        {
            if (length < 0)
            {
                length = 0;
            }

            length *= 2;

            if (GetLength(input) <= length)
            {
                return input;
            }

            string Result = "";
            int i = 0;

            while ((GetLength(Result) < length) && (i < input.Length))
            {
                Result += input[i].ToString();

                i++;
            }

            if (Result != input)
            {
                Result += "..";
            }

            return Result;
        }

        /// <summary>
        /// HTML ��ʽ�ִ���ȡ
        /// </summary>
        /// <param name="input">Ҫ��ȡ���ַ�</param>
        /// <param name="length">Ҫ��ȡ���ַ�����</param>
        /// <returns></returns>
        public static string HtmlTextCut(string input, int length)
        {
            if (length < 0)
            {
                length = 0;
            }

            length *= 2;

            if (!input.Contains("<body>"))
            {
                input = "<body>" + input;
            }

            input = HTML.HTML.StandardizationHTML(input, true, true, true);
            input = HTML.HTML.GetText(input, 0);

            return Cut(input, length);
        }

        /// <summary>
        /// ���ݳ��Ȳ���ַ���������̺��ֶ���������
        /// </summary>
        /// <param name="input">������ַ���</param>
        /// <param name="PartLength">ÿ���ֵĳ���</param>
        /// <returns>���ر���ֵĶಿ���ַ�������</returns>
        public static string[] Split(string input, int PartLength)
        {
            return Split(input, PartLength, 0);
        }

        /// <summary>
        /// ���ݳ��Ȳ���ַ���������̺��ֶ���������
        /// </summary>
        /// <param name="input">������ַ���</param>
        /// <param name="partLength">ÿ���ֵĳ���</param>
        /// <param name="maxPartNum">���ֻ���ؼ������֣��ұ߶���Ĳ��ֽ�ȡ</param>
        /// <returns>���ر���ֵĶಿ���ַ�������</returns>
        public static string[] Split(string input, int partLength, int maxPartNum)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            IList<string> list = new List<string>();
            IList<int> list_length = new List<int>();

            list.Add("");
            list_length.Add(0);

            int locate = 0;

            for (int i = 0; i < input.Length; i++)
            {
                string ch = input[i].ToString();
                int len = System.Text.Encoding.Default.GetBytes(ch).Length;

                if (list_length[locate] + len > partLength)
                {
                    locate++;

                    if ((maxPartNum > 0) && (locate >= maxPartNum))
                    {
                        break;
                    }

                    list.Add("");
                    list_length.Add(0);
                }

                list[locate] += ch;
                list_length[locate] += len;
            }

            string[] result = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                result[i] = list[i];
            }

            return result;
        }

        /// <summary>
        /// Byte[] ת����16�����ַ���������� 0x ǰ׺�� _Byte ���д˷�������������� 0x ǰ׺
        /// </summary>
        /// <param name="input">input bytes[]</param>
        /// <returns>16�����ַ�����0x......</returns>
        public static string BytesToHexString(byte[] input)
        {
            string result = "0x";

            if (input.Length == 0)
            {
                return result;
            }

            foreach (byte b in input)
            {
                result += b.ToString("X").PadLeft(2, '0');
            }

            return result;
        }

        /// <summary> 
        /// ��⺬�������ַ�����ʵ�ʳ��� ��һ�����ֻ�ȫ���ַ��� 2 ������
        /// </summary> 
        /// <param name="str">�ַ���</param> 
        public static int GetLength(string str)
        {
            //System.Text.ASCIIEncoding n = new System.Text.ASCIIEncoding();
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            int len = 0; // len Ϊ�ַ���֮ʵ�ʳ��� 
            for (int i = 0; i <= bytes.Length - 1; i++)
            {
                if (bytes[i] == 63) //�ж��Ƿ�Ϊ���ֻ�ȫ�Ƿ��� 
                {
                    len++;
                }

                len++;
            }

            return len;
        }

        /// <summary> 
        /// ��⺬�������ַ�����ʵ�ʳ��ȣ�ʹ��ָ�����ַ���
        /// </summary> 
        /// <param name="str">�ַ���</param> 
        public static int GetBytesLength(string str)
        {
            return GetBytesLength(str, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// ��⺬�������ַ�����ʵ�ʳ��ȣ�ʹ��ָ�����ַ���
        /// </summary>
        /// <param name="str"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static int GetBytesLength(string str, System.Text.Encoding encoding)
        {
            byte[] bytes = encoding.GetBytes(str);

            if (bytes != null)
            {
                return bytes.Length;
            }

            return 0;
        }

        /// <summary>
        /// �Ƿ���
        /// </summary>
        public static bool isChineseCharacters(char ch)
        {
            int Unicode = (int)(ch) - 19968;
            return ((Unicode >= 0) && (Unicode <= 20900));
        }

        /// <summary>
        /// �Ƿ�ȫ���ַ�
        /// </summary>
        public static bool isDBCCharacters(char ch)
        {
            int Unicode = (int)(ch);

            return ((Unicode == 12288) || ((Unicode > 65280) && (Unicode < 65375)));
        }

        /// <summary>
        /// ���ַ���ת��Ϊ��׼�ġ���ʶ����
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string StandardizationIdentifier(string str)
        {
            str = str.Trim();
            if (str == "")
                return "CHERY_ADD";

            if ("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_".IndexOf(str[0].ToString(), System.StringComparison.Ordinal) < 0)
                str = "CHERY_ADD_" + str;

            int i;
            string StandardizationChars = "1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_";
            string d = "";
            for (i = 0; i < str.Length; i++)
            {
                if (StandardizationChars.IndexOf(str[i].ToString(), System.StringComparison.Ordinal) >= 0)
                    d += str[i].ToString();
            }

            return d;
        }

        /// <summary>
        /// �ַ���ѹ��
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] Compress(string str)
        {
            byte[] data = Encoding.Unicode.GetBytes(str);
            Deflater f = new Deflater(Deflater.BEST_COMPRESSION);
            f.SetInput(data);
            f.Finish();

            MemoryStream o = new MemoryStream(data.Length);
            try
            {
                byte[] buf = new byte[1024];
                while (!f.IsFinished)
                {
                    int got = f.Deflate(buf);
                    o.Write(buf, 0, got);
                }
            }
            finally
            {
                o.Close();
            }

            byte[] Result = o.ToArray();
            if ((Result.Length % 2) == 0)
                return Result;

            byte[] Result2 = new byte[Result.Length + 1];
            for (int i = 0; i < Result.Length; i++)
                Result2[i] = Result[i];
            Result2[Result.Length] = 0;
            return Result2;
        }

        /// <summary>
        /// �ַ�����ѹ��
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Decompress(byte[] data)
        {
            if (data == null)
                return "";
            if (data.Length == 0)
                return "";

            Inflater f = new Inflater();
            f.SetInput(data);

            MemoryStream o = new MemoryStream(data.Length);
            try
            {
                byte[] buf = new byte[1024];
                while (!f.IsFinished)
                {
                    int got = f.Inflate(buf);
                    o.Write(buf, 0, got);
                }
            }
            finally
            {
                o.Close();
            }
            return Encoding.Unicode.GetString(o.ToArray());
        }

        //������ѹ��\��ѹ���ַ���2������������һ��д��
        /*
        public static byte[] Compress(string s)
        {
            Byte[] pBytes = System.Text.UnicodeEncoding.Unicode.GetBytes(s);

            //����֧���ڴ�洢����
            MemoryStream mMemory = new MemoryStream();
            Deflater mDeflater = new Deflater(ICSharpCode.SharpZipLib.Zip.Compression.Deflater.BEST_COMPRESSION);
            ICSharpCode.SharpZipLib.Zip.Compression.Streams.DeflaterOutputStream mStream = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.DeflaterOutputStream(mMemory,mDeflater,131072);

            mStream.Write(pBytes, 0, pBytes.Length);
            mStream.Close();

            return mMemory.ToArray();
        }

        public static string Decompress(byte[] data)
        {
            ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream mStream = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream(new MemoryStream(data));
            
            //����֧���ڴ�洢����
            MemoryStream mMemory = new MemoryStream();
            Int32 mSize;

            Byte[] mWriteData = new Byte[4096];
            while(true)
            {
                mSize = mStream.Read(mWriteData, 0, mWriteData.Length);
                if (mSize > 0)
                {
                    mMemory.Write(mWriteData, 0, mSize);
                }
                else
                {
                    break;
                }
            }

            mStream.Close();
            return System.Text.UnicodeEncoding.Unicode.GetString(mMemory.ToArray());
        }
        */

        /// <summary>
        /// ���ַ���תΪ Base64 ����
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string EncodeBase64(string str)
        {
            string strResult = "";

            if ((str != null) && (str != ""))
            {
                strResult = System.Convert.ToBase64String(Encoding.Default.GetBytes(str));
            }

            return strResult;
        }

        /// <summary>
        /// �� Base64 ����תΪ�ַ���
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DecodeBase64(string str)
        {
            string strResult = "";

            if ((str != null) && (str != ""))
            {
                strResult = Encoding.Default.GetString(System.Convert.FromBase64String(str));
            }

            return strResult;
        }

        /// <summary>
        /// Byte[] ת����16�����ַ���
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string BytesToString(byte[] bytes)
        {
            string Result = "0x";

            if (bytes.Length == 0)
            {
                return Result;
            }

            foreach (byte b in bytes)
            {
                Result += b.ToString("X").PadLeft(2, '0');
            }

            return Result;
        }

        /// <summary>
        /// �ַ���ת������
        /// </summary>
        /// <param name="input"></param>
        /// <param name="srcEncoding"></param>
        /// <param name="tarEncoding"></param>
        /// <returns></returns>
        public static string ConvertEncoding(string input, string srcEncoding, string tarEncoding)
        {
            Encoding _srcEncoding = Encoding.GetEncoding(srcEncoding);
            Encoding _tarEncoding = Encoding.GetEncoding(tarEncoding);

            return ConvertEncoding(input, _srcEncoding, _tarEncoding);
        }

        /// <summary>
        /// �ַ���ת������
        /// </summary>
        /// <param name="input"></param>
        /// <param name="srcEncoding"></param>
        /// <param name="tarEncoding"></param>
        /// <returns></returns>
        public static string ConvertEncoding(string input, Encoding srcEncoding, Encoding tarEncoding)
        {
            if (srcEncoding == tarEncoding)
            {
                return input;
            }

            byte[] temp = srcEncoding.GetBytes(input);
            byte[] temp2 = Encoding.Convert(srcEncoding, tarEncoding, temp);

            return tarEncoding.GetString(temp2);
        }

        /// <summary>
        /// У�����
        /// </summary>
        public static class Valid
        {
            /// <summary>
            /// У�� Email ��ʽ
            /// </summary>
            /// <param name="email"></param>
            /// <returns></returns>
            public static bool IsEmail(string email)
            {
                return Regex.IsMatch(email, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
            }

            /// <summary>
            /// У�����֤��ʽ_��½
            /// </summary>
            /// <param name="IDCardNumber"></param>
            /// <returns></returns>
            public static bool IsIDCardNumber(string IDCardNumber)
            {
                return Regex.IsMatch(IDCardNumber, @"(^\d{17}|^\d{14})(\d|x|X|y|Y)$");
            }

            /// <summary>
            /// У�����֤��ʽ_̨��
            /// </summary>
            /// <param name="IDCardNumber"></param>
            /// <returns></returns>
            public static bool IsIDCardNumber_Taiwan(string IDCardNumber)
            {
                return Regex.IsMatch(IDCardNumber, @"[A-Za-z][12]\d{8}");
            }

            /// <summary>
            /// У�����֤��ʽ_���
            /// </summary>
            /// <param name="IDCardNumber"></param>
            /// <returns></returns>
            public static bool IsIDCardNumber_Hongkong(string IDCardNumber)
            {
                return Regex.IsMatch(IDCardNumber, @"[A-Za-z]{1,2}\d{6}\([Aa0-9]\)");
            }

            /// <summary>
            /// У�����֤��ʽ_�¼���
            /// </summary>
            /// <param name="IDCardNumber"></param>
            /// <returns></returns>
            public static bool IsIDCardNumber_Singapore(string IDCardNumber)
            {
                return Regex.IsMatch(IDCardNumber, @"\d{7}[A-JZa-jz]");
            }

            /// <summary>
            /// У�����֤��ʽ_����
            /// </summary>
            /// <param name="IDCardNumber"></param>
            /// <returns></returns>
            public static bool IsIDCardNumber_Macau(string IDCardNumber)
            {
                return Regex.IsMatch(IDCardNumber, @"\d{7}\([0-9]\)");
            }

            /// <summary>
            /// У�����п���ʽ
            /// </summary>
            /// <param name="bankCardNumber"></param>
            /// <returns></returns>
            public static bool IsBankCardNumber(string bankCardNumber)
            {
                int valid_BankCardNumberMinLength = Convert.StrToInt(System.Configuration.ConfigurationManager.AppSettings["Valid_BankCardNumberMinLength"], 12);
                int valid_BankCardNumberMaxLength = Convert.StrToInt(System.Configuration.ConfigurationManager.AppSettings["Valid_BankCardNumberMaxLength"], 22);

                if (valid_BankCardNumberMinLength < 1)
                {
                    valid_BankCardNumberMinLength = 1;
                }

                if (valid_BankCardNumberMaxLength < valid_BankCardNumberMinLength)
                {
                    valid_BankCardNumberMaxLength = valid_BankCardNumberMinLength;
                }

                string Pattern = "^\\d{" + valid_BankCardNumberMinLength.ToString() + "," + valid_BankCardNumberMaxLength.ToString() + "}$";
                return Regex.IsMatch(bankCardNumber, Pattern);
            }

            /// <summary>
            /// У������ʱ���ʽ
            /// </summary>
            /// <param name="dateTimeString"></param>
            /// <returns></returns>
            public static bool IsDateTime(string dateTimeString)
            {
                return Regex.IsMatch(dateTimeString, @"^((\d{2}(([02468][048])|([13579][26]))[\-\/\s]?((((0?[13578])|(1[02]))[\-\/\s]?((0?[1-9])|([1-2][0-9])|(3[01])))|(((0?[469])|(11))[\-\/\s]?((0?[1-9])|([1-2][0-9])|(30)))|(0?2[\-\/\s]?((0?[1-9])|([1-2][0-9])))))|(\d{2}(([02468][1235679])|([13579][01345789]))[\-\/\s]?((((0?[13578])|(1[02]))[\-\/\s]?((0?[1-9])|([1-2][0-9])|(3[01])))|(((0?[469])|(11))[\-\/\s]?((0?[1-9])|([1-2][0-9])|(30)))|(0?2[\-\/\s]?((0?[1-9])|(1[0-9])|(2[0-8]))))))(\s(((0?[1-9])|(1[0-2]))\:([0-5][0-9])((\s)|(\:([0-5][0-9])\s))([AM|PM|am|pm]{2,2})))?$");
            }

            /// <summary>
            /// У�����ڸ�ʽ
            /// </summary>
            /// <param name="dateString"></param>
            /// <returns></returns>
            public static bool IsDate(string dateString)
            {
                return Regex.IsMatch(dateString, @"^\d{4}[\-\/\s]?((((0[13578])|(1[02]))[\-\/\s]?(([0-2][0-9])|(3[01])))|(((0[469])|(11))[\-\/\s]?(([0-2][0-9])|(30)))|(02[\-\/\s]?[0-2][0-9]))$");
            }

            /// <summary>
            /// У�� IP ��ַ��ʽ
            /// </summary>
            /// <param name="address"></param>
            /// <returns></returns>
            public static bool IsIPAddress(string address)
            {
                return Regex.IsMatch(address, @"^(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])$");
            }

            /// <summary>
            /// У��������ʽ����ʽҪ�������� http://  ftp://  https:// �ȵ�ǰ׺
            /// </summary>
            /// <param name="url"></param>
            /// <returns></returns>
            public static bool IsUrl(string url)
            {
                return IsUrl(url, true);
            }

            /// <summary>
            /// У��������ʽ
            /// </summary>
            /// <param name="url"></param>
            /// <param name="withPreFix">�Ƿ���Ҫ�� http:// ftp:// https:// ��ǰ׺</param>
            /// <returns></returns>
            public static bool IsUrl(string url, bool withPreFix)
            {
                if (withPreFix)
                {
                    return Regex.IsMatch(url, @"^(http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&:/~\+#]*[\w\-\@?^=%&/~\+#])?$");
                }
                else
                {
                    return Regex.IsMatch(url, @"^[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&:/~\+#]*[\w\-\@?^=%&/~\+#])?$");
                }

            }

            /// <summary>
            /// У���ֻ�����
            /// </summary>
            /// <param name="mobileNumber"></param>
            /// <returns></returns>
            public static bool IsMobile(string mobileNumber)
            {
                return Regex.IsMatch(mobileNumber, @"^((13[0-9])|(14[5,7])|(15[0-3,5-9])|(17[0,3,5-8])|(18[0-9])|166|198|199|(147))\d{8}$");
            }
        }
    }
}
