#region  Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>
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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Object = UnityEngine.Object;

namespace KEngine.ResourceDep
{
    /// <summary>
    /// 资源请求
    /// </summary>
    public class ResourceDepRequest
    {
        public Object Asset { get; internal set; }
        public string Path { get; internal set; }
        public System.Type Type { get; internal set; }

        public bool IsDone { get; internal set; }

        public List<KAbstractResourceLoader> Loaders = null;
    }

    /// <summary>
    /// ResourceDep资源依赖系统主入口
    /// </summary>
    public class ResourceDepUtils
    {
        //public static string ShadersPrefabName = "ResourceDepShaders.prefab";

        /// <summary>
        /// 将返回具体的资源路径，会把其余目录名的首字母合并在一起
        /// </summary>
        /// <param name="relativeAssetPath"></param>
        /// <returns></returns>
        public static string GetBuildPath(string relativeAssetPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(relativeAssetPath);
            var fileExt = Path.GetExtension(relativeAssetPath);
            var dirPath = Path.GetDirectoryName(relativeAssetPath);
            //var dirArr = dirPath.Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

            // 寻找所有目录的首字母
            //var dirFirstCharArr = new string[dirArr.Length];
            //for (var i = 0; i < dirArr.Length; i++)
            //{
            //    dirFirstCharArr[i] = dirArr[i][0].ToString().ToLower();
            //}
            string newBuildAssetPath;
            if (!string.IsNullOrEmpty(dirPath))
                newBuildAssetPath = String.Format("{0}/{1}{2}", dirPath, fileName, fileExt);
            else
            {
                // 处理根目录的情况
                newBuildAssetPath = string.Format("_{0}{1}", fileName, fileExt);
            }

            // 去掉路径，所有文件根目录
            newBuildAssetPath = newBuildAssetPath.Replace("/", "_").Replace("(", "_").Replace(")", "_"); // 去掉一些特殊字符
            return newBuildAssetPath;
        }

        /// <summary>
        /// 自动将字节码转换字符串，在分割CRLF字符，获取依赖文本列表
        /// </summary>
        /// <param name="manifestBytes"></param>
        /// <returns></returns>
        private static string[] GetManifestList(byte[] manifestBytes)
        {
            var utf8NoBom = new UTF8Encoding(false);
            var manifestList = utf8NoBom.GetString(manifestBytes)
                .Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return manifestList;
        }

        /// <summary>
        /// 是否完成shaders加载？
        /// </summary>
        private static bool IsShadersPrefabLoaded = false;

        /// <summary>
        /// 检查如果Shader对象还没有加载，旧加载
        /// </summary>
        //private static void CheckLoadShadersPrefab()
        //{
        //    if (!IsShadersPrefabLoaded)
        //    {
        //        var buildPath = GetBuildPath(ShadersPrefabName  + AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt));
        //        //KAssetBundleLoader.Load(buildPath);
        //        KAssetFileLoader.Load(buildPath);
        //        IsShadersPrefabLoaded = true;
        //    }
        //}

