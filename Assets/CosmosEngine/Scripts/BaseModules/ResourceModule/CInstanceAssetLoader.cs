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

/// <summary>
/// 这是拷一份出来的
/// </summary>
public class CInstanceAssetLoader : CBaseResourceLoader
{
    public delegate void CAssetLoaderDelegate(bool isOk, UnityEngine.Object asset, object[] args);

    private CAssetFileLoader AssetFileBridge;  // 引用ResultObject
    public override float Progress
    {
        get
        {
            return AssetFileBridge.Progress;
        }
    }

    private UnityEngine.Object TheAsset
    {
        get
        {
            if (ResultObject == null)
            {
                CDebug.LogError("[CInstanceAssetLoader:TheAsset] Null Load Asset: {0}", this.Url);
                CDebug.Assert(false);
            }
            return GameObject.Instantiate(ResultObject as UnityEngine.Object);
        }
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
        if (loader.IsDisposed)
        {
            callback(false, null);
        }
        else
        {
            var newCopyAsset = loader.TheAsset;
#if UNITY_EDITOR
            CResourceLoadObjectDebugger.Create("AssetCopy", loader.Url, newCopyAsset);
#endif

            callback(newCopyAsset != null, newCopyAsset);  
        }
    }

    protected override void Init(string url)
    {
        base.Init(url);
        AssetFileBridge = CAssetFileLoader.Load(url, (isOk, asset) =>
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

    protected override void DoDispose()
    {
        base.DoDispose();

        AssetFileBridge.Release();
    }
}
