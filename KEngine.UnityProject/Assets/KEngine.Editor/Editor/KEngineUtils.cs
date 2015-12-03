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
using UnityEditor;
using UnityEngine;

namespace KEngine.Editor
{
    public class KEngineUtils : EditorWindow
    {
        public static readonly AppVersion KEngineVersion = new AppVersion("0.9.1.0.beta");

        static KEngineUtils()
        {
            EnsureConfigFile();
        }

        private static string ConfFilePath = "Assets/Resources/KEngineConfig.txt";

        // 默認的配置文件內容
        private static string[][] DefaultConfigs = new string[][]
        {
            new string[] {"ProductRelPath", "BuildProduct/", ""},
            new string[] {"AssetBundleBuildRelPath", "StreamingAssets/", "The Relative path to build assetbundles"},
            new string[] {"AssetBundleExt", ".unity3d", "Asset bundle file extension"},
            new string[] {"IsLoadAssetBundle", "1", "Asset bundle or in resources?"},
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
                Instance = KEngineUtils .GetWindow<KEngineUtils>(true, "KEngine Options");
            } 
            Instance.Show(); 
        }

        readonly GUIStyle _headerStyle = new GUIStyle();
        KEngineUtils()
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
        public static void SetAppVersion(AppVersion appVersion)
        {
            EnsureConfigFile();

            SetConfValue(KEngineDefaultConfigs.AppVersion.ToString(), appVersion.ToString());

            Logger.Log("Save AppVersion to KEngineConfig.txt: {0}", appVersion.ToString());
        }

        private static void SetConfValue(string key, string value)
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
            AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
            GUILayout.Label(string.Format("KEngine Options"), _headerStyle);
            EditorGUILayout.LabelField("KEngine Version:", KEngineVersion.ToString());

            EditorGUILayout.Space();

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