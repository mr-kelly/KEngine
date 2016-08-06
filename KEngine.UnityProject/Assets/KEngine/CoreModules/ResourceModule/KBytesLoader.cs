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

namespace KEngine
{

    /// <summary>
    /// 读取字节，调用WWW, 会自动识别Product/Bundles/Platform目录和StreamingAssets路径
    /// </summary>
    public class BytesLoader : KAbstractResourceLoader
    {
        public byte[] Bytes { get; private set; }

        /// <summary>
        /// 异步模式中使用了WWWLoader
        /// </summary>
        private KWWWLoader _wwwLoader;

        private LoaderMode _loaderMode;

        public static BytesLoader Load(string path, LoaderMode loaderMode)
        {
            var newLoader = AutoNew<BytesLoader>(path, null, false, loaderMode);
            return newLoader;
        }

        private string _fullUrl;

        private IEnumerator CoLoad(string url)
        {
            if (!KResourceModule.GetResourceFullPath(url, out _fullUrl))
            {
                if (Debug.isDebugBuild)
                    Log.Error("[BytesLoader]Error Path: {0}", url);
                OnFinish(null);
                yield break;
            }

            if (_loaderMode == LoaderMode.Sync)
            {
                var loadSyncPath = string.Format("{0}/{1}", KResourceModule.BundlesPathWithoutFileProtocol, url);
                if (Application.isEditor) // Editor mode : 读取Product配置目录
                    Bytes = File.ReadAllBytes(loadSyncPath);
                else // product mode: read streamingAssetsPath
                {
                    Bytes = KResourceModule.LoadSyncFromStreamingAssets(loadSyncPath);
                }
            }
            else
            {
                _wwwLoader = KWWWLoader.Load(_fullUrl);
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
                    Log.Error("[BytesLoader]Error Load WWW: {0}", url);
                    OnFinish(null);
                    yield break;
                }

                Bytes = _wwwLoader.Www.bytes;
            }

            OnFinish(Bytes);
        }
        protected override void Init(string url, params object[] args)
        {
            base.Init(url, args);

            _loaderMode = (LoaderMode)args[0];
            KResourceModule.Instance.StartCoroutine(CoLoad(url));

        }

    }

}
