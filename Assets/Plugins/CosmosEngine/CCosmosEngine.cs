//-------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//-------------------------------------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// Cosmos Engine - Unity3D Game Develop Framework
public abstract class CCosmosEngine : MonoBehaviour
{
    public static bool DetailDebugMode = true;  // 是否输出详细的调试信息
    static CFpsWatcher RenderWatcher;  // 帧数监听器
    public static bool IsRootUser;  // 是否越狱iOS

    static CCosmosEngine EngineInstance;

    static Dictionary<string, string> ConfigMap = null;// 遊戲配置，讀取Resources目錄里

    private void Awake()
    {
        if (EngineInstance != null)
        {
            CBase.LogError("Duplicated Instance CCosmosEngine!!!");
        }

        EngineInstance = this;

        Init();
    }

    private void Init()
    {
        DetailDebugMode = Debug.isDebugBuild;

        IsRootUser = CBaseTool.HasWriteAccessToFolder(Application.dataPath);  // Root User运行时，能穿越沙盒写DataPath, 以此为依据

        if (Debug.isDebugBuild)
        {
            RenderWatcher = new CFpsWatcher(0.95f);
        }

        if (Debug.isDebugBuild)
        {
            CBase.Log("====================================================================================");
            CBase.Log("Application.platform = {0}", Application.platform);
            CBase.Log("Application.dataPath = {0} , WritePermission: {1}", Application.dataPath, IsRootUser);
            CBase.Log("Application.streamingAssetsPath = {0} , WritePermission: {1}", Application.streamingAssetsPath, CBaseTool.HasWriteAccessToFolder(Application.streamingAssetsPath));
            CBase.Log("Application.persistentDataPath = {0} , WritePermission: {1}", Application.persistentDataPath, CBaseTool.HasWriteAccessToFolder(Application.persistentDataPath));
            CBase.Log("Application.temporaryCachePath = {0} , WritePermission: {1}", Application.temporaryCachePath, CBaseTool.HasWriteAccessToFolder(Application.temporaryCachePath));
            CBase.Log("Application.unityVersion = {0}", Application.unityVersion);
            CBase.Log("Remote Update WebServer Url = {0}", CCosmosEngine.GetConfig("ResourceRemoteUrl"));
            CBase.Log("SystemInfo.deviceModel = {0}", SystemInfo.deviceModel);
            CBase.Log("SystemInfo.deviceUniqueIdentifier = {0}", SystemInfo.deviceUniqueIdentifier);
            CBase.Log("====================================================================================");
        }
        StartCoroutine(DoInit());
    }

    IEnumerator DoInit()
    {
        ICModule[] baseModules = new ICModule[] {  // 基础三件套
            CResourceManager.Instance, 
            CSettingManager.Instance,
            CUIManager.Instance, 
        };
        foreach (ICModule mod in baseModules)
        {
            yield return StartCoroutine(mod.Init());
        }

        CBase.Log("Finish Init ResourceManager + UIManager!");

        yield return StartCoroutine(InitGame());
    }

    protected abstract IEnumerator InitGame();

    void OnGUI()
    {
        if (Debug.isDebugBuild)
        {
            GUILayout.BeginVertical(GUILayout.Width(300));
            GUILayout.Label(RenderWatcher.Watch("FPS: {0:N0}", 1f / Time.deltaTime));
            GUILayout.EndVertical();
        }
    }

    static void EnsureConfigTab()
    {
        if (ConfigMap == null)
        {
            TextAsset textAsset;
            textAsset = Resources.Load<TextAsset>("CEngineConfig");

            CBase.Assert(textAsset);

            CTabFile configTab = CTabFile.LoadFromString(textAsset.text);
            ConfigMap = new Dictionary<string, string>();
            foreach (CTabFile.CTabRow row in configTab)
            {
                string key = row.GetString("Key");
                string value = row.GetString("Value");
                ConfigMap[key] = value;
            }
        }
    }

    public static string GetConfig(string key)
    {
        EnsureConfigTab();

        string getValue;
        if (!ConfigMap.TryGetValue(key, out getValue))
        {
            CBase.LogError("Cannot get CosmosConfig: {0}", key);
        }
        return getValue;
    }

}

class CFpsWatcher
{
    float Value;
    float Sensitivity;

    public CFpsWatcher(float sensitivity)
    {
        Value = 0f;
        Sensitivity = sensitivity;
    }

    public string Watch(string format, float value)
    {
        Value = Value * Sensitivity + value * (1f - Sensitivity);
        return string.Format(format, Value);
    }
}
