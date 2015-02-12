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

// 調用WWWLoader
public class CAssetBundleLoader : CBaseResourceLoader
{
    public delegate void CAssetBundleLoaderDelegate(bool isOk, AssetBundle ab);

    private CAssetBundleParser BundleParser;
    //private bool UnloadAllAssets; // Dispose时赋值
    public AssetBundle Bundle
    {
        get { return ResultObject as AssetBundle; }
    }

    string RelativeResourceUrl;
    string FullUrl;

    protected override void Init(string url)
    {
        base.Init(url);

        RelativeResourceUrl = url;
        if (CResourceModule.GetResourceFullPath(url, out FullUrl))
        {
            CResourceModule.LogRequest("AssetBundle", FullUrl);
            CResourceModule.Instance.StartCoroutine(LoadAssetBundle(url));
        }
        else
        {
            CDebug.LogError("[CAssetBundleLoader]Error Path: {0}", url);
            OnFinish(null);
        }
    }
    public static CAssetBundleLoader Load(string url, CAssetBundleLoaderDelegate callback = null)
    {
        CLoaderDelgate newCallback = null;
        if (callback != null)
        {
            newCallback = (isOk, obj) => callback(isOk, obj as AssetBundle);
        }
        var newLoader = AutoNew<CAssetBundleLoader>(url, newCallback);


        return newLoader;
    }

    IEnumerator LoadAssetBundle(string relativeUrl)
    {
        var wwwLoader = CWWWLoader.Load(FullUrl);
        while (!wwwLoader.IsFinished)
        {
            Progress = wwwLoader.Progress / 2f;  // 最多50%， 要算上Parser的嘛
            yield return null;
        }
        if (wwwLoader.IsError)
        {
            CDebug.LogError("[CAssetBundleLoader]Error Load AssetBundle: {0}", relativeUrl);
            OnFinish(null);
            wwwLoader.Release();
            yield break;
        }
        else
        {
            // 提前结束

            // 解密
            // 释放WWW加载的字节。。释放该部分内存，因为AssetBundle已经自己有缓存了
            var cloneBytes = (byte[])wwwLoader.Www.bytes.Clone();
            wwwLoader.Release();

            BundleParser = new CAssetBundleParser(RelativeResourceUrl, cloneBytes);
            while (!BundleParser.IsFinished)
            {
                if (IsDisposed)  // 中途释放
                {
                    OnFinish(null);
                    yield break;
                }
                Progress = BundleParser.Progress + 1/2f;  // 最多50%， 要算上WWWLoader的嘛
                yield return null;
            }
            var assetBundle = BundleParser.Bundle;

            if (assetBundle == null)
                CDebug.LogError("WWW.assetBundle is NULL: {0}", FullUrl);

            OnFinish(assetBundle);

            //Array.Clear(cloneBytes, 0, cloneBytes.Length);  // 手工释放内存
        }

        
        //GC.Collect(0);// 手工释放内存
    }

    override protected void DoDispose()
    {
        base.DoDispose();

        if (BundleParser != null)
            BundleParser.Dispose(false);
    }

    /// 舊的tips~忽略
    /// 原以为，每次都通过getter取一次assetBundle会有序列化解压问题，会慢一点，后用AddWatch调试过，发现如果把.assetBundle放到Dictionary里缓存，查询会更慢
    /// 因为，估计.assetBundle是一个纯Getter，没有做序列化问题。  （不保证.mainAsset）

}
