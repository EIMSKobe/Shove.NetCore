using System;
using System.Text;
using System.Security.Cryptography;
using System.Data;

namespace Shove.Security
{
    /// <summary>
    /// 字符串加密、解密函数
    /// </summary>
    public static class Encrypt
    {
        /// <summary>
        /// MD5 摘要，使用指定的字符集
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string MD5(string input)
        {
            return MD5(input, Encoding.Default);
        }

        /// <summary>
        /// MD5 摘要，使用指定的字符集
        /// </summary>
        /// <param name="input"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string MD5(string input, Encoding encoding)
        {
            using (MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider())
            {
                byte[] t = provider.ComputeHash(encoding.GetBytes(input));
                StringBuilder sb = new StringBuilder(32);

                for (int i = 0; i < t.Length; i++)
                {
                    sb.Append(t[i].ToString("x").PadLeft(2, '0'));
                }

                return sb.ToString();
            }
        }

        #region DES 加密解密

        /// <summary>
        /// 3DES 机密
        /// </summary>
        /// <param name="input"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string Encrypt3DES(string input, string key)
        {
            using (TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider())
            {
                des.Key = Encoding.UTF8.GetBytes(key);
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.Zeros;

                ICryptoTransform DESEncrypt = des.CreateEncryptor();

                byte[] buffer = Encoding.UTF8.GetBytes(input);
                byte[] encryptResult = DESEncrypt.TransformFinalBlock(buffer, 0, buffer.Length);

                string result = "";
                foreach (byte b in encryptResult)
                {
                    result += b.ToString("X").PadLeft(2, '0');
                }

                return result;
            }
        }

        /// <summary>
        /// 3DES 解密
        /// </summary>
        /// <param name="input"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string Decrypt3DES(string input, string key)
        {
            using (TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider())
            {
                des.Key = Encoding.UTF8.GetBytes(key);
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.Zeros;

                ICryptoTransform DESDecrypt = des.CreateDecryptor();

                byte[] Buffer = new byte[input.Length / 2];

                for (int i = 0; i < input.Length / 2; i++)
                {
                    try
                    {
                        Buffer[i] = (byte)System.Convert.ToInt16(input.Substring(i * 2, 2), 16);
                    }
                    catch
                    {
                        return "";
                    }
                }

                string Result = "";
                try
                {
                    Result = Encoding.UTF8.GetString(DESDecrypt.TransformFinalBlock(Buffer, 0, Buffer.Length));
                }
                catch
                {

                }
                return Decrypt3DES_TrimZreo(Result);
            }
        }

        private static string Decrypt3DES_TrimZreo(string input)
        {
            while (input.EndsWith("\0", StringComparison.Ordinal))
            {
                input = input.Substring(0, input.Length - 1);
            }

            return input;
        }

        #endregion

        #region AES 加密解密

        private static byte[] AES_IV = { 0x41, 0x72, 0x65, 0x79, 0x6F, 0x75, 0x6D, 0x79, 0x53, 0x6E, 0x6F, 0x77, 0x6D, 0x61, 0x6E, 0x3F };
        // Key = "12345678901234567890123456789012"; Key 示例，要求 32 位

        /// <summary>
        /// AES 加密
        /// </summary>
        /// <param name="input">待加密的字符串</param>
        /// <param name="key">加密密钥,要求为32位</param>
        /// <returns>加密成功返回加密后的字符串，失败 throw</returns>
        public static string EncryptAES(string input, string key)
        {
            byte[] inputData = Encoding.UTF8.GetBytes(input);

            RijndaelManaged rijndaelProvider = new RijndaelManaged();
            rijndaelProvider.Key = Encoding.UTF8.GetBytes(key.Substring(0, 32));
            rijndaelProvider.IV = AES_IV;
            ICryptoTransform rijndaelEncrypt = rijndaelProvider.CreateEncryptor();

            byte[] encryptedData = rijndaelEncrypt.TransformFinalBlock(inputData, 0, inputData.Length);

            return System.Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// AES 解密
        /// </summary>
        /// <param name="input">待解密的字符串</param>
        /// <param name="key">解密密钥,要求为32位,和加密密肯嗤?/param>
        /// <returns>解密成功返回解密后的字符串，失败 throw</returns>
        public static string DecryptAES(string input, string key)
        {
            byte[] inputData = System.Convert.FromBase64String(input);

            RijndaelManaged rijndaelProvider = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key.Substring(0, 32)),
                IV = AES_IV
            };
            ICryptoTransform rijndaelDecrypt = rijndaelProvider.CreateDecryptor();

            byte[] decryptedData = rijndaelDecrypt.TransformFinalBlock(inputData, 0, inputData.Length);
            rijndaelProvider.Dispose();

            return Encoding.UTF8.GetString(decryptedData);
        }

        #endregion

        #region SES 加密解密，与 ShoveEIMS3 的 C++ 代码兼容

        /// <summary>
        /// 加密，与 ShoveEIMS3 的 C++ 代码兼容
        /// </summary>
        /// <param name="input"></param>
        /// <param name="key"></param>
        /// <param name="encodingName"></param>
        /// <returns></returns>
        public static string EncryptSES(string input, string key, string encodingName)
        {
            Ses ses = new Ses(key, encodingName);

            byte[] byte_input = Encoding.GetEncoding(encodingName).GetBytes(input);
            int len = ses.GetEncryptResultLength(byte_input);

            byte[] output = new byte[len];
            ses.Encrypt(byte_input, output);

            return System.Convert.ToBase64String(output);
        }

        /// <summary>
        /// 解密，与 ShoveEIMS3 的 C++ 代码兼容
        /// </summary>
        /// <param name="input"></param>
        /// <param name="key"></param>
        /// <param name="encodingName"></param>
        /// <returns></returns>
        public static string DecryptSES(string input, string key, string encodingName)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            Ses ses = new Ses(key, encodingName);

            byte[] byte_input = System.Convert.FromBase64String(input);
            byte[] temp_output = new byte[input.Length];

            int output_len = 0;
            ses.Decrypt(byte_input, byte_input.Length, temp_output, ref output_len);

            byte[] ouput = new byte[output_len];
            Array.Copy(temp_output, ouput, output_len);

            return Encoding.GetEncoding(encodingName).GetString(ouput);
        }

        #endregion

        #region 对参数列表进行 MD5 签名

        /// <summary>
        /// 对参数进行签名
        /// </summary>
        /// <param name="key"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static string ParamterSignature(string key, params object[] _params)
        {
            string signSource = "";

            foreach (object param in _params)
            {
                signSource += ParamterToString(param);
            }

            return MD5(signSource + key);
        }

        private static string ParamterToString(object param)
        {
            if (param is DateTime)
            {
                return ((DateTime)param).ToString("yyyyMMddHHmmss");
            }
            else if (param is DataTable)
            {
                return Convert.DataTableToXML((DataTable)param);
            }
            else if (param is DataSet)
            {
                return ((DataSet)param).GetXml();
            }
            else if (param is string)
            {
                return (string)param;
            }

            return param.ToString();
        }

        #endregion
    }
}