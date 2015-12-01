using UnityEngine;
using System.Collections;
using KEngine;
public class KBitmapFontDep : KAssetDep
{

    protected override void DoProcess(string resourcePath)
    {
        ProcesBitMapFont(resourcePath);
    }

    // UILabel...  Bitmap Font
    protected void ProcesBitMapFont(string resPath)
    {
        // Load UIFont Prefab
        var loader = KStaticAssetLoader.Load(resPath, (isOk, o) =>
        {
            if (!IsDestroy)
            {
                var uiFontPrefab = (GameObject)o;
                Logger.Assert(uiFontPrefab);

                uiFontPrefab.transform.parent = DependenciesContainer.transform;

                var uiFont = uiFontPrefab.GetComponent<UIFont>();
                Logger.Assert(uiFont);
                var label = DependencyComponent as UILabel;
                //foreach (UILabel label in gameObject.GetComponents<UILabel>())
                {
                    label.bitmapFont = uiFont;
                }
                OnFinishLoadDependencies(uiFont);
            }
            else
                OnFinishLoadDependencies(null);

        });
        ResourceLoaders.Add(loader);
    }

}
