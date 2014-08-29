using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

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
    void OnGUI()
    {
        EditorGUILayout.LabelField("== Configure the CosmosEngine ==");
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
