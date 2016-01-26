#region  Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Asset Bundle framework for Unity3D
// ===================================
// 
// Filename: ResourceDepBuilder.cs
// Date:     2016/01/21
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
// License along with this library

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KEngine.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KEngine.ResourceDep.Builder
{
    /// <summary>
    /// New instead of KAssetDep
    /// 定义:
    /// Unity Asset Path = AssetDatabase.GetAssetPath(xx)的路径，带有"Assets/"
    /// Build Asset Path = UnityAssetPath去掉"Assets/"
    /// </summary>
    public partial class ResourceDepBuilder
    {
        /// <summary>
        /// 存放Push进去的对象, 这些对象对下一次打包AssetBundle自动进行依赖, 存放BuildAssetPath而不是UnityAssetPath
        /// </summary>
        //private static HashSet<UnityEngine.Object> DependencyPool = new HashSet<Object>();
        private static HashSet<string> DependencyPool = new HashSet<string>();

        /// <summary>
        /// 记录打包过的对象的路径
        /// </summary>
        public static HashSet<string> BuildedPool = new HashSet<string>();

        private static HashSet<string> TempFiles = new HashSet<string>();
        private static HashSet<string> TempDirs = new HashSet<string>();

        //private static Dictionary<IBuilderProcessor, ResourceBuildClassAttribute> _cachedDepBuildClassAttributes;


        /// <summary>
        /// 过滤，可以从一个对象，到另一个对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public delegate UnityEngine.Object BuildFilterDelegate(UnityEngine.Object obj);
        public static BuildFilterDelegate BuildObjectFilter;

        /// <summary>
        /// 收集依赖资源时的过滤器
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="unityAssetPath"></param>
        /// <param name="buildAssetPath"></param>
        /// <returns></returns>
        public delegate bool CollectDepPathFilterDelegate(UnityEngine.Object obj, string unityAssetPath, string buildAssetPath);
        public static CollectDepPathFilterDelegate CollectDepPathFilter;

        /// <summary>
        /// 用于GetBuildAssetPath过滤
        /// </summary>
        /// <param name="unityAssetPath"></param>
        /// <returns></returns>
        public delegate string BuildAssetPathFilterDelegate(string unityAssetPath);

        public static BuildAssetPathFilterDelegate BuildAssetPathFilter;

        /// <summary>
        /// Build in Asset's build asset pth
        /// </summary>
        private static Dictionary<Type, Func<UnityEngine.Object, string>> _buildInAssetType2BuildAssetPath = new Dictionary
            <Type, Func<UnityEngine.Object, string>>()
        {
            {typeof (Shader), (@object) => "Shader/" + @object.name.Replace(" ", "_") + ".shader"},
            {typeof(Texture2D), (o) =>"Texture/" + o.name.Replace(" ", "_") + ".png" },

            {typeof(Material), (o) =>"Material/" + o.name.Replace(" ", "_") + ".mat" },

            {typeof(Mesh), (o) =>"Mesh/" + o.name.Replace(" ", "_") + ".fbx" },
        };

        /// <summary>
        /// 获取资源打包相对路径，该路径跟Unity目录布置完全一致，但经过特殊字符处理
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        public static string GetBuildAssetPath(UnityEngine.Object @object)
        {
            var unityAssetPath = AssetDatabase.GetAssetPath(@object);

            var uAssetType = GetUnityAssetType(unityAssetPath);
            if (uAssetType == UnityAssetType.Builtin || uAssetType == UnityAssetType.Memory)
            {
                // 如果是Inner 类型材质, 自定义路径
                var depObjType = @object.GetType();
                Func<UnityEngine.Object, string> getNameFunc;
                if (_buildInAssetType2BuildAssetPath.TryGetValue(depObjType, out getNameFunc))
                {
                    return getNameFunc(@object);
                }
                else
                {
                    Logger.LogError("Un handle Libray builtin resource, Type:{0}, Name: {1}", depObjType, @object.name);
                }
            }

            return GetBuildAssetPath(unityAssetPath);
        }

        /// <summary>
        /// Build Asset Path = Unity Asset Path去掉"Assets/"
        /// 文件名需要特殊处理， 文件名等于前面的目录拼接起来，确保文件名唯一
        /// 可以进行过滤， 从一个路径，导出到任意自定义路径的AssetBundle
        /// </summary>
        /// <param name="unityAssetPath"></param>
        /// <returns></returns>
        public static string GetBuildAssetPath(string unityAssetPath)
        {
            var assetPrefix = "Assets/";
            var cleanAssetPath = unityAssetPath.Replace("\\", "/");

            // 去掉空格
            cleanAssetPath = cleanAssetPath.Replace(" ", "_");

            // 过滤器
            if (BuildAssetPathFilter != null)
                cleanAssetPath = BuildAssetPathFilter(cleanAssetPath);

            // 无前缀直接返回
            if (!cleanAssetPath.StartsWith(assetPrefix))
                return cleanAssetPath;

            // 去掉前缀
            var relativeAssetPath = cleanAssetPath.Substring(assetPrefix.Length,
                cleanAssetPath.Length - assetPrefix.Length);
            return ResourceDepUtils.GetBuildPath(relativeAssetPath);
        }

        public static IList<string> GetBuildAssetPaths(IList<CollectedDepAssetInfo> depAssetInfos)
        {
            var list = new List<string>();
            foreach (var info in depAssetInfos)
            {
                list.Add(info.BuildAssetPath);
            }
            return list;
        }

        /// <summary>
        /// 将Asset Path统一转换成BuildAssetPath (不带Assets/)
        /// </summary>
        /// <param name="assetPaths"></param>
        /// <returns></returns>
        public static IList<string> GetBuildAssetPaths(IList<string> assetPaths)
        {
            var list = new List<string>();
            foreach (var unityAssetPath in assetPaths)
            {
                list.Add(GetBuildAssetPath(unityAssetPath));
            }
            return list;
        }

        public static bool HasPushDep(UnityEngine.Object obj)
        {
            return DependencyPool.Contains(GetBuildAssetPath(obj));
        }

        public static void AddPushDep(CollectedDepAssetInfo info, bool forceBuild)
        {
            // Library类型Asset，没有路径，所有使用自定义的BuildAssetPath
            string assetPath;
            bool needBuild = true;
            if (info.UnityAssetType == UnityAssetType.Object)
            {
                assetPath = info.UnityAssetPath;
                needBuild = forceBuild || CheckNeedBuildAsset(info.UnityAssetType, assetPath);
                // 下面告诉要强制build，或在文件改变时才真的进行Build
            }
            else
            {
                assetPath = info.BuildAssetPath;
                needBuild = forceBuild || CheckNeedBuildAsset(info.UnityAssetType, assetPath);
                // 其实基本Library资源是肯定要打包的，这一句其实可以忽略
            }
            var depObjectsMap = CollectAndPushBuildDependencies(info.Asset, needBuild);

            if (!needBuild)
                return;

            BuildPipeline.PushAssetDependencies();

            var buildAssetPath = info.BuildAssetPath;
            BuildAssetBundle(info.Asset, buildAssetPath, GetBuildAssetPaths(depObjectsMap));
            DependencyPool.Add(buildAssetPath);
        }

        public static void AddPushDep(UnityEngine.Object obj, bool forceBuild)
        {
            var assetPath = AssetDatabase.GetAssetPath(obj);
            var needBuild = forceBuild || CheckNeedBuildAsset(assetPath); // 下面告诉要强制build，或在文件改变时才真的进行Build
            var depObjectsMap = CollectAndPushBuildDependencies(obj, needBuild);

            if (!needBuild)
                return;

            BuildPipeline.PushAssetDependencies();

            var buildAssetPath = GetBuildAssetPath(obj);
            BuildAssetBundle(obj, buildAssetPath, GetBuildAssetPaths(depObjectsMap));
            DependencyPool.Add(buildAssetPath);
        }

        /// <summary>
        /// Build AssetBundle的结果
        /// </summary>
        public struct BuildBundleResult
        {
            public uint Crc;
            public bool IsSuccess;
            public string FullPath;
            public string RelativePath;
            public string ManifestFullPath;
        }

        private static BuildBundleResult BuildAssetBundle(UnityEngine.Object obj, string path, IList<string> depFiles)
        {
            return BuildAssetBundle(obj, path, depFiles, EditorUserBuildSettings.activeBuildTarget, KResourceQuality.Sd);
        }

        /// <summary>
        /// 如果是内置类型/ Builtin的，只要不存在就重打
        /// </summary>
        /// <param name="unityAssetType"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool CheckNeedBuildAsset(UnityAssetType unityAssetType, string path)
        {
            if (unityAssetType == UnityAssetType.Object)
            {
                return CheckNeedBuildAsset(path);
            }

            var needBuild = !KAssetVersionControl.TryCheckExistRecord(path);
            if (!needBuild && !UnityEditorInternal.InternalEditorUtility.inBatchMode)
            {
                Debug.LogWarning("Builtin resource handled, no Need To Build " + path);
            }
            return needBuild;
        }

        /// <summary>
        /// 从版本控制系统中判断是否需要Build这个Asset
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        private static bool CheckNeedBuildAsset(string assetPath)
        {
            // 判断是否需要打包，根据要在依赖判断之后哦
            if (!KAssetVersionControl.TryCheckNeedBuildWithMeta(assetPath))
            {
                if (!UnityEditorInternal.InternalEditorUtility.inBatchMode)
                {
                    Debug.LogWarning("Same file, no Need To Build " + assetPath);
                }
                return false;
            }

            // 检查是否需要打包的后缀类型
            var extType = GetAssetExtType(assetPath);
            if (Define.IgnoreDepType.Contains(extType))
            {
                if (!UnityEditorInternal.InternalEditorUtility.inBatchMode)
                {
                    Logger.LogWarning("Asset {0}, Type: {1}, no need build", assetPath, extType);
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// 绝对路径转相对路径, 来避免路径过长无法写入的问题
        /// </summary>
        /// <param name="absPath"></param>
        /// <returns></returns>
        private static string AbsPath2RelativePath(string absPath)
        {
            var cleanAbsPath = absPath.Replace("\\", "/");
            var workdirPath = Environment.CurrentDirectory.Replace("\\", "/"); // 当前工作目录
            if (cleanAbsPath.StartsWith(workdirPath))
            {
                return cleanAbsPath.Substring(workdirPath.Length + 1, cleanAbsPath.Length - workdirPath.Length - 1); // +1是因为多了个/
            }
            return cleanAbsPath;
        }

        /// <summary>
        /// ResourceDep系统专用的打包AssetBundle函数
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="path"></param>
        /// <param name="depFileRelativeBuildPath">依赖文件列表,相对的AssetBundle打包路径</param>
        /// <param name="buildTarget"></param>
        /// <param name="quality"></param>
        /// <returns></returns>
        private static BuildBundleResult BuildAssetBundle(Object asset, string path,
            IList<string> depFileRelativeBuildPath, BuildTarget buildTarget, KResourceQuality quality)
        {
            //是否是Level / Scene
            var isScene = asset.ToString().Contains("SceneAsset");

            uint crc = 0;
            var time = DateTime.Now;
            // 最终完整路径
            var buildToFullPath = KBuildTools.MakeSureExportPath(path, buildTarget, quality) +
                           AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt);
            var buildToRelativePath = AbsPath2RelativePath(buildToFullPath);//buildToFullPath.Replace(workdirPath, "").Substring(1); // 转换成相对路径，避免路径过程无法打包的问题

            var assetPath = AssetDatabase.GetAssetPath(asset);

            // 版本标记
            var unityAssetType = GetUnityAssetType(assetPath);
            if (unityAssetType == UnityAssetType.Builtin || unityAssetType == UnityAssetType.Memory)
            {
                var buildAssetPath = GetBuildAssetPath(asset);
                KAssetVersionControl.TryMarkRecord(buildAssetPath);
                BuildedPool.Add(buildAssetPath);
            }
            else
            {
                KAssetVersionControl.TryMarkBuildVersion(assetPath);
                BuildedPool.Add(assetPath);
            }

            bool result = false;
            if (isScene)
            {
                var resultStr = BuildPipeline.BuildStreamedSceneAssetBundle(new string[] { assetPath }, buildToRelativePath,
                    buildTarget, out crc);
                result = string.IsNullOrEmpty(resultStr);
                if (!string.IsNullOrEmpty(resultStr))
                {
                    Debug.LogError(resultStr);
                }
            }
            else
            {
                result = BuildPipeline.BuildAssetBundle(asset, null, buildToRelativePath,
                    out crc,
                    BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.DeterministicAssetBundle |
                    BuildAssetBundleOptions.CompleteAssets,
                    buildTarget);
            }

            // 创建依赖记录文件
            string fullManifestPath = null;
            if (depFileRelativeBuildPath != null && depFileRelativeBuildPath.Any())
            {
                //var manifestFileContent = string.Join("\n", depFileRelativeBuildPath.KToArray());
                var manifestPath = path + ".manifest";
                fullManifestPath = KBuildTools.MakeSureExportPath(manifestPath, buildTarget, quality) +
                                   AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt);
                var relativeManifestPath = AbsPath2RelativePath(fullManifestPath); // 转成相对路径
                var utf8NoBom = new UTF8Encoding(false);
                //try
                //{
                File.WriteAllLines(relativeManifestPath, depFileRelativeBuildPath.KToArray(), utf8NoBom);
                //}
                //catch (Exception e)
                //{
                //    // 会出现DirectoryNotFound，但是目录明明就存在！ 先Catch
                //    Logger.LogError("Exception: {0}", e.Message);
                //    var dirPath = Path.GetDirectoryName(fullManifestPath);
                //    if (Directory.Exists(dirPath))
                //        Logger.LogError("Directory Exists: {0}", dirPath);
                //    else
                //    {
                //        Logger.LogError("Directory not exists: {0}", dirPath);
                //    }
                //}
            }

            if (result)
            {
                var abInfo = new FileInfo(buildToFullPath);
                Logger.Log("Build AssetBundle： {0} / CRC: {1} / Time: {2:F4}s / Size: {3:F3}KB / FullPath: {4} / RelPath: {5}", path, crc, (DateTime.Now - time).TotalSeconds,
                    abInfo.Length / 1024d, buildToFullPath, buildToRelativePath);
            }
            else
            {
                Logger.LogError("生成文件失败： {0}, crc: {1} 耗时: {2:F5}, 完整路径: {3}", path, crc,
                    (DateTime.Now - time).TotalSeconds, buildToFullPath);
            }
            return new BuildBundleResult
            {
                Crc = crc,
                FullPath = buildToFullPath,
                RelativePath = path,
                IsSuccess = result,
                ManifestFullPath = fullManifestPath,
            };
        }

        private static UnityAssetType GetUnityAssetType(string assetPath)
        {
            if (assetPath.StartsWith("Library/unity default resources") ||
                assetPath == "Resources/unity_builtin_extra")
            {
                return UnityAssetType.Builtin;
            }
            if (string.IsNullOrEmpty(assetPath))
            {
                return UnityAssetType.Memory;
            }

            return UnityAssetType.Object;
        }

        /// <summary>
        /// 智能收集依赖，剔除非用于AssetBundle打包的部分，返回路径list(路径去掉了'Assets/')
        /// </summary>
        /// <param name="buildObj"></param>
        /// <returns></returns>
        private static List<CollectedDepAssetInfo> CollectDependenciesPaths(UnityEngine.Object buildObj)
        {
            var assetPath = AssetDatabase.GetAssetPath(buildObj);
            var depObjects = EditorUtility.CollectDependencies(new[] { buildObj });
            // 使用Dict，去掉重复
            var depObjectsMap = new Dictionary<string, CollectedDepAssetInfo>();
            foreach (var depObj in depObjects)
            {
                if (depObj == null)
                {
                    Logger.LogError("Found NULL obj when collect dep from '{0}'", buildObj);
                    continue;
                }
                // 过滤
                var depExtType = GetAssetExtType(depObj);

                // 某些类型进行忽略
                if (Define.IgnoreDepType.Contains(depExtType))
                    continue;

                var buildAssetPath = GetBuildAssetPath(depObj);

                // 很多跟自己路径一样的
                var depAssetPath = AssetDatabase.GetAssetPath(depObj);
                if (depAssetPath == assetPath)
                    continue;

                // 可自定义过滤
                if (CollectDepPathFilter != null)
                    if (!CollectDepPathFilter(depObj, depAssetPath, buildAssetPath))
                        continue;

                var unityAssetType = GetUnityAssetType(depAssetPath);

                depObjectsMap[buildAssetPath] = new CollectedDepAssetInfo()
                {
                    Asset = depObj,
                    ExtType = depExtType,
                    UnityAssetPath = depAssetPath,
                    BuildAssetPath = buildAssetPath,
                    UnityAssetType = unityAssetType,
                };
            }
            var depObjectsList = depObjectsMap.Values.KToList();
            var comparer = new DepListComaparer();
            depObjectsList.Sort(comparer);
            return depObjectsList;
        }

        /// <summary>
        /// 自动收集依赖，并且打包处理依赖
        /// </summary>
        /// <param name="unityObject"></param>
        /// <param name="needBuild"></param>
        /// <returns></returns>
        private static List<CollectedDepAssetInfo> CollectAndPushBuildDependencies(Object unityObject, bool needBuild)
        {
            var depObjectsMap = CollectDependenciesPaths(unityObject);
            foreach (var depPath in depObjectsMap)
            {
                if (depPath.Asset == null)
                {
                    Logger.LogError("Null Object on Path: {0}", depPath.BuildAssetPath);
                    continue;
                }
                if (!HasPushDep(depPath.Asset))
                {
                    AddPushDep(depPath, needBuild);
                }
            }
            return depObjectsMap;
        }

        /// <summary>
        /// 非Scene，打包成assetBundle
        /// </summary>
        /// <param name="unityObject"></param>
        private static List<CollectedDepAssetInfo> BuildObject(UnityEngine.Object unityObject)
        {
            if (BuildObjectFilter != null)
                unityObject = BuildObjectFilter(unityObject);

            var assetPath = AssetDatabase.GetAssetPath(unityObject);

            if (string.IsNullOrEmpty(assetPath))
            {
                Logger.LogError("Error on Obj: {0}", unityObject.name);
                return null;
            }

            var needBuild = CheckNeedBuildAsset(assetPath);
            // 检查本对象是否需要build，当true时，传入CollectAndPushBuild函数则所有依赖的都要重新打包一次了
            var depObjectsMap = CollectAndPushBuildDependencies(unityObject, needBuild);

            if (needBuild && !HasPushDep(unityObject)) // 该对象可能被依赖过，依赖过，就不打了 
            {
                var buildPath = GetBuildAssetPath(unityObject);

                BuildPipeline.PushAssetDependencies();
                BuildAssetBundle(unityObject, buildPath, GetBuildAssetPaths(depObjectsMap));
                BuildPipeline.PopAssetDependencies();

                Debug.Log(string.Join("\n", GetBuildAssetPaths(depObjectsMap).ToArray()));
            }

            return depObjectsMap;
        }

        /// <summary>
        /// 打包整个目录
        /// </summary>
        /// <param name="dirPath">目录路径</param>
        /// <param name="limitMainObjCount">限制构建的主对象（不计算依赖对象）</param>
        public static void BuildDirectory(string dirPath, int limitMainObjCount = -1)
        {
            UnityEditor.EditorUtility.DisplayProgressBar("Build Directory", "Getting files list...", .2f);

            var fileList = new List<string>();
            foreach (var file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file);
                if (ext == ".meta") continue;
                var cleanPath = file.Replace("\\", "/");
                fileList.Add(cleanPath);
            }

            // 进行根据扩展名进行依赖排序
            fileList.Sort((a, b) =>
            {
                var aExt = GetAssetExtType(a);
                var bExt = GetAssetExtType(b);
                return aExt.CompareTo(bExt);
            });
            EditorUtility.ClearProgressBar();
            var buildMainObjList = new List<UnityEngine.Object>();
            foreach (var file in fileList)
            {
                if (limitMainObjCount != -1 && buildMainObjList.Count > limitMainObjCount) break;

                var cleanPath = file;
                var obj = AssetDatabase.LoadAssetAtPath(cleanPath, typeof(UnityEngine.Object));
                if (obj == null)
                {
                    throw new Exception("Not found asset: " + cleanPath);
                }
                BuildObject(obj);
                buildMainObjList.Add(obj);
            }
        }
        /// <summary>
        /// 打包一个UnityEngine.Object，会自动先设置成Prefab
        /// </summary>
        /// <param name="unityObject"></param>
        /// <returns></returns>
        public static List<CollectedDepAssetInfo> Build(UnityEngine.Object unityObject)
        {
            var type = unityObject.ToString();

            if (type.Contains("UnityEngine.DefaultAsset"))
            {
                var dirPath = AssetDatabase.GetAssetPath(unityObject);
                BuildDirectory(dirPath);
                return null;
            }
            else
            {
                return BuildObject(unityObject);
            }
        }

        /// <summary>
        /// 获取资源后缀类型，这个类型还可用于排序、辨识
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static AssetExtType GetAssetExtType(UnityEngine.Object obj)
        {
            var unityAssetPath = AssetDatabase.GetAssetPath(obj);
            var uAssetType = GetUnityAssetType(unityAssetPath);
            if (uAssetType == UnityAssetType.Builtin || uAssetType == UnityAssetType.Memory)
            {
                // 如果是Inner 类型材质, 自定义路径
                var depObjType = obj.GetType();
                if (depObjType == typeof(Shader))
                {
                    return AssetExtType.Shader;
                }
                else if (depObjType == typeof(Texture2D))
                    return AssetExtType.Png;
                else if (depObjType == typeof(Material))
                    return AssetExtType.Mat;
                else if (depObjType == typeof(Mesh))
                    return AssetExtType.Fbx;
                else
                {
                    Logger.LogError("Un handle Libray builtin resource, Type:{0}, Name: {1}", depObjType, obj.name);
                }
            }

            return GetAssetExtType(unityAssetPath);
        }

        /// <summary>
        /// 获取一个资源的后缀名，排序类型
        /// </summary>
        /// <returns></returns>
        public static AssetExtType GetAssetExtType(string assetPath)
        {
            var ext = Path.GetExtension(assetPath);
            AssetExtType xEnum;
            try
            {
                // 首字母大写
                xEnum = (AssetExtType)Enum.Parse(typeof(AssetExtType),
                    System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(ext.Substring(1).ToLower()));
            }
            catch
            {
                xEnum = AssetExtType.Default;
            }
            return xEnum;
        }

        /// <summary>
        /// 排序器
        /// </summary>
        public class DepListComaparer : IComparer<CollectedDepAssetInfo>
        {
            ////private List<UnityEngine.Object> _theObjectList; 
            //public DepListComaparer(List<UnityEngine.Object> list)
            //{
            //    _theObjectList = list;
            //}

            public int Compare(CollectedDepAssetInfo xObj, CollectedDepAssetInfo yObj)
            {
                return xObj.ExtType.CompareTo(yObj.ExtType);
            }

            /// <summary>
            /// 获取排序后的对象的路径列表
            /// </summary>
            /// <returns></returns>
            //public List<string> GetSortedList()
            //{
            //    var list = new List<string>();
            //    foreach (var item in _theObjectList)
            //    {
            //        list.Add(ResourceDepBuilder.GetBuildAssetPath(item));
            //        //list.Add(AssetDatabase.GetAssetPath(item));
            //    }
            //    return list;
            //}
        }

        public static void Clear()
        {
            foreach (var depObj in DependencyPool)
            {
                BuildPipeline.PopAssetDependencies();
            }
            Logger.Log("Clear pushed ResourceDep pool count: {0}", DependencyPool.Count);
            DependencyPool.Clear();


            // 1秒后再做清理
            //var t = new Thread(() =>
            //{
            //    Thread.Sleep(100);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 清理临时文件
            foreach (var tmpFile in TempFiles)
            {
                if (File.Exists(tmpFile))
                    File.Delete(tmpFile);
            }
            Logger.Log("Clear Temp Files Count: {0}, Files: {1}", TempFiles.Count,
                string.Join("\n", TempFiles.ToArray()));
            TempFiles.Clear();

            // 如果新创建出来的临时文件夹，删除吧
            foreach (var tmpDir in TempDirs)
            {
                if (Directory.Exists(tmpDir))
                    Directory.Delete(tmpDir, true);
            }
            Logger.Log("Clear Temp Dirs Count: {0}, Files: {1}", TempDirs.Count, string.Join("\n", TempDirs.ToArray()));
            TempDirs.Clear();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Logger.Log("Builded AssetBundle Count: {0}, They are: \n {1}", BuildedPool.Count, string.Join("\n", BuildedPool.KToArray()));
        }

        [MenuItem("Assets/Build Asset Bundles Without KEngine.ResourceDep", false, 1010)]
        public static void MenuBuildObjectNoDeps()
        {
            var objs = Selection.objects;
            if (objs == null)
            {
                Debug.LogError("No selection object");
                return;
            }
            foreach (var obj in objs)
            {
                var buildPath = GetBuildAssetPath(obj);
                BuildAssetBundle(obj, buildPath, null);
            }

            Clear();
        }

        [MenuItem("Assets/Build Asset Bundles with KEngine.ResourceDep", false, 1000)]
        public static void MenuBuildUnityObject()
        {
            var objs = Selection.objects;
            if (objs == null)
            {
                Debug.LogError("No selection object");
                return;
            }
            foreach (var obj in objs)
                Build(obj);

            Clear();
        }

        [MenuItem("Assets/Build Asset Bundles with KEngine.ResourceDep (Rebuild Version)", false, 1001)]
        public static void MenuBuildUnityObjectRebuild()
        {
            using (new KAssetVersionControl(true))
            {
                MenuBuildUnityObject();
            }
        }

        [MenuItem("Assets/Build Asset Bundles with KEngine.ResourceDep (Diff Version)", false, 1002)]
        public static void MenuBuildUnityObjectDiff()
        {
            using (new KAssetVersionControl(false))
            {
                MenuBuildUnityObject();
            }
        }
    }
}