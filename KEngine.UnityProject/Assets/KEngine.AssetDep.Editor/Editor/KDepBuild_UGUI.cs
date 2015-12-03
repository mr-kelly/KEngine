#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KDepBuild_UGUI.cs
// Date:     2015/12/03
// Author:  Kelly
// Email: 23110388@qq.com
// Github: https://github.com/mr-kelly/KEngine
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.

#endregion

using KEngine;
using KEngine.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public partial class KDependencyBuild
{
    [DepBuild(typeof (SpriteRenderer))]
    private static void ProcessSpriteRenderer(SpriteRenderer renderer)
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

    [DepBuild(typeof (Text))]
    private static void ProcessUGUIText(Text text)
    {
        if (text.font != null)
        {
            var fontPath = BuildFont(text.font);
            KAssetDep.Create<KTextDep>(text, fontPath);
            text.font = null; // 挖空依赖的数据
        }
        else
            Logger.LogWarning("UISprite null Atlas: {0}", text.name);
    }

    [DepBuild(typeof (Image))]
    private static void ProcessUGUIImage(Image image)
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