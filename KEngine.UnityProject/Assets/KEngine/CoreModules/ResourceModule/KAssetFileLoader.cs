#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
//
// Filename: KAssetFileLoader.cs
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

using System.Collections;
using System.IO;
using KEngine;
using UnityEngine;

namespace KEngine
{
    /// <summary>
    /// 根據不同模式，從AssetBundle中獲取Asset或從Resources中獲取,两种加载方式同时实现的桥接类
    /// 读取一个文件的对象，不做拷贝和引用
    /// </summary>
    public class KAssetFileLoader : KAbstractResourceLoader
    {
        public delegate void AssetFileBridgeDelegate(bool isOk, UnityEngine.Object resultObj);

        public UnityEngine.Object Asset
        {
            get { return ResultObject as UnityEngine.Object; }
        }

        private bool IsLoadAssetBundle;

        public override float Progress
        {
            get
            {
                if (_bundleLoader != null)
                    return _bundleLoader.Progress;
                return 0;
            }
        }

        private KAssetBundleLoader _bundleLoader;

        public static KAssetFileLoader Load(string path, AssetFileBridgeDelegate assetFileLoadedCallback = null, LoaderMode loaderMode = LoaderMode.Async)
        {
            LoaderDelgate realcallback = null;
            if (assetFileLoadedCallback != null)
            {
                realcallback = (isOk, obj) => assetFileLoadedCallback(isOk, obj as UnityEngine.Object);
            }

            return AutoNew<KAssetFileLoader>(path, realcallback, false, loaderMode);
        }

        protected override void Init(string url, params object[] args)
        {
            var loaderMode = (LoaderMode)args[0];

            base.Init(url, args);
            KResourceModule.Instance.StartCoroutine(_Init(Url, loaderMode));
        }

        private IEnumerator _Init(string path, LoaderMode loaderMode)
        {
            IsLoadAssetBundle = KEngine.AppEngine.GetConfig("KEngine", "IsLoadAssetBundle").ToInt32() != 0;

            UnityEngine.Object getAsset = null;
            if (!IsLoadAssetBundle)
            {
                string extension = System.IO.Path.GetExtension(path);
                path = path.Substring(0, path.Length - extension.Length); // remove extensions

                getAsset = Resources.Load<UnityEngine.Object>(path);
                if (getAsset == null)
                {
                    Log.Error("Asset is NULL(from Resources Folder): {0}", path);
                }
                OnFinish(getAsset);
            }
            else
            {
                _bundleLoader = KAssetBundleLoader.Load(path, null, loaderMode);

                while (!_bundleLoader.IsCompleted)
                {
                    if (IsReadyDisposed) // 中途释放
                    {
                        _bundleLoader.Release();
                        OnFinish(null);
                        yield break;
                    }
                    yield return null;
                }

                if (!_bundleLoader.IsSuccess)
                {
                    Log.Error("[KAssetFileLoader]Load BundleLoader Failed(Error) when Finished: {0}", path);
                    _bundleLoader.Release();
                    OnFinish(null);
                    yield break;
                }

                var assetBundle = _bundleLoader.Bundle;

                System.DateTime beginTime = System.DateTime.Now;
#if UNITY_5
                // Unity 5 下，不能用mainAsset, 要取对象名
                var abAssetName = Path.GetFileNameWithoutExtension(Url).ToLower();
                if (loaderMode == LoaderMode.Sync)
                {
                    getAsset = assetBundle.LoadAsset(abAssetName);
                    Debuger.Assert(getAsset);
                }
                else
                {
                    var request = assetBundle.LoadAssetAsync(abAssetName);
                    while (!request.isDone)
                    {
                        yield return null;
                    }
                    Debuger.Assert(getAsset = request.asset);
                }

#else
                // 经过AddWatch调试，.mainAsset这个getter第一次执行时特别久，要做序列化
                //AssetBundleRequest request = assetBundle.LoadAsync("", typeof(Object));// mainAsset
                //while (!request.isDone)
                //{
                //    yield return null;
                //}
                try
                {
                    Debuger.Assert(getAsset = assetBundle.mainAsset);
                }
                catch
                {
                    Log.Error("[OnAssetBundleLoaded:mainAsset]{0}", path);
                }
#endif

                KResourceModule.LogLoadTime("AssetFileBridge", path, beginTime);

                if (getAsset == null)
                {
                    Log.Error("Asset is NULL: {0}", path);
                }

            }

            if (Application.isEditor)
            {
                if (getAsset != null)
                    KResoourceLoadedAssetDebugger.Create(getAsset.GetType().Name, Url, getAsset as UnityEngine.Object);
            }

            if (getAsset != null)
            {
                // 更名~ 注明来源asset bundle 带有类型
                getAsset.name = string.Format("{0}~{1}", getAsset, Url);
            }
            OnFinish(getAsset);
        }

        protected override void OnReadyDisposed()
        {
            base.OnReadyDisposed();
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            _bundleLoader.Release(IsBeenReleaseNow); // 释放Bundle(WebStream)
            //if (IsFinished)
            {
                if (!IsLoadAssetBundle)
                {
                    Resources.UnloadAsset(ResultObject as UnityEngine.Object);
                }
                else
                {
                    //Object.DestroyObject(ResultObject as UnityEngine.Object);

                    // Destroying GameObjects immediately is not permitted during physics trigger/contact, animation event callbacks or OnValidate. You must use Destroy instead.
                    Object.DestroyImmediate(ResultObject as UnityEngine.Object, true);
                }

                //var bRemove = Caches.Remove(Url);
                //if (!bRemove)
                //{
                //    Log.Warning("[DisposeTheCache]Remove Fail(可能有两个未完成的，同时来到这) : {0}", Url);
                //}
            }
            //else
            //{
            //    // 交给加载后，进行检查并卸载资源
            //    // 可能情况TIPS：两个未完成的！会触发上面两次！
            //}
        }
    }

}
