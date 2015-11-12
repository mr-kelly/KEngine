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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using CosmosTable;

namespace KEngine
{

    /// <summary>
    /// Cosmos Engine - Unity3D Game Develop Framework
    /// </summary>
    public class AppEngine : MonoBehaviour
    {
        public static bool IsDebugBuild { get; private set; }  // cache Debug.isDebugBuild for multi thread
        public static bool ShowFps = Debug.isDebugBuild;
        /// <summary>
        /// To Display FPS in the Debug Mode (Debug.isDebugBuild is true)
        /// </summary>
        static CFpsWatcher RenderWatcher;  // 帧数监听器

        /// <summary>
        /// In Init func has a check if the user has the write privillige
        /// </summary>
        public static bool IsRootUser;  // 是否越狱iOS

        public static AppEngine EngineInstance { get; private set; }

        private static TableFile<CCosmosEngineInfo> _configsTable;

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

        public static AppEngine New(GameObject gameObjectToAttach, ICModule[] modules)
        {
            return New(gameObjectToAttach, modules, null, null);
        }

        /// <summary>
        /// Engine entry.... all begins from here
        /// </summary>
        public static AppEngine New(GameObject gameObjectToAttach, ICModule[] modules, CoroutineDelegate before, CoroutineDelegate after)
        {
            Logger.Assert(gameObjectToAttach != null && modules != null);
            AppEngine appEngine = gameObjectToAttach.AddComponent<AppEngine>();
            appEngine.GameModules = modules;
            appEngine.BeforeInitModules = before;
            appEngine.AfterInitModules = after;
            return appEngine;
        }

        private void Awake()
        {
            IsDebugBuild = Debug.isDebugBuild;

            if (EngineInstance != null)
            {
                Logger.LogError("Duplicated Instance Engine!!!");
            }

            EngineInstance = this;

            Init();
        }

        private void Init()
        {
            IsRootUser = CTool.HasWriteAccessToFolder(Application.dataPath);  // Root User运行时，能穿越沙盒写DataPath, 以此为依据

            if (ShowFps)
            {
                RenderWatcher = new CFpsWatcher(0.95f);
            }

            if (Debug.isDebugBuild)
            {
                Logger.Log("====================================================================================");
                Logger.Log("Application.platform = {0}", Application.platform);
                Logger.Log("Application.dataPath = {0} , WritePermission: {1}", Application.dataPath, IsRootUser);
                Logger.Log("Application.streamingAssetsPath = {0} , WritePermission: {1}", Application.streamingAssetsPath, CTool.HasWriteAccessToFolder(Application.streamingAssetsPath));
                Logger.Log("Application.persistentDataPath = {0} , WritePermission: {1}", Application.persistentDataPath, CTool.HasWriteAccessToFolder(Application.persistentDataPath));
                Logger.Log("Application.temporaryCachePath = {0} , WritePermission: {1}", Application.temporaryCachePath, CTool.HasWriteAccessToFolder(Application.temporaryCachePath));
                Logger.Log("Application.unityVersion = {0}", Application.unityVersion);
                Logger.Log("SystemInfo.deviceModel = {0}", SystemInfo.deviceModel);
                Logger.Log("SystemInfo.deviceUniqueIdentifier = {0}", SystemInfo.deviceUniqueIdentifier);
                Logger.Log("====================================================================================");
            }
            StartCoroutine(DoInit());
        }

        /// <summary>
        /// Use Coroutine to initialize the two base modules: Resource & UI
        /// </summary>
        IEnumerator DoInit()
        {
            var baseModules = new ICModule[] {  // 基础2件套
                CResourceModule.Instance, 
                CUIModule.Instance, 
            };

            yield return StartCoroutine(DoInitModules(baseModules));

            Logger.Log("Finish Init ResourceManager + UIManager!");

            if (BeforeInitModules != null)
                yield return StartCoroutine(BeforeInitModules());


            yield return StartCoroutine(DoInitModules(GameModules));
            if (AfterInitModules != null)
                yield return StartCoroutine(AfterInitModules());

        }

        IEnumerator DoInitModules(IList<ICModule> modules)
        {
            var startInitTime = 0f;
            var startMem = 0f;
            foreach (ICModule initModule in modules)
            {
                if (Debug.isDebugBuild)
                {
                    startInitTime = Time.time;
                    startMem = GC.GetTotalMemory(false);
                }
                yield return StartCoroutine(initModule.Init());
                if (Debug.isDebugBuild)
                {
                    var nowMem = GC.GetTotalMemory(false);
                    Logger.Log("Init Module: #{0}# Time:{1}, UseMem:{2}, NowMem:{3}", initModule.GetType().FullName,
                        Time.time - startInitTime, nowMem - startMem, nowMem);
                }
            }

        }
        void OnGUI()
        {
            if (ShowFps)
            {
                GUILayout.BeginVertical(GUILayout.Width(300));
                GUILayout.Label(string.Format("CodeMemory: {0}KB", GC.GetTotalMemory(false) / 1000f));
                GUILayout.Label(RenderWatcher.Watch("FPS: {0:N0}", 1f / Time.deltaTime));
                GUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Ensure the CEngineConfig file loaded.
        /// </summary>
        public static TableFile<CCosmosEngineInfo> EnsureConfigTab(bool reload = false)
        {
            if (_configsTable == null || reload)
            {
                TextAsset textAsset;
                textAsset = Resources.Load<TextAsset>("CEngineConfig");

                Logger.Assert(textAsset);
                _configsTable = new TableFile<CCosmosEngineInfo>(new TableFileConfig
                {
                    Content = textAsset.text,
                    OnExceptionEvent = (ex, args) =>
                    {
                        if (ex != TableFileExceptionType.DuplicatedKey)
                        {
                            var sb = new StringBuilder();
                            sb.Append(ex.ToString());
                            sb.Append(": ");
                            foreach (var s in args)
                            {
                                sb.Append(s);
                                sb.Append(", ");
                            }
                            throw new Exception(sb.ToString());
                        }
                    },

                });
            }
            return _configsTable;
        }

        /// <summary>
        /// Get Config from the CEngineConfig file through key
        /// </summary>
        public static string GetConfig(string key)
        {
            EnsureConfigTab();

            var conf = _configsTable.FindByPrimaryKey(key);
            if (conf == null)
            {
                Logger.LogError("Cannot get CosmosConfig: {0}", key);
                return null;
            }
            return conf.Value;
        }
        public static string GetConfig(CCosmosEngineDefaultConfig cfg)
        {
            return GetConfig(cfg.ToString());
        }
    }

    public enum CCosmosEngineDefaultConfig
    {
        AssetBundleExt,
        ProductRelPath,
        AssetBundleBuildRelPath,  // FromRelPath

        BundlesFolderName, // StreamingAssets inner folder name
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


    /// <summary>
    /// Engine Config
    /// </summary>
    public class CCosmosEngineInfo : TableRowInfo
    {
        public string Key;
        public string Value;

        public override object PrimaryKey
        {
            get { return Key; }
        }

        public CCosmosEngineInfo() { }

        public override bool IsAutoParse
        {
            get { return true; }
        }
    }

}

