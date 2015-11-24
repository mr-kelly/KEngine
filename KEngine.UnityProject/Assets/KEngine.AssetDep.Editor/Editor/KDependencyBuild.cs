using System.Reflection;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using KEngine;
using KEngine.Editor;

public class CDepCollectInfo
{
    public string Path;  // 依赖对象将要最终打包的位置
    public UnityEngine.Object Asset;  // 打包的对象
    public bool HasBuild;

    public KAssetDep AssetDep;

    // TODO:
    public CDepCollectInfo Child; // 子依赖
}

// 处理依赖关系的打包工具
public partial class KDependencyBuild
{
    public static string DepBuildToFolder = "Common"; // 图片等依赖资源打包的地方, 运行时临时改变

    [AttributeUsage(AttributeTargets.Method)]
    public class DepBuildAttribute : Attribute
    {
        public DepBuildAttribute(Type type) { TheType = type; }
        public Type TheType { get; set; }
    }

    public static event Action<string> AddCacheEvent;
    public static Dictionary<string, bool> BuildedCache = new Dictionary<string, bool>();
    public static bool IsJustCollect = false;  // 是否只收集，不作打包（照旧可以拿到BuildedCache），只是没有实际执行Build而已

    /// <summary>
    /// save the cache. which file builded
    /// </summary>
    //[MenuItem("Game/TestSaveBuildAction")]
    public static void SaveBuildAction()
    {
        if (BuildedCache.Count > 0)
        {
            var zipDirPath = "Assets/" + KEngineDef.ResourcesBuildInfosDir;
            if (!Directory.Exists(zipDirPath))
                Directory.CreateDirectory(zipDirPath);
            var zipPath = zipDirPath + "/BuildAction_" + KResourceModule.BuildPlatformName + ".zip";
            var buildActionCount = 0;
            var actionCountStr = CZipTool.GetFileContentFromZip(zipPath, "BuildActionCount.txt");
            if (!string.IsNullOrEmpty(actionCountStr))
            {
                buildActionCount = actionCountStr.ToInt32();
            }
            buildActionCount++;
            Logger.Log(" DepBuild Now Action Version: {0}", buildActionCount);
            CZipTool.SetZipFile(zipPath, "BuildActionCount.txt", buildActionCount.ToString());

            // 真实打包的资源记录
            var sbBuildAction = new StringBuilder();
            foreach (var kv in BuildedCache)
            {
                sbBuildAction.AppendLine(kv.Key);
            }

            CZipTool.SetZipFile(zipPath, string.Format("{0}.txt", buildActionCount), sbBuildAction.ToString());

        }

        // 所有尝试打包的资源记录
        //var sbBuildActionFull = new StringBuilder();
        //foreach (var kv in ResourceCache)
        //{
        //    sbBuildActionFull.AppendLine(kv);
        //}
        //CZipTool.SetZipFile(zipPath, string.Format("{0}_full.txt", buildActionCount), sbBuildActionFull.ToString());

    }
    public static void Clear()
    {
        IsJustCollect = false;
        BuildedCache.Clear();
    }

