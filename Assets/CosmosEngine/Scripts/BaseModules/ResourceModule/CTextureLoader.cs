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
using System.Security.Cryptography;
using KFramework;

[CDependencyClass(typeof(CAssetFileLoader))]
public class CTextureLoader : CBaseResourceLoader
{
    public Texture Asset { get { return ResultObject as Texture; } }

    public delegate void CTextureLoaderDelegate(bool isOk, Texture tex);

    private CAssetFileLoader AssetFileBridge;
    public override float Progress
    {
        get
        {
            return AssetFileBridge.Progress;
        }
    }
    public string Path { get; private set; }

    public static CTextureLoader Load(string path, CTextureLoaderDelegate callback = null)
    {
        CLoaderDelgate newCallback = null;
        if (callback != null)
        {
            newCallback = (isOk, obj) => callback(isOk, obj as Texture);
        }
        return AutoNew<CTextureLoader>(path, newCallback);
    }
    protected override void Init(string url)
    {
        base.Init(url);

        Path = url;
        AssetFileBridge = CAssetFileLoader.Load(Path, OnAssetLoaded);
    }

    void OnAssetLoaded(bool isOk, UnityEngine.Object obj)
    {
        if (!isOk)
        {
            CDebug.LogError("[CTextureLoader:OnAssetLoaded]Is not OK: {0}", this.Url);
        }

        OnFinish(obj);

        if (isOk)
        {
            var tex = Asset as Texture2D;            

            string format = tex != null ? tex.format.ToString() : "";
            Desc = string.Format("{0}*{1}={2}px-{3}", Asset.width, Asset.height, Asset.width*Asset.height, format);
        }
    }

    protected override void DoDispose()
    {
        base.DoDispose();
        AssetFileBridge.Release(); // all, Texture is singleton!
    }
}
