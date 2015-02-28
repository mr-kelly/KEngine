//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                     Version 0.8 (20140904)
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AssetBundle字节解析器
/// </summary>
public class CAssetBundleParser
{
    private bool IsDisposed = false;
    private bool UnloadAllAssets; // Dispose时赋值

    readonly Action<AssetBundle> Callback;
    public bool IsFinished;
    public AssetBundle Bundle;
    public static Func<string, byte[], AssetBundleCreateRequest> ParseFunc = null; // 可以放置資源加密函數
    private static int AutoPriority = 1;
    private readonly AssetBundleCreateRequest CreateRequest;
    public float Progress {get { return CreateRequest.progress; }}

    public CAssetBundleParser(string relativePath, byte[] bytes, Action<AssetBundle> callback = null)
    {
        Callback = callback;

        var func = ParseFunc ?? DefaultParseAb;
        CreateRequest = func(relativePath, bytes);
        CreateRequest.priority = AutoPriority++; // 后进先出
        CResourceModule.Instance.StartCoroutine(WaitCreateAssetBundle(CreateRequest));
    }


    IEnumerator WaitCreateAssetBundle(AssetBundleCreateRequest req)
    {
        while (!req.isDone)
        {
            yield return null;
        }

        IsFinished = true;
        Bundle = req.assetBundle;

        if (IsDisposed)
            DisposeBundle();
        else
        {
            if (Callback != null)
                Callback(Bundle);    
        }
    }


    static AssetBundleCreateRequest DefaultParseAb(string relativePath, byte[] bytes)
    {
        return AssetBundle.CreateFromMemory(bytes);
    }

    private void DisposeBundle()
    {
        Bundle.Unload(UnloadAllAssets);
    }

    public void Dispose(bool unloadAllAssets)
    {
        UnloadAllAssets = unloadAllAssets;
        if (Bundle != null)
            DisposeBundle();
        IsDisposed = true;
    }
}
