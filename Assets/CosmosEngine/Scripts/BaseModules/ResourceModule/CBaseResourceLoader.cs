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
    private readonly List<CLoaderDelgate> _afterFinishedCallbacks = new List<CLoaderDelgate>();

    #region 垃圾回收 Garbage Collect

    /// <summary>
    /// AutoNew时，清理超过5秒无引用的资源
    /// </summary>
    private const float LoaderDisposeTime = 0;

    /// <summary>
    /// 间隔多少秒做一次GC(在AutoNew时)
    /// </summary>
    private const float DoGcInterval = 1f;

    /// <summary>
    /// 上次做GC的时间
    /// </summary>
    private static float _lastGcTime = -1;

    /// <summary>
    /// 缓存起来要删掉的，供DoGarbageCollect函数用
    /// </summary>
    private static readonly List<CBaseResourceLoader> CacheLoaderToRemoveFromUnUsed = new List<CBaseResourceLoader>();

    /// <summary>
    /// 进行垃圾回收
    /// </summary>
    private static readonly Dictionary<CBaseResourceLoader, float> UnUsesLoaders =
        new Dictionary<CBaseResourceLoader, float>();
    #endregion

    /// <summary>
    /// 最终加载结果的资源
    /// </summary>
    public object ResultObject { get; private set; }

    /// <summary>
    /// 是否已经完成，它的存在令Loader可以用于协程StartCoroutine
    /// </summary>
    public bool IsFinished { get; private set; }

    /// <summary>
    /// 类似WWW, IsFinished再判断是否有错误对吧
    /// </summary>
    public bool IsError { get; private set; }

    /// <summary>
    /// RefCount 为 0，进入预备状态
    /// </summary>
    protected bool IsReadyDisposed { get; private set; }
    /// <summary>
    ///  销毁事件
    /// </summary>
    public event Action DisposeEvent;

    public int RefCount { get; private set; }
    public string Url { get; private set; }

    /// <summary>
    /// 进度百分比~ 0-1浮点
    /// </summary>
    public virtual float Progress { get; protected set; }


    public event Action<string> SetDescEvent;
    private string _desc = "";

    /// <summary>
    /// 描述, 额外文字, 一般用于资源Debugger用
    /// </summary>
    /// <returns></returns>
    public virtual string Desc
    {
        get { return _desc; }
        set
        {
            _desc = value;
            if (SetDescEvent != null)
                SetDescEvent(_desc);
        }
    }

    protected CBaseResourceLoader()
    {
        ResultObject = null;
        IsReadyDisposed = false;
        IsError = false;
        IsFinished = false;
        RefCount = 0;
    }

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
        var dict = GetTypeDict(typeof(T));
        CBaseResourceLoader loader;
        if (dict.TryGetValue(url, out loader))
        {
            return loader.RefCount;
        }
        return 0;
    }

    protected static T AutoNew<T>(string url, CLoaderDelgate callback = null) where T : CBaseResourceLoader, new()
    {
        //CheckGcCollect();

        Dictionary<string, CBaseResourceLoader> typesDict = GetTypeDict(typeof(T));
        CBaseResourceLoader loader;
        if (string.IsNullOrEmpty(url))
        {
            CDebug.LogError("[{0}:AutoNew]url为空", typeof(T));
        }

        if (!typesDict.TryGetValue(url, out loader))
        {
            loader = typesDict[url] = new T();
            loader.Init(url);

            if (Application.isEditor)
            {
                CResourceLoaderDebugger.Create(typeof(T).Name, url, loader);
            }
        }
        else
        {
            if (loader.RefCount < 0)
            {
                //loader.IsDisposed = false;  // 转死回生的可能
                CDebug.LogError("Error RefCount!");
            }
        }

        loader.RefCount++;

        loader.AddCallback(callback);

        // RefCount++了，重新激活，在队列中准备清理的Loader
        if (UnUsesLoaders.ContainsKey(loader))
        {
            UnUsesLoaders.Remove(loader);
            loader.IsReadyDisposed = false;
        }

        return loader as T;
    }

    /// <summary>
    /// 是否进行垃圾收集
    /// </summary>
    public static void CheckGcCollect()
    {
        if (_lastGcTime.Equals(-1) || (Time.time - _lastGcTime) >= DoGcInterval)
        {
            DoGarbageCollect();
            _lastGcTime = Time.time;
        }
    }

    /// <summary>
    /// 进行垃圾回收
    /// </summary>
    private static void DoGarbageCollect()
    {
        foreach (var kv in UnUsesLoaders)
        {
            var loader = kv.Key;
            var time = kv.Value;
            if ((Time.time - time) >= LoaderDisposeTime)
            {
                CacheLoaderToRemoveFromUnUsed.Add(loader);
            }
        }

        for (var i = CacheLoaderToRemoveFromUnUsed.Count - 1; i >= 0; i--)
        {
            try
            {
                var loader = CacheLoaderToRemoveFromUnUsed[i];
                UnUsesLoaders.Remove(loader);
                CacheLoaderToRemoveFromUnUsed.RemoveAt(i);
                loader.Dispose();
            }
            catch (Exception e)
            {
                CDebug.LogException(e);
            }
        }

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

        if (IsReadyDisposed)
        {
            DoDispose();
        }

        return !IsError;  // is success?
    }

    /// <summary>
    /// 在IsFinisehd后悔执行的回调
    /// </summary>
    /// <param name="callback"></param>
    protected void AddCallback(CLoaderDelgate callback)
    {
        if (callback != null)
        {
            if (IsFinished)
            {
                if (ResultObject == null)
                    CDebug.LogWarning("Null ResultAsset {0}", Url);
                callback(ResultObject != null, ResultObject);
            }
            else
                _afterFinishedCallbacks.Add(callback);
        }
    }

    protected void DoCallback(bool isOk, object resultObj)
    {
        foreach (var callback in _afterFinishedCallbacks)
            callback(isOk, resultObj);
        _afterFinishedCallbacks.Clear();
    }

    // 后边改变吧~~不叫Dispose!
    public virtual void Release()
    {
        if (IsReadyDisposed && Debug.isDebugBuild)
        {
            CDebug.LogWarning("[{0}]Too many dipose! {1}, Count: {2}", GetType().Name, this.Url, RefCount);
        }

        RefCount--;
        if (RefCount <= 0)
        {
            // TODO: 全部Loader整理好后再设这里吧
            if (Debug.isDebugBuild)
            {
                if (RefCount < 0)
                {
                    CDebug.LogError("[CBaseResourceLoader]RefCount< 0, {0} : {1}, NowRefCount: {2}, Will be fix to 0", GetType().Name, Url, RefCount);

                    RefCount = Mathf.Max(0, RefCount);
                }

                if (UnUsesLoaders.ContainsKey(this))
                {
                    CDebug.LogError("[CBaseResourceLoader]UnUsesLoader exists: {0}", this);
                }
            }

            // 加入队列，准备Dispose
            UnUsesLoaders.Add(this, Time.time);

            IsReadyDisposed = true;

            //DoGarbageCollect();
        }
    }

    //protected bool RealDisposed = false;

    /// <summary>
    /// Dispose是有引用检查的， DoDispose一般用于继承重写
    /// </summary>
    protected virtual void Dispose()
    {
        //RealDisposed = true;

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
// Unity潜规则: 等待帧最后再执行，避免一些(DestroyImmediatly)在Phycis函数内