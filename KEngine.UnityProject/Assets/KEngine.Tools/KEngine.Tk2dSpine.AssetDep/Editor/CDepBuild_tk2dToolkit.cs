#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CDepBuild_tk2dToolkit.cs
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
public partial class KDependencyBuild
{

    // Not SpriteCollectionData
    [DepBuild(typeof(tk2dSpriteCollectionData))]
    static void Processtk2dSpriteCollection(tk2dSpriteCollectionData spriteCollectionData)
    {
        if (spriteCollectionData != null)
        {
            BuildSpriteCollection(spriteCollectionData);
        }
        else
        {
            Log.Warning("空的SpriteCollectionData在SpriteCollection");
        }
    }

    //[DepBuild(typeof(tk2dTileMap))]
    //static void ProcessTileMap(tk2dTileMap tileMap)
    //{
    //    string scPath = BuildSpriteCollection(tileMap.Editor__SpriteCollection, "TileMap");
    //    //CResourceDependencies.Create(tileMap, CResourceDependencyType.TILE_MAP__SPRITE_COLLECTION, scPath);
    //    KAssetDep.Create<Ctk2dTileMapDep>(tileMap, scPath);
    //    tileMap.Editor__SpriteCollection = null;
    //}

    // 创建依赖脚本, no build\
    [DepBuild(typeof(tk2dSprite))]
    static void Process2dSprite(tk2dSprite sprite)
    {
        ProcessBaseSprite(sprite);
    }

    static void ProcessBaseSprite(tk2dSprite baseSprite)
    {
        if (baseSprite.Collection == null)
        {
            Log.Error("Null sprite Collection: {0}", baseSprite.gameObject.name);
            return;
        }
        string spriteCollectionPath = BuildSpriteCollection(baseSprite.Collection);

        if (!string.IsNullOrEmpty(spriteCollectionPath))

            //CResourceDependencies.Create(sprite, CResourceDependencyType.TK2D_SPRITE, spriteCollectionPath);
            KAssetDep.Create<CTk2dSpriteDep>(baseSprite, spriteCollectionPath);

        baseSprite.Collection = null;

        foreach (Renderer rend in baseSprite.gameObject.GetComponents<Renderer>())  // 挖空材质
        {
            rend.sharedMaterials = new Material[0];

        }
    }

    // Prefab build, 单次build缓存
    public static string BuildSpriteCollection(tk2dSpriteCollectionData data)
    {
        if (data == null)
        {
            Log.Error("[BuildSpriteColleccion]Null SpriteCol Data!!!");
            return "";
        }
        GameObject spriteColPrefab = PrefabUtility.FindPrefabRoot(data.gameObject) as GameObject;
        Log.Assert(spriteColPrefab);

        string path = AssetDatabase.GetAssetPath(spriteColPrefab);  // prefab只用来获取路径，不打包不挖空
        if (string.IsNullOrEmpty(path))
        {
            Log.Info("Null Sprite Collection {0}", path);
            return "";   // !!! SpriteCollection可能动态生成的，不打包它
        }
        bool needBuild = BuildTools.CheckNeedBuild(path);
        if (needBuild)
            BuildTools.MarkBuildVersion(path);

        path = __GetPrefabBuildPath(path);

        GameObject copySpriteColObj = GameObject.Instantiate(spriteColPrefab) as GameObject;
        tk2dSpriteCollectionData spriteColData = copySpriteColObj.GetComponent<tk2dSpriteCollectionData>();

        foreach (Material mat in spriteColData.materials) // many materials
        {
            string matPath = BuildDepMaterial(mat, GameDef.PictureScale);
            if (!string.IsNullOrEmpty(matPath))  // 材质可能动态创建的，无需打包
                //CResourceDependencies.Create(spriteColData, CResourceDependencyType.SPRITE_COLLECTION, matPath);
                KAssetDep.Create<CTk2dSpriteCollectionDep>(spriteColData, matPath);
        }

        spriteColData.materials = new Material[0]; // 挖空spriteCollections
        spriteColData.textures = new Texture[0];
        foreach (var def in spriteColData.spriteDefinitions)
        {
            def.material = null;
            // 进行缩放！
            //if (def.positions != null)
            //{
            //    // position!  size!
            //    for (var ip = 0; ip < def.positions.Length; ip++)
            //    {
            //        def.positions[ip] = def.positions[ip] / GameDef.PictureScale;
            //    }
            //    for (var ip = 0; ip < def.untrimmedBoundsData.Length; ip++)
            //    {
            //        def.untrimmedBoundsData[ip] = def.untrimmedBoundsData[ip] / GameDef.PictureScale;
            //    }
            //    for (var ip = 0; ip < def.boundsData.Length; ip++)
            //    {
            //        def.boundsData[ip] = def.boundsData[ip] / GameDef.PictureScale;
            //    }
            //}
        }

        var result = DoBuildAssetBundle(DepBuildToFolder + "/Col_" + path, copySpriteColObj, needBuild);  // Build主对象, 被挖空Material了的

        GameObject.DestroyImmediate(copySpriteColObj);

        return result.Path;
    }


}
#endif