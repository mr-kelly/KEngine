using System;
using UnityEngine;
using System.Collections;
using KEngine;
public class KUITextureDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessUITexture(resourcePath);
    }

    protected void ProcessUITexture(string resourcePath)
    {
        LoadTexture(resourcePath, (_tex) =>
        {
            if (!IsDestroy)
            {
                Logger.Assert(DependencyComponent is UITexture);
                var uiTex = (UITexture) DependencyComponent;
                uiTex.mainTexture = _tex;
                // different pixelSize !   uiTex.pixelSize = GameDef.PictureScale;
                
            }
            OnFinishLoadDependencies(gameObject);  // 返回GameObject而已哦

        });
    }

    protected void LoadTexture(string texPath, Action<Texture> exCallback = null)
    {
        TexturesWaitLoadCount++;
        var texLoader = KTextureLoader.Load(texPath, (isOk, tex) =>
        {
            if (!isOk)
            {
                Logger.LogError("无法加载依赖图片: {0}", texPath);
            }

            if (exCallback != null)
                exCallback(tex);

            TexturesWaitLoadCount--;
            if (TexturesWaitLoadCount <= 0)
            {
                foreach (var c in TexturesLoadedCallback)
                {
                    c();
                }
                TexturesLoadedCallback.Clear();
            }
        });
        ResourceLoaders.Add(texLoader);
    }
}
