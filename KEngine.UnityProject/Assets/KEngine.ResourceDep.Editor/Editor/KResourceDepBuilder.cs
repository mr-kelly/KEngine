#region Copyright (c) Kingsoft Xishanju

// KEngine - Asset Bundle framework for Unity3D
// ===================================
// 
// Filename: KResourceDepBuilder.cs
// Date:        2016/01/19
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

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KEngine.ResourceDep
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ResourceBuildClassAttribute : Attribute
    {
        public Type ClassType;

        public ResourceBuildClassAttribute(Type type)
        {
            ClassType = type;
        }
    }

    public class ResourceDepInfo
    {
        public string Path;
        public List<string> DepAssetPaths = new List<string>();
    }

    public interface IResourceBuildProcessor
    {
        List<string> Process(Component @object);
    }

    public class KResourceDep_Texture
    {
        public static List<string> Collect(Texture tex)
        {
            if (!KResourceDepBuilder.HasPushDep(tex))
            {
                KResourceDepBuilder.AddPushDep(tex);
                KBuildTools.BuildAssetBundle(tex, string.Format("TmpBundle/Texture/{0}.ab", tex.name));

                Debug.Log(tex.name);
            }
            return new List<string>()
            {
                AssetDatabase.GetAssetPath(tex)
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
                KResourceDepBuilder.AddPushDep(mat);
                KBuildTools.BuildAssetBundle(mat, string.Format("TmpBundle/Material/{0}.ab", mat.name));

                Debug.Log(mat.name);
            }
            list.Add(AssetDatabase.GetAssetPath(mat));
            return list;
        }
    }

    public class KResourceDep_NGUI
    {
        public static List<string> CollectUIAtlas(UIAtlas atlas)
        {
            var list = new List<string>();
            list.AddRange(KResourceDep_Material.Collect(atlas.spriteMaterial));
            if (!KResourceDepBuilder.HasPushDep(atlas))
            {
                KResourceDepBuilder.AddPushDep(atlas);
                KBuildTools.BuildAssetBundle(atlas, string.Format("TmpBundle/UIAtlas/{0}.ab", atlas.name));

                Debug.Log(atlas.name);
            }
            return list;
        }
    }

    [ResourceBuildClass(typeof(UISprite))]
    public class KResourceDep_UISprite : IResourceBuildProcessor
    {
        public List<string> Process(Component @object)
        {
            var list = new List<string>();
            var uiSprite = (UISprite)@object;

            list.AddRange(KResourceDep_NGUI.CollectUIAtlas(uiSprite.atlas));

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
            }
            else
            {
                Logger.LogWarning("没有Material的粒子: {0}", particle.gameObject.name);
            }

            var matPath = AssetDatabase.GetAssetPath(particle.renderer.sharedMaterial);
            listDeps.Add(matPath);
            return listDeps;
        }
    }

    /// <summary>
    /// New instead of KAssetDep
    /// </summary>
    public class KResourceDepBuilder
    {
        /// <summary>
        /// 存放Push进去的对象
        /// </summary>
        private static HashSet<UnityEngine.Object> DependencyPool = new HashSet<Object>();

        private static Dictionary<IResourceBuildProcessor, ResourceBuildClassAttribute> _cachedDepBuildClassAttributes;

        public static bool HasPushDep(UnityEngine.Object obj)
        {
            return DependencyPool.Contains(obj);
        }

        public static void AddPushDep(UnityEngine.Object obj)
        {
            BuildPipeline.PushAssetDependencies();
            DependencyPool.Add(obj);
        }

        public static ResourceDepInfo BuildGameObject(GameObject buildObj)
        {
            var depInfo = new ResourceDepInfo();

            if (_cachedDepBuildClassAttributes == null)
            {
                _cachedDepBuildClassAttributes = new Dictionary<IResourceBuildProcessor, ResourceBuildClassAttribute>();
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var processorType in asm.GetTypes())
                    {
                        var depBuildClassAttrs = processorType.GetCustomAttributes(typeof(ResourceBuildClassAttribute),
                            false);
                        if (depBuildClassAttrs.Length > 0)
                        {
                            foreach (var attr in depBuildClassAttrs)
                            {
                                var depBuildAttr = (ResourceBuildClassAttribute)attr;
                                var depBuildProcessor =
                                    Activator.CreateInstance(processorType) as IResourceBuildProcessor;
                                _cachedDepBuildClassAttributes[depBuildProcessor] = depBuildAttr;
                                break;
                            }
                        }
                    }
                }

            }

            // 依赖处理
            foreach (var kv in _cachedDepBuildClassAttributes)
            {
                var depAttr = kv.Value;
                var processor = kv.Key;

                foreach (Component component in buildObj.GetComponentsInChildren(depAttr.ClassType, true))
                {
                    depInfo.DepAssetPaths.AddRange(processor.Process(component));
                }
            }

            BuildPipeline.PushAssetDependencies();
            KBuildTools.BuildAssetBundle(buildObj, string.Format("TmpBundle/UI/{0}.ab", buildObj.name));
            BuildPipeline.PopAssetDependencies();

            Debug.Log(buildObj.name);
            return depInfo;
        }

        public static void Clear()
        {
            foreach (var depObj in DependencyPool)
            {
                BuildPipeline.PopAssetDependencies();
            }
            Logger.Log("Clear ResourceDep pool: {0}", DependencyPool.Count);
            DependencyPool.Clear();
        }
    }
}