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

[DepBuildClass(typeof(SpriteRenderer))]
public class KDepBuild_SpriteRenderer : IDepBuildProcessor
{
    public void Process(Component @object)
    {
        var renderer = @object as SpriteRenderer;

        if (renderer.sprite != null)
        {
            var spritePath = KDepBuild_UGUI.BuildSprite(renderer.sprite);
            KAssetDep.Create<KSpriteRendererDep>(renderer, spritePath);
            renderer.sprite = null; // 挖空依赖的数据
        }
        else
            KLogger.LogWarning("SpriteRenderer null sprite: {0}", renderer.name);
    }
}

[DepBuildClass(typeof(Text))]
public class KDepBuild_Text : IDepBuildProcessor
{
    public void Process(Component @object)
    {
        var text = @object as Text;
        if (text.font != null)
        {
            var fontPath = KDependencyBuild.BuildFont(text.font);
            KAssetDep.Create<KTextDep>(text, fontPath);
            text.font = null; // 挖空依赖的数据
        }
        else
            KLogger.LogWarning("UISprite null Atlas: {0}", text.name); ;
    }
}

[DepBuildClass(typeof(Image))]
public class KDepBuild_Image : IDepBuildProcessor
{
    public void Process(Component @object)
    {
        var image = @object as Image;
        if (image.sprite != null)
        {
            string spritePath = KDepBuild_UGUI.BuildSprite(image.sprite);
            KAssetDep.Create<KImageDep>(image, spritePath);
            image.sprite = null;
        }
    }
}

public class KDepBuild_UGUI
{
    // Prefab ,  build
    public static string BuildSprite(Sprite sprite)
    {
        if (sprite.packed)
            KLogger.LogWarning("Sprite: {0} is packing!!!", sprite.name);

        string assetPath = AssetDatabase.GetAssetPath(sprite);
        bool needBuild = KAssetVersionControl.TryCheckNeedBuildWithMeta(assetPath);
        if (needBuild)
            KAssetVersionControl.TryMarkBuildVersion(assetPath);

        string path = KDependencyBuild.__GetPrefabBuildPath(assetPath);
        if (string.IsNullOrEmpty(path))
            KLogger.LogWarning("[BuildSprite]不是文件的Texture, 估计是Material的原始Texture?");
        var result = KDependencyBuild.DoBuildAssetBundle("Common/Sprite_" + path, sprite, needBuild);

        return result.Path;
    }
}