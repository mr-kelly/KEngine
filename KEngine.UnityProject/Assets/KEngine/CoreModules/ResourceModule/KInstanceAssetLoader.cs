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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 这是拷一份出来的
/// </summary>
public class KInstanceAssetLoader : KAbstractResourceLoader
{
    public delegate void KAssetLoaderDelegate(bool isOk, UnityEngine.Object asset, object[] args);

    public GameObject InstanceAsset { get; private set; }
    private KAssetFileLoader _assetFileBridge;  // 引用ResultObject
    public override float Progress
    {
        get
        {
            return _assetFileBridge.Progress;
        }
    }

    // TODO: 无视AssetName暂时！
    public static KInstanceAssetLoader Load(string url, KAssetFileLoader.CAssetFileBridgeDelegate callback = null)
    {
        var loader = AutoNew<KInstanceAssetLoader>(url, (ok, resultObject) =>
        {
            if (callback != null)
                callback(ok, resultObject as UnityEngine.Object);
        }, true);

        return loader;
    }

    protected override void Init(string url, params object[] args)
    {
        base.Init(url, args);

        _assetFileBridge = KAssetFileLoader.Load(url, (isOk, asset) =>
        {
            if (IsReadyDisposed)  // 中途释放
            {
                OnFinish(null);
                return;
            }
            if (!isOk)
            {
                OnFinish(null);
                Logger.LogError("[InstanceAssetLoader]Error on assetfilebridge loaded... {0}", url);
                return;
            }

            try
            {
                InstanceAsset = (GameObject)GameObject.Instantiate(asset as UnityEngine.GameObject);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }

            if (Application.isEditor)
            {
                KResoourceLoadedAssetDebugger.Create("AssetCopy", url, InstanceAsset);
            }

            OnFinish(InstanceAsset);
        });
    }

    //仅仅是预加载，回调仅告知是否加载成功
    public static KAssetFileLoader Preload(string path, System.Action<bool> callback = null)
    {
        return KAssetFileLoader.Load(path, (isOk, asset) =>
        {
            if (callback != null)
                callback(isOk);
        });
    }

    protected override void DoDispose()
    {
        base.DoDispose();
        
        _assetFileBridge.Release();
        if (InstanceAsset != null)
        {
            Object.Destroy(InstanceAsset);
            InstanceAsset = null;
        }
    }


    //仅仅是预加载，回调仅告知是否加载成功
    public static IEnumerator CoPreload(string path, System.Action<bool> callback = null)
    {
        var w = KAssetFileLoader.Load(path, null);

        while (!w.IsFinished)
            yield return null;

        if (callback != null)
            callback(!w.IsError);  // isOk?
    }

}
