#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KDependencyBuild.cs
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

#if !UNITY_5
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KEngine;
using KEngine.Editor;
using KEngine.Lib;
using UnityEditor;
using UnityEngine;

public class CDepCollectInfo
{
    public string Path; // 依赖对象将要最终打包的位置
    public UnityEngine.Object Asset; // 打包的对象
    public bool HasBuild;

    public KAssetDep AssetDep;

    // TODO:
    public CDepCollectInfo Child; // 子依赖
}

public interface IDepBuildProcessor
{
    void Process(Component @object);
}

[AttributeUsage(AttributeTargets.Class)]
public class DepBuildClassAttribute : Attribute
{
    public Type ClassType;
    public DepBuildClassAttribute(Type type)
    {
        ClassType = type;
    }
}

/// <summary>
/// 专用于依赖打包的缓存机制
/// </summary>
/// <typeparam name="T"></typeparam>
public class KDepCollectInfoCaching
{
    private static readonly Dictionary<object, CDepCollectInfo> _cache = new Dictionary<object, CDepCollectInfo>();

    public static void Clear()
    {
        _cache.Clear();
    }

    public static bool HasCache<T>(T obj)
    {
        return _cache.ContainsKey(obj);
    }

    public static void SetCache<T>(T obj, CDepCollectInfo info)
    {
        _cache[obj] = info;

    }

    public static CDepCollectInfo GetCache<T>(T obj)
    {
        CDepCollectInfo info = null;
        _cache.TryGetValue(obj, out info);
        return info;
    }
}

// 处理依赖关系的打包工具
public class KDependencyBuild
{
    //public static string DepBuildToFolder = "Common"; // 图片等依赖资源打包的地方, 运行时临时改变

    //[AttributeUsage(AttributeTargets.Method)]
    //public class DepBuildAttribute : Attribute
    //{
    //    public DepBuildAttribute(Type type)
    //    {
    //        TheType = type;
    //    }

    //    public Type TheType { get; set; }
    //}

    public static event Action<string> AddCacheEvent;
    public static Dictionary<string, bool> BuildRecord = new Dictionary<string, bool>();
    public static bool IsJustCollect = false; // 是否只收集，不作打包（照旧可以拿到BuildedCache），只是没有实际执行Build而已
    public static List<Action> ClearActions = new List<Action>(); // Clear时执行的委托

    /// <summary>
    /// save the cache. which file builded
    /// </summary>
    //[MenuItem("Game/TestSaveBuildAction")]
    /// <summary>
    /// 完成所有后，请SaveBuildAction，并Clear！
    /// </summary>
    public static void SaveBuildAction()
    {
        if (BuildRecord.Count > 0)
        {
            var zipDirPath = KEngineDef.ResourcesBuildInfosDir;
            if (!Directory.Exists(zipDirPath))
                Directory.CreateDirectory(zipDirPath);
            var zipPath = zipDirPath + "/BuildAction_" + KResourceModule.BuildPlatformName + ".zip";
            var buildActionCount = 0;
            var actionCountStr = KZipTool.GetFileContentFromZip(zipPath, "BuildActionCount.txt");
            if (!string.IsNullOrEmpty(actionCountStr))
            {
                buildActionCount = actionCountStr.ToInt32();
            }
            buildActionCount++;
            Log.Info(" DepBuild Now Action Version: {0}", buildActionCount);
            KZipTool.SetZipFile(zipPath, "BuildActionCount.txt", buildActionCount.ToString());

            // 真实打包的资源记录
            var sbBuildAction = new StringBuilder();
            foreach (var kv in BuildRecord)
            {
                sbBuildAction.AppendLine(kv.Key);
            }

            KZipTool.SetZipFile(zipPath, string.Format("{0}.txt", buildActionCount), sbBuildAction.ToString());
        }

        // 所有尝试打包的资源记录
        //var sbBuildActionFull = new StringBuilder();
        //foreach (var kv in ResourceCache)
        //{
        //    sbBuildActionFull.AppendLine(kv);
        //}
        //KZipTool.SetZipFile(zipPath, string.Format("{0}_full.txt", buildActionCount), sbBuildActionFull.ToString());
    }

    public static void Clear()
    {
        IsJustCollect = false;
        BuildRecord.Clear();

        foreach (var action in ClearActions)
        {
            action();
        }
        ClearActions.Clear();
        KDepCollectInfoCaching.Clear();
    }

    //private static Dictionary<MethodInfo, DepBuildAttribute> _cachedDepBuildAttributes;

    private static Dictionary<IDepBuildProcessor, DepBuildClassAttribute> _cachedDepBuildClassAttributes;

