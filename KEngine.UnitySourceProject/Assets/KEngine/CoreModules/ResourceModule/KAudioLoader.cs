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

public class KAudioLoader : KAbstractResourceLoader
{
    AudioClip ResultAudioClip {get { return ResultObject as AudioClip; }}

    KAssetFileLoader AssetFileBridge;

    public override float Progress
    {
        get { return AssetFileBridge.Progress; }
    }

    public static KAudioLoader Load(string url, System.Action<bool, AudioClip> callback = null)
    {
        CLoaderDelgate newCallback = null;
        if (callback != null)
        {
            newCallback = (isOk, obj) => callback(isOk, obj as AudioClip);
        }
        return AutoNew<KAudioLoader>(url, newCallback);

    }
    protected override void Init(string url)
    {
        base.Init(url);

        AssetFileBridge = KAssetFileLoader.Load(url, (bool isOk, UnityEngine.Object obj) =>
        {
            OnFinish(obj);
        });
    }

    protected override void DoDispose()
    {
        base.DoDispose();

        AssetFileBridge.Release();
    }
}
