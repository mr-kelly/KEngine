//-------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//-------------------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class CAssetBundleParser
{
    string RelativeUrl;
    Action<AssetBundle> Callback;

    AssetBundleCreateRequest CreateRequest;

    public bool IsFinished { get { return CreateRequest.isDone; } }
    public AssetBundle Bundle {get {return CreateRequest.assetBundle;}}

    public CAssetBundleParser(string relativePath, byte[] bytes, Action<AssetBundle> callback = null)
    {
        RelativeUrl = relativePath;
        Callback = callback;

        CreateRequest = ParseAb(RelativeUrl, bytes);  // 不重複創建...

        CResourceManager.Instance.StartCoroutine(WaitCreateAssetBundle(CreateRequest));
    }


    IEnumerator WaitCreateAssetBundle(AssetBundleCreateRequest req)
    {
        while (!req.isDone)
        {
            yield return null;
        }

        if (Callback != null)
            Callback(Bundle);
    }


    static AssetBundleCreateRequest ParseAb(string relativePath, byte[] bytes)
    {
        CBase.Assert(bytes.Length > 2);

        // first step
        if (bytes[1] == 255)
            bytes[0] = 0;
        else
            bytes[1]++;

        // second step
        byte[] endMd5 = new byte[16];

        for (int i = 0; i < endMd5.Length; i++)
        {
            int copyFromIndex = i + bytes.Length - 16;
            endMd5[i] = bytes[copyFromIndex];
        }

        if (!CBaseTool.ArraysEqual<byte>(endMd5, CBaseTool.MD5_bytes(relativePath)))
        {
            CBase.LogError("Error when Decrypt AssetBundle: {0}", relativePath);
            return null;
        }

        Array.Resize<byte>(ref bytes, bytes.Length - 16);  // cut end

        return AssetBundle.CreateFromMemory(bytes);
    }
}
