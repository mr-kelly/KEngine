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
public class CWWWLoader : CBaseResourceLoader
{
    private readonly string TheUrl;
    public static event Action<string> WWWFinishCallback;

    public WWW Www;

    public override float Progress
    {
        get { return Www.progress; }
    }

    protected override void Init(string url)
    {
        base.Init(url);
        CResourceModule.Instance.StartCoroutine(CoLoad(url));//开启协程加载Assetbundle，执行Callback
    }
    /// <summary>
    /// Use this to directly load WWW by Callback or Coroutine, pass a full URL.
    /// A wrapper of Unity's WWW class.
    /// </summary>
    public static CWWWLoader Load(string url, CLoaderDelgate callback = null)
    {
        return AutoNew<CWWWLoader>(url, callback);
    }

    /// <summary>
    /// 协和加载Assetbundle，加载完后执行callback
    /// </summary>
    /// <param name="url">资源的url</param>
    /// <param name="callback"></param>
    /// <param name="callbackArgs"></param>
    /// <returns></returns>
    IEnumerator CoLoad(string url)
    {
        if (CResourceModule.LoadByQueue)
        {
            while (GetCount<CWWWLoader>() != 0)
                yield return null;
        }

        CResourceModule.LogRequest("WWW", url);

        System.DateTime beginTime = System.DateTime.Now;
        Www = new WWW(url);

        //设置AssetBundle解压缩线程的优先级
        Www.threadPriority = Application.backgroundLoadingPriority;  // 取用全局的加载优先速度
        while (!Www.isDone)
        {
            yield return null;
        }

        yield return Www;

        if (!string.IsNullOrEmpty(Www.error))
        {
            string fileProtocol = CResourceModule.GetFileProtocol();
            if (url.StartsWith(fileProtocol))
            {
                string fileRealPath = url.Replace(fileProtocol, "");
                CDebug.LogError("File {0} Exist State: {1}", fileRealPath, System.IO.File.Exists(fileRealPath));

            }
            CDebug.LogError("[CWWWLoader:Error]" + Www.error + " " + url);

            OnFinish(null);
            yield break;
        }
        else
        {
            CResourceModule.LogLoadTime("WWW", url, beginTime);
            if (WWWFinishCallback != null)
                WWWFinishCallback(url);

            OnFinish(Www);
        }

#if UNITY_EDITOR  // 预防WWW加载器永不反初始化
        while (GetCount<CWWWLoader>() > 0)
            yield return null;

        yield return new WaitForSeconds(5f);

        while (Debug.isDebugBuild && !IsDisposed)
        {
            CDebug.LogError("[CWWWLoader]Not Disposed Yet! : {0}", this.TheUrl);
            yield return null;
        }
#endif
    }

    protected override void DoDispose()
    {
        base.DoDispose();

        Www.Dispose();
        Www = null;
    }
}
