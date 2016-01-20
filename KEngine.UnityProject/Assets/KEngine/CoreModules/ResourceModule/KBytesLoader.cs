#region Copyright (c) Kingsoft Xishanju

// KEngine - Asset Bundle framework for Unity3D
// ===================================
// 
// Filename: KBytesLoader.cs
// Date:        2016/01/20
// Author:     Kelly
// Email:       23110388@qq.com
// Github:     https://github.com/mr-kelly/KEngine
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

using System.Collections;
using System.IO;
using KEngine;
using UnityEngine;

/// <summary>
/// 读取字节，调用WWW或Resources.Load
/// </summary>
public class KBytesLoader : KAbstractResourceLoader
{
    public byte[] Bytes { get; private set; }
    private KWWWLoader _wwwLoader;

    private KResourceInAppPathType _inAppPathType;
    private KAssetBundleLoaderMode _loaderMode;

    public static KBytesLoader Load(string path, KResourceInAppPathType inAppPathType, KAssetBundleLoaderMode loaderMode)
    {
        var newLoader = AutoNew<KBytesLoader>(path, null, false, inAppPathType, loaderMode);

        return newLoader;
    }

    private string FullUrl;

    private IEnumerator CoLoad(string url, KResourceInAppPathType type, KAssetBundleLoaderMode loaderMode)
    {
        if (KResourceModule.GetResourceFullPath(url, out FullUrl, _inAppPathType))
        {

        }
        else
        {
            if (Debug.isDebugBuild)
                Logger.LogError("[KBytesLoader]Error Path: {0}", url);
            OnFinish(null);
        }
        if (_inAppPathType == KResourceInAppPathType.StreamingAssetsPath)
        {
            _wwwLoader = KWWWLoader.Load(FullUrl);
            while (!_wwwLoader.IsCompleted)
            {
                Progress = _wwwLoader.Progress / 2f; // 最多50%， 要算上Parser的嘛
                yield return null;
            }

            if (!_wwwLoader.IsSuccess)
            {
                //if (AssetBundlerLoaderErrorEvent != null)
                //{
                //    AssetBundlerLoaderErrorEvent(this);
                //}
                Logger.LogError("[KBytesLoader]Error Load WWW: {0}", url);
                OnFinish(null);
                yield break;
            }

            Bytes = _wwwLoader.Www.bytes;
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
                    Logger.LogError("[KBytesLoader]Cannot Resources.Load from : {0}", pathWithoutExt);
                    OnFinish(null);
                    yield break;
                }

                Bytes = textAsset.bytes;
                Resources.UnloadAsset(textAsset);
            }
            else if (_loaderMode == KAssetBundleLoaderMode.ResourcesLoadAsync)
            {
                var loadReq = Resources.LoadAsync<TextAsset>(pathWithoutExt);
                while (!loadReq.isDone)
                {
                    Progress = loadReq.progress / 2f; // 最多50%， 要算上Parser的嘛
                }
                var loadAsset = loadReq.asset;
                var loadTextAsset = loadAsset as TextAsset;
                if (loadTextAsset == null)
                {
                    Logger.LogError("[KBytesLoader]Error Resources.LoadAsync: {0}", url);
                    OnFinish(null);
                    yield break;
                }
                Bytes = loadTextAsset.bytes;
                Resources.UnloadAsset(loadTextAsset);
            }
            else
            {
                Logger.LogError("[KBytesLoader]Unvalid LoaderMode on Resources Load Mode: {0}", _loaderMode);
                OnFinish(null);
                yield break;
            }
        }
        else
        {
            Logger.LogError("[KBytesLoader]Error InAppPathType: {0}", KResourceModule.DefaultInAppPathType);
            OnFinish(null);
            yield break;
        }
        OnFinish(Bytes);
    }
    protected override void Init(string url, params object[] args)
    {
        base.Init(url, args);

        _inAppPathType = (KResourceInAppPathType)args[0];
        _loaderMode = (KAssetBundleLoaderMode) args[1];
        KResourceModule.Instance.StartCoroutine(CoLoad(url, _inAppPathType, _loaderMode));

    }

}