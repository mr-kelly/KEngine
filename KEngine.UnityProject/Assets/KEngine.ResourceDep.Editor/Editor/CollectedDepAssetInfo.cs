using UnityEngine;
using System.Collections;

namespace KEngine.ResourceDep.Builder
{
    /// <summary>
    /// 收集到的依赖资源信息
    /// </summary>
    public struct CollectedDepAssetInfo
    {
        public UnityEngine.Object Asset;
        public string UnityAssetPath;
        public AssetExtType ExtType;
        public string BuildAssetPath;
        public UnityAssetType UnityAssetType { get; set; }
    }

}