#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KBuild_NGUI_ResourceDep.cs
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
using System.IO;
using KEngine;
using KEngine.Editor;
using UnityEditor;
using UnityEngine;

public partial class KBuild_NGUI_ResourceDep : KBuild_Base
{
    private string UIScenePath
    {
        get { return EditorApplication.currentScene; }
    }

    //UIPanel PanelRoot;
    //GameObject AnchorObject;
    //GameObject WindowObject;

    // 判断本次是否全局打包，是否剔除没用的UIlabel string
    public bool IsBuildAll = false;

    public override string GetDirectory()
    {
        return "UI";
    }

    public override string GetExtention()
    {
        return "*.unity";
    }

    public static event Action<KBuild_NGUI_ResourceDep> BeginExportEvent;
    public static event Action<KBuild_NGUI_ResourceDep, string, string, GameObject> ExportCurrentUIEvent;
    public static event Action ExportUIMenuEvent;
    public static event Action<KBuild_NGUI_ResourceDep> EndExportEvent;

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
                BuildTools.BuildAssetBundle(tempPanelObject, GetBuildRelPath(uiName));
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

    private GameObject CreateTempPrefab(GameObject windowAsset)
    {
        var sceneDir = Path.GetDirectoryName(EditorApplication.currentScene);

        var prefabPath = sceneDir + "/" + windowAsset.name + ".prefab";
        //var tempPanelObject = (GameObject) GameObject.Instantiate(windowAsset);
        var tempPanelObject = PrefabUtility.CreatePrefab(prefabPath, windowAsset);

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
            if (go.tweenTarget != null && go.transform.FindChild(go.tweenTarget.name) != null &&
                go.tweenTarget != go.transform.FindChild(go.tweenTarget.name).gameObject)
            {
                Debug.LogWarning(windowAsset + " " + go.name + " UIButton 的Target 目标不是当前UIButton 子节点 ");
            }
        }

        return tempPanelObject;
    }

    /// <summary>
    /// 删除Prefab
    /// </summary>
    /// <param name="tempPanelObject"></param>
    private void DestroyTempPrefab(GameObject tempPanelObject)
    {
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(tempPanelObject));
        //GameObject.DestroyImmediate(tempPanelObject);
        //AnchorObject = null;
        //WindowObject = null;
    }

    public static GameObject GetUIWindow()
    {
        //PanelRoot = GameObject.Find("UIRoot/PanelRoot").GetComponent<UIPanel>();
        var AnchorObject = (GameObject) GameObject.Find("UIRoot/PanelRoot/Anchor");

        if (AnchorObject == null)
        {
            //if (showMsg)
            //    BuildTools.ShowDialog("找不到UIRoot/PanelRoot/Anchor");
            //else
            Debug.LogError("找不到UIRoot/PanelRoot/Anchor");
            return null;
        }

        if (AnchorObject.transform.childCount != 1)
        {
            //if (showMsg)
            //    BuildTools.ShowDialog("UI结构错误，Ahchor下应该只有一个节点");
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
            uiRootObj.layer = (int) UnityLayerDef.UI;
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
            cameraObj.layer = (int) UnityLayerDef.UI;

            Camera camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.depth = 0;
            camera.backgroundColor = Color.grey;
            camera.cullingMask = 1 << (int) UnityLayerDef.UI;
            camera.orthographicSize = 1f;
            camera.orthographic = true;
            camera.nearClipPlane = -2f;
            camera.farClipPlane = 2f;
        }
        //var aud = cameraObj.GetComponent<AudioListener>() ?? cameraObj.AddComponent<AudioListener>();
        //var uiCam = cameraObj.GetComponent<UICamera>() ?? cameraObj.AddComponent<UICamera>();
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
        windowObj.name = "UIWindow" + Path.GetRandomFileName();
        windowObj.AddComponent<KUIWindowAsset>();

        Selection.activeGameObject = windowObj;
    }

    [MenuItem("KEngine/NGUI/Create UI %&N")]
    public static void CreateUI()
    {
        KBuild_NGUI_ResourceDep.CreateNewUI();
    }

    [MenuItem("KEngine/NGUI/Export Current UI %&U")]
    public static void ExportUIMenu()
    {
        if (ExportUIMenuEvent != null)
            ExportUIMenuEvent();

        KBuild_NGUI_ResourceDep uiBuild = new KBuild_NGUI_ResourceDep();
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
        var buildUI = new KBuild_NGUI_ResourceDep();
        buildUI.IsBuildAll = true;
        KResourceBuilder.ProductExport(buildUI);
    }
}
#endif