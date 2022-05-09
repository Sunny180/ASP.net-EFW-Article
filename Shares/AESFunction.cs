using System;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace SMTW_Management
{
    public class AESFunction
    {
        private readonly IConfiguration _configuration;

        public AESFunction(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 加密
        /// </summary>
        public string Encrypt(string secretKey, string secretIv, string text)
        {
            string result = "";

            try
            {
                // Base64 Decode Key, Iv
                Byte[] bytesKey = Convert.FromBase64String(secretKey); // 還原 Byte
                Byte[] bytesIv = Convert.FromBase64String(secretIv); // 還原 Byte
                string strKey = Encoding.UTF8.GetString(bytesKey); // 還原 UTF8 字元
                string strIv = Encoding.UTF8.GetString(bytesIv); // 還原 UTF8 字元

                //  AES 256 加密
                byte[] sourceBytes = Encoding.UTF8.GetBytes(text);
                Aes aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = Convert.FromBase64String(strKey);
                aes.IV = Convert.FromBase64String(strIv);

                ICryptoTransform transform = aes.CreateEncryptor();
                string data = System.Convert.ToBase64String(transform.TransformFinalBlock(sourceBytes, 0, sourceBytes.Length));

                //  Base64 Encode
                Byte[] bytesEncode = Encoding.UTF8.GetBytes(data);
                result = Convert.ToBase64String(bytesEncode);

            }
            catch (Exception e)
            {
                result = e.Message;
                throw;
            }
            return result;
        }

        /// <summary>
        /// 解密
        /// </summary>
        public string Decrypt(string secretKey, string secretIv, string text)
        {
            string result = "";

            try
            {
                // Base64 Decode Key, Iv
                Byte[] bytesKey = Convert.FromBase64String(secretKey); // 還原 Byte
                Byte[] bytesIv = Convert.FromBase64String(secretIv); // 還原 Byte
                string strKey = Encoding.UTF8.GetString(bytesKey); // 還原 UTF8 字元
                string strIv = Encoding.UTF8.GetString(bytesIv); // 還原 UTF8 字元

                // Base64 Decode
                Byte[] bytesDecode = Convert.FromBase64String(text); // 還原 Byte
                string resultText = Encoding.UTF8.GetString(bytesDecode); // 還原 UTF8 字元

                // AES 256 解密
                byte[] encryptBytes = System.Convert.FromBase64String(resultText);
                Aes aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = Convert.FromBase64String(strKey);
                aes.IV = Convert.FromBase64String(strIv);
                ICryptoTransform transform = aes.CreateDecryptor();
                result = Encoding.UTF8.GetString(transform.TransformFinalBlock(encryptBytes, 0, encryptBytes.Length));
            }
            catch (Exception e)
            {
                result = e.Message;
                throw;
            }
            return result;
        }

    }
}