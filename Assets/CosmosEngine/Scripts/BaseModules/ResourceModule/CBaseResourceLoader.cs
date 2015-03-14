using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


/// <summary>
/// 所有资源Loader继承这个
/// </summary>
public abstract class CBaseResourceLoader
{
    public delegate void CLoaderDelgate(bool isOk, object resultObject);
    private static readonly Dictionary<Type, Dictionary<string, CBaseResourceLoader>> Caches = new Dictionary<Type, Dictionary<string, CBaseResourceLoader>>();
    private readonly List<CLoaderDelgate> Callbacks = new List<CLoaderDelgate>();

    public virtual object ResultObject { get; protected set; }
    public bool IsFinished = false;
    public bool IsError = false;
    protected bool IsDisposed = false;
    public event Action DisposeEvent;
    public int RefCount = 0;
    public string Url;

    public virtual float Progress { get; set; }

    protected CBaseResourceLoader(){}

    protected static int GetCount<T>()
    {
        return GetTypeDict(typeof(T)).Count;
    }

    protected static Dictionary<string, CBaseResourceLoader> GetTypeDict(Type type)
    {
        Dictionary<string, CBaseResourceLoader> typesDict;
        if (!Caches.TryGetValue(type, out typesDict))
        {
            typesDict = Caches[type] = new Dictionary<string, CBaseResourceLoader>();
        }
        return typesDict;
    }

    public static int GetRefCount<T>(string url)
    {
        var dict = GetTypeDict(typeof (T));
        CBaseResourceLoader loader;
        if (dict.TryGetValue(url, out loader))
        {
            return loader.RefCount;
        }
        return 0;
    }

    protected static T AutoNew<T>(string url, CLoaderDelgate callback = null) where T : CBaseResourceLoader, new()
    {
        Dictionary<string, CBaseResourceLoader> typesDict = GetTypeDict(typeof(T));
        CBaseResourceLoader loader;
        if (string.IsNullOrEmpty(url))
        {
            CDebug.LogError("[{0}:AutoNew]url为空", typeof (T));
        }

        if (!typesDict.TryGetValue(url, out loader))
        {
            loader = typesDict[url] = new T();
            loader.Init(url);

#if UNITY_EDITOR
            CResourceLoaderDebugger.Create(typeof (T).Name, url, loader);
#endif
        }
        else
        {
            if (loader.RefCount <= 0)
            {
                loader.IsDisposed = false;  // 转死回生的可能
				CDebug.LogWarning("Death to live!");
            } 
        }

        loader.RefCount++;
        

        loader.AddCallback(callback);

        return loader as T;
    }

    protected virtual void Init(string url)
    {
        Url = url;
        Progress = 0;
    }

    protected virtual bool OnFinish(object resultObj)
    {
        Progress = 1;
        IsFinished = true;
        IsError = resultObj == null;
        ResultObject = resultObj;

        DoCallback(resultObj != null, ResultObject);

        if (IsDisposed)
        {
            DoDispose();
        }

        return !IsError;  // is success?
    }

    protected void AddCallback(CLoaderDelgate callback)
    {
        if (callback != null)
        {
            if (IsFinished)
            {
                if (ResultObject == null)
                    CDebug.LogWarning("Null ResultAsset {0}", Url);
                callback(true, ResultObject);
            }
            else
                Callbacks.Add(callback);
        }
    }

    protected void DoCallback(bool isOk, object resultObj)
    {
        foreach (var callback in Callbacks)
            callback(isOk, resultObj);
        Callbacks.Clear();
    }

    // 后边改变吧~~不叫Dispose!
    public virtual void Release()
    {
        if (IsDisposed && Debug.isDebugBuild)
        {
            CDebug.LogWarning("[{0}]Too many dipose! {1}, Count: {2}", GetType().Name, this.Url, RefCount);
        }

        RefCount--;
        if (RefCount <= 0)
        {
            // TODO: 全部Loader整理好后再设这里吧
            //if (Debug.isDebugBuild && RefCount < 0)
            //{
            //    CDebug.LogWarning("[CBaseResourceLoader]RefCount< 0 {0}", GetType().Name);
            //}
            Dispose();
        }
    }

    /// <summary>
    /// Dispose是有引用检查的， DoDispose一般用于继承重写
    /// </summary>
    protected virtual void Dispose()
    {
        IsDisposed = true;

        if (DisposeEvent != null)
            DisposeEvent();

        var type = GetType();
        var typeDict = GetTypeDict(type);
        //if (Url != null) // TODO: 以后去掉
        {
            var bRemove = typeDict.Remove(Url);
            if (!bRemove)
            {
                CDebug.LogWarning("[{0}:Dispose]No Url: {1}", type.Name, Url);
            }
        }

        if (IsFinished)
            DoDispose();
        // 未完成，在OnFinish时会执行DoDispose
    }

    protected virtual void DoDispose()
    {

    }
}
