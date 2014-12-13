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
public class CAssetBundleLoader
{
    public delegate void CAssetBundleLoaderDelegate(bool isOk, string fullUrl, AssetBundle ab, object[] args);

    class XLoadCache
    {
        public string RelativeUrl;
        public AssetBundle Ab;
        public bool IsLoadedFinish { get { return Ab != null; }}
    }

    static Dictionary<string, XLoadCache> AssetBundlesCache = new Dictionary<string, XLoadCache>();

    public bool IsError { get; private set; }
    public bool IsFinished { get { return Bundle != null; } }
    public AssetBundle Bundle { get; private set; }

    CAssetBundleLoaderDelegate Callback;  // full url, ab, args
    object[] CallbackArgs;

    string RelativeResourceUrl;
    string FullUrl;

    public CAssetBundleLoader(string url, CAssetBundleLoaderDelegate callback = null, params object[] callbackArgs)
    {
        IsError = false;
        Callback = callback;
        CallbackArgs = callbackArgs;

        RelativeResourceUrl = url;
        if (CResourceModule.GetResourceFullPath(url, out FullUrl))
        {
            CResourceModule.LogRequest("AssetBundle", FullUrl);

            CResourceModule.Instance.StartCoroutine(LoadAssetBundle(RelativeResourceUrl));
        }
        else
        {
            CDebug.LogError("[CAssetBundleLoader]Error Path: {0}", url);
            IsError = true;

            if (Callback != null)
                Callback(false, url, Bundle, CallbackArgs);
        }
    }
    
    IEnumerator LoadAssetBundle(string relativeUrl)
    {
        XLoadCache loadCache;    
        if (!AssetBundlesCache.TryGetValue(RelativeResourceUrl, out loadCache))
        {
            loadCache = new XLoadCache() { RelativeUrl = RelativeResourceUrl, Ab = null };
            AssetBundlesCache[RelativeResourceUrl] = loadCache;

            CWWWLoader wwwLoader = new CWWWLoader(FullUrl);
            while (!wwwLoader.IsFinished)
                yield return null;
            if (wwwLoader.IsError)
            {
                CDebug.LogError("[CAssetBundleLoader]Error Load AssetBundle: {0}", relativeUrl);
                IsError = true;
                if (Callback != null)
                    Callback(false, FullUrl, Bundle, CallbackArgs);
                yield break;
            }
            else
            {
                // 解密
                CAssetBundleParser parser = new CAssetBundleParser(RelativeResourceUrl, wwwLoader.Www.bytes);
                while (!parser.IsFinished)
                {
                    yield return null;
                }

                if (parser.Bundle == null)
                    CDebug.LogError("WWW.assetBundle is NULL: {0}", FullUrl);

                loadCache.Ab = parser.Bundle;

                // 释放WWW加载的字节。。释放该部分内存，因为AssetBundle已经自己有缓存了
                wwwLoader.Dispose();
            }


        }
        else
        {
            if (loadCache.IsLoadedFinish)
            {
                yield return null;  // 确保每一次异步都延迟一帧
            }

            while (!loadCache.IsLoadedFinish)
            {
                yield return null; //Wait
            }
        }

        Bundle = loadCache.Ab;

        if (Callback != null)
            Callback(true, FullUrl, Bundle, CallbackArgs);
    }

    /// 舊的tips~忽略
    /// 原以为，每次都通过getter取一次assetBundle会有序列化解压问题，会慢一点，后用AddWatch调试过，发现如果把.assetBundle放到Dictionary里缓存，查询会更慢
    /// 因为，估计.assetBundle是一个纯Getter，没有做序列化问题。  （不保证.mainAsset）
}
