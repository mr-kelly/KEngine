//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Version 0.9.1 (20151010)
//                     Copyright © 2011-2015
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
//#define MEMORY_DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using KEngine;


/// <summary>
/// 所有资源Loader继承这个
/// </summary>
public abstract class KAbstractResourceLoader
{
    static KAbstractResourceLoader()
    {
    }

    public delegate void CLoaderDelgate(bool isOk, object resultObject);
    private static readonly Dictionary<Type, Dictionary<string, KAbstractResourceLoader>> _loadersPool = new Dictionary<Type, Dictionary<string, KAbstractResourceLoader>>();
    private readonly List<CLoaderDelgate> _afterFinishedCallbacks = new List<CLoaderDelgate>();

    #region 垃圾回收 Garbage Collect

    /// <summary>
    /// Loader延迟Dispose
    /// </summary>
    private const float LoaderDisposeTime = 0;

    /// <summary>
    /// 间隔多少秒做一次GC(在AutoNew时)
    /// </summary>
    public static float GcIntervalTime
    {
        get
        {
            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.OSXEditor)
                return 1f;

            return Debug.isDebugBuild ? 5f : 10f;
        }
    }

    /// <summary>
    /// 上次做GC的时间
    /// </summary>
    private static float _lastGcTime = -1;

    /// <summary>
    /// 缓存起来要删掉的，供DoGarbageCollect函数用, 避免重复的new List
    /// </summary>
    private static readonly List<KAbstractResourceLoader> CacheLoaderToRemoveFromUnUsed = new List<KAbstractResourceLoader>();

    /// <summary>
    /// 进行垃圾回收
    /// </summary>
    private static readonly Dictionary<KAbstractResourceLoader, float> UnUsesLoaders =
        new Dictionary<KAbstractResourceLoader, float>();
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
    /// 是否处于Application退出状态
    /// </summary>
    private bool _isQuitApplication = false;

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

    protected static Dictionary<string, KAbstractResourceLoader> GetTypeDict(Type type)
    {
        Dictionary<string, KAbstractResourceLoader> typesDict;
        if (!_loadersPool.TryGetValue(type, out typesDict))
        {
            typesDict = _loadersPool[type] = new Dictionary<string, KAbstractResourceLoader>();
        }
        return typesDict;
    }

    public static int GetRefCount<T>(string url)
    {
        var dict = GetTypeDict(typeof(T));
        KAbstractResourceLoader loader;
        if (dict.TryGetValue(url, out loader))
        {
            return loader.RefCount;
        }
        return 0;
    }

    /// <summary>
    /// 统一的对象工厂
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="url"></param>
    /// <param name="callback"></param>
    /// <param name="forceCreateNew"></param>
    /// <returns></returns>
    protected static T AutoNew<T>(string url, CLoaderDelgate callback = null, bool forceCreateNew = false, params object[] initArgs) where T : KAbstractResourceLoader, new()
    {
        Dictionary<string, KAbstractResourceLoader> typesDict = GetTypeDict(typeof(T));
        KAbstractResourceLoader loader;
        if (string.IsNullOrEmpty(url))
        {
            Logger.LogError("[{0}:AutoNew]url为空", typeof(T));
        }

        if (forceCreateNew || !typesDict.TryGetValue(url, out loader))
        {
            loader = new T();
            if (!forceCreateNew)
            {
                typesDict[url] = loader;
            }

            loader.IsForceNew = forceCreateNew;
            loader.Init(url, initArgs);

            if (Application.isEditor)
            {
                KResourceLoaderDebugger.Create(typeof(T).Name, url, loader);
            }
        }
        else
        {
            if (loader.RefCount < 0)
            {
                //loader.IsDisposed = false;  // 转死回生的可能
                Logger.LogError("Error RefCount!");
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
                Logger.LogException(e);
            }
        }

        if (CacheLoaderToRemoveFromUnUsed.Count > 0)
        {
            Logger.LogError("[DoGarbageCollect]CacheLoaderToRemoveFromUnUsed muse be empty!!");
        }
    }

    /// <summary>
    /// 复活
    /// </summary>
    protected virtual void Revive()
    {
        IsReadyDisposed = false;  // 复活！
    }

    protected KAbstractResourceLoader()
    {
        RefCount = 0;
    }

    protected virtual void Init(string url, params object[] args)
    {
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
                //Dispose();
                Logger.Trace("[BaseResourceLoader:OnFinish]时，准备Disposed {0}", Url);
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
                    Logger.LogWarning("Null ResultAsset {0}", Url);
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
        if (IsReadyDisposed && Debug.isDebugBuild)
        {
            Logger.LogWarning("[{0}]Too many dipose! {1}, Count: {2}", GetType().Name, this.Url, RefCount);
        }

        RefCount--;
        if (RefCount <= 0)
        {
            // TODO: 全部Loader整理好后再设这里吧
            if (Debug.isDebugBuild)
            {
                if (RefCount < 0)
                {
                    Logger.LogError("[{3}]RefCount< 0, {0} : {1}, NowRefCount: {2}, Will be fix to 0", GetType().Name, Url, RefCount, GetType());

                    RefCount = Mathf.Max(0, RefCount);
                }

                if (UnUsesLoaders.ContainsKey(this))
                {
                    Logger.LogError("[{1}]UnUsesLoader exists: {0}", this, GetType());
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
                    Logger.LogWarning("[{0}:Dispose]No Url: {1}, Cur RefCount: {2}", type.Name, Url, RefCount);
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


    /// <summary>
    /// 强制进行Dispose，无视Ref引用数，建议用在RefCount为1的Loader上
    /// </summary>
    public virtual void ForceDispose()
    {
        if (_isQuitApplication)
            return;
        if (RefCount != 1)
        {
            Logger.LogWarning("[ForceDisose]Use force dispose to dispose loader, recommend this loader RefCount == 1");
        }
        Dispose();
    }

    /// <summary>
    /// By Unity Reflection
    /// </summary>
    protected void OnApplicationQuit()
    {
        _isQuitApplication = true;
    }
}
// Unity潜规则: 等待帧最后再执行，避免一些(DestroyImmediatly)在Phycis函数内