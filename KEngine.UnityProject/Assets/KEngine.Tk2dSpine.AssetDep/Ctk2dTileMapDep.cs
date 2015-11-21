using UnityEngine;
using System.Collections;

#if tk2dTileMap
public class Ctk2dTileMapDep : CAssetDep {

    protected override void DoProcess(string resourcePath)
    {
        ProcessTileMap_LoadSpriteCollection(resourcePath);
    }

    // 讀SpriteCollection...
    protected void ProcessTileMap_LoadSpriteCollection(string path)
    {
        var loader = CTk2dSpriteCollectionDep.LoadSpriteCollection(path, (_obj) =>
        {
            tk2dSpriteCollectionData colData = _obj as tk2dSpriteCollectionData;
            CDebug.Assert(colData);
            if (!IsDestroy)
            {
                var sprite = (tk2dTileMap)DependencyComponent;

                //foreach (tk2dTileMap sprite in gameObject.GetComponents<tk2dTileMap>())
                {
                    sprite.Editor__SpriteCollection = colData;
                    sprite.ForceBuild();
                }

            }
            OnFinishLoadDependencies(gameObject);  // 返回GameObject而已哦
            //else
            //    CDebug.LogWarning("GameObject maybe destroy!");

        });
        ResourceLoaders.Add(loader);
    }

}
#endif