#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KAssetBundleLoader.cs
// Date:     2015/12/03
// Author:  Kelly
// Email: 23110388@qq.com
// Github: https://github.com/mr-kelly/KEngine
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.

#endregion

using System;
using System.Collections;
using System.IO;
using KEngine;
using UnityEngine;

public enum KAssetBundleLoaderMode
{
    Default,
    StreamingAssetsWww, // default, use WWW class -> StreamingAssets Path
    ResourcesLoadAsync, // -> Resources path
    ResourcesLoad, // -> Resources Path
}

// 調用WWWLoader
public class KAssetBundleLoader : KAbstractResourceLoader
{
    public delegate void CAssetBundleLoaderDelegate(bool isOk, AssetBundle ab);

    public static Action<string> NewAssetBundleLoaderEvent;
    public static Action<KAssetBundleLoader> AssetBundlerLoaderErrorEvent;

    private KWWWLoader _wwwLoader;
    private KAssetBundleParser BundleParser;
    //private bool UnloadAllAssets; // Dispose时赋值
    public AssetBundle Bundle
    {
        get { return ResultObject as AssetBundle; }
    }

    private string RelativeResourceUrl;
    private string FullUrl;

    /// <summary>
    /// AssetBundle加载方式
    /// </summary>
    private KAssetBundleLoaderMode _loaderMode;

    /// <summary>
    /// AssetBundle读取原字节目录
    /// </summary>
    private KResourceInAppPathType _inAppPathType;

    public static KAssetBundleLoader Load(string url, CAssetBundleLoaderDelegate callback = null,
        KAssetBundleLoaderMode loaderMode = KAssetBundleLoaderMode.Default)
    {
        CLoaderDelgate newCallback = null;
        if (callback != null)
        {
            newCallback = (isOk, obj) => callback(isOk, obj as AssetBundle);
        }
        var newLoader = AutoNew<KAssetBundleLoader>(url, newCallback, false, loaderMode);


        return newLoader;
    }

