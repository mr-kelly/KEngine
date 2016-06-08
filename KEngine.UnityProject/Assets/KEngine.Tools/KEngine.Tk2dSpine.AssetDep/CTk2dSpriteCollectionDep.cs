#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CTk2dSpriteCollectionDep.cs
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
public class CTk2dSpriteCollectionDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessSpriteCollection(resourcePath);
    }

    public static CStaticAssetLoader LoadSpriteCollection(string resourcePath, Action<UnityEngine.Object> callback)
    {
        return CStaticAssetLoader.Load(resourcePath, (isOk, asset) => OnLoadSpriteCollectionAsset(asset, new object[] { resourcePath, callback }));
    }

    protected void ProcessSpriteCollection(string resourcePath)
    {
        //Action<Material> matCallback = (Material _mat) =>
        //{
        //    OnFinishLoadDependencies(_mat);
        //};
        //new CStaticAssetLoader(resourcePath, OnLoadMaterialScript, resourcePath, matCallback);
        LoadMaterial(resourcePath, (mat) =>
        {
            OnFinishLoadDependencies(mat);
        });
    }

    protected static void OnLoadSpriteCollectionAsset(UnityEngine.Object obj, params object[] args)
    {
        string resPath = args[0] as string;

        Action<UnityEngine.Object> externalCallback = args[1] as Action<UnityEngine.Object>;

        GameObject colObj = obj as GameObject;
        colObj.transform.parent = DepContainer.transform;

        obj.name = resPath;
        tk2dSpriteCollectionData colData = colObj.GetComponent<tk2dSpriteCollectionData>();
        Log.Assert(colData);

        var colDep = colObj.GetComponent<KAssetDep>();

        if (!(colDep && colDep.GetType() == typeof (CTk2dSpriteCollectionDep))) // 依赖材质Material, 加载后是Material
        {
            Log.Error("Wrong Collection DepType - {0}", resPath);    
        }
        
        colDep.AddFinishCallback((assetDep, _obj) =>
        {
            Material _mat = _obj as Material;
            //_mat.renderQueue = 4000; // 2D Toolkit渲染顺序靠前！
            // 塞Material进去SpriteCollection
            colData.materials = new Material[] { _mat };
            colData.textures = new Texture[] { _mat.mainTexture };
            foreach (var def in colData.spriteDefinitions)
            {
                def.material = _mat;
            }

            externalCallback(colData);
        });
    }

}

#endif