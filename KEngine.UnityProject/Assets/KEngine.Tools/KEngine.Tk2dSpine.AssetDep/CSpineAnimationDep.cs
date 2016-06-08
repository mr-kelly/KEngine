#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CSpineAnimationDep.cs
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
public class CSpineAnimationDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessSpineAnimation(resourcePath);
    }

    protected void LoadSpineDataAsset(string path, Action<SkeletonDataAsset> callback)
    {
        var loader = CStaticAssetLoader.Load(path, (_isOk, _obj) =>
        {
            SkeletonDataAsset dataAsset = _obj as SkeletonDataAsset;
            Log.Assert(dataAsset);
            dataAsset.name = path;
            callback(dataAsset);

        });

        ResourceLoaders.Add(loader);
    }

    // CSpineData 加载完后，在读取依赖的SkeletonDataAsset和SpriteCollection
    protected void LoadCSpineData(string path, Action<SkeletonDataAsset> dataCallback)
    {
        var loader = CStaticAssetLoader.Load(path, (isOk, obj) =>
        {
            //string resourcePath = args[0] as string;
            //Action<SkeletonDataAsset> externalCallback = args[1] as Action<SkeletonDataAsset>;
            if (isOk)
            {
                CSpineData spineData = obj as CSpineData;

                LoadSpineDataAsset(spineData.DataAssetPath, (SkeletonDataAsset dataAsset) =>
                {
                    var loader2 = CTk2dSpriteCollectionDep.LoadSpriteCollection(spineData.SpriteCollectionPath, (_obj) =>
                    {
                        tk2dSpriteCollectionData colData = _obj as tk2dSpriteCollectionData;
                        Log.Assert(colData);

                        dataAsset.spriteCollection = colData;

                        dataCallback(dataAsset);
                    });
                    ResourceLoaders.Add(loader2);
                });
            }
            else
            {
                Log.Warning("[CSpineAnimationDep:LoadCSpineData] Not Ok {0}", path);
                dataCallback(null);
            }
        });
        ResourceLoaders.Add(loader);
    }

    // 处理动画
    protected void ProcessSpineAnimation(string resourcePath)
    {
        //gameObject.SetActive(false);  // 故意关闭状态，SkeletonAnimation不报错
        LoadCSpineData(resourcePath, (SkeletonDataAsset _data) =>
        {
            if (!IsDestroy)
            {
                //SkeletonAnimation anim = gameObject.GetComponent<SkeletonAnimation>();
                SkeletonAnimation anim = (SkeletonAnimation)DependencyComponent;
                anim.timeScale = CGame.TimeScale;  // 動作加載完畢后，匹配上系統遊戲速度
                anim.skeletonDataAsset = _data;
                try
                {
                    anim.Reset();
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                    Debug.LogError(string.Format("[ProcessSpineAnimation] {0}", resourcePath), this);
                }


                //gameObject.SetActive(true);  // SkeletonAnimation配置好重新开启
                OnFinishLoadDependencies(anim);
            }
            else
                OnFinishLoadDependencies(null);


        });
        //gameObject.name = resourcePath;
    }

}
#endif