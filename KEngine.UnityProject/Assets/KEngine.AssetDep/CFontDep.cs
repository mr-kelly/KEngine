using UnityEngine;
using System.Collections;

public class CFontDep : CAssetDep
{
    protected override void DoProcess(string resourcePath)
    {
        ProcessFont(resourcePath);
    }

    protected void ProcessFont(string resPath)
    {
        var loader = KFontLoader.Load(resPath, (isOk, _font) =>
        {
            if (!IsDestroy)
            {
                var label = DependencyComponent as UILabel;
                //foreach (UILabel label in gameObject.GetComponents<UILabel>())
                {
                    label.trueTypeFont = _font;
                }
            }
            OnFinishLoadDependencies(_font);
        });
        this.ResourceLoaders.Add(loader);
    }

}
