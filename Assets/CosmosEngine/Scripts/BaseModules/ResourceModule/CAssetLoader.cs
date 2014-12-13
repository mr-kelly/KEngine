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
using System.Collections;
using System.Collections.Generic;

public class CAssetLoader
{
    public delegate void CAssetLoaderDelegate(bool isOk, UnityEngine.Object asset, object[] args);

    public CAssetLoaderDelegate Callback;
    public object[] CallbackArgs;

    Object ResultAsset;

    public bool IsFinished = false;

    public Object Asset { get { return ResultAsset; } }

    public CAssetLoader(string path, string assetName = null, CAssetLoaderDelegate callback = null, params object[] args)
    {
        object[] newArgs = new object[2];
        newArgs[0] = assetName;
        newArgs[1] = args;

        Callback = callback;
        CallbackArgs = args;

        new CAssetFileBridge(path, assetName, OnAssetLoaded);
    }

    void OnAssetLoaded(bool isOk, UnityEngine.Object asset)
    {
        ResultAsset = Object.Instantiate(asset);

        OnFinish(isOk);
    }

    void OnFinish(bool isOk)
    {
        if (Callback != null)
            Callback(isOk, ResultAsset, CallbackArgs);

        IsFinished = true;
    }

    //仅仅是预加载，回调仅告知是否加载成功
    public static void Preload(string path, string assetName = null, System.Action<bool> callback = null)
    {
        new CAssetFileBridge(path, assetName, (isOk, asset) =>
        {
            if (callback != null)
                callback(isOk);
        }); 
    }

    //仅仅是预加载，回调仅告知是否加载成功
    public static IEnumerator CoPreload(string path, string assetName = null, System.Action<bool> callback = null)
    {
        bool waiting = true;
        var w = new CAssetFileBridge(path, assetName, null);

        while (!w.IsFinished)
            yield return null;

        if (callback != null)
            callback(!w.IsError);  // isOk?
    }
}
