#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KTextDep.cs
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

using UnityEngine.UI;

namespace KEngine
{

    //UGUI Text
    public class KTextDep : KAssetDep
    {
        protected override void DoProcess(string resourcePath)
        {
            ProcessFont(resourcePath);
        }

        protected void ProcessFont(string resPath)
        {
            var loader = FontLoader.Load(resPath, (isOk, _font) =>
            {
                if (!IsDestroy)
                {
                    var label = DependencyComponent as Text;
                    label.font = _font;
                    label.text = label.text + " ";
                }
                OnFinishLoadDependencies(_font);
            });
            this.ResourceLoaders.Add(loader);
        }
    }

}