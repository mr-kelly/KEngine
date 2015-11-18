using System;
using UnityEngine;
using System.Collections;

#if SPINE_ANIMATION
public class CSpineAnimationDep : CAssetDep
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
            Logger.Assert(dataAsset);
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
                        Logger.Assert(colData);

                        dataAsset.spriteCollection = colData;

                        dataCallback(dataAsset);
                    });
                    ResourceLoaders.Add(loader2);
                });
            }
            else
            {
                Logger.LogWarning("[CSpineAnimationDep:LoadCSpineData] Not Ok {0}", path);
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
                    Logger.LogError(e.Message);
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