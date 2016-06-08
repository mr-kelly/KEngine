#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KNGUIBridge.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using KEngine;
using KEngine.ResourceDep;
using KEngine.UI;
using UnityEngine;

internal class GameDef
{
    public const float PictureScale = 1f;
    public const int ScreenPixelY = 1080;
    public const int ScreenPixelX = 1920;
    public const int DefaultPixelPerMeters = 100;
}

public class KNGUIBridge : IUIBridge
{
    public UICamera UiCamera;
    public UIRoot UiRoot;
    public UIPanel PanelRoot;
    public UIWidget PressWidget; // 只要PressUI,立刻启动这个遮罩！预防穿透到场景

    // new UI
    //private Canvas UICanvas;

    private readonly Dictionary<string, Transform> AnchorSide = new Dictionary<string, Transform>();

    public static KNGUIBridge Instance;

    public void InitBridge()
    {
        Instance = this;
        CreateUIRoot();
        //CreateUGUI();

        // 全局事件系统标记标准UI
#if GAME_CLIENT
        CUIModule.OnOpenEvent += (ui) => CActionRecords.Mark(ActionRecordsType.OpenWindow, ui.UITemplateName);
        CUIModule.OnCloseEvent += (ui) =>
        {
            if (CBehaviour.IsApplicationQuited)
            {
                return;
            }
            CActionRecords.Mark(ActionRecordsType.CloseWindow, ui.UITemplateName);

            //CUINavController.AutoReleaseAssetDep(ui.CachedGameObject);
        };
#endif
    }

    public UIController CreateUIController(GameObject uiObj, string uiTemplateName)
    {
        UIController uiBase = uiObj.AddComponent("KUI" + uiTemplateName) as UIController;
        KEngine.Debuger.Assert(uiBase);
        return uiBase;
    }
    public void UIObjectFilter(UIController ui, GameObject uiObj)
    {
        //if (ui is IUGUIWindow)
        //{
        //    KTool.SetChild(uiObj, UICanvas.gameObject);// 放进UICanvas
        //    KTool.SetLayer(uiObj, (int)CLayerDef.UI);
        //}
        //else
        {
            uiObj.transform.parent = AnchorSide[UIAnchor.Side.Center.ToString()];
            uiObj.transform.localPosition = Vector3.zero;
            uiObj.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    public IEnumerator LoadUIAsset(CUILoadState openState, UILoadRequest request)
    {
        var name = openState.TemplateName;
        // 具体加载逻辑
        // manifest
        string manifestPath = ResourceDepUtils.GetBuildPath(string.Format("BundleResources/NGUI/{0}.prefab.manifest{1}", name,
            AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt)));
        var manifestLoader = KBytesLoader.Load(manifestPath, KResourceInAppPathType.PersistentAssetsPath, KAssetBundleLoaderMode.PersitentDataPathSync);
        while (!manifestLoader.IsCompleted)
            yield return null;
        var manifestBytes = manifestLoader.Bytes;
        manifestLoader.Release(); // 释放掉文本字节
        var utf8NoBom = new UTF8Encoding(false);
        var manifestList = utf8NoBom.GetString(manifestBytes).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < manifestList.Length; i++)
        {
            var depPath = manifestList[i] + AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleExt);
            var depLoader = KAssetFileLoader.Load(depPath);
            while (!depLoader.IsCompleted)
            {
                yield return null;
            }

        }
        string path = ResourceDepUtils.GetBuildPath(string.Format("BundleResources/NGUI/{0}.prefab{1}", name, KEngine.AppEngine.GetConfig("AssetBundleExt")));

