#region  Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Asset Bundle framework for Unity3D
// ===================================
// 
// Filename: CollectedDepAssetInfo.cs
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