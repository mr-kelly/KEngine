#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KCrypt.cs
// Date:     2015/12/03
// Author:  Kelly
// Email: 23110388@qq.com
// Github: https://github.com/mr-kelly/KEngine
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.

#endregion

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KEngine
{
    /// <summary>
    /// 加密解密，依赖表CosmosEngineConfig DEC
    /// </summary>
    public class KCrypt
    {
        public byte[] CustomKeys = null;
        private readonly byte[] DefaultKeys = {0x00, 0x01, 0x02, 0x03, 0xAB, 0xCD, 0xEF, 0x05};

        public KCrypt()
        {
            CustomKeys = DefaultKeys;
        }

        public KCrypt(byte[] keys)
        {
            CustomKeys = keys;
        }

        /// DES加密字符串        
        /// 待加密的字符串
        /// 加密密钥,要求为8位
        /// 加密成功返回加密后的字符串，失败返回源串
        public string EncryptDES(string encryptString)
        {
            string encryptKey = AppEngine.GetConfig("KEngine", "CryptKey") ?? "testkey"; // 钥匙
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
                byte[] rgbIV = CustomKeys ?? DefaultKeys;
                byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);
                DESCryptoServiceProvider dCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, dCSP.CreateEncryptor(rgbKey, rgbIV),
                    CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Convert.ToBase64String(mStream.ToArray());
            }
            catch
            {
                return encryptString;
            }
        }

        /// DES解密字符串        
        /// 待解密的字符串
        /// 解密密钥,要求为8位,和加密密钥相同
        /// 解密成功返回解密后的字符串，失败返源串
        public string DecryptDES(string decryptString)
        {
            string decryptKey = AppEngine.GetConfig("KEngine", "CryptKey") ?? "testkey";
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey);
                byte[] rgbIV = CustomKeys ?? DefaultKeys;
                byte[] inputByteArray = Convert.FromBase64String(decryptString);
                DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV),
                    CryptoStreamMode.Write);
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