    /// <summary>
    /// 移除一个GameObject里所存在的Prefab引用
    /// </summary>
    /// <param name="copyGameObject"></param>
    [Obsolete]
    private static void RemoveGameObjectChildPrefab(GameObject copyGameObject)
    {
        // Prefab检查
        //var gameObjectAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
        //if (gameObjectAsset != null)
        {
            // 遍历是否存在Prefab
            foreach (var child in copyGameObject.GetComponentsInChildren<Transform>(true))
            {
                if (child == copyGameObject.transform) continue; // 忽略自己

                var prefabParent = PrefabUtility.GetPrefabParent(child.gameObject);
                var prefabObject = PrefabUtility.GetPrefabObject(child.gameObject);
                if (//PrefabUtility.GetPrefabParent(child.gameObject) == null &&
                    PrefabUtility.GetPrefabObject(child.gameObject) != null)
                {
                    Debug.LogWarning("一个Prefab中的Prefab保持引用,剥除。。。" + child.name);
                    PrefabUtility.DisconnectPrefabInstance(child.gameObject);
                }
            }
        }

    }
    private static Dictionary<MethodInfo, DepBuildAttribute> _cachedDepBuildAttributes;
    // 可選擇是否打包自己，還是只打包依賴
    public static void BuildGameObject(GameObject obj, string path, bool buildSelf = true, bool allInOne = false, bool keepCopyObjToDebug = false)  // TODO: All in One
    {
        GameObject copyObj = GameObject.Instantiate(obj) as GameObject;

        if (_cachedDepBuildAttributes == null)
        {
            _cachedDepBuildAttributes = new Dictionary<MethodInfo, DepBuildAttribute>();

            foreach (var methodInfo in typeof(KDependencyBuild).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var depAttrs = methodInfo.GetCustomAttributes(typeof(DepBuildAttribute), true);
                Logger.Assert(depAttrs.Length <= 1); // 不会太大
                foreach (var attrbute in depAttrs)
                {
                    var depAttr = (DepBuildAttribute)attrbute;
                    _cachedDepBuildAttributes[methodInfo] = depAttr;
                    break;
                }
            }
        }

        foreach (var kv in _cachedDepBuildAttributes)
        {
            var depAttr = kv.Value;
            var methodInfo = kv.Key;

            foreach (Component component in copyObj.GetComponentsInChildren(depAttr.TheType, true))
            {
                methodInfo.Invoke(null, new object[] { Convert.ChangeType(component, depAttr.TheType) });
            }
        }


        // Build主对象
        if (buildSelf)
        {
            DoBuildAssetBundle(path, copyObj);  // TODO: BuildResult...
        }
        if (!keepCopyObjToDebug)
            GameObject.DestroyImmediate(copyObj);
    }


    // 
    /// <summary>
    /// 如果非realBuild,僅返回最終路徑
    /// 
    /// DoBuildAssetBundle和__DoBuildScriptableObject有两个开关，决定是否真的Build
    /// realBuildOrJustPath由外部传入，一般用于进行md5比较后，传入来的，【不收集Build缓存】 TODO：其实可以收集的。。
    /// 
    /// IsJustCollect用于全局的否决真Build，【收集Build缓存】
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="asset"></param>
    /// <param name="realBuildOrJustPath"></param>
    /// <returns></returns>
    public static CDepCollectInfo DoBuildAssetBundle(string path, UnityEngine.Object asset, bool realBuildOrJustPath = true)
    {
        path = Path.ChangeExtension(path, AppEngine.GetConfig("AssetBundleExt"));
        //asset.name = fullAssetPath;
        var hasBuilded = false;

        if (!BuildedCache.ContainsKey(path))
        {
            if (realBuildOrJustPath)
                AddCache(path);
            if (IsJustCollect)
                AddCache(path);
            if (!IsJustCollect && realBuildOrJustPath)
            {
                //BuildedCache[fullAssetPath] = true;
                KBuildTools.BuildAssetBundle(asset, path);
                hasBuilded = true;
            }
        }

        return new CDepCollectInfo
        {
            Path = path,
            Asset = asset,
            HasBuild = hasBuilded,
        };
    }

    public static void AddCache(string res)
    {
        BuildedCache[res] = true;
        if (AddCacheEvent != null)
            AddCacheEvent(res);
    }

    public static CDepCollectInfo __DoBuildScriptableObject(string fullAssetPath, ScriptableObject so, bool realBuildOrJustPath = true)
    {
        var hasBuilded = false;
        fullAssetPath = Path.ChangeExtension(fullAssetPath, AppEngine.GetConfig("AssetBundleExt"));

        if (so == null)
        {
            Logger.LogError("Error Null ScriptableObject: {0}", fullAssetPath);
        }
        else
        {
            //so.name = fullAssetPath;
            if (!BuildedCache.ContainsKey(fullAssetPath))
            {
                AddCache(fullAssetPath);
                if (!IsJustCollect && realBuildOrJustPath)
                {
                    KBuildTools.BuildScriptableObject(so, fullAssetPath);
                    hasBuilded = true;
                }
            }

        }

        return new CDepCollectInfo
        {
            Path = fullAssetPath,
            Asset = so,
            HasBuild = hasBuilded,
        };
    }


