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
using System.Reflection;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum CResourceManagerPathType
{
    StreamingAssetsPathPriority, // 忽略PersitentDataPath
    PersistentDataPathPriority,  // 尝试在Persistent目錄尋找，找不到再去StreamingAssets
}
public class CResourceModule : MonoBehaviour, ICModule
{
    public delegate void ASyncLoadABAssetDelegate(Object asset, object[] args);
    public enum LoadingLogLevel
    {
        Quite,
        ShowTime,
        ShowDetail,
    }

    private static CResourceModule _Instance;
    public static CResourceModule Instance
    {
        get
        {
            if (_Instance == null)
            {
                GameObject resMgr = GameObject.Find("ResourceManager");
                if (resMgr == null)
                    resMgr = new GameObject("ResourceManager");

                _Instance = resMgr.AddComponent<CResourceModule>();
            }
            return _Instance;
        }
    }
    public static bool LoadByQueue = false;
    public static int LogLevel = (int)LoadingLogLevel.Quite;
    public static string BuildPlatformName;
    public static string ResourcesPath;
    public static string ResourcesPathWithOutFileProtocol;
    public static string ApplicationPath;
    public static string DocumentResourcesPathWithOutFileStart;
    private static string DocumentResourcesPath;

    public static CResourceManagerPathType ResourcePathType = CResourceManagerPathType.StreamingAssetsPathPriority;  // 是否優先找下載的資源?還是app本身資源優先

    public static System.Func<string, string> CustomGetResourcesPath; // 自定义资源路径。。。

    public static string GetResourcesPath(string url)
    {
        if (string.IsNullOrEmpty(url))
            CBase.LogError("尝试获取一个空的资源路径！");

        string docUrl;
        bool hasDocUrl = TryGetDocumentResourceUrl(url, out docUrl);

        string inAppUrl;
        bool hasInAppUrl = TryGetInAppResourceUrl(url, out inAppUrl);

        if (ResourcePathType == CResourceManagerPathType.PersistentDataPathPriority)  // 優先下載資源模式
        {
            if (hasDocUrl)
            {
                if (Application.isEditor)
                    CBase.LogWarning("使用外部资源 {0}", docUrl);
                return docUrl;
            }
            else
            {
                return inAppUrl;  // 優先下載資源，但又沒有下載資源文件！使用本地資源目錄
            }
        }
        else
        {
            if (!hasInAppUrl)
                CBase.LogError("找不到InApp的資源: {0}", url);
            return inAppUrl;  // 直接使用本地資源！

            // ？？ 沒有本地資源但又遠程資源？竟然！!?
        }
    }

    /// <summary>
    /// 獲取app數據目錄，可寫，同Application.PersitentDataPath，但在windows平台時為了避免www類中文目錄無法讀取問題，單獨實現
    /// </summary>
    /// <returns></returns>
    public static string GetAppDataPath()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsWebPlayer)
        {
            string dataPath = Application.dataPath + "/../Temp/UnityWinPersistentDataPath";
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);
            return dataPath;
        }

        return Application.persistentDataPath;
    }

    public static bool TryGetInAppResourceUrl(string url, out string newUrl)
    {
        newUrl = ResourcesPath + url;

        // 注意，StreamingAssetsPath在Android平台時，壓縮在apk里面，不要做文件檢查了
        if (Application.platform != RuntimePlatform.Android && !File.Exists(ResourcesPathWithOutFileProtocol + url))
        {
            CBase.LogError("[GetResourcePath:InAppUrl]Not Exist File: {0}", newUrl);
            return false;
        }

        return true;
    }

    public static bool TryGetDocumentResourceUrl(string url, out string newUrl)
    {
        newUrl = DocumentResourcesPath + url;
        if (File.Exists(DocumentResourcesPathWithOutFileStart + url))
        {
            return true;
        }

        return false;
    }

    void Awake()
    {
        if (_Instance != null)
            CBase.Assert(_Instance == this);
    }

    public IEnumerator Init()
    {
        InitResourcePath();
        
        yield break;
    }

    public IEnumerator UnInit()
    {
        yield break;
    }

    /// <summary>
    /// Different platform's assetBundles is incompatible. 
    /// CosmosEngine put different platform's assetBundles in different folder.
    /// Here, get Platform name that represent the AssetBundles Folder.
    /// </summary>
    /// <returns>Platform folder Name</returns>
    public static string GetBuildPlatformName()
    {
        string buildPlatformName = "Win32"; // default
#if UNITY_EDITOR
        // 根据编辑器的当前编译环境, 来确定读取哪个资源目录
        // 因为美术库是根据编译环境来编译资源的，这样可以在Unity编辑器上， 快速验证其资源是否正确再放到手机上
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                buildPlatformName = "Win32";
                break;
            case BuildTarget.Android:
                buildPlatformName = "Android";
                break;
            case BuildTarget.iPhone:
                buildPlatformName = "IOS";
                break;
        }
