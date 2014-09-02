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
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Load www, A wrapper of WWW
/// </summary>
[CDependencyClass(typeof(CResourceManager))]
public class CWWWLoader
{
    class CLoadingCache
    {
        public string Url;
        public WWW Www;
        public CLoadingCache(string url)
        {
            Url = url;
        }
    }

    public static event Action<string> WWWFinishCallback;

    static Dictionary<string, CLoadingCache> Loaded = new Dictionary<string, CLoadingCache>();
    CLoadingCache WwwCache = null;

    public bool IsFinished { get { return WwwCache != null; } }  // 可协程不停判断， 或通过回调

    bool _IsError = false;
    public bool IsError { get { return _IsError; } private set { _IsError = value; } }

    public WWW Www { get { return WwwCache.Www; } }

    public float Progress = 0;

    /// <summary>
    /// Use this to directly load WWW by Callback or Coroutine, pass a full URL.
    /// A wrapper of Unity's WWW class.
    /// </summary>
    public CWWWLoader(string url, Action<WWW, object[]> callback = null, params object[] callbackArgs)
    {
        CResourceManager.Instance.StartCoroutine(CoLoad(url, callback, callbackArgs));//开启协程加载Assetbundle，执行Callback
    }

    IEnumerator CoLoad(string url, Action<WWW, object[]> callback = null, params object[] callbackArgs)
    {
        if (CResourceManager.LoadByQueue)
        {
            while (Loaded.Count != 0)
                yield return null;
        }

        CResourceManager.LogRequest("WWW", url);

        CLoadingCache cache = null;

        if (!Loaded.TryGetValue(url, out cache))
        {
            cache = new CLoadingCache(url);
            Loaded.Add(url, cache);
            System.DateTime beginTime = System.DateTime.Now;
            WWW www = new WWW(url);

            www.threadPriority = Application.backgroundLoadingPriority;  // 取用全局的加载优先速度
            while (!www.isDone)
            {
                Progress = www.progress;
                yield return null;
            }

            yield return www;

            if (!string.IsNullOrEmpty(www.error))
            {
                IsError = true;
                string fileProtocol = CResourceManager.GetFileProtocol();
                if (url.StartsWith(fileProtocol))
                {
                    string fileRealPath = url.Replace(fileProtocol, "");
                    CBase.LogError("File {0} Exist State: {1}", fileRealPath, System.IO.File.Exists(fileRealPath));

                }
                CBase.LogError(www.error + " " + url);
            }

            CResourceManager.LogLoadTime("WWW", url, beginTime);

            cache.Www = www;

            if (WWWFinishCallback != null)
                WWWFinishCallback(url);

        }
        else
        {
            if (cache.Www != null)
                yield return null;  // 确保每一次异步读取资源都延迟一帧

            while (cache.Www == null)  // 未加载完
                yield return null;
        }

        Progress = cache.Www.progress;
        WwwCache = cache;
        if (callback != null)
            callback(WwwCache.Www, callbackArgs);
    }
}
