using UnityEngine;
using System.Collections;
using KEngine;
public class CUIFontDep : CAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessUIFont(resourcePath);
    }

    protected void ProcessUIFont(string resPath)
    {
        var loader = CUIAtlasDep.LoadUIAtlas(resPath, atlas =>
        {
            if (!IsDestroy)
            {
                Logger.Assert(atlas);

                UIFont uiFont = DependencyComponent as UIFont;
                Logger.Assert(uiFont);
                //foreach (UIFont uiFont in this.gameObject.GetComponents<UIFont>())
                {
                    uiFont.atlas = atlas;
                    uiFont.material = atlas.spriteMaterial;
                }

            }
            OnFinishLoadDependencies(gameObject);  // 返回GameObject而已哦
        });
        ResourceLoaders.Add(loader);
    }
}
