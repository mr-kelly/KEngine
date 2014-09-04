//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                         version 0.8
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CCosmosEngineWindow : EditorWindow
{
    static CCosmosEngineWindow()
    {
        CheckConfigFile();
    }

    enum CUIBridgeType
    {
        NO_UI,
        NGUI,
    }
    static string ConfFilePath = "Assets/Resources/CEngineConfig.txt";

    // 默認的配置文件內容
    static string[][] DefaultConfigFileContent = new string[][]{
        new string[] {"ProductRelPath", "BuildProduct/", ""},
        new string[] {"AssetBundleRelPath", "StreamingAssets/", "The Relative path to build assetbundles"},
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
            confFile.NewRow();
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

            CBase.Log("新建CosmosEngine配置文件: {0}", ConfFilePath);
            AssetDatabase.Refresh();
        }

        ConfFile = CTabFile.LoadFromFile(ConfFilePath);
    }

    string GetConfValue(string key)
    {
        foreach (CTabFile.CTabRow row in ConfFile)
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
        foreach (CTabFile.CTabRow row in ConfFile)
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

    void OnGUI_SetUIBridge()
    {
        string uiBridgeType = GetConfValue("UIBridgeType");
        CUIBridgeType curUiType = CUIBridgeType.NO_UI;
        if (uiBridgeType == "NGUI")
        {
            curUiType = CUIBridgeType.NGUI;
        }


        CUIBridgeType selUiType = (CUIBridgeType)EditorGUILayout.EnumPopup("Use UI Bridge", curUiType);
        if (selUiType != curUiType)
        {
            string uiType = selUiType.ToString();
            DoSetUIBridge(uiType);
            SetConfValue("UIBridgeType", uiType);
        }
    }

    void RemoveDefineSymbols(string symbol)
    {
        foreach (BuildTargetGroup target in System.Enum.GetValues(typeof(BuildTargetGroup)))
        {
            string symbolStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            List<string> symbols = new List<string>(symbolStr.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));
            if (symbols.Contains(symbol))
                symbols.Remove(symbol);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", symbols.ToArray()));
        }


    }

    void AddDefineSymbols(string symbol)
    {
        foreach (BuildTargetGroup target in System.Enum.GetValues(typeof(BuildTargetGroup)))
        {
            string symbolStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            List<string> symbols = new List<string>(symbolStr.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));
            if (!symbols.Contains(symbol))
            {
                symbols.Add(symbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", symbols.ToArray()));
            }
        }
    }

    void DoSetUIBridge(string uiType)
    {
        switch (uiType)
        {
            case "NGUI":
                AddDefineSymbols("NGUI");

                break;
            default:
                RemoveDefineSymbols("NGUI");
                break;
        }
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("== Configure the CosmosEngine ==");
        OnGUI_SetUIBridge();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("== Advanced Setting ==");
        bool tabDirty = false;
        foreach (CTabFile.CTabRow row in ConfFile)
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