    static string __GetPrefabBuildPath(string path)
    {
        // 层次太深，只取后两位
        string[] strs = path.Replace(" ", "").Split('/');
        if (strs.Length > 2)
        {
            strs = strs.Skip(strs.Length - 2).Take(2).ToArray();
        }

        if (strs.Length == 2)
        {
            //if (strs.Length < 10)
            path = strs[0] + "_" + strs[1];  // 连目录名并在一起，防文件名重复
            //else
            //    path = strs[1];
        }
        else
            path = strs[0];

        return path;
    }

    //static HashSet<string> _depTextureScaleList = new HashSet<string>();  // 进行过Scale的图片
    static string BuildDepTexture(Texture tex, float scale = 1f)
    {
        Logger.Assert(tex);

        string assetPath = AssetDatabase.GetAssetPath(tex);
        bool needBuild = KAssetVersionControl.TryCheckNeedBuildWithMeta(assetPath);
        if (needBuild)
            KAssetVersionControl.TryMarkBuildVersion(assetPath);

        Texture newTex;
        if (tex is Texture2D)
        {
            var tex2d = (Texture2D)tex;

            if (needBuild &&
                !scale.Equals(1f))  // 需要进行缩放，才进来拷贝
            {
                var cacheDir = "Assets/" + KEngineDef.ResourcesBuildCacheDir + "/BuildDepTexture/";
                var cacheFilePath = cacheDir + Path.GetFileName(assetPath);

                //var needScale = !BuildedCache.ContainsKey("BuildDepTexture:" + assetPath);
                //if (needScale && !IsJustCollect)
                //{
                //    CFolderSyncTool.TexturePackerScaleImage(assetPath, cacheFilePath, scale);  // do scale
                //    var actionName = "BuildDepTexture:" + assetPath;
                //    //BuildedCache[actionName] = true;  // 下次就别scale了！蛋痛
                //    AddCache(actionName);
                //}

                newTex = AssetDatabase.LoadAssetAtPath(cacheFilePath, typeof(Texture2D)) as Texture2D;
                if (newTex == null)
                {
                    Logger.LogError("TexturePacker scale failed... {0}", assetPath);
                    newTex = tex2d;
                }

                SyncTextureImportSetting(tex2d, newTex as Texture2D);

                // TODO： mark to write
                //var texPath = AssetDatabase.GetAssetPath(tex2d);

                //var newTex2D = new Texture2D(tex2d.width, tex2d.height);
                //if (!string.IsNullOrEmpty(texPath)) // Assets内的纹理
                //{
                //    var bytes = File.ReadAllBytes(texPath);
                //    newTex2D.LoadImage(bytes);
                //}
                //else
                //{
                //    var bytes = tex2d.EncodeToPNG();
                //    newTex2D.LoadImage(bytes);
                //}

                //newTex2D.Apply();
                //GC.Collect();
                //// 进行缩放
                //TextureScaler.Bilinear(newTex2D, (int) (tex.width*scale), (int) (tex.height*scale));
                //GC.Collect();

                //newTex = newTex2D;
            }
            else
            {
                newTex = tex2d;
            }

        }
        else
        {
            newTex = tex;
            if (!scale.Equals(1f))
                Logger.LogWarning("[BuildDepTexture]非Texture2D: {0}, 无法进行Scale缩放....", tex);
        }
        //if (!IsJustCollect)
        //    CTextureCompressor.CompressTextureAsset(assetPath, newTex as Texture2D);

        string path = __GetPrefabBuildPath(assetPath);
        if (string.IsNullOrEmpty(path))
            Logger.LogWarning("[BuildTexture]不是文件的Texture, 估计是Material的原始Texture?");
        var result = DoBuildAssetBundle(DepBuildToFolder + "/Tex_" + path, newTex, needBuild);


        GC.Collect(0);
        return result.Path;
    }

    /// <summary>
    /// 图片打包工具，直接打包，不用弄成Texture!, 需要借助TexturePacker
    /// </summary>
    //public static TextAsset GetImageFromPath(string imageSystemFullPath, string toImageFormat)
    //{
    //    var cleanPath = imageSystemFullPath.Replace("\\", "/");
    //    var cacheDir = "Assets/" + CCosmosEngineDef.ResourcesBuildCacheDir + "/CacheImage/";
    //    var cacheFilePath = cacheDir + Path.GetFileName(cleanPath);
    //    cacheFilePath = cacheFilePath + ".bytes";
    //    CFolderSyncTool.TexturePackerScaleImage(cleanPath, cacheFilePath, CResourceModule.TextureScale, toImageFormat);
    //    var asset = AssetDatabase.LoadAssetAtPath(cacheFilePath, typeof (TextAsset)) as TextAsset;
    //    return asset;
    //}

