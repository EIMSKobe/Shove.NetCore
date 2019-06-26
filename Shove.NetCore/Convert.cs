using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Data;
using System.Text.RegularExpressions;

using Microsoft.International.Converters.PinYinConverter;

namespace Shove
{
    /// <summary>
    /// 常用的字符，Asc的转换以及其他转换工具。
    /// </summary>
    public static class Convert
    {
        /// <summary>
        /// 字符串转 Short
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static short StrToShort(string str, short defaultValue)
        {
            short result;

            if (!short.TryParse(str, out result))
            {
                result = defaultValue;
            }

            return result;
        }

        /// <summary>
        /// 字符串转 Int
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int StrToInt(string str, int defaultValue)
        {
            int result;

            if (!int.TryParse(str, out result))
            {
                result = defaultValue;
            }

            return result;
        }

        /// <summary>
        /// 字符串转 Long
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static long StrToLong(string str, long defaultValue)
        {
            long result;

            if (!long.TryParse(str, out result))
            {
                result = defaultValue;
            }

            return result;
        }

        /// <summary>
        /// 字符串转 Double
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double StrToDouble(string str, double defaultValue)
        {
            double result;

            if (!double.TryParse(str, out result))
            {
                result = defaultValue;
            }
            else if (double.IsNaN(result))
            {
                result = 0.0d;
            }

            return result;
        }

        /// <summary>
        /// 字符串转 Float
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static float StrToFloat(string str, float defaultValue)
        {
            float result;

            if (!float.TryParse(str, out result))
            {
                result = defaultValue;
            }
            else if (float.IsNaN(result))
            {
                result = 0.0f;
            }

            return result;
        }

        /// <summary>
        /// 字符串转 Float
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal StrToDecimal(string str, decimal defaultValue)
        {
            decimal result = defaultValue;

            if (!decimal.TryParse(str, out result))
            {
                result = defaultValue;
            }

            return result;
        }

        /// <summary>
        /// 字符串转 Boolean
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool StrToBool(string str, bool defaultValue)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(str.Trim()))
            {
                return defaultValue;
            }

            str = str.Trim();

            if ((str == "-1") || (str == "1") || (str.ToLower() == "y") || (str.ToLower() == "yes") || (str.ToLower() == "t") || (str.ToLower() == "true"))
            {
                return true;
            }

            if ((str == "0") || (str.ToLower() == "n") || (str.ToLower() == "no") || (str.ToLower() == "f") || (str.ToLower() == "false"))
            {
                return false;
            }

            bool result;

            if (!bool.TryParse(str, out result))
            {
                result = defaultValue;
            }

