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
#if NGUI
using System;
using System.Collections.Generic;
using KEngine;
using KEngine.Editor;
using UnityEditor;
using UnityEngine;

namespace KEngine.AssetDep.Editor
{
    [DepBuildClass(typeof(UILabel))]
    public class KDepBuild_UILabel : IDepBuildProcessor
    {
        public void Process(Component @object)
        {
            var label = (UILabel)@object;
            // 图片字体！ 打包字
            if (label.bitmapFont != null)
            {
                string uiFontPath = KDepBuild_NGUI.BuildUIFont(label.bitmapFont);
                //CResourceDependencies.Create(label, CResourceDependencyType.BITMAP_FONT, uiFontPath);
                KAssetDep.Create<KBitmapFontDep>(label, uiFontPath);

                label.bitmapFont = null;
            }
            else if (label.trueTypeFont != null)
            {
                string fontPath = KDependencyBuild.BuildFont(label.trueTypeFont);

                //CResourceDependencies.Create(label, CResourceDependencyType.FONT, fontPath);
                KAssetDep.Create<KUILabelDep>(label, fontPath);
                label.trueTypeFont = null; // 挖空依赖的数据
            }
            else
            {
                Log.Warning("找不到Label的字体: {0}, 场景: {1}", label.name, EditorApplication.currentScene);
            }
        }
    }

    [DepBuildClass(typeof(UISprite))]
    public class KDepBuild_UISprite : IDepBuildProcessor
    {
        public void Process(Component @object)
        {
            var sprite = (UISprite)@object;
            if (sprite.atlas != null)
            {
                string atlasPath = KDepBuild_NGUI.BuildUIAtlas(sprite.atlas);
                //CResourceDependencies.Create(sprite, CResourceDependencyType.UI_SPRITE, atlasPath);
                KAssetDep.Create<KUISpriteDep>(sprite, atlasPath);
                sprite.atlas = null;
            }
            else
                Log.Warning("UISprite null Atlas: {0}, Scene: {1}", sprite.name, EditorApplication.currentScene);
        }
    }


    [DepBuildClass(typeof(UITexture))]
    public class KDepBuild_UITexture : IDepBuildProcessor
    {
        public void Process(Component @object)
        {
            var tex = (UITexture)@object;
            if (tex.mainTexture != null)
            {
                string texPath = KDependencyBuild.BuildDepTexture(tex.mainTexture, 1f);
                //CResourceDependencies.Create(tex, CResourceDependencyType.UI_TEXTURE, texPath);
                KAssetDep.Create<KUITextureDep>(tex, texPath);
                tex.mainTexture = null; // 挖空依赖的数据

                // UITexture的有bug，强行缩放有问题的！
                tex.border *= KResourceModule.TextureScale;
            }
            else
            {
                //Log.Info("缺少Texture的UiTexture: {0}", tex.name);
            }
        }
    }

    public class KDepBuild_NGUI
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

        /// <summary>
        /// 图集 打包结果缓存起来加速
        /// </summary>
        /// <param name="atlas"></param>
        /// <returns></returns>
        public static string BuildUIAtlas(UIAtlas atlas)
        {
            CDepCollectInfo result;
            // 使用缓存，确保Atlas不会重复处理，浪费性能
            if (KDepCollectInfoCaching.HasCache(atlas))
            {
                result = KDepCollectInfoCaching.GetCache(atlas);
                return result.Path;
            }
            var scale = 1f; // TODO: scale read
            GameObject atlasPrefab = PrefabUtility.FindPrefabRoot(atlas.gameObject) as GameObject;
            Log.Assert(atlasPrefab);
            string path = AssetDatabase.GetAssetPath(atlasPrefab); // prefab只用来获取路径，不打包不挖空
            bool needBuild = KAssetVersionControl.TryCheckNeedBuildWithMeta(path);
            if (needBuild)
                KAssetVersionControl.TryMarkBuildVersion(path);

            Log.Assert(path);

            path = KDependencyBuild.__GetPrefabBuildPath(path);

            GameObject copyAtlasObj = GameObject.Instantiate(atlasPrefab) as GameObject;

            UIAtlas copyAtlas = copyAtlasObj.GetComponent<UIAtlas>();

            if (BeforeBuildUIAtlasFilter != null)
            {
                BeforeBuildUIAtlasFilter(copyAtlas);
            }

            Material cacheMat = copyAtlas.spriteMaterial;
            string matPath = KDepBuild_Material.BuildDepMaterial(cacheMat, scale); // 缩放

            // 缩放
            copyAtlas.pixelSize = 1 / PictureScale;
            foreach (var spriteData in copyAtlas.spriteList)
            {
                spriteData.x = Mathf.FloorToInt(spriteData.x * PictureScale);
                spriteData.y = Mathf.FloorToInt(spriteData.y * PictureScale);
                spriteData.width = Mathf.FloorToInt(spriteData.width * PictureScale);
                spriteData.height = Mathf.FloorToInt(spriteData.height * PictureScale);
                spriteData.borderLeft = Mathf.FloorToInt(spriteData.borderLeft * PictureScale);
                spriteData.borderRight = Mathf.FloorToInt(spriteData.borderRight * PictureScale);
                spriteData.borderTop = Mathf.FloorToInt(spriteData.borderTop * PictureScale);
                spriteData.borderBottom = Mathf.FloorToInt(spriteData.borderBottom * PictureScale);
                // padding 不变， ngui bug
                spriteData.paddingBottom = Mathf.FloorToInt(spriteData.paddingBottom * PictureScale);
                spriteData.paddingTop = Mathf.FloorToInt(spriteData.paddingTop * PictureScale);
                spriteData.paddingLeft = Mathf.FloorToInt(spriteData.paddingLeft * PictureScale);
                spriteData.paddingRight = Mathf.FloorToInt(spriteData.paddingRight * PictureScale);
            }

            KAssetDep.Create<KUIAtlasDep>(copyAtlas, matPath);

            copyAtlas.spriteMaterial = null; // 挖空atlas

            result = KDependencyBuild.DoBuildAssetBundle("UIAtlas/UIAtlas_" + path, copyAtlasObj, needBuild); // Build主对象, 被挖空Material了的

            if (AfterBuildUIAtlasFilter != null)
            {
                AfterBuildUIAtlasFilter(copyAtlas);
            }
            GameObject.DestroyImmediate(copyAtlasObj);

            KDepCollectInfoCaching.SetCache(atlas, result);
            return result.Path;
        }

        /// <summary>
        /// NGUI 的字体集
        /// </summary>
        public static string BuildUIFont(UIFont uiFont)
        {
            CDepCollectInfo result;
            if (KDepCollectInfoCaching.HasCache(uiFont))
            {
                result = KDepCollectInfoCaching.GetCache(uiFont);
                return result.Path;
            }
            if (uiFont.atlas == null)
            {
                Log.Error("[BuildUIFont]uiFont Null Atlas: {0}, Scene: {1}", uiFont.name, EditorApplication.currentScene);
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

            result = KDependencyBuild.DoBuildAssetBundle("UIFont/UIFont_" + uiFont.name, copyUIFontObj, needBuild);

            GameObject.DestroyImmediate(copyUIFontObj);
            KDepCollectInfoCaching.SetCache(uiFont, result);
            return result.Path;
        }

    }

}
#endif