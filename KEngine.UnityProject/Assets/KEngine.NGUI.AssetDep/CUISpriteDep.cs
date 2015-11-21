using UnityEngine;
using System.Collections;
using KEngine;
public class CUISpriteDep : CAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessUISprite(resourcePath);
    }

    protected void ProcessUISprite(string resourcePath)
    {
        var loader = CUIAtlasDep.LoadUIAtlas(resourcePath, (atlas) =>
        {
            if (!IsDestroy)
            {
                //UIAtlas atlas = _obj as UIAtlas;
                Logger.Assert(atlas);

                Logger.Assert(DependencyComponent);
                var sprite = DependencyComponent as UISprite;

                Logger.Assert(sprite);
                sprite.atlas = atlas;

                //对UISpriteAnimation处理
                foreach (UISpriteAnimation spriteAnim in this.gameObject.GetComponents<UISpriteAnimation>())
                {
                    spriteAnim.RebuildSpriteList();
                }

            }
            OnFinishLoadDependencies(gameObject);  // 返回GameObject而已哦
        });
        ResourceLoaders.Add(loader);
    }

}
