#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KUISpriteDep.cs
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
using KEngine;

public class KUISpriteDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessUISprite(resourcePath);
    }

    protected void ProcessUISprite(string resourcePath)
    {
        var loader = KUIAtlasDep.LoadUIAtlas(resourcePath, (atlas) =>
        {
            if (!IsDestroy)
            {
                //UIAtlas atlas = _obj as UIAtlas;
                Debuger.Assert(atlas);

                Debuger.Assert(DependencyComponent);
                var sprite = DependencyComponent as UISprite;

                Debuger.Assert(sprite);
                sprite.atlas = atlas;

                //对UISpriteAnimation处理
                foreach (UISpriteAnimation spriteAnim in this.gameObject.GetComponents<UISpriteAnimation>())
                {
                    spriteAnim.RebuildSpriteList();
                }
            }
            OnFinishLoadDependencies(gameObject); // 返回GameObject而已哦
        });
        ResourceLoaders.Add(loader);
    }
}
#endif