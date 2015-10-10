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
using Object = UnityEngine.Object;
using CosmosEngine;

/// <summary>
/// 根據不同模式，從AssetBundle中獲取Asset或從Resources中獲取,一個橋接類
/// 读取一个文件的对象，不做拷贝和引用
/// </summary>
public class CAssetFileLoader : CBaseResourceLoader
{
    public delegate void CAssetFileBridgeDelegate(bool isOk, UnityEngine.Object resultObj);
    string AssetInBundleName;  // AssetBundle里的名字, Resources時不用  TODO: 暂时没用额

    public UnityEngine.Object Asset { get { return ResultObject as UnityEngine.Object; } }
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

    private CAssetBundleLoader _bundleLoader;

    public static CAssetFileLoader Load(string path, CAssetFileBridgeDelegate assetFileLoadedCallback = null)
    {
        CLoaderDelgate realcallback = null;
        if (assetFileLoadedCallback != null)
        {
            realcallback = (isOk, obj) => assetFileLoadedCallback(isOk, obj as UnityEngine.Object);
        }

        return AutoNew<CAssetFileLoader>(path, realcallback);
    }

    protected override void Init(string url)
    {
        base.Init(url);
        CResourceModule.Instance.StartCoroutine(_Init(Url, null));
    }

    IEnumerator _Init(string path, string assetName)
    {
        IsLoadAssetBundle = CCosmosEngine.GetConfig("IsLoadAssetBundle").ToInt32() != 0;
        AssetInBundleName = assetName;

        UnityEngine.Object getAsset = null;
        if (!IsLoadAssetBundle)
        {
            string extension = System.IO.Path.GetExtension(path);
            path = path.Substring(0, path.Length - extension.Length);  // remove extensions

            getAsset = Resources.Load<UnityEngine.Object>(path);
            if (getAsset == null)
            {
                CDebug.LogError("Asset is NULL(from Resources Folder): {0}", path);
            }
            OnFinish(getAsset);
        }
        else
        {
            _bundleLoader = CAssetBundleLoader.Load(path);

            while (!_bundleLoader.IsFinished)
            {
                if (IsReadyDisposed)   // 中途释放
                {
                    _bundleLoader.Release();
                    OnFinish(null);
                    yield break;
                }
                yield return null;
            }

            if (!_bundleLoader.IsOk)
            {
                CDebug.LogError("[CAssetFileLoader]Load BundleLoader Failed(Error) when Finished: {0}", path);
                _bundleLoader.Release();
                OnFinish(null);
                yield break;
            }

            var assetBundle = _bundleLoader.Bundle;

            System.DateTime beginTime = System.DateTime.Now;
            if (AssetInBundleName == null)
            {
                // 经过AddWatch调试，.mainAsset这个getter第一次执行时特别久，要做序列化
                //AssetBundleRequest request = assetBundle.LoadAsync("", typeof(Object));// mainAsset
                //while (!request.isDone)
                //{
                //    yield return null;
                //}
                try
                {
                    CDebug.Assert(getAsset = assetBundle.mainAsset);
                }
                catch
                {
                    CDebug.LogError("[OnAssetBundleLoaded:mainAsset]{0}", path);
                }
            }
            else
            {
                // TODO: 未测试过这几行!~~
                AssetBundleRequest request = assetBundle.LoadAsync(AssetInBundleName, typeof(Object));
                while (!request.isDone)
                {
                    yield return null;
                }

                getAsset = request.asset;
            }

            CResourceModule.LogLoadTime("AssetFileBridge", path, beginTime);

            if (getAsset == null)
            {
                CDebug.LogError("Asset is NULL: {0}", path);
            }

            _bundleLoader.Release();  // 释放Bundle(WebStream)
        }

        if (Application.isEditor)
        {
            if (getAsset != null)
                CResourceLoadObjectDebugger.Create(getAsset.GetType().Name, Url, getAsset as UnityEngine.Object);
        }

        if (getAsset != null)
        {
            // 更名~ 注明来源asset bundle 带有类型
            getAsset.name = string.Format("{0}~{1}", getAsset, Url);
        }
        OnFinish(getAsset);
    }

    protected override void DoDispose()
    {
        base.DoDispose();
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
            //    CDebug.LogWarning("[DisposeTheCache]Remove Fail(可能有两个未完成的，同时来到这) : {0}", Url);
            //}
        }
        //else
        //{
        //    // 交给加载后，进行检查并卸载资源
        //    // 可能情况TIPS：两个未完成的！会触发上面两次！
        //}
    }
}
