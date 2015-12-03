#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KDepBuild_NGUI.cs
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

using System;
using KEngine;
using KEngine.Editor;
using UnityEditor;
using UnityEngine;

public partial class KDependencyBuild
{
    private static float PictureScale = 1f;

    /// <summary>
    /// 打包UIAtlas前处理
    /// </summary>
    public static Action<UIAtlas> BeforeBuildUIAtlasFilter;

    /// <summary>
    ///  打包UIAtlas后处理
    /// </summary>
    public static Action<UIAtlas> AfterBuildUIAtlasFilter;

    // Prefab ,  build
    public static string BuildUIAtlas(UIAtlas atlas)
    {
        var scale = 1f; // TODO: scale read
        GameObject atlasPrefab = PrefabUtility.FindPrefabRoot(atlas.gameObject) as GameObject;
        Logger.Assert(atlasPrefab);
        string path = AssetDatabase.GetAssetPath(atlasPrefab); // prefab只用来获取路径，不打包不挖空
        bool needBuild = KAssetVersionControl.TryCheckNeedBuildWithMeta(path);
        if (needBuild)
            KAssetVersionControl.TryMarkBuildVersion(path);

        Logger.Assert(path);

        path = __GetPrefabBuildPath(path);

        GameObject copyAtlasObj = GameObject.Instantiate(atlasPrefab) as GameObject;

        UIAtlas copyAtlas = copyAtlasObj.GetComponent<UIAtlas>();

        if (BeforeBuildUIAtlasFilter != null)
        {
            BeforeBuildUIAtlasFilter(copyAtlas);
        }

        Material cacheMat = copyAtlas.spriteMaterial;
        string matPath = BuildDepMaterial(cacheMat, scale); // 缩放

        // 缩放
        copyAtlas.pixelSize = 1/PictureScale;
        foreach (var spriteData in copyAtlas.spriteList)
        {
            spriteData.x = Mathf.FloorToInt(spriteData.x*PictureScale);
            spriteData.y = Mathf.FloorToInt(spriteData.y*PictureScale);
            spriteData.width = Mathf.FloorToInt(spriteData.width*PictureScale);
            spriteData.height = Mathf.FloorToInt(spriteData.height*PictureScale);
            spriteData.borderLeft = Mathf.FloorToInt(spriteData.borderLeft*PictureScale);
            spriteData.borderRight = Mathf.FloorToInt(spriteData.borderRight*PictureScale);
            spriteData.borderTop = Mathf.FloorToInt(spriteData.borderTop*PictureScale);
            spriteData.borderBottom = Mathf.FloorToInt(spriteData.borderBottom*PictureScale);
            // padding 不变， ngui bug
            spriteData.paddingBottom = Mathf.FloorToInt(spriteData.paddingBottom*PictureScale);
            spriteData.paddingTop = Mathf.FloorToInt(spriteData.paddingTop*PictureScale);
            spriteData.paddingLeft = Mathf.FloorToInt(spriteData.paddingLeft*PictureScale);
            spriteData.paddingRight = Mathf.FloorToInt(spriteData.paddingRight*PictureScale);
        }

        KAssetDep.Create<KUIAtlasDep>(copyAtlas, matPath);

        copyAtlas.spriteMaterial = null; // 挖空atlas

        var result = DoBuildAssetBundle("UIAtlas/UIAtlas_" + path, copyAtlasObj, needBuild); // Build主对象, 被挖空Material了的

        if (AfterBuildUIAtlasFilter != null)
        {
            AfterBuildUIAtlasFilter(copyAtlas);
        }
        GameObject.DestroyImmediate(copyAtlasObj);

        return result.Path;
    }


    [DepBuild(typeof (UISprite))]
    private static void ProcessUISprite(UISprite sprite)
    {
        if (sprite.atlas != null)
        {
            string atlasPath = BuildUIAtlas(sprite.atlas);
            //CResourceDependencies.Create(sprite, CResourceDependencyType.UI_SPRITE, atlasPath);
            KAssetDep.Create<KUISpriteDep>(sprite, atlasPath);
            sprite.atlas = null;
        }
        else
            Logger.LogWarning("UISprite null Atlas: {0}", sprite.name);
    }

    /// <summary>
    /// NGUI 的字体集
    /// </summary>
    private static string BuildUIFont(UIFont uiFont)
    {
        if (uiFont.atlas == null)
        {
            Logger.LogError("[BuildUIFont]uiFont Null Atlas: {0}", uiFont.name);
            return "";
        }
        string uiFontPrefabPath = AssetDatabase.GetAssetPath(uiFont.gameObject);
        bool needBuild = KAssetVersionControl.TryCheckNeedBuildWithMeta(uiFontPrefabPath);
        if (needBuild)
            KAssetVersionControl.TryMarkBuildVersion(uiFontPrefabPath);

        var copyUIFontObj = GameObject.Instantiate(uiFont.gameObject) as GameObject;
        var copyUIFont = copyUIFontObj.GetComponent<UIFont>();

        var uiAtlas = BuildUIAtlas(copyUIFont.atlas); // 依赖的UI Atlas
        copyUIFont.atlas = null; // 清空依赖
        copyUIFont.material = null;
        //CResourceDependencies.Create(copyUIFont, CResourceDependencyType.NGUI_UIFONT, uiAtlas);
        KAssetDep.Create<KUIFontDep>(copyUIFont, uiAtlas);

        var result = DoBuildAssetBundle("UIFont/UIFont_" + uiFont.name, copyUIFontObj, needBuild);

        GameObject.DestroyImmediate(copyUIFontObj);

        return result.Path;
    }


    [DepBuild(typeof (UITexture))]
    private static void ProcessUITexture(UITexture tex)
    {
        if (tex.mainTexture != null)
        {
            string texPath = BuildDepTexture(tex.mainTexture, 1f);
            //CResourceDependencies.Create(tex, CResourceDependencyType.UI_TEXTURE, texPath);
            KAssetDep.Create<KUITextureDep>(tex, texPath);
            tex.mainTexture = null; // 挖空依赖的数据

            // UITexture的有bug，强行缩放有问题的！
            tex.border *= KResourceModule.TextureScale;
        }
        else
        {
            //Logger.Log("缺少Texture的UiTexture: {0}", tex.name);
        }
    }

    [DepBuild(typeof (UILabel))]
    private static void ProcessUILabel(UILabel label)
    {
        // 图片字体！ 打包字
        if (label.bitmapFont != null)
        {
            string uiFontPath = BuildUIFont(label.bitmapFont);
            //CResourceDependencies.Create(label, CResourceDependencyType.BITMAP_FONT, uiFontPath);
            KAssetDep.Create<KBitmapFontDep>(label, uiFontPath);

            label.bitmapFont = null;
        }
        else if (label.trueTypeFont != null)
        {
            string fontPath = BuildFont(label.trueTypeFont);

            //CResourceDependencies.Create(label, CResourceDependencyType.FONT, fontPath);
            KAssetDep.Create<KUILabelDep>(label, fontPath);
            label.trueTypeFont = null; // 挖空依赖的数据
        }
        else
        {
            Logger.LogWarning("找不到Label的字体: {0}", label.name);
        }
    }
}