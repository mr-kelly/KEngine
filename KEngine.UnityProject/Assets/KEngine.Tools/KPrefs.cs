#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KPrefs.cs
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
using UnityEngine;

namespace KEngine
{
    /// <summary>
    /// CosmosEngine Prefs的封装，带有加密!
    /// </summary>
    public class KPrefs
    {
        private readonly KCrypt Crypter;

        public KPrefs(ulong secretKey)
        {
            var bytes = BitConverter.GetBytes(secretKey); // 8 bytes secret key
            Crypter = new KCrypt(bytes);
        }

        public string GetKey(string key, bool crypt = true)
        {
            var content = PlayerPrefs.GetString(key);
            if (!string.IsNullOrEmpty(content))
                return crypt ? Crypter.DecryptDES(content) : content;

            return null;
        }

        public void SetKey(string key, string content, bool crypt = true)
        {
            if (string.IsNullOrEmpty(content))
                PlayerPrefs.DeleteKey(key);
            else
                PlayerPrefs.SetString(key, crypt ? Crypter.EncryptDES(content) : content);

            PlayerPrefs.Save();
        }
    }
}