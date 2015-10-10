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
using CosmosEngine;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace CosmosEngine.Editor
{
    [CustomEditor(typeof(CCosmosEngine))]
    public class CCosmosEngineInspector : UnityEditor.Editor
    {
        static CCosmosEngineInspector()
        {
            SceneView.onSceneGUIDelegate -= OnSceneViewGUI;
            SceneView.onSceneGUIDelegate += OnSceneViewGUI;
        }

        static void OnSceneViewGUI(SceneView view)
        {
            // 检查编译中，立刻暂停游戏！
            if (EditorApplication.isCompiling)
            {
                EditorApplication.isPlaying = false;
            }

        }
    }
    public class CCosmosEngineWindow : EditorWindow
    {
        static CCosmosEngineWindow()
        {
            CheckConfigFile();
        }

        static string ConfFilePath = "Assets/Resources/CEngineConfig.txt";

        // 默認的配置文件內容
        static string[][] DefaultConfigFileContent = new string[][]{
        new string[] {"ProductRelPath", "BuildProduct/", ""},
        new string[] {"AssetBundleBuildRelPath", "StreamingAssets/", "The Relative path to build assetbundles"},
       new string[] { "AssetBundleExt", ".unity3d", "Asset bundle file extension"},
       new string[] { "IsLoadAssetBundle", "1", "Asset bundle or in resources?"},
    };

        static CCosmosEngineWindow Instance;

        static CTabFile ConfFile;

        [MenuItem("CosmosEngine/Configuration")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:		
            CheckConfigFile();

            if (Instance == null)
            {
                Instance = ScriptableObject.CreateInstance<CCosmosEngineWindow>();
            }
            Instance.Show();
        }

        static void CheckConfigFile()
        {
            if (!File.Exists(ConfFilePath))
            {
                CTabFile confFile = new CTabFile();
                confFile.NewColumn("Key");
                confFile.NewColumn("Value");
                confFile.NewColumn("Comment");


                foreach (string[] strArr in DefaultConfigFileContent)
                {
                    int row = confFile.NewRow();
                    confFile.SetValue<string>(row, "Key", strArr[0]);
                    confFile.SetValue<string>(row, "Value", strArr[1]);
                    confFile.SetValue<string>(row, "Comment", strArr[2]);
                }
                confFile.Save(ConfFilePath);

                CDebug.Log("新建CosmosEngine配置文件: {0}", ConfFilePath);
                AssetDatabase.Refresh();
            }

            ConfFile = CTabFile.LoadFromFile(ConfFilePath);
        }

        string GetConfValue(string key)
        {
            foreach (CTabFile.RowInterator row in ConfFile)
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

        void SetConfValue(string key, string value)
        {
            foreach (CTabFile.RowInterator row in ConfFile)
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

        void OnGUI()
        {
            EditorGUILayout.LabelField("== Configure the CosmosEngine ==");

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("== Advanced Setting ==");
            bool tabDirty = false;
            foreach (CTabFile.RowInterator row in ConfFile)
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
                ConfFile.Save(ConfFilePath);
                AssetDatabase.Refresh();
            }


        }
    }
}
