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

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CFontLoader : CBaseResourceLoader
{
    private CAssetFileLoader Bridge;
    public override float Progress
    {
        get
        {
            return Bridge.Progress;
        }
    }

    public static CFontLoader Load(string path, Action<bool, Font> callback = null)
    {
        CLoaderDelgate realcallback = null;
        if (callback != null)
        {
            realcallback = (isOk, obj) => callback(isOk, obj as Font);
        }

        return AutoNew<CFontLoader>(path, realcallback);
    }
    protected override void Init(string url)
    {
        base.Init(url);

        Bridge = CAssetFileLoader.Load(Url, (_isOk, _obj) =>
        {
            OnFinish(_obj);
        });
    }
    protected override void DoDispose()
    {
        base.DoDispose();
        Bridge.Release();
    }
}
