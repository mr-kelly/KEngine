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
using System.Collections;
using System.Collections.Generic;
using KEngine;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 静态对象加载，通常用于全局唯一的GameObject，  
/// 跟其它TextureLoader不一样的是,它会拷一份
/// 原加载对象(AssetFileBridge)会被删除，节省内存
/// </summary>
public class KStaticAssetLoader : KAbstractResourceLoader
{
    public UnityEngine.Object TheAsset // Copy
    {
        get { return (UnityEngine.Object) ResultObject; }
    }
        
    private KAssetFileLoader _assetFileLoader;
    public override float Progress
    {
        get
        {
            return _assetFileLoader.Progress;
        }
    }
    public static KStaticAssetLoader Load(string url, KAssetFileLoader.CAssetFileBridgeDelegate callback = null)
    {
        CLoaderDelgate newCallback = null;
        if (callback != null)
        {
            newCallback = (isOk, obj) => callback(isOk, obj as UnityEngine.Object);
        }

        return AutoNew<KStaticAssetLoader>(url, newCallback);
    }

    protected override void Init(string path)
    {
        base.Init(path);
        if (string.IsNullOrEmpty(path))
            Logger.LogError("XStaticAssetLoader 空资源路径!");

        _assetFileLoader = KAssetFileLoader.Load(path, (_isOk, _obj) =>
        {
            OnFinish(_obj);

            if (Application.isEditor)
                if (TheAsset != null)
                    KResoourceLoadedAssetDebugger.Create("StaticAsset", path, TheAsset);
        });
    }

    protected override void OnFinish(object resultObj)
    {
        // 拷一份
        var copyAsset = Object.Instantiate(resultObj as UnityEngine.Object);

        base.OnFinish(copyAsset);
    }

    protected override void DoDispose()
    {
        base.DoDispose();

        GameObject.Destroy(TheAsset);
        _assetFileLoader.Release();
    }
}
