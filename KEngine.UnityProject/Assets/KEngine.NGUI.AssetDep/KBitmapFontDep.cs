#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KBitmapFontDep.cs
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

using KEngine;
using UnityEngine;
#if NGUI
public class KBitmapFontDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcesBitMapFont(resourcePath);
    }

    // UILabel...  Bitmap Font
    protected void ProcesBitMapFont(string resPath)
    {
        // Load UIFont Prefab
        var loader = KStaticAssetLoader.Load(resPath, (isOk, o) =>
        {
            if (!IsDestroy)
            {
                var uiFontPrefab = (GameObject) o;
                Debuger.Assert(uiFontPrefab);

                uiFontPrefab.transform.parent = DependenciesContainer.transform;

                var uiFont = uiFontPrefab.GetComponent<UIFont>();
                Debuger.Assert(uiFont);
                var label = DependencyComponent as UILabel;
                //foreach (UILabel label in gameObject.GetComponents<UILabel>())
                {
                    label.bitmapFont = uiFont;
                }
                OnFinishLoadDependencies(uiFont);
            }
            else
                OnFinishLoadDependencies(null);
        });
        ResourceLoaders.Add(loader);
    }
}
#endif