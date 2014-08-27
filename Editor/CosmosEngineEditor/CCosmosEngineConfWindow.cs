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

    // 默認的配置文件內容
    static string[][] DefaultConfigFileContent = new string[][]{
        new string[] {"ProductRelPath", "BuildProduct/", ""},
        new string[] {"AssetBundleRelPath", "StreamingAssets/", "The Relative path to build assetbundles"},
       new string[] { "AssetBundleExt", ".unity3d", "Asset bundle file extension"},
    };

    [MenuItem("CosmosEngine/Configuration")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:		
        CheckConfigFile();
        var window = ScriptableObject.CreateInstance<CCosmosEngineWindow>();
        window.Show();
    }

    static void CheckConfigFile()
    {
        string confPath = "Assets/Resources/CEngineConfig.txt";
        if (!File.Exists(confPath))
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
            confFile.Save(confPath);

            CBase.Log("新建CosmosEngine配置文件: {0}", confPath);
            AssetDatabase.Refresh();
        }
    }
    void OnGUI()
    {
        EditorGUILayout.LabelField("== Configure the CosmosEngine ==");
        EditorGUILayout.TextField("Product,", "Field");
    }
}
