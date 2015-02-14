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
using Object = UnityEngine.Object;

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
            var bundleLoader = CAssetBundleLoader.Load(path);

            while (!bundleLoader.IsFinished)
            {
                if (IsDisposed)   // 中途释放
                {
                    bundleLoader.Release();
                    OnFinish(null);
                    yield break;
                }
                Progress = bundleLoader.Progress;
                yield return null;
            }
            var assetBundle = bundleLoader.Bundle;

            System.DateTime beginTime = System.DateTime.Now;
            if (AssetInBundleName == null)
            {
                // 经过AddWatch调试，.mainAsset这个getter第一次执行时特别久，要做序列化
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

            bundleLoader.Release();  // 释放Bundle(WebStream)
        }

#if UNITY_EDITOR
        if (getAsset != null)
            CResourceLoadObjectDebugger.Create(getAsset.GetType().Name, Url, getAsset as UnityEngine.Object);
#endif

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
