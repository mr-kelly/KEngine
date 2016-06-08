#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KUIAtlasDep.cs
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

using System;
using KEngine;
using UnityEngine;
#if NGUI
public class KUIAtlasDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessUIAtlas(resourcePath);
    }

    protected void ProcessUIAtlas(string path)
    {
        //Action<Material> matCallback = (Material _mat) => // 依赖材质
        //{
        //    OnFinishLoadDependencies(_mat);
        //};
        //new CStaticAssetLoader(path, OnLoadMaterialScript, path, matCallback);
        LoadMaterial(path, (mat) => { OnFinishLoadDependencies(mat); });
    }

    public static KStaticAssetLoader LoadUIAtlas(string resourcePath, Action<UIAtlas> callback, KAssetBundleLoaderMode loaderMode = KAssetBundleLoaderMode.Default)
    {
        //System.Func<bool> doCheckCache = () =>
        //{
        //    UIAtlas cacheAtlas;  // 这里有个问题的，如果正在加载中，还没放进Cache列表...还是会多次执行, 但不要紧， CWWWLoader已经避免了重复加载, 这里确保快速回调，不延迟1帧
        //    if (CachedUIAtlas.TryGetValue(resourcePath, out cacheAtlas))
        //    {
        //        if (callback != null)
        //            callback(cacheAtlas);
        //        return true;
        //    }
        //    return false;
        //};

        //if (doCheckCache())
        //    return;
        bool exist = KStaticAssetLoader.GetRefCount<KStaticAssetLoader>(resourcePath) > 0;
        return KStaticAssetLoader.Load(resourcePath, (isOk, obj) =>
        {
            //if (doCheckCache())
            //    return;

            GameObject gameObj = obj as GameObject; // Load UIAtlas Object Prefab
            gameObj.transform.parent = DependenciesContainer.transform;

            gameObj.name = resourcePath;
            UIAtlas atlas = gameObj.GetComponent<UIAtlas>();
            Debuger.Assert(atlas);

            if (!exist)
            {
                // Wait Load Material
                var colDep = gameObj.GetComponent<KAssetDep>();
                Debuger.Assert(colDep && colDep.GetType() == typeof (KUIAtlasDep)); // CResourceDependencyType.UI_ATLAS);
                // 依赖材质Material, 加载后是Material
                colDep.AddFinishCallback((assetDep, _obj) =>
                {
                    // 塞Material进去UIAtlas
                    if (atlas.spriteMaterial == null) // 不为空意味已经加载过了！
                    {
                        Material _mat = _obj as Material;
                        atlas.spriteMaterial = _mat; // 注意，这一行性能消耗非常的大！
                    }
                    else
                        Log.LogWarning("Atlas依赖的材质多次加载了（未缓存)!!!!!!!!!!!!!");

                    if (callback != null)
                        callback(atlas);
                });
            }
            else
            {
                if (callback != null)
                    callback(atlas);
            }
        }, loaderMode);
    }
}
#endif