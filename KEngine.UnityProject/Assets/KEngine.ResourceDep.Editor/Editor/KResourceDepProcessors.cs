#region Copyright (c) Kingsoft Xishanju

// KEngine - Asset Bundle framework for Unity3D
// ===================================
// 
// Filename: KResourceDepProcessors.cs
// Date:        2016/01/20
// Author:     Kelly
// Email:       23110388@qq.com
// Github:     https://github.com/mr-kelly/KEngine
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

using System.Collections.Generic;
using UnityEngine;

namespace KEngine.ResourceDep
{

    #region Unity Base Class Processor

    public class KResourceDep_Texture
    {
        public static List<string> Collect(Texture tex)
        {
            if (!KResourceDepBuilder.HasPushDep(tex))
            {
                KResourceDepBuilder.AddPushDep(tex, null);
            }
            return new List<string>()
            {
                KResourceDepBuilder.GetRelativeAssetPath(tex)
            };
        }
    }

    public class KResourceDep_Material
    {
        public static List<string> Collect(Material mat)
        {
            var list = new List<string>();
            list.AddRange(KResourceDep_Texture.Collect(mat.mainTexture));
            if (!KResourceDepBuilder.HasPushDep(mat))
            {
                KResourceDepBuilder.AddPushDep(mat, list);

                Debug.Log(mat.name);
            }
            list.Add(KResourceDepBuilder.GetRelativeAssetPath(mat));
            return list;
        }
    }

    [ResourceBuildClass(typeof(ParticleSystem))]
    public class KResourceDep_ParticleSystem : IResourceBuildProcessor
    {
        public List<string> Process(Component @object)
        {
            var listDeps = new List<string>();

            var particleCom = (ParticleSystem)@object;
            var particle = particleCom;
            if (particle.renderer.sharedMaterial != null)
            {
                //string matPath = KDepBuild_Material.BuildDepMaterial(particle.renderer.sharedMaterial);
                ////CResourceDependencies.Create(particle, CResourceDependencyType.PARTICLE_SYSTEM, matPath);
                //KAssetDep.Create<KParticleSystemDep>(particle, matPath);
                //particle.renderer.sharedMaterial = null;
                listDeps.AddRange(KResourceDep_Material.Collect(particle.renderer.sharedMaterial));
            }
            else
            {
                Logger.LogWarning("没有Material的粒子: {0}", particle.gameObject.name);
            }

            var matPath = KResourceDepBuilder.GetRelativeAssetPath(particle.renderer.sharedMaterial);
            listDeps.Add(matPath);
            return listDeps;
        }
    }

    #endregion

#if NGUI
    public class KResourceDep_NGUI
    {
        public static List<string> CollectUIAtlas(UIAtlas atlas)
        {
            var list = new List<string>();
            list.AddRange(KResourceDep_Material.Collect(atlas.spriteMaterial));
            if (!KResourceDepBuilder.HasPushDep(atlas))
            {
                KResourceDepBuilder.AddPushDep(atlas, list);
            }
            list.Add(KResourceDepBuilder.GetRelativeAssetPath(atlas));
            return list;
        }

        public static IEnumerable<string> CollectFont(Font font)
        {
            var list = new List<string>();
            if (!KResourceDepBuilder.HasPushDep(font))
            {
                KResourceDepBuilder.AddPushDep(font, list);
            }
            list.Add(KResourceDepBuilder.GetRelativeAssetPath(font));
            return list;
        }
    }

    [ResourceBuildClass(typeof (UITexture))]
    public class KResourceDep_UITexture : IResourceBuildProcessor
    {
        public List<string> Process(Component @object)
        {
            var list = new List<string>();
            var uiTexture = (UITexture) @object;

            list.AddRange(KResourceDep_Texture.Collect(uiTexture.mainTexture));

            return list;
        }
    }

    [ResourceBuildClass(typeof (UISprite))]
    public class KResourceDep_UISprite : IResourceBuildProcessor
    {
        public List<string> Process(Component @object)
        {
            var list = new List<string>();
            var uiSprite = (UISprite) @object;

            list.AddRange(KResourceDep_NGUI.CollectUIAtlas(uiSprite.atlas));

            return list;
        }
    }
    [ResourceBuildClass(typeof(UILabel))]
    public class KResourceDep_UILabel : IResourceBuildProcessor
    {
        public List<string> Process(Component @object)
        {
            var list = new List<string>();
            var uiLabel = (UILabel)@object;

            if (uiLabel.trueTypeFont != null)
                list.AddRange(KResourceDep_NGUI.CollectFont(uiLabel.trueTypeFont));
            // TODO: bitmap Font

            return list;
        }
    }
#endif
}