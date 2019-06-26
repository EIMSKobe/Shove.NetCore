using System;
using System.Text;

namespace Shove
{
    /// <summary>
    /// �ַ�����ء�
    /// </summary>
    public class Byte
    {
        /// <summary>
        /// ���� Byte[] ������ȫ�Ƚ�
        /// </summary>
        /// <param name="input1"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static bool ByteCompare(byte[] input1, byte[] input2)
        {
            if ((input1 == null) || (input2 == null))
            {
                return false;
            }

            if (input1.Length != input2.Length)
            {
                return false;
            }

            for (int i = 0; i < input1.Length; i++)
            {
                if (input1[i] != input2[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// �� Source ����Ĳ���Ԫ�� CopyTo Destination ������ƶ���ʼλ��
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="destination"></param>
        /// <param name="destinationStartIndex"></param>
        public static void ByteCopy(byte[] source, int startIndex, int count, byte[] destination, int destinationStartIndex)
        {
            for (int i = startIndex; i < startIndex + count; i++)
            {
                destination[destinationStartIndex + i - startIndex] = source[i];
            }
        }

        /// <summary>
        /// �� Buffer ��ָ����λ����ȡһ���µ�������
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static byte[] ExtractBytesFromBuffer(byte[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                return null;
            }

            if (index >= buffer.Length)
            {
                return null;
            }

            byte[] result = new byte[count];

            for (int i = index; i < index + count; i++)
            {
                result[i - index] = buffer[i];
            }

            return result;
        }

        /// <summary>
        /// �� Buffer ��ָ����λ����ȡһ���ַ���
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static string ExtractStringFromBuffer(byte[] buffer, int index, int count)
        {
            string str = Encoding.Default.GetString(buffer, index, count);

            while (str.Length > 0 && str[str.Length - 1] == '\0')
            {
                str = str.Remove(str.Length - 1);
            }

            return str;
        }

        /// <summary>
        /// �� Buffer ��ָ����λ����ȡһ�� int
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static int ExtractIntFromBuffer(byte[] buffer, int index, int count)
        {
            return BitConverter.ToInt32(buffer, index);
        }

        /// <summary>
        /// �� Buffer ��ָ����λ����ȡһ�� lonog
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static long ExtractLongFromBuffer(byte[] buffer, int index, int count)
        {
            return BitConverter.ToInt64(buffer, index);
        }

        /// <summary>
        /// �� Buffer ��ָ����λ����ȡһ�� float
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static float ExtractFloatFromBuffer(byte[] buffer, int index, int count)
        {
            return BitConverter.ToSingle(buffer, index);
        }

        /// <summary>
        /// �� Buffer ��ָ����λ����ȡһ�� double
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static double ExtractDoubleFromBuffer(byte[] buffer, int index, int count)
        {
            return BitConverter.ToDouble(buffer, index);
        }

        /// <summary>
        /// Byte[] ת����16�����ַ��������� 0x ǰ׺�� _String ���д˷�������������� 0x ǰ׺
        /// </summary>
        /// <param name="index">input bytes[]</param>
        /// <returns>16�����ַ�����0x......</returns>
        public static string BytesToHexString(byte[] index)
        {
            string result = "";

            if (index.Length == 0)
            {
                return result;
            }

            foreach (byte b in index)
            {
                result += b.ToString("X").PadLeft(2, '0');
            }

            return result;
        }

        /// <summary>
        /// 16�������ִ�תΪ��ͨ�ַ���
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string HexToString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            if (input.Length % 2 != 0)
            {
                return "";
            }

            string Result = "";

            for (int i = 0; i < input.Length / 2; i++)
            {
                string str = input.Substring(i * 2, 2);
                byte b = 0;

                try
                {
                    b = System.Convert.ToByte(str, 16);
                }
                catch
                {
                    return "";
                }

                Result += ((char)b).ToString();
            }

            return Result;
        }

        /// <summary>
        /// 16�������ִ�תΪ Byte ����
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] HexToBytes(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            if (input.Length % 2 != 0)
            {
                return null;
            }

            byte[] Result = new byte[input.Length / 2];

            for (int i = 0; i < input.Length / 2; i++)
            {
                string str = input.Substring(i * 2, 2);

                byte b;

                try
                {
                    b = System.Convert.ToByte(str, 16);
                }
                catch
                {
                    return null;
                }

                Result[i] = b;
            }

            return Result;
        }

        /// <summary>
        /// �ַ���תΪ Byte[]
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string StringToHex(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            string Result = "";
            byte[] Buffer = Encoding.Default.GetBytes(input);

            foreach (byte b in Buffer)
            {
                Result += string.Format("{0:X}", b);
            }

            return Result;
        }
    }
}
