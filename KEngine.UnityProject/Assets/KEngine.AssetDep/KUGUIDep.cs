using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class KUGUIDep : KAssetDep
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
                var label = DependencyComponent as Text;
                //foreach (UILabel label in gameObject.GetComponents<UILabel>())
                {
                    label.font = _font;
                }
            }
            OnFinishLoadDependencies(_font);
        });
        this.ResourceLoaders.Add(loader);
    }

}
