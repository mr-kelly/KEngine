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

using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using KEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class KBuild_NGUI : KBuild_Base
{
    private string UIScenePath
    {
        get
        {
            return EditorApplication.currentScene;
        }
    }

    //UIPanel PanelRoot;
    //GameObject AnchorObject;
    //GameObject WindowObject;

    // 判断本次是否全局打包，是否剔除没用的UIlabel string
    public bool IsBuildAll = false;

    public override string GetDirectory() { return "UI"; }
    public override string GetExtention() { return "*.unity"; }

    public static event Action<KBuild_NGUI> BeginExportEvent;
    public static event Action<KBuild_NGUI, string, string, GameObject> ExportCurrentUIEvent;
    public static event Action ExportUIMenuEvent;
    public static event Action<KBuild_NGUI> EndExportEvent;

    public static string GetBuildRelPath(string uiName)
    {
        return string.Format("UI/{0}_UI{1}", uiName, AppEngine.GetConfig("AssetBundleExt"));
    }

    public void ExportCurrentUI()
    {
        //if (changeLayer)
        //{
        // 去掉一些残留的Clone
        //var allTrans = GameObject.FindObjectsOfType<Transform>();
        //for (var i = allTrans.Length - 1; i >= 0 ; i--)
        //{
        //    var child = allTrans[i];
        //    if (child != null && child.gameObject.name.EndsWith("Window(Clone)"))
        //    {
        //        GameObject.DestroyImmediate(child.gameObject);
        //    }
        //}
        EditorApplication.SaveScene(); // 强制保存一下，保证一些Prefab更新

        //}

        foreach (var windowAsset in GameObject.FindObjectsOfType<KUIWindowAsset>())
        {
            var uiName = windowAsset.name;
            var tempPanelObject = CreateTempPrefab(windowAsset.gameObject);

            if (ExportCurrentUIEvent != null)
            {
                ExportCurrentUIEvent(this, UIScenePath, uiName, tempPanelObject);
            }
            else
            {
                KBuildTools.BuildAssetBundle(tempPanelObject, GetBuildRelPath(uiName));
            }
            DestroyTempPrefab(tempPanelObject);
        }
    }

    public override void BeforeExport()
    {
        if (BeginExportEvent != null)
        {
            BeginExportEvent(this);
        }
    }

    public override void Export(string path)
    {
        EditorApplication.OpenScene(path);

        if (!CheckUI(false))
            return;

        ExportCurrentUI();
    }

    public override void AfterExport()
    {
        if (EndExportEvent != null)
        {
            EndExportEvent(this);
        }

    }

    GameObject CreateTempPrefab(GameObject windowAsset)
    {
        var tempPanelObject = (GameObject)GameObject.Instantiate(windowAsset);

        //if (WindowObject.GetComponent<UIPanel>() == null)
        //{
        //    // 读取UIPanel的depth, 遍历所有UI控件，将其depth加上UIPanel Depth， 以此设置层级关系
        //    // 如PanelRoot Depth填10,  子控件填0,1,2   打包后，子控件层级为 10 << 5 | 1 = 320, 321, 322
        //    foreach (UIWidget uiWidget in TempPanelObject.GetComponentsInChildren<UIWidget>(true))
        //    {
        //        uiWidget.depth = (PanelRoot.depth + 15) << 5 | (uiWidget.depth + 15);  // + 15是为了杜绝负数！不要填-15以上的
        //    }
        //}

        foreach (UIButton go in tempPanelObject.GetComponentsInChildren<UIButton>(true))
        {
            if (go.tweenTarget != null && go.transform.FindChild(go.tweenTarget.name) != null && go.tweenTarget != go.transform.FindChild(go.tweenTarget.name).gameObject)
            {
                Debug.LogWarning(windowAsset + " " + go.name + " UIButton 的Target 目标不是当前UIButton 子节点 ");
            }
        }

        return tempPanelObject;
    }

    void DestroyTempPrefab(GameObject tempPanelObject)
    {
        GameObject.DestroyImmediate(tempPanelObject);
        //AnchorObject = null;
        //WindowObject = null;
    }

    public static GameObject GetUIWindow()
    {
        //PanelRoot = GameObject.Find("UIRoot/PanelRoot").GetComponent<UIPanel>();
        var AnchorObject = (GameObject)GameObject.Find("UIRoot/PanelRoot/Anchor");

        if (AnchorObject == null)
        {
            //if (showMsg)
            //    KBuildTools.ShowDialog("找不到UIRoot/PanelRoot/Anchor");
            //else
            Debug.LogError("找不到UIRoot/PanelRoot/Anchor");
            return null;
        }

        if (AnchorObject.transform.childCount != 1)
        {
            //if (showMsg)
            //    KBuildTools.ShowDialog("UI结构错误，Ahchor下应该只有一个节点");
            //else
            Debug.LogError("UI结构错误，Ahchor下应该只有一个节点");
            return null;
        }
        return AnchorObject.transform.GetChild(0).gameObject;

    }
    public bool CheckUI(bool showMsg)
    {
        var windowAssets = GameObject.FindObjectsOfType<KUIWindowAsset>();
        if (windowAssets.Length == 0)
            return false;


        // 確保Layer正確
        //bool changeLayer = false;
        //foreach (Transform loopTrans in GameObject.FindObjectsOfType<Transform>())
        //{
        //    if (loopTrans.gameObject.layer != (int)CLayerDef.UI)
        //    {
        //        NGUITools.SetLayer(loopTrans.gameObject, (int)CLayerDef.UI);
        //        //changeLayer = true;
        //    }
        //}

        //foreach (Camera cam in GameObject.FindObjectsOfType<Camera>())
        //{
        //    NGUITools.SetLayer(cam.gameObject, (int)CLayerDef.UI);
        //    if (cam.cullingMask != 1 << (int)CLayerDef.UI)
        //    {
        //        cam.cullingMask = 1 << (int)CLayerDef.UI;
        //        //changeLayer = true;
        //    }

        //}

        return true;
    }

    public static void CreateNewUI()
    {
        GameObject mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null)
            GameObject.DestroyImmediate(mainCamera);

        GameObject uiRootObj = GameObject.Find("UIRoot");
        if (uiRootObj == null)
        {
            uiRootObj = new GameObject("UIRoot");
            uiRootObj.layer = (int)UnityLayerDef.UI;
        }

        UIRoot uiRoot = uiRootObj.GetComponent<UIRoot>() ?? uiRootObj.AddComponent<UIRoot>();
        uiRoot.scalingStyle = UIRoot.Scaling.FixedSizeOnMobiles;
        uiRoot.manualHeight = 1920;
        //uiRoot.manualWidth = 1080;
        //uiRoot.fitHeight = true;
        //uiRoot.fitWidth = true;


        GameObject cameraObj = GameObject.Find("UICamera");
        if (cameraObj == null)
        {
            cameraObj = new GameObject("UICamera");
            cameraObj.layer = (int)UnityLayerDef.UI;

            Camera camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.depth = 0;
            camera.backgroundColor = Color.grey;
            camera.cullingMask = 1 << (int)UnityLayerDef.UI;
            camera.orthographicSize = 1f;
            camera.orthographic = true;
            camera.nearClipPlane = -2f;
            camera.farClipPlane = 2f;

        }
        var aud = cameraObj.GetComponent<AudioListener>() ?? cameraObj.AddComponent<AudioListener>();
        var uiCam = cameraObj.GetComponent<UICamera>() ?? cameraObj.AddComponent<UICamera>();
        var panelRootTran = uiRootObj.transform.Find("PanelRoot");
        if (panelRootTran == null)
        {
            UIPanel uiPanel = NGUITools.AddChild<UIPanel>(uiRootObj);
            uiPanel.gameObject.name = "PanelRoot";
            //uiPanel.gameObject.AddComponent<Canvas>();
            panelRootTran = uiPanel.transform;
        }

        var anchorTran = panelRootTran.Find("Anchor");
        if (anchorTran == null)
        {
            UIAnchor uiAnchor = NGUITools.AddChild<UIAnchor>(panelRootTran.gameObject);
            anchorTran = uiAnchor.transform;
        }

        GameObject windowObj = NGUITools.AddChild(anchorTran.gameObject);
        windowObj.name = "UIWindow"+Path.GetRandomFileName();
        windowObj.AddComponent<KUIWindowAsset>();

        Selection.activeGameObject = windowObj;
    }

    [MenuItem("KEngine/NGUI/Create UI %&N")]
    public static void CreateUI()
    {
        KBuild_NGUI.CreateNewUI();
    }

    [MenuItem("KEngine/NGUI/Export Current UI %&U")]
    public static void ExportUIMenu()
    {
        if (ExportUIMenuEvent != null)
            ExportUIMenuEvent();

        KBuild_NGUI uiBuild = new KBuild_NGUI();
        uiBuild.IsBuildAll = false;
        if (!uiBuild.CheckUI(true))
            return;

        uiBuild.BeforeExport();
        uiBuild.ExportCurrentUI();
        uiBuild.AfterExport();
    }

    /// <summary>
    /// Buidl All UI Scene under Assets/"+ CCosmosEngineDef.ResourcesBuildDir + "/ folder
    /// </summary>
    [MenuItem("KEngine/NGUI/Export All UI")]
    public static void ExportAllUI()
    {
        var buildUI = new KBuild_NGUI();
        buildUI.IsBuildAll = true;
        KResourceBuilder.ProductExport(buildUI);
    }
}
