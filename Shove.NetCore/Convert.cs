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
    /// ���õ��ַ���Asc��ת���Լ�����ת�����ߡ�
    /// </summary>
    public static class Convert
    {
        /// <summary>
        /// �ַ���ת Short
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
        /// �ַ���ת Int
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
        /// �ַ���ת Long
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
        /// �ַ���ת Double
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
        /// �ַ���ת Float
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
        /// �ַ���ת Float
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
        /// �ַ���ת Boolean
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
        /// �ַ���תʱ���ʽ
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
        /// �ַ�תASCII
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static int Asc(char ch)
        {
            return (int)ch;
        }

        /// <summary>
        /// ASCII ת�ַ�
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static char Chr(int i)
        {
            return (char)i;
        }

        /// <summary>
        /// ��ͨ TextBox ���ı�����Ҫͨ�����ת����Label ��ʾ�Ż᲻��ʧ���س��������ո񡱵ȸ�ʽ��
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <returns></returns>
        public static string ToHtmlCode(string sourceStr)
        {
            return sourceStr.Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "''").Replace(" ", "&nbsp;").Replace("\r\n", "<br/>").Replace("\n", "<br/>").Trim();
        }

        /// <summary>
        /// ������� ToHtmlCode ת������������ Label ��ʾ����Ҫ TextBox ��ʾ����Ӧ�����������ת���������ٸ�ֵ�� TextBox �ؼ���ʾ��
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <returns></returns>
        public static string ToTextCode(string sourceStr)
        {
            return sourceStr.Replace("&lt;", "<").Replace("&gt;", ">").Replace("''", "'").Replace("&nbsp;", " ").Replace("<br/>", "\r\n").Replace("<br>", "\n").Trim();
        }

        /// <summary>
        /// תȫ�ǵĺ���(SBC case)
        /// </summary>
        /// <param name="input">�����ַ���</param>
        /// <returns>ȫ���ַ���</returns>
        ///<remarks>
        ///ȫ�ǿո�Ϊ12288����ǿո�Ϊ32
        ///�����ַ����(33-126)��ȫ��(65281-65374)�Ķ�Ӧ��ϵ�ǣ������65248
        ///</remarks>        
        public static string ToSBC(string input)
        {
            //���תȫ�ǣ�
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
        /// ת��ǵĺ���(DBC case)
        /// </summary>
        /// <param name="input">�����ַ���</param>
        /// <returns>����ַ���</returns>
        ///<remarks>
        ///ȫ�ǿո�Ϊ12288����ǿո�Ϊ32
        ///�����ַ����(33-126)��ȫ��(65281-65374)�Ķ�Ӧ��ϵ�ǣ������65248
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
        /// ���Ĵ�д���ת��
        /// </summary>
        public class ChineseMoney
        {
            /// <summary>
            /// 
            /// </summary>
            public string Yuan = "Ԫ";                        // ��Ԫ�������Ը�Ϊ��Բ������¬����֮��
            /// <summary>
            /// 
            /// </summary>
            public string Jiao = "��";                        // ���ǡ������Ը�Ϊ��ʰ��
            /// <summary>
            /// 
            /// </summary>
            public string Fen = "��";                        // ���֡������Ը�Ϊ�����֡�֮��

            static string Digit = "��Ҽ��������½��ƾ�";      // ��д����

            bool isAllZero = true;                        // Ƭ�����Ƿ�ȫ��
            bool isPreZero = true;                        // ��һλ�����Ƿ�����
            bool Overflow = false;                       // �����־
            long money100;                                   // ���*100�����ԡ��֡�Ϊ��λ�Ľ��
            long value;                                      // money100�ľ���ֵ

            StringBuilder sb = new StringBuilder();         // ��д����ַ���������

            /// <summary>
            /// ֻ������: "��Ԫ"
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
            /// ���� ToString() ���������ش�д����ַ���
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                if (Overflow) return "������Χ";
                if (money100 == 0) return ZeroString;
                string[] Unit = { Yuan, "��", "��", "��", "����" };
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
                if (money100 < 0) sb.Append("��");
                return String.Reverse(sb.ToString());
            }

            // ������Ƭ�Ρ�: ���Ƿ�(2λ)���������ڵ�һ��(4λ)��
            void ParseSection(bool isJiaoFen)
            {
                string[] Unit = isJiaoFen ?
                    new string[] { Fen, Jiao } :
                    new string[] { "", "ʰ", "��", "Ǫ" };
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
        /// �������
        /// </summary>
        public class Chinese
        {
            /// <summary>
            /// ת��������
            /// </summary>
            /// <param name="str"></param>
            /// <returns></returns>
            public static string ToTraditional(string str)
            {
                System.Globalization.CultureInfo cl = new System.Globalization.CultureInfo("zh-CN", false);
                return Microsoft.VisualBasic.Strings.StrConv(str, Microsoft.VisualBasic.VbStrConv.TraditionalChinese, cl.LCID);
            }

            /// <summary>
            /// ת��������
            /// </summary>
            /// <param name="str"></param>
            /// <returns></returns>
            public static string ToSimplified(string str)
            {
                System.Globalization.CultureInfo cl = new System.Globalization.CultureInfo("zh-CN", false);
                return Microsoft.VisualBasic.Strings.StrConv(str, Microsoft.VisualBasic.VbStrConv.SimplifiedChinese, cl.LCID);
            }

            /// <summary>
            /// ת�����ֵ�ƴ��
            /// </summary>
            /// <param name="input">����ĺ����ִ�</param>
            /// <returns></returns>
            public static string ToPinYin(string input)
            {
                return ToPinYin(input, 1);
            }

            /// <summary>
            /// ת�����ֵ�ƴ������ĸ
            /// </summary>
            /// <param name="input">����ĺ����ִ�</param>
            /// <returns></returns>
            public static string ToPinYinFirstCharacter(string input)
            {
                return ToPinYin(input, 2);
            }

            /// <summary>
            /// ת�����ֵ�����ƴ��(������)
            /// </summary>
            /// <param name="input">����ĺ����ִ�</param>
            /// <returns></returns>
            public static string ToPinYinFull(string input)
            {
                return ToPinYin(input, 3);
            }

            /// <summary>
            /// ת�����ֵ�ƴ��
            /// </summary>
            /// <param name="input"></param>
            /// <param name="type">1 ȫƴ 2 ��ĸ 3 ԭʼ��������������</param>
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
        /// DataTable ת���� XML �ַ���
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
        /// Xml ����תΪ DataTable
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
        /// Unicode \u1234 ���͵ı���תΪ����
        /// </summary>
        /// <param name="input">Ϊ \u1234\u2345\u2345 ���͵� Unicode ���ֱ����ַ���</param>
        /// <returns>����Ϊ�����ַ���</returns>
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
