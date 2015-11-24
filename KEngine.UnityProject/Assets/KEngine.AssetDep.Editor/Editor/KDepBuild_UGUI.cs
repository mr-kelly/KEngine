using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using KEngine;
using KEngine.Editor;

public partial class KDependencyBuild
{
    [DepBuild(typeof(SpriteRenderer))]
    static void ProcessSpriteRenderer(SpriteRenderer renderer)
    {
        if (renderer.sprite != null)
        {
            var spritePath = BuildSprite(renderer.sprite);
            KAssetDep.Create<KSpriteRendererDep>(renderer, spritePath);
            renderer.sprite = null; // 挖空依赖的数据
        }
        else
            Logger.LogWarning("SpriteRenderer null sprite: {0}", renderer.name);
    }

    [DepBuild(typeof(Text))]
    static void ProcessUGUIText(Text text)
    {
        if(text.font != null)
        {

            var fontPath = BuildFont(text.font);
            KAssetDep.Create<KTextDep>(text, fontPath);
            text.font = null; // 挖空依赖的数据
        }
        else
            Logger.LogWarning("UISprite null Atlas: {0}", text.name);
    }

    [DepBuild(typeof(Image))]
    static void ProcessUGUIImage(Image image)
    {
        if (image.sprite != null)
        {
            string spritePath = BuildSprite(image.sprite);
            KAssetDep.Create<KImageDep>(image, spritePath);
            image.sprite = null;
        }
    }
    // Prefab ,  build
    public static string BuildSprite(Sprite sprite)
    {
        if (sprite.packed)
            Logger.LogWarning("Sprite: {0} is packing!!!", sprite.name);
        
        string assetPath = AssetDatabase.GetAssetPath(sprite);
        bool needBuild = KAssetVersionControl.TryCheckNeedBuildWithMeta(assetPath);
        if (needBuild)
            KAssetVersionControl.TryMarkBuildVersion(assetPath);

        string path = __GetPrefabBuildPath(assetPath);
        if (string.IsNullOrEmpty(path))
            Logger.LogWarning("[BuildSprite]不是文件的Texture, 估计是Material的原始Texture?");
        var result = DoBuildAssetBundle("Common/Sprite_" + path, sprite, needBuild);

        return result.Path;
    }

}