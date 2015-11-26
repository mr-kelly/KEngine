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
/// 只在编辑器下出现，分别对应一个Loader~生成一个GameObject对象，为了方便调试！
/// </summary>
public class KResourceLoaderDebugger : MonoBehaviour
{
    public KAbstractResourceLoader TheLoader;
    public int RefCount;
    public float FinishUsedTime; // 参考，完成所需时间
    public static bool IsApplicationQuit = false;

    public static KResourceLoaderDebugger Create(string type, string url, KAbstractResourceLoader loader)
    {
        if (IsApplicationQuit) return null;

        const string bigType = "ResourceLoaderDebuger";

        Func<string> getName = () => string.Format("{0}-{1}-{2}", type, url, loader.Desc);

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

    void OnApplicationQuit()
    {
        IsApplicationQuit = true;
    }
}
