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

/// <summary>
/// Load www, A wrapper of WWW.  
/// Current version, loaded Resource will never release in memory
/// </summary>
[CDependencyClass(typeof(CResourceModule))]
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
        CResourceModule.Instance.StartCoroutine(CoLoad(url, callback, callbackArgs));//开启协程加载Assetbundle，执行Callback
    }

	/// <summary>
	/// 协和加载Assetbundle，加载完后执行callback
	/// </summary>
	/// <param name="url">资源的url</param>
	/// <param name="callback"></param>
	/// <param name="callbackArgs"></param>
	/// <returns></returns>
    IEnumerator CoLoad(string url, Action<WWW, object[]> callback = null, params object[] callbackArgs)
    {
        if (CResourceModule.LoadByQueue)
        {
            while (Loaded.Count != 0)
                yield return null;
        }

        CResourceModule.LogRequest("WWW", url);

        CLoadingCache cache = null;

        if (!Loaded.TryGetValue(url, out cache))
        {
            cache = new CLoadingCache(url);
            Loaded.Add(url, cache);
            System.DateTime beginTime = System.DateTime.Now;
            WWW www = new WWW(url);

			//设置AssetBundle解压缩线程的优先级
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
                string fileProtocol = CResourceModule.GetFileProtocol();
                if (url.StartsWith(fileProtocol))
                {
                    string fileRealPath = url.Replace(fileProtocol, "");
                    CBase.LogError("File {0} Exist State: {1}", fileRealPath, System.IO.File.Exists(fileRealPath));

                }
                CBase.LogError(www.error + " " + url);
            }

            CResourceModule.LogLoadTime("WWW", url, beginTime);

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
