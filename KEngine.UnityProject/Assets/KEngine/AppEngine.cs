#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: AppEngine.cs
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CosmosTable;
using UnityEngine;

namespace KEngine
{
    /// <summary>
    /// Cosmos Engine - Unity3D Game Develop Framework
    /// </summary>
    public class AppEngine : MonoBehaviour
    {
        public static bool IsDebugBuild { get; private set; } // cache Debug.isDebugBuild for multi thread
        public bool ShowFps = Debug.isDebugBuild;

        /// <summary>
        /// To Display FPS in the Debug Mode (Debug.isDebugBuild is true)
        /// </summary>
        public static CFpsWatcher RenderWatcher { get; private set; } // 帧数监听器

        /// <summary>
        /// In Init func has a check if the user has the write privillige
        /// </summary>
        public static bool IsRootUser; // 是否越狱iOS

        public static AppEngine EngineInstance { get; private set; }

        private static TableFile<CCosmosEngineInfo> _configsTable;

        public static TableFile<CCosmosEngineInfo> ConfigsTable
        {
            get
            {
                EnsureConfigTab();
                return _configsTable;
            }
        }

        //private static AppVersion _appVersion = null;

        /// <summary>
        /// Get App Version from KEngineConfig.txt
        /// </summary>
        //public static AppVersion AppVersion
        //{
        //    get
        //    {
        //        if (_appVersion == null)
        //        {
        //            var appVersionStr = GetConfig(KEngineDefaultConfigs.AppVersion);
        //            if (string.IsNullOrEmpty(appVersionStr))
        //            {
        //                Logger.LogError("Cannot find AppVersion in KEngineConfig.txt, use 1.0.0.0 as default");
        //                appVersionStr = "1.0.0.0.alpha.default";
        //            }
        //            _appVersion = new AppVersion(appVersionStr);
        //        }
        //        return _appVersion;
        //    }
        //}

        /// <summary>
        /// Read Tab file (CEngineConfig.txt), cache to here
        /// </summary>
        /// <summary>
        /// Modules passed from the CosmosEngine.New function. All your custom game logic modules
        /// </summary>
        public KEngine.IModule[] GameModules { get; private set; }

        /// <summary>
        /// 是否初始化完成
        /// </summary>
        public bool IsInited { get; private set; }

        public delegate IEnumerator CoroutineDelegate();

        private CoroutineDelegate BeforeInitModules = null;
        private CoroutineDelegate AfterInitModules = null;

        public static AppEngine New(GameObject gameObjectToAttach, IModule[] modules)
        {
            return New(gameObjectToAttach, modules, null, null);
        }

        /// <summary>
        /// Engine entry.... all begins from here
        /// </summary>
        public static AppEngine New(GameObject gameObjectToAttach, IModule[] modules, CoroutineDelegate before,
            CoroutineDelegate after)
        {
            Debuger.Assert(gameObjectToAttach != null && modules != null);
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
            IsRootUser = KTool.HasWriteAccessToFolder(Application.dataPath); // Root User运行时，能穿越沙盒写DataPath, 以此为依据

            if (Debug.isDebugBuild)
            {
                Logger.Log("====================================================================================");
                Logger.Log("Application.platform = {0}", Application.platform);
                Logger.Log("Application.dataPath = {0} , WritePermission: {1}", Application.dataPath, IsRootUser);
                Logger.Log("Application.streamingAssetsPath = {0} , WritePermission: {1}",
                    Application.streamingAssetsPath, KTool.HasWriteAccessToFolder(Application.streamingAssetsPath));
                Logger.Log("Application.persistentDataPath = {0} , WritePermission: {1}", Application.persistentDataPath,
                    KTool.HasWriteAccessToFolder(Application.persistentDataPath));
                Logger.Log("Application.temporaryCachePath = {0} , WritePermission: {1}", Application.temporaryCachePath,
                    KTool.HasWriteAccessToFolder(Application.temporaryCachePath));
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
        private IEnumerator DoInit()
        {
            var baseModules = new KEngine.IModule[]
            {
                // 基础2件套
                KResourceModule.Instance,
            };

            yield return StartCoroutine(DoInitModules(baseModules));

            Logger.Log("Finish Init ResourceManager + UIManager!");

            if (BeforeInitModules != null)
                yield return StartCoroutine(BeforeInitModules());


            yield return StartCoroutine(DoInitModules(GameModules));
            if (AfterInitModules != null)
                yield return StartCoroutine(AfterInitModules());

            IsInited = true;
        }

        private IEnumerator DoInitModules(IList<IModule> modules)
        {
            var startInitTime = 0f;
            var startMem = 0f;
            foreach (IModule initModule in modules)
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

        private void OnGUI()
        {
            if (ShowFps)
            {
                if (RenderWatcher == null)
                    RenderWatcher = new CFpsWatcher(0.95f);
                GUILayout.BeginVertical(GUILayout.Width(300));
                GUILayout.Label(string.Format("Memory: {0:F3}KB", UnityEngine.Profiler.GetMonoUsedSize() / 1024f));
                GUILayout.Label(RenderWatcher.Watch("FPS: {0:N0}", 1f / Time.deltaTime));
                GUILayout.EndVertical();
            }
        }
        static string ConfigFilePath = "Assets/Resources/KEngineConfig.txt";
        /// <summary>
        /// Ensure the CEngineConfig file loaded.
        /// </summary>
        public static TableFile<CCosmosEngineInfo> EnsureConfigTab(bool reload = false)
        {
            if (_configsTable == null || reload)
            {
                string configContent;
                if (Application.isEditor && !Application.isPlaying)
                {
                    // prevent Resources.Load fail on Batch Mode
                    configContent = System.IO.File.ReadAllText(ConfigFilePath);
                }
                else
                {
                    var textAsset = Resources.Load<TextAsset>("KEngineConfig");
                    Debuger.Assert(textAsset);
                    configContent = textAsset.text;
                }

                _configsTable = new TableFile<CCosmosEngineInfo>(new TableFileConfig
                {
                    Content = configContent, 
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
        /// Check whetehr exist a config key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool HasConfig(string key)
        {
            return ConfigsTable.HasPrimaryKey(key);
        }

        /// <summary>
        /// Get Config from the CEngineConfig file through key
        /// </summary>
        public static string GetConfig(string key, bool showLog = true)
        {
            EnsureConfigTab();

            var conf = ConfigsTable.FindByPrimaryKey(key);
            if (conf == null)
            {
                if (showLog)
                    Logger.LogError("Cannot get CosmosConfig: {0}", key);
                return null;
            }
            return conf.Value;
        }

        public static string GetConfig(KEngineDefaultConfigs cfg)
        {
            return GetConfig(cfg.ToString());
        }

        public static void SetConfig(string key, string value)
        {
            EnsureConfigTab();
            if (!Application.isEditor)
            {
                Logger.LogError("Set Config is Editor only");
                return;
            }

            var item = ConfigsTable.FindByPrimaryKey(key);
            var writer = new TabFileWriter<CCosmosEngineInfo>(ConfigsTable);
            var row = writer.GetRow(item.RowNumber);
            row.Value = value;

            writer.Save(ConfigFilePath);
        }
    }

    public enum KEngineDefaultConfigs
    {
        AssetBundleExt,
        ProductRelPath,
        AssetBundleBuildRelPath, // FromRelPath

        StreamingBundlesFolderName,
        // StreamingAssets inner folder name, when build, will link the Bundle build Path to here
    }

    public class CFpsWatcher
    {
        private float Value;
        private float Sensitivity;

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
        public override bool IsAutoParse
        {
            get { return true; }
        }
    }
}