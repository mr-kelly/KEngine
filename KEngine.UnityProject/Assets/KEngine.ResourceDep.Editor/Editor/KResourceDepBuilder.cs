#region Copyright (c) Kingsoft Xishanju

// KEngine - Asset Bundle framework for Unity3D
// ===================================
// 
// Filename: KResourceDepBuilder.cs
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KEngine.ResourceDep
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ResourceBuildClassAttribute : Attribute
    {
        public Type ClassType;

        public ResourceBuildClassAttribute(Type type)
        {
            ClassType = type;
        }
    }

    public class ResourceDepInfo
    {
        public string Path;
        public HashSet<string> DepAssetPaths = new HashSet<string>();
    }

    public interface IResourceBuildProcessor
    {
        List<string> Process(Component @object);
    }

    /// <summary>
    /// New instead of KAssetDep
    /// </summary>
    public class KResourceDepBuilder
    {
        /// <summary>
        /// 存放Push进去的对象
        /// </summary>
        private static HashSet<UnityEngine.Object> DependencyPool = new HashSet<Object>();

        private static HashSet<string> TempFiles = new HashSet<string>();
        private static HashSet<string> TempDirs = new HashSet<string>();

        private static Dictionary<IResourceBuildProcessor, ResourceBuildClassAttribute> _cachedDepBuildClassAttributes;


        /// <summary>
        /// 获取资源相对路径，该路径跟Unity目录布置完全一致
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        public static string GetRelativeAssetPath(UnityEngine.Object @object)
        {
            var assetPrefix = "Assets/";
            var assetPath = AssetDatabase.GetAssetPath(@object);
            var cleanAssetPath = assetPath.Replace("\\", "/");
            var relativeAssetPath = cleanAssetPath.Replace(assetPrefix, "");

            return relativeAssetPath;
        }

        public static bool HasPushDep(UnityEngine.Object obj)
        {
            return DependencyPool.Contains(obj);
        }

        public static void AddPushDep(UnityEngine.Object obj, IList<string> depFiles)
        {
            BuildPipeline.PushAssetDependencies();

            var relativeAssetPath = GetRelativeAssetPath(obj);
            BuildAssetBundle(obj, relativeAssetPath, depFiles);
            DependencyPool.Add(obj);
        }

        public static BuildBundleResult BuildAssetBundle(UnityEngine.Object obj, string path, IEnumerable<string> depFiles)
        {
            return BuildAssetBundle(obj, path, depFiles, EditorUserBuildSettings.activeBuildTarget, KResourceQuality.Sd);
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

        /// <summary>
        /// ResourceDep系统专用的打包AssetBundle函数
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="path"></param>
        /// <param name="depFiles">依赖文件列表,相对的AssetBundle打包路径</param>
        /// <param name="buildTarget"></param>
        /// <param name="quality"></param>
        /// <returns></returns>
        public static BuildBundleResult BuildAssetBundle(Object asset, string path, IEnumerable<string> depFiles, BuildTarget buildTarget, KResourceQuality quality)
        {
            uint crc;
            var time = DateTime.Now;
            var fullPath = KBuildTools.MakeSureExportPath(path, buildTarget, quality) + AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt);
            var result = BuildPipeline.BuildAssetBundle(asset, null, fullPath,
                out crc,
                BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.DeterministicAssetBundle |
                BuildAssetBundleOptions.CompleteAssets,
                EditorUserBuildSettings.activeBuildTarget);

            // 创建依赖记录文件
            var manifestFileContent = depFiles == null ? "" : string.Join("\n", depFiles.KToArray());
            var manifestPath = path + ".manifest";
            var fullManifestPath = KBuildTools.MakeSureExportPath(manifestPath, buildTarget, quality) + AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt);
            var utf8NoBom = new UTF8Encoding(false);
            File.WriteAllText(fullManifestPath, manifestFileContent, utf8NoBom);

            if (result)
                Logger.Log("生成文件： {0}, crc: {1} 耗时: {2:F5}, 完整路径: {3}", path, crc, (DateTime.Now - time).TotalSeconds, fullPath);
            else
            {
                Logger.LogError("生成文件失败： {0}, crc: {1} 耗时: {2:F5}, 完整路径: {3}", path, crc, (DateTime.Now - time).TotalSeconds, fullPath);
            }
            return new BuildBundleResult
            {
                Crc = crc,
                FullPath = fullPath,
                RelativePath = path,
                IsSuccess = result,
                ManifestFullPath = fullManifestPath,
            };
        }

        /// <summary>
        /// 打包一个GameObject，会自动先设置成Prefab
        /// </summary>
        /// <param name="buildObj"></param>
        /// <returns></returns>
        public static ResourceDepInfo BuildGameObject(GameObject buildObj)
        {
            var assetPath = AssetDatabase.GetAssetPath(buildObj);

            // 是否临时创建Prefab的标识变量，最后会对临时生成的文件或文件夹进行清理
            string tmpDirPath = null;
            string tmpPrefabPath = null;
            if (string.IsNullOrEmpty(assetPath))
            {
                var scenePath = EditorApplication.currentScene;
                tmpDirPath = Path.Combine(Path.GetDirectoryName(scenePath), Path.GetFileNameWithoutExtension(EditorApplication.currentScene));
                if (!Directory.Exists(tmpDirPath))
                {
                    Directory.CreateDirectory(tmpDirPath);
                    TempDirs.Add(tmpDirPath);
                }
                tmpPrefabPath = tmpDirPath + "/" + buildObj.name + ".prefab";

                TempFiles.Add(tmpPrefabPath);

                // 非Prefab创建Prefab
                Logger.LogWarning("遇到场景GameObject，创建Prefab: {0}", tmpPrefabPath);
                buildObj = PrefabUtility.CreatePrefab(tmpPrefabPath, buildObj);// 成prefab了
            }

            var depInfo = new ResourceDepInfo();

            if (_cachedDepBuildClassAttributes == null)
            {
                _cachedDepBuildClassAttributes = new Dictionary<IResourceBuildProcessor, ResourceBuildClassAttribute>();
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var processorType in asm.GetTypes())
                    {
                        var depBuildClassAttrs = processorType.GetCustomAttributes(typeof(ResourceBuildClassAttribute),
                            false);
                        if (depBuildClassAttrs.Length > 0)
                        {
                            foreach (var attr in depBuildClassAttrs)
                            {
                                var depBuildAttr = (ResourceBuildClassAttribute)attr;
                                var depBuildProcessor =
                                    Activator.CreateInstance(processorType) as IResourceBuildProcessor;
                                _cachedDepBuildClassAttributes[depBuildProcessor] = depBuildAttr;
                                break;
                            }
                        }
                    }
                }
            }

            // 依赖处理
            foreach (var kv in _cachedDepBuildClassAttributes)
            {
                var depAttr = kv.Value;
                var processor = kv.Key;

                foreach (Component component in buildObj.GetComponentsInChildren(depAttr.ClassType, true))
                {
                    depInfo.DepAssetPaths.AddRange(processor.Process(component));
                }
            }

            BuildPipeline.PushAssetDependencies();
            var buildPath = GetRelativeAssetPath(buildObj);
            BuildAssetBundle(buildObj, buildPath, depInfo.DepAssetPaths);
            BuildPipeline.PopAssetDependencies();

            Debug.Log(buildObj.name);

            return depInfo;
        }

        public static void Clear()
        {
            foreach (var depObj in DependencyPool)
            {
                BuildPipeline.PopAssetDependencies();
            }
            Logger.Log("Clear ResourceDep pool count: {0}", DependencyPool.Count);
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
            Logger.Log("Clear Temp Files Count: {0}, Files: {1}", TempFiles.Count, string.Join("\n", TempFiles.ToArray()));
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
            //});
            //t.Start();

        }
    }
}