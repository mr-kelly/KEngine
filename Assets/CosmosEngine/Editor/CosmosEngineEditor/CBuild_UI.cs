using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public partial class CBuild_UI : AutoBuildBase
{
    string UIName;
    UIPanel PanelRoot;
    GameObject AnchorObject;
    GameObject WindowObject;
    GameObject TempPanelObject;
    string UIScenePath;

    public override string GetDirectory() { return "UI"; }
    public override string GetExtention() { return "*.unity"; }

    public static string GetBuildRelPath(string uiName)
    {
        return string.Format("UI/{0}_UI{1}", uiName, CCosmosEngine.GetConfig("AssetBundleExt"));
    }

    public void ExportCurrentUI()
    {
        if (!CBuildTools.HookFunc(typeof(CBuild_UI), "Custom_ExportCurrentUI", UIScenePath, UIName, TempPanelObject))
        {
            CBuildTools.BuildAssetBundle(TempPanelObject, GetBuildRelPath(UIName));
        }

    }

    public override void BeginExport()
    {
        CBuildTools.HookFunc(typeof(CBuild_UI), "Custom_BeginExport", this);
    }

    public override void Export(string path)
    {
        UIScenePath = path;

        EditorApplication.OpenScene(path);

        if (!CheckUI(false))
            return;

        CreateTempPrefab();

        ExportCurrentUI();
    }

    public override void EndExport()
    {
        CBuildTools.HookFunc(typeof(CBuild_UI), "Custom_EndExport", this);

        DestroyTempPrefab();
    }

    void CreateTempPrefab()
    {
        TempPanelObject = (GameObject)GameObject.Instantiate(WindowObject);

        if (WindowObject.GetComponent<UIPanel>() == null)
        {
            // 读取UIPanel的depth, 遍历所有UI控件，将其depth加上UIPanel Depth， 以此设置层级关系
            // 如PanelRoot Depth填10,  子控件填0,1,2   打包后，子控件层级为 10 << 5 | 1 = 320, 321, 322
            foreach (UIWidget uiWidget in TempPanelObject.GetComponentsInChildren<UIWidget>(true))
            {
                uiWidget.depth = (PanelRoot.depth + 15) << 5 | (uiWidget.depth + 15);  // + 15是为了杜绝负数！不要填-15以上的
            }
        }

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
        AnchorObject = null;
        WindowObject = null;
        TempPanelObject = null;
    }

    public bool CheckUI(bool showMsg)
    {
        PanelRoot = GameObject.Find("UIRoot/PanelRoot").GetComponent<UIPanel>();
        AnchorObject = (GameObject)GameObject.Find("UIRoot/PanelRoot/Anchor");

        if (AnchorObject == null)
        {
            if (showMsg)
                CBuildTools.ShowDialog("找不到UIRoot/PanelRoot/Anchor");
            else
                Debug.Log("找不到UIRoot/PanelRoot/Anchor");
            return false;
        }

        if (AnchorObject.transform.childCount != 1)
        {
            if (showMsg)
                CBuildTools.ShowDialog("UI结构错误，Ahchor下应该只有一个节点");
            else
                Debug.Log("UI结构错误，Ahchor下应该只有一个节点");
            return false;
        }

        WindowObject = AnchorObject.transform.GetChild(0).gameObject;
        UIName = EditorApplication.currentScene.Substring(EditorApplication.currentScene.LastIndexOf('/') + 1);
        UIName = UIName.Substring(0, UIName.LastIndexOf('.'));

        // 確保Layer正確
        bool changeLayer = false;
        foreach (Transform loopTrans in GameObject.FindObjectsOfType<Transform>())
        {
            if (loopTrans.gameObject.layer != (int)CLayerDef.UI)
            {
                NGUITools.SetLayer(loopTrans.gameObject, (int)CLayerDef.UI);
                changeLayer = true;
            }
        }

        foreach (Camera cam in GameObject.FindObjectsOfType<Camera>())
        {
            NGUITools.SetLayer(cam.gameObject, (int)CLayerDef.UI);
            if (cam.cullingMask != 1 << (int)CLayerDef.UI)
            {
                cam.cullingMask = 1 << (int)CLayerDef.UI;
                changeLayer = true;
            }

        }

        if (changeLayer)
        {
            EditorApplication.SaveScene();
        }



        return true;
    }

    public static void CreateNewUI()
    {
        GameObject mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null)
            GameObject.DestroyImmediate(mainCamera);

        GameObject uiRootObj = new GameObject("UIRoot");
        uiRootObj.layer = (int)CLayerDef.UI;

        UIRoot uiRoot = uiRootObj.AddComponent<UIRoot>();
        uiRoot.scalingStyle = UIRoot.Scaling.ConstrainedOnMobiles;
        uiRoot.manualHeight = 960;

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
        camera.gameObject.AddComponent<UICamera>();

        UIPanel uiPanel = NGUITools.AddChild<UIPanel>(uiRootObj);
        uiPanel.gameObject.name = "PanelRoot";

        UIAnchor uiAnchor = NGUITools.AddChild<UIAnchor>(uiPanel.gameObject);
        GameObject windowObj = NGUITools.AddChild(uiAnchor.gameObject);
        windowObj.name = "Window";

        Selection.activeGameObject = windowObj;
    }

    [MenuItem("CosmosEngine/UI/Create UI %&N")]
    public static void CreateUI()
    {
        CBuild_UI.CreateNewUI();
    }

    [MenuItem("CosmosEngine/UI/Export Current UI %&U")]
    public static void ExportUIMenu()
    {
        CBuildTools.HookFunc(typeof(CBuild_UI), "Custom_ExportUIMenu");

        CBuild_UI uiBuild = new CBuild_UI();
        if (!uiBuild.CheckUI(true))
            return;

        uiBuild.BeginExport();
        uiBuild.ExportCurrentUI();
        uiBuild.EndExport();
    }


}
