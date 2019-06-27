using System;
using System.Collections.Generic;
using System.Text;

namespace Shove.Security
{
    /// <summary>
    /// Ses 对称加密
    /// </summary>
    public class Ses
    {
        private byte[] SES_IV = { 0xe0, 0x20, 0x3a, 0x08, 0x49, 0x06, 0x24, 0x5c, 0xc2, 0x29, 0xac, 0x12, 0x91, 0x95, 0xe4, 0x79 };
        private byte[] key = new byte[16];

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="key"></param>
        /// <param name="encodingName"></param>
        public Ses(string key, string encodingName)
            : this(key, Encoding.GetEncoding(encodingName))
        {
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="key"></param>
        /// <param name="encoding"></param>
        public Ses(string key, System.Text.Encoding encoding)
        {
            int len = key.Length;

            if (len < 16)
            {
                throw new Exception("key length must be greater than or equal to 16 characters.");
            }

            Array.Copy(encoding.GetBytes(key), 0, this.key, 0, 16);

            for (int i = 0; i < 16; i++)
            {
                this.key[i] ^= SES_IV[i];
            }
        }

        /// <summary>
        /// SES 加密，input, output 均为已经申请好了内存的指针。output 的大小可以用 GetResultLength(input) 获得。
        /// </summary>
        /// <param name="input">源文</param>
        /// <param name="output">密文</param>
        public void Encrypt(byte[] input, byte[] output)
        {
            int len = ComplementInput(input, output);
            EncryptMatrixTransform(output, len);
            Xor(output, len);
        }

        /// <summary>
        /// SES 解密，input, output 均为已经申请好了内存的指针。output 的大小先与 input 设置为相同，ResultLength 参数将返回实际的长度。
        /// </summary>
        /// <param name="input">密文</param>
        /// <param name="len">源串的长度</param>
        /// <param name="output">源文</param>
        /// <param name="decryptResultLength">密文的实际长度，之前无法预知</param>
        public void Decrypt(byte[] input, int len, byte[] output, ref int decryptResultLength)
        {
            if (len < 8)
            {
                throw new Exception("The encryption result of insufficeient length, the minimum length should be 8.");
            }

            Array.Copy(input, output, len);
            Xor(output, len);
            DecryptMatrixTransform(output, len);
            decryptResultLength = GetDecryptResultLength(output, len);
        }

        /// <summary>
        /// 预知加密后的长度（根据源串，获得加密结果的长度）
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public int GetEncryptResultLength(byte[] input)
        {
            int len = input.Length;
            int complement_len = 8 - len % 8;

            if (complement_len == 1)
            {
                complement_len = 9;
            }

            return complement_len + len;
        }

        private int GetDecryptResultLength(byte[] output, int len)
        {
            if (len < 8)
            {
                throw new Exception("The encryption result of insufficeient length, the minimum length should be 8.");
            }

            int complement_len = 0;
            int i = len - 1;

            while (output[i--] == 0)
            {
                complement_len++;
            }

            int num = output[i + 1];

            if ((complement_len > 0) && (num != complement_len))
            {
                throw new Exception("Invalid ciphertext format.");
            }

            return (complement_len == 0) ? len : len - complement_len - 1;
        }

        private int ComplementInput(byte[] input, byte[] output)
        {
            int len = input.Length;
            int complement_len = GetEncryptResultLength(input) - len;

            Array.Copy(input, output, len);

            if (complement_len == 0)
            {
                return len;
            }

            for (int i = len; i < len + complement_len; i++)
            {
                output[i] = 0;
            }

            output[len] = (byte)(complement_len - 1);

            return len + complement_len;
        }

        private void EncryptMatrixTransform(byte[] output, int len)
        {
            int row = len / 4;
            byte[] t = new byte[row];

            for (int i = 0; i < row; i++)
            {
                t[i] = output[i * 4];
            }

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    output[i * 4 + j] = output[i * 4 + j + 1];
                }

                output[i * 4 + 3] = t[i];
            }

            t = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                t[i] = output[i];
            }

            for (int i = 0; i < row - 1; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    output[i * 4 + j] = output[(i + 1) * 4 + j];
                }
            }

            for (int i = 0; i < 4; i++)
            {
                output[(row - 1) * 4 + i] = t[i];
            }
        }

        private void DecryptMatrixTransform(byte[] output, int len)
        {
            int row = len / 4;
            byte[] t = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                t[i] = output[(row - 1) * 4 + i];
            }

            for (int i = row - 1; i > 0; i--)
            {
                for (int j = 0; j < 4; j++)
                {
                    output[i * 4 + j] = output[(i - 1) * 4 + j];
                }
            }

            for (int i = 0; i < 4; i++)
            {
                output[i] = t[i];
            }

            t = new byte[row];

            for (int i = 0; i < row; i++)
            {
                t[i] = output[i * 4 + 3];
            }

            for (int i = 0; i < row; i++)
            {
                for (int j = 3; j > 0; j--)
                {
                    output[i * 4 + j] = output[i * 4 + j - 1];
                }

                output[i * 4] = t[i];
            }
        }

        private void Xor(byte[] output, int len)
        {
            int key_position = 0;

            for (int i = 0; i < len; i++)
            {
                output[i] ^= this.key[key_position++];

                if (key_position >= 16)
                {
                    key_position = 0;
                }
            }
        }
    }
}