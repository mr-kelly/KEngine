using System;
using System.Collections.Generic;
using UnityEngine;
using KEngine;
// Sprite Renderer
public class KSpriteRendererDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        var loader = KSpriteLoader.Load(resourcePath, (isOk, sprite) =>
        {
            if (!IsDestroy)
            {
                var spriteRenderer = DependencyComponent as SpriteRenderer;
                Logger.Assert(spriteRenderer);
                spriteRenderer.sprite = sprite;
            }
            OnFinishLoadDependencies(gameObject);  // 返回GameObject而已哦
        });

        this.ResourceLoaders.Add(loader);
    }
}