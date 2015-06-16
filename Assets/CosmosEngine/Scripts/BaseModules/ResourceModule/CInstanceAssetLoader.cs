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

    private Object _newCopyAsset = null;
    private CAssetFileLoader _assetFileBridge;  // 引用ResultObject
    public override float Progress
    {
        get
        {
            return _assetFileBridge.Progress;
        }
    }

    // TODO: 无视AssetName暂时！
    public static CInstanceAssetLoader Load(string url, CAssetFileLoader.CAssetFileBridgeDelegate callback = null)
    {
        var loader = AutoNew<CInstanceAssetLoader>(url, (ok, resultObject) =>
        {
            if (callback != null)
                callback(ok, resultObject as UnityEngine.Object);
        }, true);

        return loader;
    }

    protected override void Init(string url)
    {
        base.Init(url);

        _assetFileBridge = CAssetFileLoader.Load(url, (isOk, asset) =>
        {
            if (IsReadyDisposed)  // 中途释放
            {
                OnFinish(null);
                return;
            }
            if (!isOk)
            {
                OnFinish(null);
                CDebug.LogError("[InstanceAssetLoader]Error on assetfilebridge loaded... {0}", url);
                return;
            }

            try
            {
                _newCopyAsset = GameObject.Instantiate(asset as UnityEngine.Object);
            }
            catch (Exception e)
            {
                CDebug.LogException(e);
            }

            if (Application.isEditor)
            {
                CResourceLoadObjectDebugger.Create("AssetCopy", url, _newCopyAsset);
            }

            OnFinish(_newCopyAsset);
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

    protected override void DoDispose()
    {
        base.DoDispose();
        
        _assetFileBridge.Release();
        if (_newCopyAsset != null)
        {
            Object.Destroy(_newCopyAsset);
            _newCopyAsset = null;
        }
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

}
