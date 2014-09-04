//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                         version 0.8
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
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

    public static Func<string, byte[], AssetBundleCreateRequest> ParseFunc = null; // 可以放置資源加密函數

    public CAssetBundleParser(string relativePath, byte[] bytes, Action<AssetBundle> callback = null)
    {
        RelativeUrl = relativePath;
        Callback = callback;

        var func = ParseFunc ?? DefaultParseAb;
        CreateRequest = func(RelativeUrl, bytes);  // 不重複創建...

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


    static AssetBundleCreateRequest DefaultParseAb(string relativePath, byte[] bytes)
    {
        return AssetBundle.CreateFromMemory(bytes);
    }
}
