using UnityEngine;
using System.Collections;
using UnityEngine.UI;

//UGUI Text
public class KTextDep : KAssetDep
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
                label.font = _font;
                label.text = label.text + " ";

            }
            OnFinishLoadDependencies(_font);
        });
        this.ResourceLoaders.Add(loader);
    }

    
}
