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
using System.Collections;
using System.Collections.Generic;

public class XAssetLoader
{
    public CResourceManager.ASyncLoadABAssetDelegate Callback;
    public object[] CallbackArgs;

    Object ResultAsset;

    public bool IsFinished = false;

    public Object Asset { get { return ResultAsset; } }

    public XAssetLoader(string path, string assetName = null, CResourceManager.ASyncLoadABAssetDelegate callback = null, params object[] args)
    {
        object[] newArgs = new object[2];
        newArgs[0] = assetName;
        newArgs[1] = args;

        Callback = callback;
        CallbackArgs = args;

        new CAssetFileBridge(path, assetName, OnAssetLoaded, newArgs);
    }

    void OnAssetLoaded(UnityEngine.Object asset, object[] args)
    {
        ResultAsset = Object.Instantiate(asset);

        OnFinish();
    }

    void OnFinish()
    {
        if (Callback != null)
            Callback(ResultAsset, CallbackArgs);

        IsFinished = true;
    }
}
