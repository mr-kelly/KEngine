#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CTk2dSpriteDep.cs
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


#if TK2D
public class CTk2dSpriteDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessTk2dSprite(resourcePath);
    }

    protected void ProcessTk2dSprite(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            Log.Error("[ProcessTk2dSprite]Null ResourcePath {0}", gameObject.name);
            return;
        }

        var loader = CTk2dSpriteCollectionDep.LoadSpriteCollection(resourcePath, (_obj) =>
        {
            tk2dSpriteCollectionData colData = _obj as tk2dSpriteCollectionData;
            Log.Assert(colData);
            if (!IsDestroy)
            {
                Log.Assert(DependencyComponent is tk2dBaseSprite);
                tk2dBaseSprite sprite = (tk2dBaseSprite)DependencyComponent;
                sprite.Collection = colData;
                sprite.Build();

            }
            OnFinishLoadDependencies(colData);  // 返回GameObject而已哦
            //else
            //    Log.Warning("GameObject maybe destroy!");
        });
        ResourceLoaders.Add(loader);
    }

}
#endif