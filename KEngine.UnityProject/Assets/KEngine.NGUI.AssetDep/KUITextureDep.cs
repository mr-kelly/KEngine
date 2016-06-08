#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KUITextureDep.cs
// Date:     2015/12/03
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
// License along with this library.

#endregion

#if NGUI
using System;
using KEngine;
using UnityEngine;

public class KUITextureDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessUITexture(resourcePath);
    }

    protected void ProcessUITexture(string resourcePath)
    {
        LoadTexture(resourcePath, (_tex) =>
        {
            if (!IsDestroy)
            {
                Debuger.Assert(DependencyComponent is UITexture);
                var uiTex = (UITexture) DependencyComponent;
                uiTex.mainTexture = _tex;
                // different pixelSize !   uiTex.pixelSize = GameDef.PictureScale;
            }
            OnFinishLoadDependencies(gameObject); // 返回GameObject而已哦
        });
    }

    protected void LoadTexture(string texPath, Action<Texture> exCallback = null)
    {
        TexturesWaitLoadCount++;
        var texLoader = KTextureLoader.Load(texPath, (isOk, tex) =>
        {
            if (!isOk)
            {
                Log.LogError("无法加载依赖图片: {0}", texPath);
            }

            if (exCallback != null)
                exCallback(tex);

            TexturesWaitLoadCount--;
            if (TexturesWaitLoadCount <= 0)
            {
                foreach (var c in TexturesLoadedCallback)
                {
                    c();
                }
                TexturesLoadedCallback.Clear();
            }
        });
        ResourceLoaders.Add(texLoader);
    }
}
#endif