    /// <summary>
    /// 可選擇是否打包自己，還是只打包依賴 
    /// TODO: Bug,如果引用发生变化，理论上自己也应该强制打包，否则引用地址已经发生改变
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="path"></param>
    /// <param name="buildSelf"></param>
    /// <param name="allInOne"></param>
    /// <param name="keepCopyObjToDebug"></param>
    /// <param name="instantiateObjecctToBuild ">是否拷贝一份，再进行打包？这样不会影响破坏源对象, 打包完成后会拷贝删除拷贝对象</param>
    public static void BuildGameObject(GameObject obj, string path, bool buildSelf = true, bool allInOne = false,
        bool keepCopyObjToDebug = false, bool instantiateObjectToBuild = true) // TODO: All in One
    {
        GameObject buildObj = instantiateObjectToBuild ? (GameObject.Instantiate(obj) as GameObject) : obj;

        if (_cachedDepBuildClassAttributes == null)
        {
            _cachedDepBuildClassAttributes = new Dictionary<IDepBuildProcessor, DepBuildClassAttribute>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var processorType in asm.GetTypes())
                {
                    var depBuildClassAttrs = processorType.GetCustomAttributes(typeof(DepBuildClassAttribute), false);
                    if (depBuildClassAttrs.Length > 0)
                    {
                        foreach (var attr in depBuildClassAttrs)
                        {
                            var depBuildAttr = (DepBuildClassAttribute)attr;
                            var depBuildProcessor =
                                Activator.CreateInstance(processorType) as IDepBuildProcessor;
                            _cachedDepBuildClassAttributes[depBuildProcessor] = depBuildAttr;
                            break;
                        }
                    }
                }
            }
        }

        //foreach (var kv in _cachedDepBuildAttributes)
        //{
        //    var depAttr = kv.Value;
        //    var methodInfo = kv.Key;

        //    foreach (Component component in copyObj.GetComponentsInChildren(depAttr.TheType, true))
        //    {
        //        methodInfo.Invoke(null, new object[] { Convert.ChangeType(component, depAttr.TheType) });
        //    }
        //}

        // 依赖处理
        foreach (var kv in _cachedDepBuildClassAttributes)
        {
            var depAttr = kv.Value;
            var processor = kv.Key;

            foreach (Component component in buildObj.GetComponentsInChildren(depAttr.ClassType, true))
            {
                processor.Process(component);
            }


        }

        // Build主对象
        DoBuildAssetBundle(path, buildObj, buildSelf); // TODO: BuildBundleResult...

        if (instantiateObjectToBuild)
            if (!keepCopyObjToDebug)
                GameObject.DestroyImmediate(buildObj);
    }


    // 
    /// <summary>
    /// 如果非realBuild,僅返回最終路徑
    /// DoBuildAssetBundle和__DoBuildScriptableObject有两个开关，决定是否真的Build
    /// realBuildOrJustPath由外部传入，一般用于进行md5比较后，传入来的，【不收集Build缓存】 TODO：其实可以收集的。。
    /// IsJustCollect用于全局的否决真Build，【收集Build缓存】
    /// </summary>
    /// <param name="path"></param>
    /// <param name="asset"></param>
    /// <param name="realBuildOrJustPath"></param>
    /// <returns></returns>
    public static CDepCollectInfo DoBuildAssetBundle(string path, UnityEngine.Object asset,
        bool realBuildOrJustPath = true)
    {
        path = Path.ChangeExtension(path, AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt));
        //asset.name = fullAssetPath;
        var hasBuilded = false;

        if (!BuildRecord.ContainsKey(path))
        {
            if (realBuildOrJustPath)
                AddCache(path);
            if (IsJustCollect)
                AddCache(path);
            if (!IsJustCollect && realBuildOrJustPath)
            {
                //BuildedCache[fullAssetPath] = true;
                BuildTools.BuildAssetBundle(asset, path);
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
        BuildRecord[res] = true;
        if (AddCacheEvent != null)
            AddCacheEvent(res);
    }

    public static CDepCollectInfo __DoBuildScriptableObject(string fullAssetPath, ScriptableObject so,
        bool realBuildOrJustPath = true)
    {
        var hasBuilded = false;
        fullAssetPath = Path.ChangeExtension(fullAssetPath, AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt));

        if (so == null)
        {
            Log.Error("Error Null ScriptableObject: {0}", fullAssetPath);
        }
        else
        {
            //so.name = fullAssetPath;
            if (!BuildRecord.ContainsKey(fullAssetPath))
            {
                AddCache(fullAssetPath);
                if (!IsJustCollect && realBuildOrJustPath)
                {
                    BuildTools.BuildScriptableObject(so, fullAssetPath);
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


    public static string __GetPrefabBuildPath(string path)
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
            path = strs[0] + "_" + strs[1]; // 连目录名并在一起，防文件名重复
            //else
            //    path = strs[1];
        }
        else
            path = strs[0];

        return path;
    }

    //static HashSet<string> _depTextureScaleList = new HashSet<string>();  // 进行过Scale的图片
    public static string BuildDepTexture(Texture tex, float scale = 1f)
    {
        Debuger.Assert(tex);
        CDepCollectInfo result = KDepCollectInfoCaching.GetCache(tex);
        if (result != null)
        {
            return result.Path;
        }

        string assetPath = AssetDatabase.GetAssetPath(tex);
        bool needBuild = AssetVersionControl.TryCheckNeedBuildWithMeta(assetPath);
        if (needBuild)
            AssetVersionControl.TryMarkBuildVersion(assetPath);

        Texture newTex;
        if (tex is Texture2D)
        {
            var tex2d = (Texture2D)tex;

            if (needBuild &&
                !scale.Equals(1f)) // 需要进行缩放，才进来拷贝
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
                    Log.Error("TexturePacker scale failed... {0}", assetPath);
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
                //GC.CollectMaterial();
                //// 进行缩放
                //TextureScaler.Bilinear(newTex2D, (int) (tex.width*scale), (int) (tex.height*scale));
                //GC.CollectMaterial();

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
                Log.Warning("[BuildDepTexture]非Texture2D: {0}, 无法进行Scale缩放....", tex);
        }
        //if (!IsJustCollect)
        //    CTextureCompressor.CompressTextureAsset(assetPath, newTex as Texture2D);

        string path = __GetPrefabBuildPath(assetPath);
        if (string.IsNullOrEmpty(path))
            Log.Warning("[BuildTexture]不是文件的Texture, 估计是Material的原始Texture?");
        result = DoBuildAssetBundle("Texture/Texture_" + path, newTex, needBuild);

        KDepCollectInfoCaching.SetCache(tex, result);
        GC.Collect(0);
        return result.Path;
    }

    /// <summary>
    /// 对依赖的字体进行打包，返回打包结果路径
    /// </summary>
    /// <param name="font"></param>
    /// <returns></returns>
    public static string BuildFont(Font font)
    {
        string fontAssetPath = AssetDatabase.GetAssetPath(font);

        if (string.IsNullOrEmpty(fontAssetPath) || fontAssetPath == "Library/unity default resources")
        {
            Log.Error("[BuildFont]无法打包字体...{0}", font);
            return null;
        }
        //fontAssetPath = __GetPrefabBuildPath(fontAssetPath).Replace("Atlas_", "");
        //string[] splitArr = fontAssetPath.Split('/');

        bool needBuild = AssetVersionControl.TryCheckNeedBuildWithMeta(fontAssetPath);
        if (needBuild)
            AssetVersionControl.TryMarkBuildVersion(fontAssetPath);

        var result = DoBuildAssetBundle("Font/Font_" + font.name, font, needBuild);

        return result.Path;
    }


    /// <summary>
    /// 图片打包工具，直接打包，不用弄成Texture!, 需要借助TexturePacker
    /// </summary>
    /// <summary>
    /// 单独打包的纹理图片, 这种图片，不在Unity目录内，用bytes读取, 指定保存的目录
    /// </summary>
    /// <param name="saveCacheFolderName"></param>
    /// <param name="tex"></param>
    /// <returns></returns>
    public static string BuildIOTexture(string saveCacheFolderName, string imageSystemFullPath, float scale = 1)
    {
        var cleanPath = imageSystemFullPath.Replace("\\", "/");

        var fileName = Path.GetFileNameWithoutExtension(cleanPath);
        var buildPath = string.Format("{0}/{1}_{0}{2}", saveCacheFolderName, fileName,
            AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt));
        var needBuild = AssetVersionControl.TryCheckNeedBuildWithMeta(cleanPath);

        var texture = new Texture2D(1, 1);

        if (needBuild && !IsJustCollect)
        {
            var cacheDir = "Assets/" + KEngineDef.ResourcesBuildCacheDir + "/BuildIOTexture/" + saveCacheFolderName +
                           "/";
            var cacheFilePath = cacheDir + Path.GetFileName(cleanPath);

            //CFolderSyncTool.TexturePackerScaleImage(cleanPath, cacheFilePath, Math.Min(1, GameDef.PictureScale * scale));  // TODO: do scale

            texture = AssetDatabase.LoadAssetAtPath(cacheFilePath, typeof(Texture2D)) as Texture2D;
            if (texture == null)
            {
                Log.Error("[BuildIOTexture]TexturePacker scale failed... {0}", cleanPath);
                return null;
            }

            //CTextureCompressor.CompressTextureAsset(cleanPath, texture);

            //CTextureCompressor.AutoCompressTexture2D(texture, EditorUserBuildSettings.activeBuildTarget);

            //if (!texture.LoadImage(File.ReadAllBytes(cleanPath)))
            //{
            //    Log.Error("无法LoadImage的Texture: {0}", cleanPath);
            //    return null;
            //}
            //texture.name = fileName;

            //// 多线程快速，图像缩放，双线性过滤插值
            //TextureScaler.Bilinear(texture, (int)(texture.width * GameDef.PictureScale),
            //    (int)(texture.height * GameDef.PictureScale));
            //GC.CollectMaterial();
        }

        // card/xxx_card.box
        var result = KDependencyBuild.DoBuildAssetBundle(buildPath, texture, needBuild);

        if (needBuild)
            AssetVersionControl.TryMarkBuildVersion(cleanPath);

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
            Log.Error("[SyncTextureImportSetting]Null Importer");
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
            AssetDatabase.ImportAsset(newTexPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
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
#endif