//#region Copyright (c) Kingsoft Xishanju

//// KEngine - Asset Bundle framework for Unity3D
//// ===================================
//// 
//// Filename: KResourceDepProcessors.cs
//// Date:        2016/01/20
//// Author:     Kelly
//// Email:       23110388@qq.com
//// Github:     https://github.com/mr-kelly/KEngine
//// 
//// This library is free software; you can redistribute it and/or
//// modify it under the terms of the GNU Lesser General Public
//// License as published by the Free Software Foundation; either
//// version 3.0 of the License, or (at your option) any later version.
//// 
//// This library is distributed in the hope that it will be useful,
//// but WITHOUT ANY WARRANTY; without even the implied warranty of
//// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//// Lesser General Public License for more details.
//// 
//// You should have received a copy of the GNU Lesser General Public
//// License along with this library.

//#endregion

//using System.Collections.Generic;
//using UnityEngine;

//namespace KEngine.ResourceDep.Builder
//{
//    #region Unity Base Class Processor

//    public partial class ResourceDepBuilder
//    {
//        public static IEnumerable<string> CollectMesh(Mesh meshAsset)
//        {
//            var list = new List<string>();
//            if (!HasPushDep(meshAsset))
//            {
//                AddPushDep(meshAsset, null);
//            }
//            list.Add(GetBuildAssetPath(meshAsset));
//            return list;
//        }

//        public static List<string> CollectMaterial(Material mat)
//        {
//            var list = new List<string>();
//            list.AddRange(CollectTexture(mat.mainTexture));
//            if (!HasPushDep(mat))
//            {
//                AddPushDep(mat, list);

//                Debug.Log(mat.name);
//            }
//            list.Add(ResourceDepBuilder.GetBuildAssetPath(mat));
//            return list;
//        }

//        public static List<string> CollectTexture(Texture tex)
//        {
//            if (!HasPushDep(tex))
//            {
//                AddPushDep(tex, null);
//            }
//            return new List<string>()
//            {
//                GetBuildAssetPath(tex)
//            };
//        }

//        public static List<string> CollectAvatar(Avatar avatar)
//        {
//            var list = new List<string>();
//            if (!HasPushDep(avatar))
//            {
//                AddPushDep(avatar, null);
//            }
//            return list;
//        }
//    }

//    [ResourceBuildClass(typeof(ParticleSystem))]
//    public class ParticleSystemProcessor : IBuilderProcessor
//    {
//        public List<string> Process(Component @object)
//        {
//            var listDeps = new List<string>();

//            var particleCom = (ParticleSystem)@object;
//            var particle = particleCom;
//            if (particle.renderer.sharedMaterial != null)
//            {
//                //string matPath = KDepBuild_Material.BuildDepMaterial(particle.renderer.sharedMaterial);
//                ////CResourceDependencies.Create(particle, CResourceDependencyType.PARTICLE_SYSTEM, matPath);
//                //KAssetDep.Create<KParticleSystemDep>(particle, matPath);
//                //particle.renderer.sharedMaterial = null;
//                listDeps.AddRange(ResourceDepBuilder.CollectMaterial(particle.renderer.sharedMaterial));
//            }
//            else
//            {
//                Logger.LogWarning("没有Material的粒子: {0}", particle.gameObject.name);
//            }

//            var matPath = ResourceDepBuilder.GetBuildAssetPath(particle.renderer.sharedMaterial);
//            listDeps.Add(matPath);
//            return listDeps;
//        }
//    }

//    [ResourceBuildClass(typeof(MeshRenderer))]
//    public class MeshRendererProcessor : IBuilderProcessor
//    {
//        public List<string> Process(Component @object)
//        {
//            var list = new List<string>();
//            var meshRenderer = (MeshRenderer)@object;

//            foreach (var mat in meshRenderer.sharedMaterials)
//            {
//                list.AddRange(ResourceDepBuilder.CollectMaterial(mat));
//            }

//            return list;
//        }
//    }
//    [ResourceBuildClass(typeof(Animator))]
//    public class AnimatorProcessor : IBuilderProcessor
//    {
//        public List<string> Process(Component @object)
//        {
//            var list = new List<string>();
//            var animator = (Animator)@object;

//            //animator.avatar
//            list.AddRange(ResourceDepBuilder.CollectAvatar(animator.avatar));

//            return list;
//        }
//    }
//    [ResourceBuildClass(typeof(MeshFilter))]
//    public class MeshFilterProcessor: IBuilderProcessor
//    {
//        public List<string> Process(Component @object)
//        {
//            var list = new List<string>();
//            var meshFiler = (MeshFilter)@object;


//            list.AddRange(ResourceDepBuilder.CollectMesh(meshFiler.sharedMesh));

//            return list;
//        }
//    }
//    #endregion

//#if NGUI
//    public class KResourceDep_NGUI
//    {
//        public static List<string> CollectUIAtlas(UIAtlas atlas)
//        {
//            var list = new List<string>();
//            list.AddRange(ResourceDepBuilder.CollectMaterial(atlas.spriteMaterial));
//            if (!ResourceDepBuilder.HasPushDep(atlas))
//            {
//                ResourceDepBuilder.AddPushDep(atlas, list);
//            }
//            list.Add(ResourceDepBuilder.GetBuildAssetPath(atlas));
//            return list;
//        }

//        public static IEnumerable<string> CollectFont(Font font)
//        {
//            var list = new List<string>();
//            if (!ResourceDepBuilder.HasPushDep(font))
//            {
//                ResourceDepBuilder.AddPushDep(font, list);
//            }
//            list.Add(ResourceDepBuilder.GetBuildAssetPath(font));
//            return list;
//        }
//    }

//    [ResourceBuildClass(typeof (UITexture))]
//    public class KResourceDep_UITexture : IBuilderProcessor
//    {
//        public List<string> Process(Component @object)
//        {
//            var list = new List<string>();
//            var uiTexture = (UITexture) @object;

//            list.AddRange(ResourceDepBuilder.CollectTexture(uiTexture.mainTexture));

//            return list;
//        }
//    }

//    [ResourceBuildClass(typeof (UISprite))]
//    public class KResourceDep_UISprite : IBuilderProcessor
//    {
//        public List<string> Process(Component @object)
//        {
//            var list = new List<string>();
//            var uiSprite = (UISprite) @object;

//            list.AddRange(KResourceDep_NGUI.CollectUIAtlas(uiSprite.atlas));

//            return list;
//        }
//    }
//    [ResourceBuildClass(typeof(UILabel))]
//    public class KResourceDep_UILabel : IBuilderProcessor
//    {
//        public List<string> Process(Component @object)
//        {
//            var list = new List<string>();
//            var uiLabel = (UILabel)@object;

//            if (uiLabel.trueTypeFont != null)
//                list.AddRange(KResourceDep_NGUI.CollectFont(uiLabel.trueTypeFont));
//            // TODO: bitmap Font

//            return list;
//        }
//    }
//#endif
//}