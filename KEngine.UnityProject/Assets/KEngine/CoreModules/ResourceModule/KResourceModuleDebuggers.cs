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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// 专门用于资源Debugger用到的父对象自动生成
/// 
/// DebuggerObject - 用于管理虚拟对象（只用于显示调试信息的对象）
/// </summary>
public class KDebuggerObjectTool
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

        KTool.SetChild(obj, theParent.gameObject);

        theParent.gameObject.name = GetNameWithCount(smallType, typeCount);

    }

    public static void RemoveFromParent(string bigType, string smallType, GameObject obj)
    {
        if (!KBehaviour.IsApplicationQuited)
        {
            if (obj != null)
                GameObject.Destroy(obj);

        }

        var newCount = --Counts[GetUri(bigType, smallType)];
        if (!KBehaviour.IsApplicationQuited)
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
            KTool.SetChild(theParent, bigTypeObj.transform);
            Parents[uri] = theParent;
        }
        return theParent;
    }
}

// 只在编辑器下出现，分别对应一个Loader~生成一个对象，为了方便调试！
public class KResourceLoaderDebugger : MonoBehaviour
{
    public KAbstractResourceLoader TheLoader;
    public int RefCount;
    public float FinishUsedTime; // 参考，完成所需时间

    public static KResourceLoaderDebugger Create(string type, string url, KAbstractResourceLoader loader)
    {
        const string bigType = "ResourceLoaders";

        Func<string> getName = () => string.Format("{0}-{1}-{2}", type, loader.Desc, url);

        var newHelpGameObject = new GameObject(getName());
        KDebuggerObjectTool.SetParent(bigType, type, newHelpGameObject);
        var newHelp = newHelpGameObject.AddComponent<KResourceLoaderDebugger>();
        newHelp.TheLoader = loader;

        loader.SetDescEvent += (newDesc) =>
        {
            if (loader.RefCount > 0)
                newHelpGameObject.name = getName();
        };

        loader.DisposeEvent += () =>
        {
            KDebuggerObjectTool.RemoveFromParent(bigType, type, newHelpGameObject);
        };
        

        return newHelp;
    }

    void Update()
    {
        RefCount = TheLoader.RefCount;
        FinishUsedTime = TheLoader.FinishUsedTime;
    }
}

public class KResourceLoadObjectDebugger : MonoBehaviour
{
    public UnityEngine.Object TheObject;
    const string bigType = "LoadedObjects";
    public string Type;
    private bool IsRemoveFromParent = false;
    public static KResourceLoadObjectDebugger Create(string type, string url, UnityEngine.Object theObject)
    {
        var newHelpGameObject = new GameObject(string.Format("LoadedObject-{0}-{1}", type, url));
        KDebuggerObjectTool.SetParent(bigType, type, newHelpGameObject);

        var newHelp = newHelpGameObject.AddComponent<KResourceLoadObjectDebugger>();
        newHelp.Type = type;
        newHelp.TheObject = theObject;
        return newHelp;
    }

    void Update()
    {
        if (TheObject == null && !IsRemoveFromParent)
        {
            KDebuggerObjectTool.RemoveFromParent(bigType, Type, gameObject);
            IsRemoveFromParent = true;
        }
    }
    // 可供调试删资源
    void OnDestroy()
    {
        if (!IsRemoveFromParent)
        {
            KDebuggerObjectTool.RemoveFromParent(bigType, Type, gameObject);
            IsRemoveFromParent = true;
        }

    }
}