#else
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

        }
#endif

        return buildPlatformName;
    }

    /// <summary>
    /// On Windows, file protocol has a strange rule that has one more slash
    /// </summary>
    /// <returns>string, file protocol string</returns>
    public static string GetFileProtocol()
    {
        string fileProtocol = "file://";
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsWebPlayer)
            fileProtocol = "file:///";

        return fileProtocol;
    }

    /// <summary>
    /// Initialize the path of AssetBundles store place ( Maybe in PersitentDataPath or StreamingAssetsPath )
    /// </summary>
    /// <returns></returns>
    public static void InitResourcePath()
    {
        string productPath = Path.Combine(Application.dataPath, CCosmosEngine.GetConfig("ProductRelPath"));
        string assetBundlePath = Path.Combine(Application.dataPath, CCosmosEngine.GetConfig("AssetBundleRelPath"));
        string resourceDirName = Path.GetFileName(CCosmosEngine.GetConfig("AssetBundleRelPath"));

        BuildPlatformName = GetBuildPlatformName();

        string fileProtocol = GetFileProtocol();

        DocumentResourcesPathWithOutFileStart = string.Format("{0}/{1}/{2}/", GetAppDataPath(), resourceDirName, GetBuildPlatformName());  // 各平台通用
        DocumentResourcesPath = fileProtocol + DocumentResourcesPathWithOutFileStart;

        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.OSXEditor:
                {
                    ApplicationPath = string.Format("{0}{1}/", fileProtocol, productPath);
                    ResourcesPath = fileProtocol + assetBundlePath + "/" + BuildPlatformName + "/";
                    ResourcesPathWithOutFileProtocol = assetBundlePath + "/" + BuildPlatformName + "/";

                }
                break;
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.OSXPlayer:
                {
                    string path = Application.dataPath.Replace('\\', '/');
                    path = path.Substring(0, path.LastIndexOf('/') + 1);
                    ApplicationPath = string.Format("{0}{1}/", fileProtocol, path);
                    ResourcesPath = string.Format("{0}{1}{2}/{3}/", fileProtocol, path, resourceDirName, GetBuildPlatformName());
                    ResourcesPathWithOutFileProtocol = string.Format("{0}{1}/{2}/", path, resourceDirName, GetBuildPlatformName());

                }
                break;
            case RuntimePlatform.Android:
                {
                    ApplicationPath = string.Concat("jar:", fileProtocol, Application.dataPath, "!/assets/");

                    ResourcesPath = string.Concat(ApplicationPath, GetBuildPlatformName(), "/");
                    ResourcesPathWithOutFileProtocol = string.Concat(Application.dataPath, "!/assets/", GetBuildPlatformName() + "/");  // 注意，StramingAsset在Android平台中，是在壓縮的apk里，不做文件檢查
                }
                break;
            case RuntimePlatform.IPhonePlayer:
                {
                    ApplicationPath = System.Uri.EscapeUriString(fileProtocol + Application.streamingAssetsPath + "/");  // MacOSX下，带空格的文件夹，空格字符需要转义成%20
                    ResourcesPath = string.Format("{0}{1}/", ApplicationPath, GetBuildPlatformName());  // only iPhone need to Escape the fucking Url!!! other platform works without it!!! Keng Die!
                    ResourcesPathWithOutFileProtocol = Application.streamingAssetsPath + "/" + GetBuildPlatformName() + "/";
                }
                break;
            default:
                {
                    CBase.Assert(false);
                }
                break;
        }

        if (Debug.isDebugBuild)
        {
            CBase.Log("ResourceManager ApplicationPath: {0}", ApplicationPath);
            CBase.Log("ResourceManager ResourcesPath: {0}", ResourcesPath);
            CBase.Log("ResourceManager DocumentResourcesPath: {0}", DocumentResourcesPath);
            CBase.Log("================================================================================");
        }
    }

    public static void LogRequest(string resType, string resPath)
    {
        if (LogLevel < (int)LoadingLogLevel.ShowDetail)
            return;

        CBase.Log("[Request] {0}, {1}", resType, resPath);
    }

    public static void LogLoadTime(string resType, string resPath, System.DateTime begin)
    {
        if (LogLevel < (int)LoadingLogLevel.ShowTime)
            return;

        CBase.Log("[Load] {0}, {1}, {2}s", resType, resPath, (System.DateTime.Now - begin).TotalSeconds);
    }

    public static void Collect()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

}
