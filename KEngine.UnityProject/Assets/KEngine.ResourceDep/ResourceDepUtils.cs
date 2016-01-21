using UnityEngine;
using System.Collections;
using System.IO;

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
    }

}
