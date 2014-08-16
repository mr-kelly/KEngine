//-------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//-------------------------------------------------------------------------
using UnityEngine;
using System.Collections;

/// <summary>
/// 根據不同模式，從AssetBundle中獲取Asset或從Resources中獲取,一個橋接類
/// </summary>
public class CAssetFileBridge
{
    string Path;
    System.Action<UnityEngine.Object, object[]> AssetFileLoadedCallback;
    object[] CallbackArgs;
    string AssetInBundleName;  // AssetBundle里的名字, Resources時不用

    public CAssetFileBridge(string path, System.Action<UnityEngine.Object, object[]> assetFileLoadedCallback, params object[] args)
    {
        _Init(path, null, assetFileLoadedCallback, args);
    }

    // AssetBundle或Resource文件夾的資源文件
    public CAssetFileBridge(string path, string assetName, System.Action<UnityEngine.Object, object[]> assetFileLoadedCallback, params object[] args)
    {
        _Init(path, assetName, assetFileLoadedCallback, args);
    }

    void _Init(string path, string assetName, System.Action<UnityEngine.Object, object[]> assetFileLoadedCallback, object[] args)
    {

        Path = path;
        AssetFileLoadedCallback = assetFileLoadedCallback;
        AssetInBundleName = assetName;

        if (CCosmosEngine.GetConfig("IsLoadAssetBundle").ToInt32() == 0)
        {
            CResourceManager.Instance.StartCoroutine(LoadInResourceFolder(path));
        }
        else
        {
            new CAssetBundleLoader(path, OnAssetBundleLoaded);
        }
    }

    IEnumerator LoadInResourceFolder(string path)
    {
        yield return null; // 延遲1幀

        UnityEngine.Object asset = Resources.Load<UnityEngine.Object>(path);
        if (asset == null)
        {
            CBase.LogError("Asset is NULL(from Resources Folder): {0}", path);
        }
        AssetFileLoadedCallback(asset, CallbackArgs);
    }

    void OnAssetBundleLoaded(string url, AssetBundle assetBundle, params object[] args)
    {
        Object asset = null;
        System.DateTime beginTime = System.DateTime.Now;
        if (AssetInBundleName == null)
        {
            // 经过AddWatch调试，.mainAsset这个getter第一次执行时特别久，要做序列化
            asset = assetBundle.mainAsset;
        }
        else
        {
            AssetBundleRequest request = assetBundle.LoadAsync(AssetInBundleName, typeof(Object));
            asset = request.asset;
        }

        CResourceManager.LogLoadTime("AssetFileBridge", url, beginTime);

        if (asset == null)
        {
            CBase.LogError("Asset is NULL: {0}", url);
        }

        AssetFileLoadedCallback(asset, CallbackArgs);
    }
}
