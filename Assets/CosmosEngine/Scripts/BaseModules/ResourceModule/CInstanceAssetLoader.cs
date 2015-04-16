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

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;

/// <summary>
/// 这是拷一份出来的
/// </summary>
public class CInstanceAssetLoader : CBaseResourceLoader
{
    public delegate void CAssetLoaderDelegate(bool isOk, UnityEngine.Object asset, object[] args);

    private CAssetFileLoader _assetFileBridge;  // 引用ResultObject
    public override float Progress
    {
        get
        {
            return _assetFileBridge.Progress;
        }
    }

    public List<UnityEngine.Object> CopyAssets = new List<Object>();

    private UnityEngine.Object CopyAsset()
    {
        if (ResultObject == null)
        {
            CDebug.LogError("[CInstanceAssetLoader:TheAsset] Null Load Asset: {0}", this.Url);
            return null;
        }

        Object copyAsset = null;
        try
        {
            copyAsset = GameObject.Instantiate(ResultObject as UnityEngine.Object);
        }
        catch (Exception e)
        {
            CDebug.LogException(e);
            return null;
        }
        

        CopyAssets.Add(copyAsset);

        return copyAsset;
    }

    // TODO: 无视AssetName暂时！
    public static CInstanceAssetLoader Load(string url, CAssetFileLoader.CAssetFileBridgeDelegate callback = null)
    {
        var loader = AutoNew<CInstanceAssetLoader>(url);

        CResourceModule.Instance.StartCoroutine(CoLoad(loader, callback));

        return loader;
    }

    static IEnumerator CoLoad(CInstanceAssetLoader loader, CAssetFileLoader.CAssetFileBridgeDelegate callback)
    {
        while (!loader.IsFinished)
            yield return null;
        if (loader.IsReadyDisposed)
        {
            callback(false, null);
        }
        else
        {
            var newCopyAsset = loader.CopyAsset();
            if (Application.isEditor)
            {
                CResourceLoadObjectDebugger.Create("AssetCopy", loader.Url, newCopyAsset);
            }

            callback(newCopyAsset != null, newCopyAsset);
        }
    }

    protected override void Init(string url)
    {
        base.Init(url);
        _assetFileBridge = CAssetFileLoader.Load(url, (isOk, asset) =>
        {
            OnFinish(asset);
        });
    }

    //仅仅是预加载，回调仅告知是否加载成功
    public static CAssetFileLoader Preload(string path, System.Action<bool> callback = null)
    {
        return CAssetFileLoader.Load(path, (isOk, asset) =>
        {
            if (callback != null)
                callback(isOk);
        });
    }

    //仅仅是预加载，回调仅告知是否加载成功
    public static IEnumerator CoPreload(string path, System.Action<bool> callback = null)
    {
        var w = CAssetFileLoader.Load(path, null);

        while (!w.IsFinished)
            yield return null;

        if (callback != null)
            callback(!w.IsError);  // isOk?
    }

    public override void Release()
    {
        base.Release();

        // 立刻清理拷贝的对象, 但loader保留
        if (RefCount <= 0)
        {
            // 确保复制品删除
            foreach (var copyAsset in CopyAssets)
            {
                Object.Destroy(copyAsset);
            }
            CopyAssets.Clear();
        }
    }

    protected override void DoDispose()
    {
        base.DoDispose();
        _assetFileBridge.Release();

    }
}
