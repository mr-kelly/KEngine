using UnityEngine;
using System.Collections;
#if TK2D
public class CTk2dSpriteDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessTk2dSprite(resourcePath);
    }

    protected void ProcessTk2dSprite(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            Logger.LogError("[ProcessTk2dSprite]Null ResourcePath {0}", gameObject.name);
            return;
        }

        var loader = CTk2dSpriteCollectionDep.LoadSpriteCollection(resourcePath, (_obj) =>
        {
            tk2dSpriteCollectionData colData = _obj as tk2dSpriteCollectionData;
            Logger.Assert(colData);
            if (!IsDestroy)
            {
                Logger.Assert(DependencyComponent is tk2dBaseSprite);
                tk2dBaseSprite sprite = (tk2dBaseSprite)DependencyComponent;
                sprite.Collection = colData;
                sprite.Build();

            }
            OnFinishLoadDependencies(colData);  // 返回GameObject而已哦
            //else
            //    Logger.LogWarning("GameObject maybe destroy!");
        });
        ResourceLoaders.Add(loader);
    }

}
#endif