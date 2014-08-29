using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public class CCommonProductPrefabExporter : AutoBuildBase
{
    public override string GetDirectory() { return ""; }
    public override string GetExtention() { return "dir"; }

    public override void BeginExport()
    {
    }
    public override void Export(string path)
    {
        path = path.Replace('\\', '/');

        string[] fileArray = Directory.GetFiles(path, "*.prefab");
        foreach (string file in fileArray)
        {
            string filePath = file.Replace('\\', '/');
            CBase.Log("Build Func To: " + filePath);
        }
    }

    public override void EndExport()
    {
    }

    [MenuItem("CosmosEngine/Build Product Folder Prefabs")]
    static void BuildProductFolderPrefabs()
    {
        CAutoResourceBuilder.ProductExport(new CCommonProductPrefabExporter());
    }
}
