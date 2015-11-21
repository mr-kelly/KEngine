using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using KEngine;
public class KImageDep : KAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        var loader = KSpriteLoader.Load(resourcePath, (isOk, sprite) =>
        {
            if (!IsDestroy)
            {
                var image = DependencyComponent as Image;
                Logger.Assert(image);
                image.sprite = sprite;
            }
            OnFinishLoadDependencies(gameObject);  // 返回GameObject而已哦
        });

        this.ResourceLoaders.Add(loader);
    }
}