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
#if NGUI
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class CNGUIBridge : ICUIBridge
{
    public UICamera UiCamera;
    public UIRoot UiRoot;
    public UIPanel PanelRoot;

    Dictionary<string, Transform> AnchorSide = new Dictionary<string, Transform>();

    public static CNGUIBridge Instance;

    public void InitBridge()
    {
        Instance = this;
        CreateUIRoot();
    }

    public void UIObjectFilter(GameObject uiObj)
    {
        //string side = uiConfig.Side ?? "";
        uiObj.transform.parent = AnchorSide[UIAnchor.Side.Center.ToString()];
        //uiObj.transform.localPosition = new Vector3(uiConfig.OffsetX, uiConfig.OffsetY, uiConfig.OffsetZ); // 根据表中偏移
        uiObj.transform.localPosition = Vector3.zero;
        uiObj.transform.localScale = new Vector3(1, 1, 1);
    }

    public object GetUIComponent(string comName)
    {
        if (comName == "Camera")
            return UiCamera;

        CBase.Assert(false);
        return null;
    }

    void CreateUIRoot()
    {
        GameObject uiRootobj = new GameObject("UIRoot");
        UiRoot = uiRootobj.AddComponent<UIRoot>();
        CBase.Assert(UiRoot);
        UiRoot.scalingStyle = UIRoot.Scaling.ConstrainedOnMobiles;
        UiRoot.manualHeight = 1920;
        UiRoot.manualWidth = 1080;

        GameObject panelRootObj = new GameObject("PanelRoot");
        CTool.SetChild(panelRootObj.transform, uiRootobj.transform);

        Transform panelTrans = panelRootObj.transform;
        PanelRoot = panelRootObj.AddComponent<UIPanel>();
        CBase.Assert(PanelRoot);
        PanelRoot.generateNormals = true;

        GameObject uiCamObj = new GameObject("UICamera");
        CTool.SetChild(uiCamObj.transform, UiRoot.transform);
        UiCamera = uiCamObj.AddComponent<UICamera>();
        UiCamera.cachedCamera.cullingMask = 1 << (int)CLayerDef.UI;
        UiCamera.cachedCamera.clearFlags = CameraClearFlags.Depth;
        UiCamera.cachedCamera.orthographic = true;
        UiCamera.cachedCamera.orthographicSize = 1;
        UiCamera.cachedCamera.nearClipPlane = -2;
        UiCamera.cachedCamera.farClipPlane = 2;
        //panelTrans.gameObject.isStatic = true;

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

        NGUITools.SetLayer(uiRootobj, (int)CLayerDef.UI);

    }

}
#endif