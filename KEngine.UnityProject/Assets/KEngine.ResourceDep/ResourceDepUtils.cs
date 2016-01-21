using System;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;

namespace KEngine.ResourceDep
{
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
            var manifestLoader = KBytesLoader.Load(manifestPath, KResourceInAppPathType.ResourcesAssetsPath, KAssetBundleLoaderMode.ResourcesLoad);
            //while (!manifestLoader.IsCompleted)
            //    yield return null;
            var manifestBytes = manifestLoader.Bytes;
            manifestLoader.Release(); // 释放掉文本字节
            var utf8NoBom = new UTF8Encoding(false);
            var manifestList = utf8NoBom.GetString(manifestBytes).Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < manifestList.Length; i++)
            {
                var depPath = manifestList[i] + AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt);
                var depLoader = KAssetFileLoader.Load(depPath);
                //while (!depLoader.IsCompleted)
                //{
                //    yield return null;
                //}

            }
            string path = GetBuildPath(string.Format("{0}{1}", relativePath, KEngine.AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt)));

            var assetLoader = KAssetFileLoader.Load(path);
            //while (!assetLoader.IsCompleted)
            //    yield return null;

            return assetLoader.Asset;
        }
    }

}
