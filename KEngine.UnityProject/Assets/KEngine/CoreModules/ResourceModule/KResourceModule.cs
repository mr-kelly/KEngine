#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KResourceModule.cs
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
using Object = UnityEngine.Object;

public enum KResourceQuality
{
    Sd = 2,
    Hd = 1,
    Ld = 4,
}

/// <summary>
/// In App Path, 指Streaming Assets目录或Resources目录，使用哪一种方式去加载AssetBundle
/// StreamingAssets目录，不能使用同步语法去加载，但Resources可以
/// 使用StreamingAssets读取AssetBundle，还是使用Resources目录使用AssetBundle？
/// </summary>
public enum KResourceInAppPathType
{
    Invalid,
    StreamingAssetsPath,

    ResourcesAssetsPath,

    /// <summary>
    /// 将采用KResourceModule中的DefaultInAppPathType来设置
    /// </summary>
    Default,
}

/// <summary>
/// 资源路径优先级，优先使用
/// </summary>
public enum KResourcePathPriorityType
{
    Invalid,

    /// <summary>
    /// 忽略PersitentDataPath, 优先寻找Resources或StreamingAssets路径 (取决于ResourcePathType)
    /// </summary>
    InAppPathPriority,

    /// <summary>
    /// 尝试在Persistent目錄尋找，找不到再去StreamingAssets,
    /// 这一般用于进行热更新版本号判断后，设置成该属性
    /// </summary>
    PersistentDataPathPriority,
}

public class KResourceModule : MonoBehaviour, IModule
{
    public delegate void ASyncLoadABAssetDelegate(Object asset, object[] args);

    public enum LoadingLogLevel
    {
        None,
        ShowTime,
        ShowDetail,
    }

    public static KResourceQuality Quality = KResourceQuality.Sd;

    public static KResourceInAppPathType DefaultInAppPathType = KResourceInAppPathType.ResourcesAssetsPath;

    public static float TextureScale
    {
        get { return 1f/(float) Quality; }
    }

    private static KResourceModule _Instance;

    public static KResourceModule Instance
    {
        get
        {
            if (_Instance == null)
            {
                GameObject resMgr = GameObject.Find("_ResourceModule_");
                if (resMgr == null)
                {
                    resMgr = new GameObject("_ResourceModule_");
                    GameObject.DontDestroyOnLoad(resMgr);
                }

                _Instance = resMgr.AddComponent<KResourceModule>();
            }
            return _Instance;
        }
    }

    public static bool LoadByQueue = false;
    public static int LogLevel = (int) LoadingLogLevel.None;

    public static string BuildPlatformName
    {
        get { return GetBuildPlatformName(); }
    } // ex: IOS, Android, AndroidLD

    public static string FileProtocol
    {
        get { return GetFileProtocol(); }
    } // for WWW...with file:///xxx

    /// <summary>
    /// Product Folder's Relative Path   -  Default: ../Product,   which means Assets/../Product
    /// </summary>
    public static string ProductRelPath
    {
        get { return KEngine.AppEngine.GetConfig(KEngineDefaultConfigs.ProductRelPath); }
    }

    /// <summary>
    /// Product Folder Full Path , Default: C:\xxxxx\xxxx\../Product
    /// </summary>
    public static string EditorProductFullPath
    {
        get { return Path.Combine(Application.dataPath, ProductRelPath); }
    }

    /// <summary>
    /// StreamingAssetsPath/Bundles/Android/ etc.
    /// </summary>
    public static string StreamingPlatformPath;

    public static string StreamingPlatformPathWithoutFileProtocol;

    /// <summary>
    /// Resources/Bundles/Android/ etc...
    /// </summary>
    public static string ResourceFolderPlatformPath;

    public static string ApplicationPath;

    public static string DocumentResourcesPathWithoutFileProtocol
    {
        get { return string.Format("{0}/{1}/{2}/", GetAppDataPath(), BundlesDirName, GetBuildPlatformName()); // 各平台通用
        }
    }

    public static string DocumentResourcesPath;

    public static KResourcePathPriorityType ResourcePathPriorityType =
        KResourcePathPriorityType.PersistentDataPathPriority; // 是否優先找下載的資源?還是app本身資源優先

    public static System.Func<string, string> CustomGetResourcesPath; // 自定义资源路径。。。

    /// <summary>
    /// 统一在字符串后加上.box, 取决于配置的AssetBundle后缀
    /// </summary>
    /// <param name="path"></param>
    /// <param name="formats"></param>
    /// <returns></returns>
    public static string GetAssetBundlePath(string path, params object[] formats)
    {
        return string.Format(path + KEngine.AppEngine.GetConfig("AssetBundleExt"), formats);
    }

