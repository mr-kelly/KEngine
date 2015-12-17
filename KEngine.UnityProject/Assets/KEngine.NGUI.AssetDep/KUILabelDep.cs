#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KUILabelDep.cs
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

[Obsolete("KUILabelDep Instead")]
public class KFontDep : KUILabelDep
{
}

public class KUILabelDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessFont(resourcePath);
    }

    protected void ProcessFont(string resPath)
    {
        var loader = KFontLoader.Load(resPath, (isOk, _font) =>
        {
            if (!IsDestroy)
            {
                var label = DependencyComponent as UILabel;
                //foreach (UILabel label in gameObject.GetComponents<UILabel>())
                {
                    label.trueTypeFont = _font;
                }
            }
            OnFinishLoadDependencies(_font);
        });
        this.ResourceLoaders.Add(loader);
    }
}
#endif