    ///// <summary>
    ///// TODO:
    ///// </summary>
    ///// <param name="folderName"></param>
    ///// <param name="imageSystemFullPath"></param>
    ///// <returns></returns>
    //public static string BuildImage(string folderName, string imageSystemFullPath)
    //{
    //    var asset = GetImageFromPath(imageSystemFullPath, "etc1");
    //    return null;
    //}
    /// <summary>
    /// 单独打包的纹理图片, 用bytes读取, 指定保存的目录
    /// </summary>
    /// <param name="folderName"></param>
    /// <param name="tex"></param>
    /// <returns></returns>
    public static string BuildIOTexture(string folderName, string imageSystemFullPath, float scale = 1)
    {
        var cleanPath = imageSystemFullPath.Replace("\\", "/");

        var fileName = Path.GetFileNameWithoutExtension(cleanPath);
        var buildPath = string.Format("{0}/{1}_{0}{2}", folderName, fileName, AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt));
        var needBuild = KAssetVersionControl.TryCheckNeedBuildWithMeta(cleanPath);

        var texture = new Texture2D(1, 1);

        if (needBuild && !IsJustCollect)
        {
            var cacheDir = "Assets/" + KEngineDef.ResourcesBuildCacheDir + "/BuildIOTexture/" + folderName + "/";
            var cacheFilePath = cacheDir + Path.GetFileName(cleanPath);

            //CFolderSyncTool.TexturePackerScaleImage(cleanPath, cacheFilePath, Math.Min(1, GameDef.PictureScale * scale));  // TODO: do scale

            texture = AssetDatabase.LoadAssetAtPath(cacheFilePath, typeof(Texture2D)) as Texture2D;
            if (texture == null)
            {
                Logger.LogError("[BuildIOTexture]TexturePacker scale failed... {0}", cleanPath);
                return null;
            }

            //CTextureCompressor.CompressTextureAsset(cleanPath, texture);

            //CTextureCompressor.AutoCompressTexture2D(texture, EditorUserBuildSettings.activeBuildTarget);

            //if (!texture.LoadImage(File.ReadAllBytes(cleanPath)))
            //{
            //    Logger.LogError("无法LoadImage的Texture: {0}", cleanPath);
            //    return null;
            //}
            //texture.name = fileName;

            //// 多线程快速，图像缩放，双线性过滤插值
            //TextureScaler.Bilinear(texture, (int)(texture.width * GameDef.PictureScale),
            //    (int)(texture.height * GameDef.PictureScale));
            //GC.Collect();
        }

        // card/xxx_card.box
        var result = KDependencyBuild.DoBuildAssetBundle(buildPath, texture, needBuild);

        if (needBuild)
            KAssetVersionControl.TryMarkBuildVersion(cleanPath);

        return result.Path;
    }

    // 同步两个纹理的导入设置
    public static void SyncTextureImportSetting(Texture2D srcTexture, Texture2D newTexture)
    {
        var srcImporter = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(srcTexture));
        var newTexPath = AssetDatabase.GetAssetPath(newTexture);
        var newImporter = (TextureImporter)TextureImporter.GetAtPath(newTexPath);
        if (srcImporter == null || newImporter == null)
        {
            Logger.LogError("[SyncTextureImportSetting]Null Importer");
            return;
        }

        bool changed = false;

        if (srcImporter.alphaIsTransparency != newImporter.alphaIsTransparency)
        {
            srcImporter.alphaIsTransparency = newImporter.alphaIsTransparency;
            changed = true;
        }

        if (changed)
        {
            AssetDatabase.ImportAsset(newTexPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
    }

    /// <summary>
    /// 不用依赖判断，整个打包！
    /// </summary>
    /// <param name="tmpSpineObj"></param>
    /// <param name="path"></param>
    /// <param name="checkNeedBuild"></param>
    public static void BuildPureGameObject(GameObject tmpSpineObj, string path, bool checkNeedBuild)
    {
        KDependencyBuild.DoBuildAssetBundle(path, tmpSpineObj, checkNeedBuild);
    }
}
