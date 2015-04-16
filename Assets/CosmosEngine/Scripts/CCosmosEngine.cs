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
using System.Collections;
using System.Collections.Generic;
using System;
using CosmosTable;

namespace CosmosEngine
{

    /// <summary>
    /// Cosmos Engine - Unity3D Game Develop Framework
    /// </summary>
    public class CCosmosEngine : MonoBehaviour
    {
        /// <summary>
        /// To Display FPS in the Debug Mode (Debug.isDebugBuild is true)
        /// </summary>
        static CFpsWatcher RenderWatcher;  // 帧数监听器

        /// <summary>
        /// In Init func has a check if the user has the write privillige
        /// </summary>
        public static bool IsRootUser;  // 是否越狱iOS

        public static CCosmosEngine EngineInstance { get; private set; }

        /// <summary>
        /// Read Tab file (CEngineConfig.txt), cache to here
        /// </summary>
        //static Dictionary<string, string> ConfigMap = null;// 遊戲配置，讀取Resources目錄里

        /// <summary>
        /// Modules passed from the CosmosEngine.New function. All your custom game logic modules 
        /// </summary>
        private ICModule[] GameModules;

        public delegate IEnumerator CoroutineDelegate();
        private CoroutineDelegate BeforeInitModules = null;
        private CoroutineDelegate AfterInitModules = null;

        public static CCosmosEngine New(GameObject gameObjectToAttach, ICModule[] modules)
        {
            return New(gameObjectToAttach, modules, null, null);
        }

        /// <summary>
        /// Engine entry.... all begins from here
        /// </summary>
        public static CCosmosEngine New(GameObject gameObjectToAttach, ICModule[] modules, CoroutineDelegate before, CoroutineDelegate after)
        {
            CDebug.Assert(gameObjectToAttach != null && modules != null);
            CCosmosEngine engine = gameObjectToAttach.AddComponent<CCosmosEngine>();
            engine.GameModules = modules;
            engine.BeforeInitModules = before;
            engine.AfterInitModules = after;
            return engine;
        }

        private void Awake()
        {
            if (EngineInstance != null)
            {
                CDebug.LogError("Duplicated Instance CCosmosEngine!!!");
            }

            EngineInstance = this;

            Init();
        }

        private void Init()
        {
            IsRootUser = CTool.HasWriteAccessToFolder(Application.dataPath);  // Root User运行时，能穿越沙盒写DataPath, 以此为依据

            if (Debug.isDebugBuild)
            {
                RenderWatcher = new CFpsWatcher(0.95f);
            }

            if (Debug.isDebugBuild)
            {
                CDebug.Log("====================================================================================");
                CDebug.Log("Application.platform = {0}", Application.platform);
                CDebug.Log("Application.dataPath = {0} , WritePermission: {1}", Application.dataPath, IsRootUser);
                CDebug.Log("Application.streamingAssetsPath = {0} , WritePermission: {1}", Application.streamingAssetsPath, CTool.HasWriteAccessToFolder(Application.streamingAssetsPath));
                CDebug.Log("Application.persistentDataPath = {0} , WritePermission: {1}", Application.persistentDataPath, CTool.HasWriteAccessToFolder(Application.persistentDataPath));
                CDebug.Log("Application.temporaryCachePath = {0} , WritePermission: {1}", Application.temporaryCachePath, CTool.HasWriteAccessToFolder(Application.temporaryCachePath));
                CDebug.Log("Application.unityVersion = {0}", Application.unityVersion);
                CDebug.Log("SystemInfo.deviceModel = {0}", SystemInfo.deviceModel);
                CDebug.Log("SystemInfo.deviceUniqueIdentifier = {0}", SystemInfo.deviceUniqueIdentifier);
                CDebug.Log("====================================================================================");
            }
            StartCoroutine(DoInit());
        }

        /// <summary>
        /// Use Coroutine to initialize the two base modules: Resource & UI
        /// </summary>
        IEnumerator DoInit()
        {
            var baseModules = new ICModule[] {  // 基础三件套
            CResourceModule.Instance, 
            CUIModule.Instance, 
        };

            var startInitTime = 0f;
            foreach (ICModule mod in baseModules)
            {
                if (Debug.isDebugBuild)
                    startInitTime = Time.time;
                yield return StartCoroutine(mod.Init());
                if (Debug.isDebugBuild)
                    CDebug.Log("Init Module: #{0}# Time:{1}", mod.GetType().FullName, Time.time - startInitTime);
            }

            CDebug.Log("Finish Init ResourceManager + UIManager!");

            if (BeforeInitModules != null)
                yield return StartCoroutine(BeforeInitModules());


            yield return StartCoroutine(DoInitModules());
            if (AfterInitModules != null)
                yield return StartCoroutine(AfterInitModules());
        }

        IEnumerator DoInitModules()
        {
            var startInitTime = 0f;
            foreach (ICModule initModule in GameModules)
            {
                if (Debug.isDebugBuild)
                    startInitTime = Time.time;
                yield return StartCoroutine(initModule.Init());
                if (Debug.isDebugBuild)
                    CDebug.Log("Init Module: #{0}# Time:{1}", initModule.GetType().FullName, Time.time - startInitTime);
            }

        }
        void OnGUI()
        {
            if (Debug.isDebugBuild)
            {
                GUILayout.BeginVertical(GUILayout.Width(300));
                GUILayout.Label(string.Format("CodeMemory: {0}KB", GC.GetTotalMemory(false) / 1000f));
                GUILayout.Label(RenderWatcher.Watch("FPS: {0:N0}", 1f / Time.deltaTime));
                GUILayout.EndVertical();
            }
        }

        private static TableFile<CCosmosEngineConfig> Confs;

        /// <summary>
        /// Ensure the CEngineConfig file loaded.
        /// </summary>
        static void EnsureConfigTab()
        {
            if (Confs == null)
            {
                TextAsset textAsset;
                textAsset = Resources.Load<TextAsset>("CEngineConfig");

                CDebug.Assert(textAsset);
                Confs = new TableFile<CCosmosEngineConfig>(new TableFileConfig
                {
                    Content = textAsset.text,
                    OnExceptionEvent = (ex, args) =>
                    {
                        if (ex != TableFileExceptionType.DuplicatedKey)
                        {
                            throw new Exception(ex.ToString());
                        }
                    },

                });
            }
        }

        /// <summary>
        /// Get Config from the CEngineConfig file through key
        /// </summary>
        public static string GetConfig(string key)
        {
            EnsureConfigTab();

            var conf = Confs.FindByPrimaryKey(key);
            if (conf == null)
            {
                CDebug.LogError("Cannot get CosmosConfig: {0}", key);
                return null;
            }
            return conf.Value;
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
}

/// <summary>
/// Engine Config
/// </summary>
public class CCosmosEngineConfig : TabRow
{
    [TabColumn]
    public string Key;
    [TabColumn]
    public string Value;

    public override object PrimaryKey
    {
        get { return Key; }
    }

    public CCosmosEngineConfig() { }

    public override bool IsAutoParse
    {
        get { return true; }
    }
}
