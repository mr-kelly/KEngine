//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Version 0.9.1 (20151010)
//                     Copyright © 2011-2015
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using System.IO;
using KFramework;
using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CUGUIBuilder : CBuild_Base
{
    [MenuItem("CosmosEngine/UI(UGUI)/Export Current UI")]
    public static void ExportCurrentUI()
    {
        var UIName = Path.GetFileNameWithoutExtension(EditorApplication.currentScene);
        
        var uiRoot = GameObject.Find("UI");
        CBuildTools.BuildAssetBundle(uiRoot, GetBuildRelPath(UIName));
    }
    public static string GetBuildRelPath(string uiName)
    {
        return string.Format("UI/{0}_UI{1}", uiName, CCosmosEngine.GetConfig("AssetBundleExt"));
    }

    [MenuItem("CosmosEngine/UI(UGUI)/Create UI(UGUI)")]
    public static void CreateNewUI()
    {
        GameObject mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null)
            GameObject.DestroyImmediate(mainCamera);

        GameObject uiObj = new GameObject("UI");
        uiObj.layer = (int)CLayerDef.UI;
        var canvas = uiObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiObj.AddComponent<CanvasScaler>();
        uiObj.AddComponent<GraphicRaycaster>();

        var evtSystemObj = new GameObject("EventSystem");
        evtSystemObj.AddComponent<EventSystem>();
        evtSystemObj.AddComponent<StandaloneInputModule>();
        evtSystemObj.AddComponent<TouchInputModule>();

        GameObject cameraObj = new GameObject("Camera");
        cameraObj.layer = (int)CLayerDef.UI;

        Camera camera = cameraObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.depth = 0;
        camera.backgroundColor = Color.grey;
        camera.cullingMask = 1 << (int)CLayerDef.UI;
        camera.orthographicSize = 1f;
        camera.orthographic = true;
        camera.nearClipPlane = -2f;
        camera.farClipPlane = 2f;

        camera.gameObject.AddComponent<AudioListener>();

        Selection.activeGameObject = uiObj;
    }

    public override void Export(string path)
    {
        
    }
    public override string GetDirectory() { return "UI"; }
    public override string GetExtention() { return "*.unity"; }
}
