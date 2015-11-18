//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                     Version 0.8 (20140904)
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using KEngine;
//public interface IUGUIWindow
//{
//}


class GameDef
{
    public const float PictureScale = 1f;
    public const int ScreenPixelY = 1080;
    public const int ScreenPixelX = 1920;
    public const int DefaultPixelPerMeters = 100;
}

public class CNGUIBridge : IKUIBridge
{
    public UICamera UiCamera;
    public UIRoot UiRoot;
    public UIPanel PanelRoot;
    public UIWidget PressWidget; // 只要PressUI,立刻启动这个遮罩！预防穿透到场景

    // new UI
    //private Canvas UICanvas;

    readonly Dictionary<string, Transform> AnchorSide = new Dictionary<string, Transform>();

    public static CNGUIBridge Instance;

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

    public void UIObjectFilter(KUIController ui, GameObject uiObj)
    {
        //if (ui is IUGUIWindow)
        //{
        //    CTool.SetChild(uiObj, UICanvas.gameObject);// 放进UICanvas
        //    CTool.SetLayer(uiObj, (int)CLayerDef.UI);
        //}
        //else
        {
            uiObj.transform.parent = AnchorSide[UIAnchor.Side.Center.ToString()];
            uiObj.transform.localPosition = Vector3.zero;
            uiObj.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    public object GetUIComponent(string comName)
    {
        if (comName == "Camera")
            return UiCamera;

        Logger.Assert(false);
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
    //    CTool.SetChild(evtSysObj, canvasObj);
    //    evtSysObj.AddComponent<EventSystem>();
    //}

    void CreateUIRoot()
    {
        GameObject uiRootobj = GameObject.Find("NGUIRoot") ?? new GameObject("NGUIRoot");
        UiRoot = uiRootobj.GetComponent<UIRoot>() ??uiRootobj.AddComponent<UIRoot>();
        Logger.Assert(UiRoot);
        UiRoot.scalingStyle = UIRoot.Scaling.FixedSizeOnMobiles;

        // 尝试将NGUI转化成跟2dToolkit镜头一致显示
        UiRoot.manualHeight = 1080; //GameDef.ScreenPixelY;//(int)(GameDef.ScreenPixelY / (GameDef.ScreenPixelY / 2f / GameDef.DefaultPixelPerMeters)); // fit width!
        //UiRoot.manualWidth = 1920;//GameDef.ScreenPixelX;

        // 屏幕中间位置
        //UiRoot.transform.localPosition = new Vector3(GameDef.ScreenPixelX / 2f / GameDef.DefaultPixelPerMeters,
        //    GameDef.ScreenPixelY / 2f / GameDef.DefaultPixelPerMeters, -50);
        //var scale = 1 / GameDef.DefaultPixelPerMeters;
        //// 覆盖NGUI的Uiroot自动缩放
        //UiRoot.transform.localScale = new Vector3(scale, scale, scale);
        //UiRoot.enabled = false;

        GameObject panelRootObj = new GameObject("PanelRoot");
        CTool.SetChild(panelRootObj.transform, uiRootobj.transform);

        Transform panelTrans = panelRootObj.transform;
        PanelRoot = panelRootObj.AddComponent<UIPanel>();
        Logger.Assert(PanelRoot);
        PanelRoot.generateNormals = true;

        var uiCamTrans = uiRootobj.transform.Find("UICamera");
        GameObject uiCamObj = uiCamTrans != null ? uiCamTrans.gameObject : new GameObject("UICamera");

        CTool.SetChild(uiCamObj.transform, UiRoot.transform);
        UiCamera = uiCamObj.GetComponent<UICamera>() ?? uiCamObj.AddComponent<UICamera>();
        UiCamera.cachedCamera.cullingMask = 1 << (int)UnityLayerDef.UI;
        UiCamera.cachedCamera.clearFlags = CameraClearFlags.Depth;
        UiCamera.cachedCamera.orthographic = true;
        UiCamera.cachedCamera.orthographicSize = GameDef.ScreenPixelY / GameDef.DefaultPixelPerMeters / 2f; // 9.6，一屏19.2米，跟GameCamera一致
        UiCamera.cachedCamera.nearClipPlane = -500;
        UiCamera.cachedCamera.farClipPlane = 500;

        foreach (UIAnchor.Side side in Enum.GetValues(typeof(UIAnchor.Side)))
        {
            GameObject anchorObj = new GameObject(side.ToString());
            CTool.SetChild(anchorObj.transform, panelTrans);
            AnchorSide[side.ToString()] = anchorObj.transform;
        }

        GameObject nullAnchor = new GameObject("Null");
        CTool.SetChild(nullAnchor.transform, panelTrans);
        AnchorSide["Null"] = nullAnchor.transform;
        AnchorSide[""] = AnchorSide[UIAnchor.Side.Center.ToString()]; // default

        NGUITools.SetLayer(uiRootobj, (int)UnityLayerDef.UI);


        PressWidget = new GameObject("PressWidget").AddComponent<UIWidget>();
        NGUITools.SetLayer(PressWidget.gameObject, (int)UnityLayerDef.UI);
        CTool.SetChild(PressWidget.gameObject, panelRootObj);
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