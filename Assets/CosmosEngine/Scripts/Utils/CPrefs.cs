using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// CosmosEngine Prefs的封装，带有加密!
/// </summary>
public class CPrefs
{
    private readonly CCrypt Crypter;

    public CPrefs(ulong secretKey)
    {
        var bytes = BitConverter.GetBytes(secretKey);  // 8 bytes secret key
        Crypter = new CCrypt(bytes);
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
