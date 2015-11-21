using System;
using UnityEngine;
using System.Collections;
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
        Logger.Assert(colData);

        var colDep = colObj.GetComponent<KAssetDep>();

        if (!(colDep && colDep.GetType() == typeof (CTk2dSpriteCollectionDep))) // 依赖材质Material, 加载后是Material
        {
            Logger.LogError("Wrong Collection DepType - {0}", resPath);    
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