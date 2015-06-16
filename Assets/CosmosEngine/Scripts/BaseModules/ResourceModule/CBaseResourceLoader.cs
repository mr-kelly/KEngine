//#define MEMORY_DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


/// <summary>
/// 所有资源Loader继承这个
/// </summary>
public abstract class CBaseResourceLoader
{
    static CBaseResourceLoader()
    {
        GcIntervalTime = Debug.isDebugBuild ? 1f : 5f;
    }

    public delegate void CLoaderDelgate(bool isOk, object resultObject);
    private static readonly Dictionary<Type, Dictionary<string, CBaseResourceLoader>> Caches = new Dictionary<Type, Dictionary<string, CBaseResourceLoader>>();
    private readonly List<CLoaderDelgate> _afterFinishedCallbacks = new List<CLoaderDelgate>();

    #region 垃圾回收 Garbage Collect

    /// <summary>
    /// Loader延迟Dispose
    /// </summary>
    private const float LoaderDisposeTime = 0;

    /// <summary>
    /// 间隔多少秒做一次GC(在AutoNew时)
    /// </summary>
    public static float GcIntervalTime = 1f;

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
    /// 是否可用
    /// </summary>
    public bool IsOk
    {
        get { return !IsError && ResultObject != null && !IsReadyDisposed; }
    }

    /// <summary>
    /// ForceNew的，非AutoNew
    /// </summary>
    protected bool IsForceNew;
    /// <summary>
    /// RefCount 为 0，进入预备状态
    /// </summary>
    protected bool IsReadyDisposed { get; private set; }
    /// <summary>
    ///  销毁事件
    /// </summary>
    public event Action DisposeEvent;

    [System.NonSerialized]
    public float InitTiming = -1;
    [System.NonSerialized]
    public float FinishTiming = -1;
    
    /// <summary>
    /// 用时
    /// </summary>
    public float FinishUsedTime
    {
        get
        {
            if (!IsFinished) return -1;
            return FinishTiming - InitTiming;
        }
    }
    /// <summary>
    /// 引用计数
    /// </summary>
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

    protected static T AutoNew<T>(string url, CLoaderDelgate callback = null, bool forceCreateNew = false) where T : CBaseResourceLoader, new()
    {
        Dictionary<string, CBaseResourceLoader> typesDict = GetTypeDict(typeof(T));
        CBaseResourceLoader loader;
        if (string.IsNullOrEmpty(url))
        {
            CDebug.LogError("[{0}:AutoNew]url为空", typeof(T));
        }

        if (forceCreateNew || !typesDict.TryGetValue(url, out loader))
        {
            loader = new T();
            if (!forceCreateNew)
            {
                typesDict[url] = loader;
            }

            loader.IsForceNew = forceCreateNew;
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

        // RefCount++了，重新激活，在队列中准备清理的Loader
        if (UnUsesLoaders.ContainsKey(loader))
        {
            UnUsesLoaders.Remove(loader);
            loader.Revive();
        }

        loader.AddCallback(callback);

        return loader as T;
    }

    /// <summary>
    /// 是否进行垃圾收集
    /// </summary>
    public static void CheckGcCollect()
    {
        if (_lastGcTime.Equals(-1) || (Time.time - _lastGcTime) >= GcIntervalTime)
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

    /// <summary>
    /// 复活
    /// </summary>
    protected virtual void Revive()
    {
        IsReadyDisposed = false;  // 复活！
    }

    protected CBaseResourceLoader()
    {
        RefCount = 0;
    }
#if MEMORY_DEBUG
    protected float MemoryOnStart;
#endif

    protected virtual void Init(string url)
    {
#if MEMORY_DEBUG
        MemoryOnStart = GC.GetTotalMemory(true) / 1000f;
#endif
        InitTiming = Time.realtimeSinceStartup;
        ResultObject = null;
        IsReadyDisposed = false;
        IsError = false;
        IsFinished = false;

        Url = url;
        Progress = 0;

    }

    protected virtual void OnFinish(object resultObj)
    {
        Action doFinish = () =>
        {
#if MEMORY_DEBUG
        CDebug.Log("OnFinish {0}:{1} Memory Diff: {2}", this, Url, (GC.GetTotalMemory(true) / 1000f) - MemoryOnStart);
#endif
            // 如果ReadyDispose，无效！不用传入最终结果！
            ResultObject = resultObj;

            // 如果ReadyDisposed, 依然会保存ResultObject, 但在回调时会失败~无回调对象
            var callbackObject = !IsReadyDisposed ? ResultObject : null;

            FinishTiming = Time.realtimeSinceStartup;
            Progress = 1;
            IsError = callbackObject == null;

            IsFinished = true;
            DoCallback(IsOk, callbackObject);

            if (IsReadyDisposed)
            {
                Dispose();
                //CDebug.DevLog("[BaseResourceLoader:OnFinish]时，准备Disposed {0}", Url);
            }
        };

        doFinish();
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
        Action justDo = () =>
        {
            foreach (var callback in _afterFinishedCallbacks)
                callback(isOk, resultObj);
            _afterFinishedCallbacks.Clear();
        };


        {
            justDo();
        }

    }

    // 后边改变吧~~不叫Dispose!
    public virtual void Release()
    {
#if UNITY_EDITOR
        if (Url.Contains("Arial"))
        {
            CDebug.LogError("要释放Arial字体！！错啦！！:{0}", Url);
            UnityEditor.EditorApplication.isPaused = true;
        }
#endif
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
                    CDebug.LogError("[{3}]RefCount< 0, {0} : {1}, NowRefCount: {2}, Will be fix to 0", GetType().Name, Url, RefCount, GetType());

                    RefCount = Mathf.Max(0, RefCount);
                }

                if (UnUsesLoaders.ContainsKey(this))
                {
                    CDebug.LogError("[{1}]UnUsesLoader exists: {0}", this, GetType());
                }
            }

            // 加入队列，准备Dispose
            UnUsesLoaders[this] = Time.time;

            IsReadyDisposed = true;

            //DoGarbageCollect();
        }
    }

    //protected bool RealDisposed = false;

    /// <summary>
    /// Dispose是有引用检查的， DoDispose一般用于继承重写
    /// </summary>
    private void Dispose()
    {
        //RealDisposed = true;

        if (DisposeEvent != null)
            DisposeEvent();

        if (!IsForceNew)
        {
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