    // 检查资源是否存在
    public static bool ContainsResourceUrl(string resourceUrl,
        KResourceInAppPathType inAppPathType = KResourceInAppPathType.StreamingAssetsPath)
    {
        string fullPath;
        return GetResourceFullPath(resourceUrl, out fullPath, inAppPathType, false);
    }

    /// <summary>
    /// 完整路径，www加载
    /// </summary>
    /// <param name="url"></param>
    /// <param name="inAppPathType"></param>
    /// <param name="isLog"></param>
    /// <returns></returns>
    public static string GetResourceFullPath(string url,
        KResourceInAppPathType inAppPathType = KResourceInAppPathType.StreamingAssetsPath, bool isLog = true)
    {
        string fullPath;
        if (GetResourceFullPath(url, out fullPath, inAppPathType, isLog))
            return fullPath;

        return null;
    }

    /// <summary>
    /// 根据相对路径，获取到StreamingAssets完整路径，或Resources中的路径
    /// </summary>
    /// <param name="url"></param>
    /// <param name="fullPath"></param>
    /// <param name="inAppPathType"></param>
    /// <param name="isLog"></param>
    /// <returns></returns>
    public static bool GetResourceFullPath(string url, out string fullPath,
        KResourceInAppPathType inAppPathType = KResourceInAppPathType.Default, bool isLog = true)
    {
        if (string.IsNullOrEmpty(url))
            Logger.LogError("尝试获取一个空的资源路径！");

        if (inAppPathType == KResourceInAppPathType.Default)
            inAppPathType = DefaultInAppPathType;

        string docUrl;
        bool hasDocUrl = TryGetDocumentResourceUrl(url, out docUrl);

        string inAppUrl;
        bool hasInAppUrl;
        if (inAppPathType == KResourceInAppPathType.StreamingAssetsPath)
        {
            hasInAppUrl = TryGetInAppStreamingUrl(url, out inAppUrl);
        }
        else if (inAppPathType == KResourceInAppPathType.ResourcesAssetsPath)
        {
            hasInAppUrl = TryGetInAppResourcesFolderUrl(url, out inAppUrl); // 使用Resources某
        }
        else
        {
            Logger.LogError("[GetResourceFullPath]Invalid InAppPathType: {0}", DefaultInAppPathType);
            hasInAppUrl = false;
            inAppUrl = null;
        }

        if (ResourcePathPriorityType == KResourcePathPriorityType.PersistentDataPathPriority) // 優先下載資源模式
        {
            if (hasDocUrl)
            {
                if (Application.isEditor)
                    Logger.LogWarning("[Use PersistentDataPath] {0}", docUrl);
                fullPath = docUrl;
                return true;
            }
            // 優先下載資源，但又沒有下載資源文件！使用本地資源目錄 
        }

        if (!hasInAppUrl) // 连本地资源都没有，直接失败吧 ？？ 沒有本地資源但又遠程資源？竟然！!?
        {
            if (isLog)
                Logger.LogError("[Not Found] StreamingAssetsPath Url Resource: {0}", url);
            fullPath = null;
            return false;
        }

        fullPath = inAppUrl; // 直接使用本地資源！

        return true;
    }

    /// <summary>
    /// 獲取app數據目錄，可寫，同Application.PersitentDataPath，但在windows平台時為了避免www類中文目錄無法讀取問題，單獨實現
    /// </summary>
    /// <returns></returns>
    public static string GetAppDataPath()
    {
        // Windows 时使用特定的目录，避免中文User的存在 
        // 去掉自定义PersistentDataPath, 2015/11/18， 务必要求Windows Users是英文
        //if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsWebPlayer)
        //{
        //    string dataPath = Application.dataPath + "/../Library/UnityWinPersistentDataPath";
        //    if (!Directory.Exists(dataPath))
        //        Directory.CreateDirectory(dataPath);
        //    return dataPath;
        //}

        return Application.persistentDataPath;
    }

    /// <summary>
    /// 使用Resources来放置AssetBundle
    /// </summary>
    /// <param name="relativePath"></param>
    /// <param name="inAppUrl"></param>
    /// <returns></returns>
    private static bool TryGetInAppResourcesFolderUrl(string relativePath, out string inAppUrl)
    {
        inAppUrl = ResourceFolderPlatformPath + relativePath;
        return true;
    }

    /// <summary>
    /// (not android ) only! Android资源不在目录！
    /// Editor返回文件系统目录，运行时返回StreamingAssets目录
    /// </summary>
    /// <param name="url"></param>
    /// <param name="newUrl"></param>
    /// <returns></returns>
    public static bool TryGetInAppStreamingUrl(string url, out string newUrl)
    {
        newUrl = StreamingPlatformPath + url;

        // 注意，StreamingAssetsPath在Android平台時，壓縮在apk里面，不要做文件檢查了
        if (Application.platform != RuntimePlatform.Android &&
            !File.Exists(StreamingPlatformPathWithoutFileProtocol + url))
        {
            return false;
        }
        // Windows/Edtiro平台下，进行大小敏感判断
        if (Application.isEditor)
        {
            var result = FileExistsWithDifferentCase(StreamingPlatformPathWithoutFileProtocol + url);
            if (!result)
            {
                Logger.LogError("[大小写敏感]发现一个资源 {0}，大小写出现问题，在Windows可以读取，手机不行，请改表修改！", url);
            }
        }
        return true;
    }

