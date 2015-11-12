//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Version 0.9.1 (20151010)
//                     Copyright © 2011-2015
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using KEngine;
using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using System;
using System.Text;
using System.IO;

namespace KEngine
{
    /// <summary>
    /// 加密解密，依赖表CosmosEngineConfig DEC
    /// </summary>
    public class Crypt
    {
        public byte[] CustomKeys = null;
        private readonly byte[] DefaultKeys = { 0x00, 0x01, 0x02, 0x03, 0xAB, 0xCD, 0xEF, 0x05 };

        public Crypt()
        {
            CustomKeys = DefaultKeys;
        }
        public Crypt(byte[] keys)
        {
            CustomKeys = keys;
        }

        /// DES加密字符串        
        /// 待加密的字符串
        /// 加密密钥,要求为8位
        /// 加密成功返回加密后的字符串，失败返回源串 
        public string EncryptDES(string encryptString)
        {
            string encryptKey = KEngine.GetConfig("CryptKey");// 钥匙
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
                byte[] rgbIV = CustomKeys ?? DefaultKeys;
                byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);
                DESCryptoServiceProvider dCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, dCSP.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Convert.ToBase64String(mStream.ToArray());
            }
            catch
            {
                return encryptString;
            }
        }
        /// 
        /// DES解密字符串        
        /// 待解密的字符串
        /// 解密密钥,要求为8位,和加密密钥相同
        /// 解密成功返回解密后的字符串，失败返源串
        public string DecryptDES(string decryptString)
        {
            string decryptKey = KEngine.GetConfig("CryptKey");
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey);
                byte[] rgbIV = CustomKeys ?? DefaultKeys;
                byte[] inputByteArray = Convert.FromBase64String(decryptString);
                DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Encoding.UTF8.GetString(mStream.ToArray());
            }
            catch
            {
                return decryptString;
            }
        }
    }

}