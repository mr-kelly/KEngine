using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace KEngine.ResourceDep.Builder
{
    public enum UnityAssetType
    {
        Object, // Common, Asset
        Builtin, // Library/unity_default_resources  /  Resources/unity_builtin_extra
        Memory, // 可能是存放在内存中的资源
    }

    /// <summary>
    /// 常量,配置定义
    /// </summary>
    public class Define
    {
        /// <summary>
        /// 列表中的不进行push打包
        /// </summary>
        public static HashSet<AssetExtType> IgnoreBuildType = new HashSet<AssetExtType>
        {
            AssetExtType.Cs,
        };

    }
    /// <summary>
    /// 文件名后缀排序, 越小的越靠前
    /// </summary>
    public enum AssetExtType
    {
        Png,
        Tga,
        Anim,
        Shader,
        Fbx,
        Ttf, // 字体, 可能依赖GUI/Text Shader

        Mat,
        Prefab,
        Scene,

        Cs,
        Default,

    }

}