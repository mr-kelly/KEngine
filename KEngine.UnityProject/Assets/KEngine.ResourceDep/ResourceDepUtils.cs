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
    }

    /// <summary>
    /// ResourceDep资源依赖系统主入口
    /// </summary>
    public class ResourceDepUtils
    {
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
            var dirArr = dirPath.Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

            // 寻找所有目录的首字母
            var dirFirstCharArr = new string[dirArr.Length];
            for (var i = 0; i < dirArr.Length; i++)
            {
                dirFirstCharArr[i] = dirArr[i][0].ToString().ToLower();
            }
            var newBuildAssetPath = string.Format("{0}/{1}_{2}{3}", dirPath, string.Join("", dirFirstCharArr), fileName, fileExt);
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
        /// 同步加载AssetBundle
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public static UnityEngine.Object LoadAssetBundleSync(string relativePath)
        {
            // manifest
            string manifestPath = ResourceDepUtils.GetBuildPath(string.Format("{0}.manifest{1}", relativePath,
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
                }
            }
            else
            {
                Logger.LogWarning("Cannot find Manifest: {0}", relativePath);
            }

            string path =
                GetBuildPath(string.Format("{0}{1}", relativePath,
                    KEngine.AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt)));

            //while (!assetLoader.IsCompleted)
            //    yield return null;
            // 获取后缀名
            var ext = Path.GetExtension(relativePath);
            if (ext == ".unity")
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
            var request = new ResourceDepRequest { Path = relativePath };
            AppEngine.EngineInstance.StartCoroutine(CoLoadAssetBundleAsync(relativePath, request));
            return request;
        }

        private static IEnumerator CoLoadAssetBundleAsync(string relativePath, ResourceDepRequest request)
        {
            // manifest
            string manifestPath = ResourceDepUtils.GetBuildPath(string.Format("{0}.manifest{1}", relativePath,
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
                    while (!depLoader.IsCompleted)
                    {
                        yield return null;
                    }
                }
            }
            string path =
                GetBuildPath(string.Format("{0}{1}", relativePath,
                    KEngine.AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt)));

            // 获取后缀名
            var ext = Path.GetExtension(relativePath);
            if (ext == ".unity")
            {
                // Scene 
                var sceneLoader = KAssetBundleLoader.Load(path);
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

        public static ResourceDepRequest LoadLevelAdditiveAsync(string path)
        {
            var req = new ResourceDepRequest();
            KResourceModule.Instance.StartCoroutine(CoLoadLevelAdditiveAsync(path, req));
            return req;
        }

        static IEnumerator CoLoadLevelAdditiveAsync(string path, ResourceDepRequest req)
        {
            var abReq = LoadAssetBundleAsync(path);
            while (!abReq.IsDone)
                yield return null;

            var levelName = Path.GetFileNameWithoutExtension(path);
            var op = Application.LoadLevelAdditiveAsync(levelName);
            while (!op.isDone)
                yield return null;

            Logger.Log("[LoadLevelAdditiveAsync]Load Level `{0}` Complete!", path);
        }
    }
}