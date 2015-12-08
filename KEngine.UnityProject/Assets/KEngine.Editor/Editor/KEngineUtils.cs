#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KEngineUtils.cs
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
using System.IO;
using KUnityEditorTools;
using UnityEditor;
using UnityEngine;

namespace KEngine.Editor
{
    public class KEngineUtils : EditorWindow
    {
        public static readonly Version KEngineVersion = new Version("0.9.2.0");

        static KEngineUtils()
        {
            EnsureConfigFile();
        }

        private static string ConfFilePath = "Assets/Resources/KEngineConfig.txt";

        // 默認的配置文件內容
        private static string[][] DefaultConfigs = new string[][]
        {
            new string[] {"ProductRelPath", "../Product", ""},
            new string[] {"AssetBundleBuildRelPath", "../Product/Bundles", "The Relative path to build assetbundles"},
            new [] {"StreamingBundlesFolderName", "Bundles"},
            new [] {"UIModuleBridge","UGUI"},
            new string[] {"AssetBundleExt", ".bytes", "Asset bundle file extension"},
            new string[] {"IsLoadAssetBundle", "1", "Asset bundle or in resources?"},
            new [] {"SettingSourcePath", "../Product/SettingSource"},
            new [] {"SettingPath", "Resources/Setting"},

        };

        private static KEngineUtils Instance;

        private static KTabFile ConfFile;

        [MenuItem("KEngine/Options")]
        private static void Init()
        {
            // Get existing open window or if none, make a new one:		
            EnsureConfigFile();

            if (Instance == null)
            {
                Instance = KEngineUtils.GetWindow<KEngineUtils>(true, "KEngine Options");
            }
            Instance.Show();
        }

        private readonly GUIStyle _headerStyle = new GUIStyle();

        private KEngineUtils()
        {
            _headerStyle.fontSize = 22;
            _headerStyle.normal.textColor = Color.white;
        }

        private static void EnsureConfigFile()
        {
            if (!File.Exists(ConfFilePath))
            {
                KTabFile confFile = new KTabFile();
                confFile.NewColumn("Key");
                confFile.NewColumn("Value");
                confFile.NewColumn("Comment");


                foreach (string[] strArr in DefaultConfigs)
                {
                    int row = confFile.NewRow();
                    confFile.SetValue<string>(row, "Key", strArr[0]);
                    confFile.SetValue<string>(row, "Value", strArr[1]);
                    confFile.SetValue<string>(row, "Comment", strArr[2]);
                }
                confFile.Save(ConfFilePath);

                Logger.Log("新建CosmosEngine配置文件: {0}", ConfFilePath);
                AssetDatabase.Refresh();
            }

            ConfFile = KTabFile.LoadFromFile(ConfFilePath);
        }

        private string GetConfValue(string key)
        {
            foreach (KTabFile.RowInterator row in ConfFile)
            {
                string key2 = row.GetString("Key");
                if (key == key2)
                {
                    string value = row.GetString("Value");
                    return value;
                }
            }

            return null;
        }

        /// <summary>
        /// Set AppVersion of KEngineConfig.txt
        /// </summary>
        /// <param name="appVersion"></param>
        //public static void SaveAppVersion(AppVersion appVersion)
        //{
        //    EnsureConfigFile();

        //    SetConfValue(KEngineDefaultConfigs.AppVersion.ToString(), appVersion.ToString());

        //    Logger.Log("Save AppVersion to KEngineConfig.txt: {0}", appVersion.ToString());
        //}

        /// <summary>
        /// Set KEngineConfig.txt file,  and reload AppEngine's instance of EngineConfigs, (Editor only)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetConfValue(string key, string value)
        {
            foreach (KTabFile.RowInterator row in ConfFile)
            {
                string key2 = row.GetString("Key");
                if (key == key2)
                {
                    ConfFile.SetValue<string>(row.Row, "Value", value);
                }
            }
            ConfFile.Save(ConfFilePath);

            AppEngine.EnsureConfigTab(true); // reload current appengine instance

            AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
            GUILayout.Label(string.Format("KEngine Options"), _headerStyle);
            EditorGUILayout.LabelField("KEngine Version:", KEngineVersion.ToString());

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("== Addon ==");
            var isNgui = KDefineSymbolsHelper.HasDefineSymbol("NGUI");
            var newIsNgui = EditorGUILayout.Toggle("NGUI", isNgui);
            if (isNgui != newIsNgui)
            {
                if (newIsNgui)
                {
                    KDefineSymbolsHelper.AddDefineSymbols("NGUI");
                }
                else
                {
                    KDefineSymbolsHelper.RemoveDefineSymbols("NGUI");
                }
            }

            EditorGUILayout.LabelField("== KEngineConfig.txt ==");
            bool tabDirty = false;
            foreach (KTabFile.RowInterator row in ConfFile)
            {
                string value = row.GetString("Value");
                string newValue = EditorGUILayout.TextField(row.GetString("Key"), value);
                if (value != newValue)
                {
                    ConfFile.SetValue<string>(row.Row, "Value", newValue);
                    tabDirty = true;
                }
            }

            if (tabDirty)
            {
                SaveConfigFile();
            }
        }

        /// <summary>
        /// Save to EngineConfig
        /// </summary>
        private void SaveConfigFile()
        {
            ConfFile.Save(ConfFilePath);
            AssetDatabase.Refresh();
        }
    }
}