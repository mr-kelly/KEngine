#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CBuild_UGUI.cs
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

using System.IO;
using KEngine.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KEngine.Editor
{
    public class KUGUIBuilder : KBuild_Base
    {
        [MenuItem("KEngine/UI(UGUI)/Export Current UI")]
        public static void ExportCurrentUI()
        {
            //var UIName = Path.GetFileNameWithoutExtension(EditorApplication.currentScene);
            var windowAssets = GameObject.FindObjectsOfType<KUIWindowAsset>();
            if (windowAssets.Length <= 0)
            {
                KLogger.LogError("Not found KUIWindowAsset in scene `{0}`", EditorApplication.currentScene);
            }
            else
            {
                foreach (var windowAsset in windowAssets)
                {
                    var uiName = windowAsset.name;
                    KBuildTools.BuildAssetBundle(windowAsset.gameObject, GetBuildRelPath(uiName));
                }
            }
        }

        public static string GetBuildRelPath(string uiName)
        {
            return string.Format("UI/{0}_UI{1}", uiName, KEngine.AppEngine.GetConfig("AssetBundleExt"));
        }

        [MenuItem("KEngine/UI(UGUI)/Create UI(UGUI)")]
        public static void CreateNewUI()
        {
            GameObject mainCamera = GameObject.Find("Main Camera");
            if (mainCamera != null)
                GameObject.DestroyImmediate(mainCamera);

            GameObject uiObj = new GameObject("NewUI_" + Path.GetRandomFileName());
            uiObj.layer = (int)UnityLayerDef.UI;
            var canvas = uiObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiObj.AddComponent<CanvasScaler>();
            uiObj.AddComponent<GraphicRaycaster>();

            var evtSystemObj = new GameObject("EventSystem");
            evtSystemObj.AddComponent<EventSystem>();
            evtSystemObj.AddComponent<StandaloneInputModule>();
            evtSystemObj.AddComponent<TouchInputModule>();

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

            Selection.activeGameObject = uiObj;
        }

        public override void Export(string path)
        {
            EditorApplication.OpenScene(path);

            ExportCurrentUI();
        }

        public override string GetDirectory()
        {
            return "UI";
        }

        public override string GetExtention()
        {
            return "*.unity";
        }
    }
}