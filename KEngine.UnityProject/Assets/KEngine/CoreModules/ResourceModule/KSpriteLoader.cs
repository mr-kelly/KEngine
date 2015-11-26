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
using System;
using System.Collections;
using System.Collections.Generic;

[CDependencyClass(typeof(KAssetFileLoader))]
public class KSpriteLoader : KAbstractResourceLoader
{
    public Sprite Asset { get { return ResultObject as Sprite; } }

    public delegate void CSpriteLoaderDelegate(bool isOk, Sprite tex);

    private KAssetFileLoader AssetFileBridge;
    public override float Progress
    {
        get
        {
            return AssetFileBridge.Progress;
        }
    }
    public string Path { get; private set; }

    public static KSpriteLoader Load(string path, CSpriteLoaderDelegate callback = null)
    {
        CLoaderDelgate newCallback = null;
        if (callback != null)
        {
            newCallback = (isOk, obj) => callback(isOk, obj as Sprite);
        }
        return AutoNew<KSpriteLoader>(path, newCallback);
    }
    protected override void Init(string url, params object[] args)
    {
        base.Init(url, args);
        Path = url;
        AssetFileBridge = KAssetFileLoader.Load(Path, OnAssetLoaded);
    }

    void OnAssetLoaded(bool isOk, UnityEngine.Object obj)
    {
        OnFinish(obj);
    }

    protected override void DoDispose()
    {
        base.DoDispose();
        AssetFileBridge.Release(); // all, Texture is singleton!
    }
}
