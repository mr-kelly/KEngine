#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KUGUIBridge.cs
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

using System.Collections;
using KEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Unity原生UI桥接器
/// </summary>
public class KUGUIBridge : IKUIBridge
{
    public EventSystem eventSystem;
    // Init the UI Bridge, necessary
    public void InitBridge()
    {
        eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
        eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        eventSystem.gameObject.AddComponent<TouchInputModule>();
    }

    // Get a component inside the UI Bridge
    public object GetUIComponent(string comName)
    {
        return null;
    }

    public void UIObjectFilter(KUIController ui, GameObject uiObject)
    {
    }

    public IEnumerator LoadUIAsset(CUILoadState loadState, UILoadRequest request)
    {
        string path = string.Format("UI/{0}_UI{1}", loadState.TemplateName, KEngine.AppEngine.GetConfig("AssetBundleExt"));
        var assetLoader = KStaticAssetLoader.Load(path);
        loadState.UIResourceLoader = assetLoader; // 基本不用手工释放的
        while (!assetLoader.IsCompleted)
            yield return null;

        request.Asset = assetLoader.TheAsset;

    }
}