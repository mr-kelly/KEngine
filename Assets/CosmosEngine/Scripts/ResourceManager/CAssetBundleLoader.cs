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
using System;
using System.Collections;
using System.Collections.Generic;

// 調用WWWLoader
public class CAssetBundleLoader
{
    class XLoadCache
    {
        public string RelativeUrl;
        public AssetBundle Ab;
        public bool IsLoadedFinish { get { return Ab != null; }}
    }

    static Dictionary<string, XLoadCache> AssetBundlesCache = new Dictionary<string, XLoadCache>();

    public bool IsFinished { get { return Bundle != null; } }
    public AssetBundle Bundle { get; private set; }

    Action<string, AssetBundle, object[]> Callback;
    object[] CallbackArgs;

    string RelativeResourceUrl;
    string FullUrl;

    public CAssetBundleLoader(string url, Action<string, AssetBundle, object[]> callback = null, params object[] callbackArgs)
    {
        Callback = callback;
        CallbackArgs = callbackArgs;

        RelativeResourceUrl = url;
        FullUrl = CResourceManager.GetResourcesPath(url);

        CResourceManager.LogRequest("AssetBundle", FullUrl);

        CResourceManager.Instance.StartCoroutine(LoadAssetBundle(RelativeResourceUrl));

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

            // 解密
            CAssetBundleParser parser = new CAssetBundleParser(RelativeResourceUrl, wwwLoader.Www.bytes);
            while (!parser.IsFinished)
            {
                yield return null;
            }

            if (parser.Bundle == null)
                CBase.LogError("WWW.assetBundle is NULL: {0}", FullUrl);

            loadCache.Ab = parser.Bundle;

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
            Callback(FullUrl, Bundle, CallbackArgs);
    }

    /// 舊的tips~忽略
    /// 原以为，每次都通过getter取一次assetBundle会有序列化解压问题，会慢一点，后用AddWatch调试过，发现如果把.assetBundle放到Dictionary里缓存，查询会更慢
    /// 因为，估计.assetBundle是一个纯Getter，没有做序列化问题。  （不保证.mainAsset）
}
