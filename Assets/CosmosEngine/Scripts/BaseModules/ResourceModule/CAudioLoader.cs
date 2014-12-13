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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CAudioLoader
{
    AudioClip ResultAudioClip;

    public bool IsFinished { get { return ResultAudioClip != null; } }

    public AudioClip Clip { get { return ResultAudioClip; } }

    public CAudioLoader(string url, System.Action<bool, AudioClip> callback = null)
    {
        new CAssetFileBridge(url, (bool isOk, UnityEngine.Object obj) =>
        {
            AudioClip clip = obj as AudioClip;

            if (clip == null)
                CDebug.LogError("Null Audio Clip!!!: {0}", url);

            ResultAudioClip = clip;

            if (callback != null)
                callback(isOk, ResultAudioClip);
        });
    }
}
