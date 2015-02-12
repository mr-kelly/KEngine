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

#if UNITY_EDITOR

/// <summary>
/// 专门用于资源Debugger用到的父对象自动生成
/// </summary>
public class CDebuggerParentTool
{
    private static readonly Dictionary<string, Transform> Parents = new Dictionary<string, Transform>();
    private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>(); // 数量统计...

    static string GetUri(string bigType, string smallType)
    {
        var uri = string.Format("{0}/{1}", bigType, smallType);
        return uri;
    }

    public static void SetParent(string bigType, string smallType, GameObject obj)
    {
        var uri = GetUri(bigType, smallType);
        Transform theParent = GetParent(bigType, smallType);

        int typeCount;
        if (!Counts.TryGetValue(uri, out typeCount))
        {
            Counts[uri] = 0;
        }
        typeCount = ++Counts[uri];

        CTool.SetChild(obj, theParent.gameObject);

        theParent.gameObject.name = GetNameWithCount(smallType, typeCount);

    }

    public static void RemoveFromParent(string bigType, string smallType, GameObject obj)
    {
        if (!CBehaviour.IsApplicationQuited)
        {
            if (obj != null)
                GameObject.Destroy(obj);

        }

        var newCount = --Counts[GetUri(bigType, smallType)];
        if (!CBehaviour.IsApplicationQuited)
        {
            GetParent(bigType, smallType).gameObject.name = GetNameWithCount(smallType, newCount);
        }
    }

    /// <summary>
    /// 设置Parent名字,带有数量
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="smallType"></param>
    /// <param name="count"></param>
    protected static string GetNameWithCount(string smallType, int count)
    {
        return string.Format("{0}({1})", smallType, count);
    }

    protected static Transform GetParent(string bigType, string smallType)
    {
        var uri = GetUri(bigType, smallType);
        Transform theParent;

        if (!Parents.TryGetValue(uri, out theParent))
        {
            var bigTypeObjName = string.Format("__{0}__", bigType);
            var bigTypeObj = GameObject.Find(bigTypeObjName) ?? new GameObject(bigTypeObjName);

            theParent = new GameObject(smallType).transform;
            CTool.SetChild(theParent, bigTypeObj.transform);
            Parents[uri] = theParent;
        }
        return theParent;
    }
}

// 只在编辑器下出现，分别对应一个Loader~生成一个对象，为了方便调试！
public class CResourceLoaderDebugger : MonoBehaviour
{
    public CBaseResourceLoader TheLoader;
    public int RefCount;
    public static CResourceLoaderDebugger Create(string type, string url, CBaseResourceLoader loader)
    {
        const string bigType = "ResourceLoaders";

        var newHelpGameObject = new GameObject(string.Format("{0}-{1}", type, url));
        CDebuggerParentTool.SetParent(bigType, type, newHelpGameObject);
        var newHelp = newHelpGameObject.AddComponent<CResourceLoaderDebugger>();
        newHelp.TheLoader = loader;
        loader.DisposeEvent += () => CDebuggerParentTool.RemoveFromParent(bigType, type, newHelpGameObject);
        return newHelp;
    }

    void Update()
    {
        RefCount = TheLoader.RefCount;
    }
}

public class CResourceLoadObjectDebugger : MonoBehaviour
{
    public UnityEngine.Object TheObject;
    const string bigType = "LoadedObjects";
    public string Type;
    private bool IsRemoveFromParent = false;
    public static CResourceLoadObjectDebugger Create(string type, string url, UnityEngine.Object theObject)
    {
        var newHelpGameObject = new GameObject(string.Format("LoadedObject-{0}-{1}", type, url));
        CDebuggerParentTool.SetParent(bigType, type, newHelpGameObject);

        var newHelp = newHelpGameObject.AddComponent<CResourceLoadObjectDebugger>();
        newHelp.Type = type;
        newHelp.TheObject = theObject;
        return newHelp;
    }

    void Update()
    {
        if (TheObject == null && !IsRemoveFromParent)
        {
            CDebuggerParentTool.RemoveFromParent(bigType, Type, gameObject);
            IsRemoveFromParent = true;
        }
    }
    // 可供调试删资源
    void OnDestroy()
    {
        if (!IsRemoveFromParent)
        {
            CDebuggerParentTool.RemoveFromParent(bigType, Type, gameObject);
            IsRemoveFromParent = true;    
        }
        
    }
}

#endif
