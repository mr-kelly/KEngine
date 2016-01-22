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
        public string Path { get; internal set; }
        public System.Type Type { get; internal set; }

        public bool IsDone { get; internal set; }
        public Object Asset { get; internal set; }
    }

    /// <summary>
    /// ResourceDep资源依赖系统主入口
    /// </summary>
    public class ResourceDepUtils
    {
        /// <summary>
        /// 将返回具体的资源路径，会根据目录进行匹配
        /// </summary>
        /// <param name="relativeAssetPath"></param>
        /// <returns></returns>
        public static string GetBuildPath(string relativeAssetPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(relativeAssetPath);
            var fileExt = Path.GetExtension(relativeAssetPath);
            var dirPath = Path.GetDirectoryName(relativeAssetPath);
            var dirArr = dirPath.Split('/');
            var newBuildAssetPath = string.Format("{0}/{1}_{2}{3}", dirPath, string.Join("_", dirArr), fileName, fileExt);
            return newBuildAssetPath;
        }


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
            var utf8NoBom = new UTF8Encoding(false);
            var manifestList = utf8NoBom.GetString(manifestBytes)
                .Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < manifestList.Length; i++)
            {
                var depPath = manifestList[i] + AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt);
                var depLoader = KAssetFileLoader.Load(depPath);
                //while (!depLoader.IsCompleted)
                //{
                //    yield return null;
                //}
            }
            string path =
                GetBuildPath(string.Format("{0}{1}", relativePath,
                    KEngine.AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt)));

            var assetLoader = KAssetFileLoader.Load(path);
            //while (!assetLoader.IsCompleted)
            //    yield return null;

            return assetLoader.Asset;
        }

        public static ResourceDepRequest LoadAssetBundleAsync(string relativePath)
        {
            var request = new ResourceDepRequest {Path = relativePath};
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
            var manifestBytes = manifestLoader.Bytes;
            manifestLoader.Release(); // 释放掉文本字节
            var utf8NoBom = new UTF8Encoding(false);
            var manifestList = utf8NoBom.GetString(manifestBytes)
                .Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < manifestList.Length; i++)
            {
                var depPath = manifestList[i] + AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt);
                var depLoader = KAssetFileLoader.Load(depPath);
                while (!depLoader.IsCompleted)
                {
                    yield return null;
                }
            }
            string path =
                GetBuildPath(string.Format("{0}{1}", relativePath,
                    KEngine.AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt)));

            var assetLoader = KAssetFileLoader.Load(path);
            while (!assetLoader.IsCompleted)
                yield return null;

            request.IsDone = true;
            request.Asset = assetLoader.Asset;
        }
    }
}