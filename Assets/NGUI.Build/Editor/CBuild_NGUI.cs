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
using UnityEngine.EventSystems;
using UnityEngine.UI;
using KEngine;

public partial class CBuild_NGUI : KBuild_Base
{
    string UIName;
    string UIScenePath;
    //UIPanel PanelRoot;
    //GameObject AnchorObject;
    GameObject WindowObject;
    GameObject TempPanelObject;

    // 判断本次是否全局打包，是否剔除没用的UIlabel string
    public bool IsBuildAll = false; 

    public override string GetDirectory() { return "NGUI"; }
    public override string GetExtention() { return "*.unity"; }

    public static event Action<CBuild_NGUI> BeginExportEvent;
    public static event Action<CBuild_NGUI, string, string, GameObject> ExportCurrentUIEvent;
    public static event Action ExportUIMenuEvent;
    public static event Action<CBuild_NGUI> EndExportEvent;

    public static string GetBuildRelPath(string uiName)
    {
        return string.Format("NGUI/{0}_UI{1}", uiName, AppEngine.GetConfig("AssetBundleExt"));
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

        CreateTempPrefab();

        if (ExportCurrentUIEvent != null)
        {
            ExportCurrentUIEvent(this, UIScenePath, UIName, TempPanelObject);
        }
        else
        {
            CBuildTools.BuildAssetBundle(TempPanelObject, GetBuildRelPath(UIName));
        }
        DestroyTempPrefab();
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

    void CreateTempPrefab()
    {
        TempPanelObject = (GameObject)GameObject.Instantiate(WindowObject);

        //if (WindowObject.GetComponent<UIPanel>() == null)
        //{
        //    // 读取UIPanel的depth, 遍历所有UI控件，将其depth加上UIPanel Depth， 以此设置层级关系
        //    // 如PanelRoot Depth填10,  子控件填0,1,2   打包后，子控件层级为 10 << 5 | 1 = 320, 321, 322
        //    foreach (UIWidget uiWidget in TempPanelObject.GetComponentsInChildren<UIWidget>(true))
        //    {
        //        uiWidget.depth = (PanelRoot.depth + 15) << 5 | (uiWidget.depth + 15);  // + 15是为了杜绝负数！不要填-15以上的
        //    }
        //}

        foreach (UIButton go in TempPanelObject.GetComponentsInChildren<UIButton>(true))
        {
            if (go.tweenTarget != null && go.transform.FindChild(go.tweenTarget.name) != null && go.tweenTarget != go.transform.FindChild(go.tweenTarget.name).gameObject)
            {
                Debug.LogWarning(UIName + " " + go.name + " UIButton 的Target 目标不是当前UIButton 子节点 ");
            }
        }
    }

    void DestroyTempPrefab()
    {
        GameObject.DestroyImmediate(TempPanelObject);
        UIName = null;
        //AnchorObject = null;
        WindowObject = null;
        TempPanelObject = null;
    }

    public static GameObject GetUIWindow()
    {
        //PanelRoot = GameObject.Find("UIRoot/PanelRoot").GetComponent<UIPanel>();
        var AnchorObject = (GameObject)GameObject.Find("UIRoot/PanelRoot/Anchor");

        if (AnchorObject == null)
        {
            //if (showMsg)
            //    CBuildTools.ShowDialog("找不到UIRoot/PanelRoot/Anchor");
            //else
            Debug.LogError("找不到UIRoot/PanelRoot/Anchor");
            return null;
        }

        if (AnchorObject.transform.childCount != 1)
        {
            //if (showMsg)
            //    CBuildTools.ShowDialog("UI结构错误，Ahchor下应该只有一个节点");
            //else
            Debug.LogError("UI结构错误，Ahchor下应该只有一个节点");
            return null;
        }
        return AnchorObject.transform.GetChild(0).gameObject;
        
    }
    public bool CheckUI(bool showMsg)
    {
        WindowObject = GetUIWindow();
        if (WindowObject == null)
            return false;

        UIName = EditorApplication.currentScene.Substring(EditorApplication.currentScene.LastIndexOf('/') + 1);
        UIName = UIName.Substring(0, UIName.LastIndexOf('.'));

        UIScenePath = EditorApplication.currentScene;

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

        GameObject uiRootObj = new GameObject("UIRoot");
        uiRootObj.layer = (int)UnityLayerDef.UI;

        UIRoot uiRoot = uiRootObj.AddComponent<UIRoot>();
        uiRoot.scalingStyle = UIRoot.Scaling.ConstrainedOnMobiles;
        uiRoot.manualHeight = 1920;
        uiRoot.manualWidth = 1080;
        uiRoot.fitHeight = true;
        uiRoot.fitWidth = true;

        GameObject cameraObj = new GameObject("Camera");
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

        camera.gameObject.AddComponent<AudioListener>();
        camera.gameObject.AddComponent<UICamera>();

        UIPanel uiPanel = NGUITools.AddChild<UIPanel>(uiRootObj);
        uiPanel.gameObject.name = "PanelRoot";
        uiPanel.gameObject.AddComponent<Canvas>();

        UIAnchor uiAnchor = NGUITools.AddChild<UIAnchor>(uiPanel.gameObject);
        GameObject windowObj = NGUITools.AddChild(uiAnchor.gameObject);
        windowObj.name = "Window";

        Selection.activeGameObject = windowObj;
    }

    [MenuItem("KEngine/NGUI/Create UI %&N")]
    public static void CreateUI()
    {
        CBuild_NGUI.CreateNewUI();
    }

    [MenuItem("KEngine/NGUI/Export Current UI %&U")]
    public static void ExportUIMenu()
    {
        if (ExportUIMenuEvent != null)
            ExportUIMenuEvent();
        
        CBuild_NGUI uiBuild = new CBuild_NGUI();
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
    [MenuItem("CosmosEngine/UI/Export All UI")]
    public static void ExportAllUI()
    {
        var buildUI = new CBuild_NGUI();
        buildUI.IsBuildAll = true;
        KResourceBuilder.ProductExport(buildUI);
    }
}