    /// <summary>
    /// 大小写敏感地进行文件判断, Windows Only
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private static bool FileExistsWithDifferentCase(string filePath)
    {
        if (File.Exists(filePath))
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileTitle = Path.GetFileName(filePath);
            string[] files = Directory.GetFiles(directory, fileTitle);
            var realFilePath = files[0].Replace("\\", "/");
            filePath = filePath.Replace("\\", "/");

            return String.CompareOrdinal(realFilePath, filePath) == 0;
        }
        return false;
    }

    public static bool TryGetDocumentResourceUrl(string url, out string newUrl)
    {
        newUrl = DocumentResourcesPath + url;
        if (File.Exists(DocumentResourcesPathWithoutFileProtocol + url))
        {
            return true;
        }

        return false;
    }

    private void Awake()
    {
        if (_Instance != null)
            Logger.Assert(_Instance == this);

        //InvokeRepeating("CheckGcCollect", 0f, 3f);
    }

    private void Update()
    {
        KAbstractResourceLoader.CheckGcCollect();
    }

    public IEnumerator Init()
    {
        InitResourcePath();

        if (Debug.isDebugBuild)
        {
            Logger.Log("ResourceManager ApplicationPath: {0}", ApplicationPath);
            Logger.Log("ResourceManager StreamingPlatformPath: {0}", StreamingPlatformPath);
            Logger.Log("ResourceManager DocumentResourcesPath: {0}", DocumentResourcesPath);
            Logger.Log("================================================================================");
        }
        yield break;
    }

    public IEnumerator UnInit()
    {
        yield break;
    }


    private static string _unityEditorEditorUserBuildSettingsActiveBuildTarget;

    /// <summary>
    /// UnityEditor.EditorUserBuildSettings.activeBuildTarget, Can Run in any platform~
    /// </summary>
    public static string UnityEditor_EditorUserBuildSettings_activeBuildTarget
    {
        get
        {
            if (Application.isPlaying && !string.IsNullOrEmpty(_unityEditorEditorUserBuildSettingsActiveBuildTarget))
            {
                return _unityEditorEditorUserBuildSettingsActiveBuildTarget;
            }
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var a in assemblies)
            {
                if (a.GetName().Name == "UnityEditor")
                {
                    Type lockType = a.GetType("UnityEditor.EditorUserBuildSettings");
                    //var retObj = lockType.GetMethod(staticMethodName,
                    //    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                    //    .Invoke(null, args);
                    //return retObj;
                    var p = lockType.GetProperty("activeBuildTarget");

                    var em = p.GetGetMethod().Invoke(null, new object[] {}).ToString();
                    _unityEditorEditorUserBuildSettingsActiveBuildTarget = em;
                    return em;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Different platform's assetBundles is incompatible.
    /// CosmosEngine put different platform's assetBundles in different folder.
    /// Here, get Platform name that represent the AssetBundles Folder.
    /// </summary>
    /// <returns>Platform folder Name</returns>
    private static string GetBuildPlatformName()
    {
        string buildPlatformName = "Win32"; // default

        if (Application.isEditor)
        {
            var buildTarget = UnityEditor_EditorUserBuildSettings_activeBuildTarget;
            //UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            switch (buildTarget)
            {
                case "StandaloneWindows": // UnityEditor.BuildTarget.StandaloneWindows:
                case "StandaloneWindows64": // UnityEditor.BuildTarget.StandaloneWindows64:
                    buildPlatformName = "Win32";
                    break;
                case "Android": // UnityEditor.BuildTarget.Android:
                    buildPlatformName = "Android";
                    break;
                case "iPhone": // UnityEditor.BuildTarget.iPhone:
                    buildPlatformName = "IOS";
                    break;
                default:
                    Logger.Assert(false);
                    break;
            }
        }
        else
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    buildPlatformName = "Android";
                    break;
                case RuntimePlatform.IPhonePlayer:
                    buildPlatformName = "IOS";
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsWebPlayer:
                    buildPlatformName = "Win32";
                    break;
                default:
                    Logger.Assert(false);
                    break;
            }
        }

        if (Quality != KResourceQuality.Sd) // SD no need add
            buildPlatformName += Quality.ToString().ToUpper();
        return buildPlatformName;
    }

    /// <summary>
    /// On Windows, file protocol has a strange rule that has one more slash
    /// </summary>
    /// <returns>string, file protocol string</returns>
    public static string GetFileProtocol()
    {
        string fileProtocol = "file://";
        if (Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.WindowsWebPlayer)
            fileProtocol = "file:///";

        return fileProtocol;
    }

    public static string BundlesDirName
    {
        get { return KEngine.AppEngine.GetConfig(KEngineDefaultConfigs.StreamingBundlesFolderName); }
    }

    /// <summary>
    /// Unity Editor load AssetBundle directly from the Asset Bundle Path,
    /// whth file:// protocol
    /// </summary>
    public static string EditorAssetBundlePath
    {
        get
        {
            string editorAssetBundlePath = Path.Combine(Application.dataPath,
                KEngine.AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleBuildRelPath)); // for editoronly

            return editorAssetBundlePath;
        }
    }

    /// <summary>
    /// Initialize the path of AssetBundles store place ( Maybe in PersitentDataPath or StreamingAssetsPath )
    /// </summary>
    /// <returns></returns>
    public static void InitResourcePath()
    {
        string editorProductPath = EditorProductFullPath;

        DocumentResourcesPath = FileProtocol + DocumentResourcesPathWithoutFileProtocol;

        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.OSXEditor:
            {
                ApplicationPath = string.Format("{0}{1}/", GetFileProtocol(), editorProductPath);
                StreamingPlatformPath = GetFileProtocol() + EditorAssetBundlePath + "/" + BuildPlatformName + "/";
                StreamingPlatformPathWithoutFileProtocol = EditorAssetBundlePath + "/" + BuildPlatformName + "/";
                ResourceFolderPlatformPath = string.Format("{0}/{1}/", BundlesDirName, GetBuildPlatformName());
                // Resources folder
            }
                break;
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.OSXPlayer:
            {
                string path = Application.dataPath.Replace('\\', '/');
                path = path.Substring(0, path.LastIndexOf('/') + 1);
                ApplicationPath = string.Format("{0}{1}/", GetFileProtocol(), path);
                StreamingPlatformPath = string.Format("{0}{1}/{2}/{3}/", GetFileProtocol(), path, BundlesDirName,
                    GetBuildPlatformName());
                StreamingPlatformPathWithoutFileProtocol = string.Format("{0}/{1}/{2}/", path, BundlesDirName,
                    GetBuildPlatformName());
                ResourceFolderPlatformPath = string.Format("{0}/{1}/", BundlesDirName, GetBuildPlatformName());
                // Resources folder
            }
                break;
            case RuntimePlatform.Android:
            {
                ApplicationPath = string.Concat("jar:", GetFileProtocol(), Application.dataPath,
                    string.Format("!/assets/{0}/", BundlesDirName));
                StreamingPlatformPath = string.Concat(ApplicationPath, GetBuildPlatformName(), "/");
                StreamingPlatformPathWithoutFileProtocol = string.Concat(Application.dataPath,
                    "!/assets/" + BundlesDirName + "/", GetBuildPlatformName() + "/");
                // 注意，StramingAsset在Android平台中，是在壓縮的apk里，不做文件檢查
                ResourceFolderPlatformPath = string.Format("{0}/{1}/", BundlesDirName, GetBuildPlatformName());
                // Resources folder
            }
                break;
            case RuntimePlatform.IPhonePlayer:
            {
                ApplicationPath =
                    System.Uri.EscapeUriString(GetFileProtocol() + Application.streamingAssetsPath + "/" +
                                               BundlesDirName + "/"); // MacOSX下，带空格的文件夹，空格字符需要转义成%20
                StreamingPlatformPath = string.Format("{0}{1}/", ApplicationPath, GetBuildPlatformName());
                // only iPhone need to Escape the fucking Url!!! other platform works without it!!! Keng Die!
                StreamingPlatformPathWithoutFileProtocol = Application.streamingAssetsPath + "/" + BundlesDirName + "/" +
                                                           GetBuildPlatformName() + "/";
                ResourceFolderPlatformPath = string.Format("{0}/{1}/", BundlesDirName, GetBuildPlatformName());
                // Resources folder
            }
                break;
            default:
            {
                Logger.Assert(false);
            }
                break;
        }
    }

    public static void LogRequest(string resType, string resPath)
    {
        if (LogLevel < (int) LoadingLogLevel.ShowDetail)
            return;

        Logger.Log("[Request] {0}, {1}", resType, resPath);
    }

    public static void LogLoadTime(string resType, string resPath, System.DateTime begin)
    {
        if (LogLevel < (int) LoadingLogLevel.ShowTime)
            return;

        Logger.Log("[Load] {0}, {1}, {2}s", resType, resPath, (System.DateTime.Now - begin).TotalSeconds);
    }

    public static void Collect()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}