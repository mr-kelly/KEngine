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
using System;
using UnityEngine;
using System.Collections;
using KEngine;

namespace KEngine
{
    /// <summary>
    /// CosmosEngine Prefs的封装，带有加密!
    /// </summary>
    public class CPrefs
    {
        private readonly Crypt Crypter;

        public CPrefs(ulong secretKey)
        {
            var bytes = BitConverter.GetBytes(secretKey);  // 8 bytes secret key
            Crypter = new Crypt(bytes);
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
