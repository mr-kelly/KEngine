#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: Ctk2dTileMapDep.cs
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


#if tk2dTileMap
public class Ctk2dTileMapDep : KAssetDep {

    protected override void DoProcess(string resourcePath)
    {
        ProcessTileMap_LoadSpriteCollection(resourcePath);
    }

    // 讀SpriteCollection...
    protected void ProcessTileMap_LoadSpriteCollection(string path)
    {
        var loader = CTk2dSpriteCollectionDep.LoadSpriteCollection(path, (_obj) =>
        {
            tk2dSpriteCollectionData colData = _obj as tk2dSpriteCollectionData;
            CDebug.Assert(colData);
            if (!IsDestroy)
            {
                var sprite = (tk2dTileMap)DependencyComponent;

                //foreach (tk2dTileMap sprite in gameObject.GetComponents<tk2dTileMap>())
                {
                    sprite.Editor__SpriteCollection = colData;
                    sprite.ForceBuild();
                }

            }
            OnFinishLoadDependencies(gameObject);  // 返回GameObject而已哦
            //else
            //    CDebug.LogWarning("GameObject maybe destroy!");

        });
        ResourceLoaders.Add(loader);
    }

}
#endif