        var assetLoader = KStaticAssetLoader.Load(path);
        openState.UIResourceLoader = assetLoader; // 基本不用手工释放的
        while (!assetLoader.IsCompleted)
            yield return null;
        request.Asset = assetLoader.TheAsset;
    }

    public object GetUIComponent(string comName)
    {
        if (comName == "Camera")
            return UiCamera;

        Debuger.Assert(false);
        return null;
    }

    //void CreateUGUI()
    //{
    //    var canvasObj = new GameObject("UICanvas");
    //    UICanvas = canvasObj.AddComponent<Canvas>();
    //    UICanvas.renderMode = RenderMode.ScreenSpaceOverlay;

    //    //UICanvas.worldCamera = UiCamera.cachedCamera;
    //    canvasObj.AddComponent<GraphicRaycaster>();
    //    var scaler = canvasObj.AddComponent<CanvasScaler>();
    //    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;  // 屏幕固定大小
    //    scaler.referenceResolution = new Vector2(1080, 1920);

    //    var evtSysObj = new GameObject("EventSystem");
    //    KTool.SetChild(evtSysObj, canvasObj);
    //    evtSysObj.AddComponent<EventSystem>();
    //}

    private void CreateUIRoot()
    {
        GameObject uiRootobj = GameObject.Find("NGUIRoot") ?? new GameObject("NGUIRoot");
        UiRoot = uiRootobj.GetComponent<UIRoot>() ?? uiRootobj.AddComponent<UIRoot>();
        Debuger.Assert(UiRoot);
        UiRoot.scalingStyle = UIRoot.Scaling.FixedSizeOnMobiles;

        // 尝试将NGUI转化成跟2dToolkit镜头一致显示
        UiRoot.manualHeight = 1080;
        //GameDef.ScreenPixelY;//(int)(GameDef.ScreenPixelY / (GameDef.ScreenPixelY / 2f / GameDef.DefaultPixelPerMeters)); // fit width!
        //UiRoot.manualWidth = 1920;//GameDef.ScreenPixelX;

        // 屏幕中间位置
        //UiRoot.transform.localPosition = new Vector3(GameDef.ScreenPixelX / 2f / GameDef.DefaultPixelPerMeters,
        //    GameDef.ScreenPixelY / 2f / GameDef.DefaultPixelPerMeters, -50);
        //var scale = 1 / GameDef.DefaultPixelPerMeters;
        //// 覆盖NGUI的Uiroot自动缩放
        //UiRoot.transform.localScale = new Vector3(scale, scale, scale);
        //UiRoot.enabled = false;

        GameObject panelRootObj = new GameObject("PanelRoot");
        KTool.SetChild(panelRootObj.transform, uiRootobj.transform);

        Transform panelTrans = panelRootObj.transform;
        PanelRoot = panelRootObj.AddComponent<UIPanel>();
        Debuger.Assert(PanelRoot);
        PanelRoot.generateNormals = true;

        var uiCamTrans = uiRootobj.transform.Find("UICamera");
        GameObject uiCamObj = uiCamTrans != null ? uiCamTrans.gameObject : new GameObject("UICamera");

        KTool.SetChild(uiCamObj.transform, UiRoot.transform);
        UiCamera = uiCamObj.GetComponent<UICamera>() ?? uiCamObj.AddComponent<UICamera>();
        UiCamera.cachedCamera.cullingMask = 1 << (int)UnityLayerDef.UI;
        UiCamera.cachedCamera.clearFlags = CameraClearFlags.Depth;
        UiCamera.cachedCamera.orthographic = true;
        UiCamera.cachedCamera.orthographicSize = GameDef.ScreenPixelY / GameDef.DefaultPixelPerMeters / 2f;
        // 9.6，一屏19.2米，跟GameCamera一致
        UiCamera.cachedCamera.nearClipPlane = -500;
        UiCamera.cachedCamera.farClipPlane = 500;

        foreach (UIAnchor.Side side in Enum.GetValues(typeof(UIAnchor.Side)))
        {
            GameObject anchorObj = new GameObject(side.ToString());
            KTool.SetChild(anchorObj.transform, panelTrans);
            AnchorSide[side.ToString()] = anchorObj.transform;
        }

        GameObject nullAnchor = new GameObject("Null");
        KTool.SetChild(nullAnchor.transform, panelTrans);
        AnchorSide["Null"] = nullAnchor.transform;
        AnchorSide[""] = AnchorSide[UIAnchor.Side.Center.ToString()]; // default

        NGUITools.SetLayer(uiRootobj, (int)UnityLayerDef.UI);


        PressWidget = new GameObject("PressWidget").AddComponent<UIWidget>();
        NGUITools.SetLayer(PressWidget.gameObject, (int)UnityLayerDef.UI);
        KTool.SetChild(PressWidget.gameObject, panelRootObj);
        PressWidget.SetDimensions(2000, 2000);
        var col = PressWidget.gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(2000, 2000);
        PressWidget.autoResizeBoxCollider = true;
        PressWidget.gameObject.SetActive(false);
        //UICamera.onDragStart = (go) =>
        //{
        //    if (go != null)  // 点击任意NGUI控件，出现阻挡
        //        PressWidget.gameObject.SetActive(true);
        //};
        //UICamera.onPress = (go, state) =>
        //{
        //    if (!state)  // 点击任意NGUI控件，出现阻挡
        //        PressWidget.gameObject.SetActive(false);

        //    //if (go != null)
        //    //{
        //    //    if (go.GetComponent<UIButton>() == null)
        //    //    {
        //    //        if (go.GetComponent<UIEventListener>() != null && go.GetComponent<UISprite>() != null)
        //    //        {
        //    //            if (Debug.isDebugBuild)
        //    //            {
        //    //                Debug.LogWarning("自动加UIButton和ButtonScale - " + go.name, go);
        //    //            }
        //    //            // 当不包含ButtonScale动画
        //    //            // 并且拥有EventListener和UISprite!
        //    //            // 统一加上ButtonScale!
        //    //            var bScale = go.GetComponent<UIButtonScale>() ?? go.AddComponent<UIButtonScale>();
        //    //            bScale.pressed = bScale.hover;
        //    //            bScale.hover = Vector3.one;
        //    //            var bColor = go.GetComponent<UIButton>() ?? go.AddComponent<UIButton>();
        //    //            bColor.hover = Color.white;
        //    //            bColor.pressed = Color.white*.8f; // 小小灰
        //    //        }
        //    //    }
        //    //}
        //};
    }
}
#endif