            return result;
        }

        /// <summary>
        /// 字符串转时间格式
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static DateTime StrToDateTime(string str, string defaultValue)
        {
            DateTime result;

            if (!DateTime.TryParse(str, out result))
            {
                result = DateTime.Parse(defaultValue);
            }

            return result;
        }

        /// <summary>
        /// 字符转ASCII
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static int Asc(char ch)
        {
            return (int)ch;
        }

        /// <summary>
        /// ASCII 转字符
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static char Chr(int i)
        {
            return (char)i;
        }

        /// <summary>
        /// 普通 TextBox 的文本，需要通过这个转换后，Label 显示才会不丢失“回车”、“空格”等格式。
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <returns></returns>
        public static string ToHtmlCode(string sourceStr)
        {
            return sourceStr.Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "''").Replace(" ", "&nbsp;").Replace("\r\n", "<br/>").Replace("\n", "<br/>").Trim();
        }

        /// <summary>
        /// 如果经过 ToHtmlCode 转换过，可以让 Label 显示，但要 TextBox 显示，则应该用这个函数转换回来，再赋值给 TextBox 控件显示。
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <returns></returns>
        public static string ToTextCode(string sourceStr)
        {
            return sourceStr.Replace("&lt;", "<").Replace("&gt;", ">").Replace("''", "'").Replace("&nbsp;", " ").Replace("<br/>", "\r\n").Replace("<br>", "\n").Trim();
        }

        /// <summary>
        /// 转全角的函数(SBC case)
        /// </summary>
        /// <param name="input">任意字符串</param>
        /// <returns>全角字符串</returns>
        ///<remarks>
        ///全角空格为12288，半角空格为32
        ///其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
        ///</remarks>        
        public static string ToSBC(string input)
        {
            //半角转全角：
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 32)
                {
                    c[i] = (char)12288;
                    continue;
                }
                if (c[i] < 127)
                    c[i] = (char)(c[i] + 65248);
            }
            return new string(c);
        }

        /// <summary>
        /// 转半角的函数(DBC case)
        /// </summary>
        /// <param name="input">任意字符串</param>
        /// <returns>半角字符串</returns>
        ///<remarks>
        ///全角空格为12288，半角空格为32
        ///其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
        ///</remarks>
        public static string ToDBC(string input)
        {
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 12288)
                {
                    c[i] = (char)32;
                    continue;
                }
                if (c[i] > 65280 && c[i] < 65375)
                    c[i] = (char)(c[i] - 65248);
            }
            return new string(c);
        }

        /// <summary>
        /// 中文大写金额转换
        /// </summary>
        public class ChineseMoney
        {
            /// <summary>
            /// 
            /// </summary>
            public string Yuan = "元";                        // “元”，可以改为“圆”、“卢布”之类
            /// <summary>
            /// 
            /// </summary>
            public string Jiao = "角";                        // “角”，可以改为“拾”
            /// <summary>
            /// 
            /// </summary>
            public string Fen = "分";                        // “分”，可以改为“美分”之类

            static string Digit = "零壹贰叁肆伍陆柒捌玖";      // 大写数字

            bool isAllZero = true;                        // 片段内是否全零
            bool isPreZero = true;                        // 低一位数字是否是零
            bool Overflow = false;                       // 溢出标志
            long money100;                                   // 金额*100，即以“分”为单位的金额
            long value;                                      // money100的绝对值

            StringBuilder sb = new StringBuilder();         // 大写金额字符串，逆序

            /// <summary>
            /// 只读属性: "零元"
            /// </summary>
            public string ZeroString
            {
                get { return Digit[0] + Yuan; }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="money"></param>
            public ChineseMoney(decimal money)
            {
                try { money100 = (long)(money * 100m); }
                catch { Overflow = true; }
                if (money100 == long.MinValue) Overflow = true;
            }

            /// <summary>
            /// 重载 ToString() 方法，返回大写金额字符串
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                if (Overflow) return "金额超出范围";
                if (money100 == 0) return ZeroString;
                string[] Unit = { Yuan, "万", "亿", "万", "亿亿" };
                value = Math.Abs(money100);
                ParseSection(true);
                for (int i = 0; i < Unit.Length && value > 0; i++)
                {
                    if (isPreZero && !isAllZero) sb.Append(Digit[0]);
                    if (i == 4 && sb.ToString().EndsWith(Unit[2], StringComparison.Ordinal))
                        sb.Remove(sb.Length - Unit[2].Length, Unit[2].Length);
                    sb.Append(Unit[i]);
                    ParseSection(false);
                    if ((i % 2) == 1 && isAllZero)
                        sb.Remove(sb.Length - Unit[i].Length, Unit[i].Length);
                }
                if (money100 < 0) sb.Append("负");
                return String.Reverse(sb.ToString());
            }

            // 解析“片段”: “角分(2位)”或“万以内的一段(4位)”
            void ParseSection(bool isJiaoFen)
            {
                string[] Unit = isJiaoFen ?
                    new string[] { Fen, Jiao } :
                    new string[] { "", "拾", "佰", "仟" };
                isAllZero = true;
                for (int i = 0; i < Unit.Length && value > 0; i++)
                {
                    int d = (int)(value % 10);
                    if (d != 0)
                    {
                        if (isPreZero && !isAllZero) sb.Append(Digit[0]);
                        sb.AppendFormat("{0}{1}", Unit[i], Digit[d]);
                        isAllZero = false;
                    }
                    isPreZero = (d == 0);
                    value /= 10;
                }
            }
        }

        /// <summary>
        /// 汉字相关
        /// </summary>
        public class Chinese
        {
            /// <summary>
            /// 转换到繁体
            /// </summary>
            /// <param name="str"></param>
            /// <returns></returns>
            public static string ToTraditional(string str)
            {
                System.Globalization.CultureInfo cl = new System.Globalization.CultureInfo("zh-CN", false);
                return Microsoft.VisualBasic.Strings.StrConv(str, Microsoft.VisualBasic.VbStrConv.TraditionalChinese, cl.LCID);
            }

            /// <summary>
            /// 转换到简体
            /// </summary>
            /// <param name="str"></param>
            /// <returns></returns>
            public static string ToSimplified(string str)
            {
                System.Globalization.CultureInfo cl = new System.Globalization.CultureInfo("zh-CN", false);
                return Microsoft.VisualBasic.Strings.StrConv(str, Microsoft.VisualBasic.VbStrConv.SimplifiedChinese, cl.LCID);
            }

            /// <summary>
            /// 转换汉字到拼音
            /// </summary>
            /// <param name="input">输入的汉字字串</param>
            /// <returns></returns>
            public static string ToPinYin(string input)
            {
                return ToPinYin(input, 1);
            }

            /// <summary>
            /// 转换汉字到拼音首字母
            /// </summary>
            /// <param name="input">输入的汉字字串</param>
            /// <returns></returns>
            public static string ToPinYinFirstCharacter(string input)
            {
                return ToPinYin(input, 2);
            }

            /// <summary>
            /// 转换汉字到完整拼音(带声调)
            /// </summary>
            /// <param name="input">输入的汉字字串</param>
            /// <returns></returns>
            public static string ToPinYinFull(string input)
            {
                return ToPinYin(input, 3);
            }

            /// <summary>
            /// 转换汉字到拼音
            /// </summary>
            /// <param name="input"></param>
            /// <param name="type">1 全拼 2 声母 3 原始完整（带声调）</param>
            /// <returns></returns>
            private static string ToPinYin(string input, int type)
            {
                if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(input.Trim()))
                {
                    return "";
                }

                string result = "";

                foreach (char ch in input.ToCharArray())
                {
                    ChineseChar x = null;
                    try
                    {
                        x = new ChineseChar(ch);
                    }
                    catch
                    {
                        result += ch.ToString();

                        continue;
                    }

                    System.Collections.ObjectModel.ReadOnlyCollection<string> roc = null;

                    try
                    {
                        roc = x.Pinyins;
                    }
                    catch
                    {
                        result += ch.ToString();

                        continue;
                    }

                    if ((roc == null) || (roc.Count == 0))
                    {
                        result += ch.ToString();

                        continue;
                    }

                    if (type == 3)
                    {
                        result += roc[0];
                    }
                    else
                    {
                        string s = FormatPinYinResult(roc[0]);
                        result += (type == 1 ? s : s[0].ToString());
                    }
                }

                return result;
            }

            private static string FormatPinYinResult(string input)
            {
                if (string.IsNullOrEmpty(input))
                {
                    return "";
                }

                if ((input[input.Length - 1] >= '0') && (input[input.Length - 1] <= '9'))
                {
                    input = input.Substring(0, input.Length - 1);
                }

                return input.ToLower();
            }
        }

        /// <summary>
        /// DataTable 转换到 XML 字符串
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string DataTableToXML(DataTable dt)
        {
            MemoryStream stream = null;
            XmlTextWriter writer = null;

            string Result = "";

            try
            {
                stream = new MemoryStream();
                writer = new XmlTextWriter(stream, Encoding.UTF8);

                dt.WriteXml(writer);

                int count = (int)stream.Length;
                byte[] buffer = new byte[count];

                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(buffer, 0, count);

                Result = new UTF8Encoding().GetString(buffer).Trim();
            }
            catch
            {

            }
            finally
            {
                if (writer != null) writer.Close();
            }

            return Result;
        }

        /// <summary>
        /// Xml 数据转为 DataTable
        /// </summary>
        /// <param name="Xml"></param>
        /// <returns></returns>
        public static DataTable XMLToDataTable(string Xml)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            DataSet ds = new DataSet();

            try
            {
                stream = new StringReader(Xml);
                reader = new XmlTextReader(stream);
                ds.ReadXml(reader);
            }
            catch
            {
                ds = null;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }

                if (stream != null)
                {
                    stream.Close();
                }
            }

            if ((ds == null) || (ds.Tables.Count < 1))
            {
                return null;
            }

            return ds.Tables[0];
        }

        /// <summary>
        /// Unicode \u1234 类型的编码转为汉字
        /// </summary>
        /// <param name="input">为 \u1234\u2345\u2345 类型的 Unicode 汉字编码字符串</param>
        /// <returns>返回为汉字字符串</returns>
        public static string UnicodeToChinese(string input)
        {
            Match m;
            Regex r = new Regex("(?<code>\\\\u[a-z0-9]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            for (m = r.Match(input); m.Success; m = m.NextMatch())
            {
                string str = m.Result("${code}");
                int num = Int32.Parse(str.Substring(2, 4), System.Globalization.NumberStyles.HexNumber);
                string ch = string.Format("{0}", (char)num);
                input = input.Replace(str, ch);
            }

            return input;
        }
    }
}
