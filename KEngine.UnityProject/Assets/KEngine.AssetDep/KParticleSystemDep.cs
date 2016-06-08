#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KParticleSystemDep.cs
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

public class KParticleSystemDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessParticleSystem(resourcePath);
    }

    /// <summary>
    /// 加载粒子系统以来的材质
    /// </summary>
    /// <param name="path"></param>
    protected void ProcessParticleSystem(string path)
    {
        var p = (ParticleSystem) DependencyComponent;
        p.Stop(); // 先暂停粒子播放
        var pRenderer = (ParticleSystemRenderer) p.GetComponent<Renderer>();

        pRenderer.enabled = false;
        LoadMaterial(path, (mat) =>
        {
            if (IsDestroy)
            {
                Log.LogError("[ProcessParticleSystem]Material loaded, but Destroyed Dep: {0}, Material: {1}", path,
                    mat);
            }

            if (!IsDestroy)
            {
                pRenderer.sharedMaterial = mat;
                pRenderer.enabled = true;
            }

            OnFinishLoadDependencies(mat);

            if (!IsDestroy)
            {
                // callback后再play
                p.Play();
            }
        });
    }
}