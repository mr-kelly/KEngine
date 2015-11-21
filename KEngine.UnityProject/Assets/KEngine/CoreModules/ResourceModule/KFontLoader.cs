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
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class KFontLoader : KAbstractResourceLoader
{
    private KAssetFileLoader _bridge;
    public override float Progress
    {
        get
        {
            return _bridge.Progress;
        }
    }

    public static KFontLoader Load(string path, Action<bool, Font> callback = null)
    {
        CLoaderDelgate realcallback = null;
        if (callback != null)
        {
            realcallback = (isOk, obj) => callback(isOk, obj as Font);
        }

        return AutoNew<KFontLoader>(path, realcallback);
    }
    protected override void Init(string url)
    {
        base.Init(url);

        _bridge = KAssetFileLoader.Load(Url, (_isOk, _obj) =>
        {
            OnFinish(_obj);
        });
    }
    protected override void DoDispose()
    {
        base.DoDispose();
        _bridge.Release();
    }
}
