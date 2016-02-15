#region  Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Asset Bundle framework for Unity3D
// ===================================
// 
// Filename: Define.cs
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
        /// 列表中的不进行push依赖打包
        /// </summary>
        public static HashSet<AssetExtType> IgnoreDepType = new HashSet<AssetExtType>
        {
            AssetExtType.Cs, // 脚本不打包
            //AssetExtType.Prefab, // Prefab不需要依赖
            //AssetExtType.Shader,
        };
    }

    /// <summary>
    /// 文件名后缀排序, 越小的越靠前
    /// </summary>
    public enum AssetExtType
    {
        Jpg,
        Png,
        Tga,
        Bmp,

        Anim,
        Shader,
        Fbx,
        Ttf, // 字体, 可能依赖GUI/Text Shader

        Mat,
        Prefab,
        Unity, // Scene/Level

        Cs,
        Default,
    }
}