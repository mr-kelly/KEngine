#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CDepBuild_Spine.cs
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


#if SPINE_ANIMATION
public partial class KDependencyBuild
{

    // 包含json！，不包含圖集
    static CDepCollectInfo GetBuildSpineData(SkeletonDataAsset data)
    {
        string path = AssetDatabase.GetAssetPath(data);

        // DataAsset
        bool needBuildDataAsset = BuildTools.CheckNeedBuild(path);
        if (needBuildDataAsset)
            BuildTools.MarkBuildVersion(path);

        // Spine的JSON
        string textAssetPath = AssetDatabase.GetAssetPath(data.skeletonJSON);
        bool needBuildJsonTextAsset = BuildTools.CheckNeedBuild(textAssetPath);
        if (needBuildJsonTextAsset)
            BuildTools.MarkBuildVersion(textAssetPath);

        //string originPath = path;
        //string tmpPath = "Assets/~TempSkeletonDataAsset.asset";
        //bool copyResult = AssetDatabase.CopyAsset(path, tmpPath);
        //Log.Assert(copyResult);
        //SkeletonDataAsset copyData = AssetDatabase.LoadAssetAtPath(tmpPath, typeof(SkeletonDataAsset)) as SkeletonDataAsset;
        if (data.spriteCollection == null || data.skeletonJSON == null)
        {
            Log.Error("Err Spine Data: {0}, Lack of SpriteCollection or Json", data.name);
            //return "";
        }

        string spriteColPath = BuildSpriteCollection(data.spriteCollection);
        string spriteColAssetPath = AssetDatabase.GetAssetPath(data.spriteCollection.gameObject);
        bool needBuildSpriteCol = BuildTools.CheckNeedBuild(spriteColAssetPath);
        if (needBuildSpriteCol)
            BuildTools.MarkBuildVersion(spriteColAssetPath);

        SkeletonDataAsset copyData = GameObject.Instantiate(data) as SkeletonDataAsset;
        copyData.spriteCollection = null; // 挖空图集, 保留Json!


        // SpineData包括了这个SkeletonData!
        var skeletonDataBuildResult = __DoBuildScriptableObject(DepBuildToFolder + "/SkeletonData_" + data.name, copyData, needBuildDataAsset || needBuildJsonTextAsset);  // json文件直接放在SkeletonDataAsset打包！ 分离图集

        CSpineData spineData = ScriptableObject.CreateInstance<CSpineData>();
        spineData.SpriteCollectionPath = spriteColPath;
        spineData.DataAssetPath = skeletonDataBuildResult.Path; // 保留json文件，不挖空 copyData.skeletonJSON

        path = __GetPrefabBuildPath(path);
        // DataAsset或圖集或Json任一重打包了，都要重新打包CSpineData(記錄圖集保存地方和Jsondataasset保存地方)
        var spineDataBuildResult = __DoBuildScriptableObject(DepBuildToFolder + "/SpineData_" + path, spineData, needBuildDataAsset || needBuildSpriteCol || needBuildJsonTextAsset);
        spineDataBuildResult.Child = skeletonDataBuildResult;

        GameObject.DestroyImmediate(copyData);

        return spineDataBuildResult;
    }

    [DepBuild(typeof(SkeletonAnimation))]
    static CDepCollectInfo ProcessSkeletonAnimation(SkeletonAnimation sa)
    {
        if (sa.skeletonDataAsset == null)
        {
            Log.Error("SkeletonAnimation {0}缺少DataAsset，无法打包", sa.gameObject.name);
            return null;
        }
        else
        {
            if (sa.skeletonDataAsset.spriteCollection == null)
            {
                Log.Error("SkeletonDataAsset {0}缺少SpriteCollection，无法打包", sa.skeletonDataAsset.name);
                return null;
            }
            if (sa.skeletonDataAsset.skeletonJSON == null)
            {
                Log.Error("SkeletonDataAsset {0}缺少Json，无法打包", sa.skeletonDataAsset.name);
                return null;
            }

            var spineDataResult = GetBuildSpineData(sa.skeletonDataAsset);  // 依赖SpineData
            //CResourceDependencies.Create(sa, CResourceDependencyType.SPINE_ANIMATION, spineDataPath);
            spineDataResult.AssetDep = KAssetDep.Create<CSpineAnimationDep>(sa, spineDataResult.Path);

            sa.skeletonDataAsset = null; // 挖空依赖的数据

            // 如果存在MeshFilter和MeshRenderer也挖空... 理论上skeletonDataAsset变空会改变，但这里可能存在对象为disable导致无法自动挖空
            var mesh = sa.gameObject.GetComponent<MeshFilter>();
            if (mesh != null)
                mesh.sharedMesh = null;
            var meshRend = sa.gameObject.GetComponent<MeshRenderer>();
            if (meshRend != null)
                meshRend.sharedMaterial = null; // 挖空

            return spineDataResult;
        }
    }

}
#endif