    protected override void Init(string url, params object[] args)
    {
        base.Init(url);

        _loaderMode = (KAssetBundleLoaderMode) args[0];

        // 如果是默认模式，则要判断ResourceModule.InAppPathType的默认为依据
        if (_loaderMode == KAssetBundleLoaderMode.Default)
        {
            _inAppPathType = KResourceModule.DefaultInAppPathType;
            switch (_inAppPathType)
            {
                case KResourceInAppPathType.StreamingAssetsPath:
                    _loaderMode = KAssetBundleLoaderMode.StreamingAssetsWww;
                    break;
                case KResourceInAppPathType.ResourcesAssetsPath:
                    _loaderMode = KAssetBundleLoaderMode.ResourcesLoad;
                    break;
                default:
                    Logger.LogError("Error DefaultInAppPathType: {0}", _inAppPathType);
                    break;
            }
        }
        // 不同的AssetBundle加载方式，对应不同的路径
        switch (_loaderMode)
        {
            case KAssetBundleLoaderMode.ResourcesLoad:
            case KAssetBundleLoaderMode.ResourcesLoadAsync:
                _inAppPathType = KResourceInAppPathType.ResourcesAssetsPath;
                break;
            case KAssetBundleLoaderMode.StreamingAssetsWww:
                _inAppPathType = KResourceInAppPathType.StreamingAssetsPath;
                break;
            default:
                Logger.LogError("[KAssetBundleLoader:Init]Unknow loader mode: {0}", _loaderMode);
                break;
        }


        if (NewAssetBundleLoaderEvent != null)
            NewAssetBundleLoaderEvent(url);

        RelativeResourceUrl = url;
        if (KResourceModule.GetResourceFullPath(url, out FullUrl, _inAppPathType))
        {
            KResourceModule.LogRequest("AssetBundle", FullUrl);
            KResourceModule.Instance.StartCoroutine(LoadAssetBundle(url));
        }
        else
        {
            if (Debug.isDebugBuild)
                Logger.LogError("[KAssetBundleLoader]Error Path: {0}", url);
            OnFinish(null);
        }
    }
    private IEnumerator LoadAssetBundle(string relativeUrl)
    {
        byte[] bundleBytes;
        if (_inAppPathType == KResourceInAppPathType.StreamingAssetsPath)
        {
            _wwwLoader = KWWWLoader.Load(FullUrl);
            while (!_wwwLoader.IsCompleted)
            {
                Progress = _wwwLoader.Progress/2f; // 最多50%， 要算上Parser的嘛
                yield return null;
            }

            if (!_wwwLoader.IsSuccess)
            {
                if (AssetBundlerLoaderErrorEvent != null)
                {
                    AssetBundlerLoaderErrorEvent(this);
                }
                Logger.LogError("[KAssetBundleLoader]Error Load AssetBundle: {0}", relativeUrl);
                OnFinish(null);
                yield break;
            }

            bundleBytes = _wwwLoader.Www.bytes;
        }
        else if (_inAppPathType == KResourceInAppPathType.ResourcesAssetsPath) // 使用Resources文件夹模式
        {
            var pathExt = Path.GetExtension(FullUrl); // Resources.Load无需扩展名
            var pathWithoutExt = FullUrl.Substring(0,
                FullUrl.Length - pathExt.Length);
            if (_loaderMode == KAssetBundleLoaderMode.ResourcesLoad)
            {
                var textAsset = Resources.Load<TextAsset>(pathWithoutExt);
                if (textAsset == null)
                {
                    if (AssetBundlerLoaderErrorEvent != null)
                        AssetBundlerLoaderErrorEvent(this);
                    Logger.LogError("[LoadAssetBundle]Cannot Resources.Load from : {0}", pathWithoutExt);
                    OnFinish(null);
                    yield break;
                }

                bundleBytes = textAsset.bytes;
            }
            else if (_loaderMode == KAssetBundleLoaderMode.ResourcesLoadAsync)
            {
                var loadReq = Resources.LoadAsync<TextAsset>(pathWithoutExt);
                while (!loadReq.isDone)
                {
                    Progress = loadReq.progress/2f; // 最多50%， 要算上Parser的嘛
                }
                var loadAsset = loadReq.asset;
                var loadTextAsset = loadAsset as TextAsset;
                if (loadTextAsset == null)
                {
                    if (AssetBundlerLoaderErrorEvent != null)
                        AssetBundlerLoaderErrorEvent(this);
                    Logger.LogError("[KAssetBundleLoader]Error Resources.LoadAsync: {0}", relativeUrl);
                    OnFinish(null);
                    yield break;
                }
                bundleBytes = loadTextAsset.bytes;
            }
            else
            {
                if (AssetBundlerLoaderErrorEvent != null)
                    AssetBundlerLoaderErrorEvent(this);
                Logger.LogError("[LoadAssetBundle]Unvalid LoaderMode on Resources Load Mode: {0}", _loaderMode);
                OnFinish(null);
                yield break;
            }
        }
        else
        {
            Logger.LogError("[LoadAssetBundle]Error InAppPathType: {0}", KResourceModule.DefaultInAppPathType);
            OnFinish(null);
            yield break;
        }

        Progress = 1/2f;

        BundleParser = new KAssetBundleParser(RelativeResourceUrl, bundleBytes);
        while (!BundleParser.IsFinished)
        {
            if (IsReadyDisposed) // 中途释放
            {
                OnFinish(null);
                yield break;
            }
            Progress = BundleParser.Progress/2f + 1/2f; // 最多50%， 要算上WWWLoader的嘛
            yield return null;
        }

        Progress = 1f;
        var assetBundle = BundleParser.Bundle;
        if (assetBundle == null)
            Logger.LogError("WWW.assetBundle is NULL: {0}", FullUrl);

        OnFinish(assetBundle);

        //Array.Clear(cloneBytes, 0, cloneBytes.Length);  // 手工释放内存

        //GC.Collect(0);// 手工释放内存
    }

    protected override void OnFinish(object resultObj)
    {
        if (_wwwLoader != null)
        {
            // 释放WWW加载的字节。。释放该部分内存，因为AssetBundle已经自己有缓存了
            _wwwLoader.Release();
            _wwwLoader = null;
        }
        base.OnFinish(resultObj);
    }

    protected override void DoDispose()
    {
        base.DoDispose();

        if (BundleParser != null)
            BundleParser.Dispose(false);
    }

    public override void Release()
    {
        if (Application.isEditor)
        {
            if (Url.Contains("Arial"))
            {
                Logger.LogError("要释放Arial字体！！错啦！！builtinextra:{0}", Url);
                //UnityEditor.EditorApplication.isPaused = true;
            }
        }

        base.Release();
    }

    /// 舊的tips~忽略
    /// 原以为，每次都通过getter取一次assetBundle会有序列化解压问题，会慢一点，后用AddWatch调试过，发现如果把.assetBundle放到Dictionary里缓存，查询会更慢
    /// 因为，估计.assetBundle是一个纯Getter，没有做序列化问题。  （不保证.mainAsset）
}