        /// <summary>
        /// 编辑器模式下，对全部GameObject刷新一下Material
        /// </summary>
        public static void RefreshAllMaterialsShaders()
        {
            foreach (var renderer in GameObject.FindObjectsOfType<Renderer>())
            {
                if (renderer.sharedMaterials != null)
                {
                    foreach (var mat in renderer.sharedMaterials)
                    {
                        if (mat != null && mat.shader != null)
                        {
                            mat.shader = Shader.Find(mat.shader.name);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 同步加载AssetBundle
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public static Object LoadAssetBundleSync(string relativePath)
        {
            //CheckLoadShadersPrefab();
            // manifest
            string manifestPath = ResourceDepUtils.GetBuildPath(String.Format("{0}.manifest{1}", relativePath,
                AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt)));
            var manifestLoader = KBytesLoader.Load(manifestPath, KResourceInAppPathType.ResourcesAssetsPath,
                KAssetBundleLoaderMode.ResourcesLoad);
            //while (!manifestLoader.IsCompleted)
            //    yield return null;
            var manifestBytes = manifestLoader.Bytes;
            manifestLoader.Release(); // 释放掉文本字节
            if (manifestBytes != null)
            {
                var manifestList = GetManifestList(manifestBytes);
                for (var i = 0; i < manifestList.Length; i++)
                {
                    var depPath = manifestList[i] + AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt);
                    var depLoader = KAssetFileLoader.Load(depPath);
                    //while (!depLoader.IsCompleted)
                    //{
                    //    yield return null;
                    //}

                    /*if (Application.isEditor)
                    {
                        Logger.Log("Load dep sync:{0}, from: {1}", depPath, relativePath);
                    }*/
                }
            }
            else
            {
                Logger.LogWarning("Cannot find Manifest: {0}", relativePath);
            }

            string path =
                GetBuildPath(String.Format("{0}{1}", relativePath,
                    AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt)));

            //while (!assetLoader.IsCompleted)
            //    yield return null;
            // 获取后缀名
            var ext = Path.GetExtension(relativePath);
            if (ext == ".unity" || ext == ".shader")
            {
                // Scene 
                var sceneLoader = KAssetBundleLoader.Load(path);
                //while (!sceneLoader.IsCompleted)
                //    yield return null;
                return null;
            }
            else
            {
                var assetLoader = KAssetFileLoader.Load(path);
                //while (!assetLoader.IsCompleted)
                //    yield return null;
                return assetLoader.Asset;
            }
        }

        /// <summary>
        /// 异步加载Asset Bundle，自动处理依赖
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public static ResourceDepRequest LoadAssetBundleAsync(string relativePath)
        {
            //CheckLoadShadersPrefab();
            var request = new ResourceDepRequest { Path = relativePath };
            AppEngine.EngineInstance.StartCoroutine(CoLoadAssetBundleAsync(relativePath, request));
            return request;
        }

        private static IEnumerator CoLoadAssetBundleAsync(string relativePath, ResourceDepRequest request)
        {
            // manifest
            string manifestPath = ResourceDepUtils.GetBuildPath(String.Format("{0}.manifest{1}", relativePath,
                AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt)));
            var manifestLoader = KBytesLoader.Load(manifestPath, KResourceInAppPathType.ResourcesAssetsPath,
                KAssetBundleLoaderMode.ResourcesLoad);
            while (!manifestLoader.IsCompleted)
                yield return null;

            // manifest读取失败，可能根本没有manifest，是允许的
            if (manifestLoader.IsSuccess)
            {
                var manifestBytes = manifestLoader.Bytes;
                manifestLoader.Release(); // 释放掉文本字节
                string[] manifestList = GetManifestList(manifestBytes);
                for (var i = 0; i < manifestList.Length; i++)
                {
                    var depPath = manifestList[i] + AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt);
                    var depLoader = KAssetFileLoader.Load(depPath);
                    if (request.Loaders == null)
                        request.Loaders = new List<KAbstractResourceLoader>();
                    request.Loaders.Add(depLoader);
                    while (!depLoader.IsCompleted)
                    {
                        yield return null;
                    }
                }
            }
            string path =
                GetBuildPath(String.Format("{0}{1}", relativePath,
                    AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt)));

            // 获取后缀名
            var ext = Path.GetExtension(relativePath);
            if (ext == ".unity" || ext == ".shader")
            {
                // Scene 
                var sceneLoader = KAssetBundleLoader.Load(path);

                if (request.Loaders == null)
                    request.Loaders = new List<KAbstractResourceLoader>();
                request.Loaders.Add(sceneLoader);
                while (!sceneLoader.IsCompleted)
                    yield return null;
            }
            else
            {
                var assetLoader = KAssetFileLoader.Load(path);
                while (!assetLoader.IsCompleted)
                    yield return null;
                request.Asset = assetLoader.Asset;
            }

            request.IsDone = true;
        }

        /// <summary>
        /// 暂时不用
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ResourceDepRequest LoadLevelAdditiveAsync(string path)
        {
            var req = new ResourceDepRequest();
            KResourceModule.Instance.StartCoroutine(CoLoadLevelAdditiveAsync(path, req));
            return req;
        }

        /// <summary>
        /// 暂时不用，交给KResources
        /// </summary>
        /// <param name="path"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        static IEnumerator CoLoadLevelAdditiveAsync(string path, ResourceDepRequest req)
        {
            var abReq = LoadAssetBundleAsync(path);
            while (!abReq.IsDone)
                yield return null;

            var levelName = Path.GetFileNameWithoutExtension(path);
            var op = Application.LoadLevelAdditiveAsync(levelName);
            while (!op.isDone)
                yield return null;

            RefreshAllMaterialsShaders();
            req.IsDone = true;
            Logger.Log("[LoadLevelAdditiveAsync]Load Level `{0}` Complete!", path);
        }
    }
}