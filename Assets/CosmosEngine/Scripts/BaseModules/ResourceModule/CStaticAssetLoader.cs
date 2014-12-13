//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                          Version 0.8
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CStaticAssetLoader
{
    static Dictionary<string, Object> CachcedAssets = new Dictionary<string, Object>();

    Object ResultAsset;

    public bool IsFinished = false;

    public Object Asset { get { return ResultAsset; } }

    public CStaticAssetLoader(string path, CResourceModule.ASyncLoadABAssetDelegate callback = null, params object[] args)
    {
        if (string.IsNullOrEmpty(path))
            CBase.LogError("XStaticAssetLoader 空资源路径!");

        new CAssetFileBridge(path, (_isOk, _obj) =>
        {
            Object asset = null;
            if (!CachcedAssets.TryGetValue(path, out asset))
            {
                asset = Object.Instantiate(_obj);
                CachcedAssets[path] = asset;
            }
            
            if (callback != null)
                callback(asset, args);

            OnLoad(path, asset);

        });
    }

    void OnLoad(string path, UnityEngine.Object staticAsset)
    {
        ResultAsset = staticAsset;

        IsFinished = true;
    }
}
