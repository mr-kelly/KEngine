using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class XBuild_Audio : AutoBuildBase
{
	public override string GetDirectory() { return "Audio"; }
	public override string GetExtention() { return "dir"; }

	public override void BeginExport()
	{
	}

	public override void Export(string path)
	{
		if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows)
		{
			ExportPkg(path);
		}
		else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ||
			EditorUserBuildSettings.activeBuildTarget == BuildTarget.iPhone)
		{
			//ExportMobile(path);
            ExportPkg(path);
		}
		else
		{
			CBase.LogError("Error Build Audio {0}", path);
		}
	}

	public override void EndExport()
	{
	}

	void ExportPkg(string path)
	{
		path = path.Replace('\\', '/');

		DirectoryInfo	dirInfo			= new DirectoryInfo(path);
		FileInfo[]		fileInfoList	= dirInfo.GetFiles();

		foreach (FileInfo fileInfo in fileInfoList)
		{
			string file = string.Format("{0}/{1}", path, fileInfo.Name);
            if (!BuildPkg(file))
                continue;

		}
	}
    public bool BuildPkg(string file)
    {
        //if (!CBuildTools.CheckNeedBuild(file))
        //{
        //    return false;
        //}

        AudioClip audioClip = AssetDatabase.LoadAssetAtPath(file, typeof(AudioClip)) as AudioClip;
        if (audioClip != null)
        {
            string subDirName = Path.GetFileName(Path.GetDirectoryName(file));
            string exportFile = string.Format("Audio/{0}/{1}_Audio{2}", subDirName, Path.GetFileNameWithoutExtension(file), CCosmosEngine.GetConfig("AssetBundleExt"));

            CBuildTools.BuildAssetBundle(audioClip, exportFile);

            //CBuildTools.MarkBuildVersion(file);
        }

        return true;
    }

    //void ExportMobile(string path)
    //{
    //    path = path.Replace('\\', '/');

    //    DirectoryInfo	dirInfo			= new DirectoryInfo(path);
    //    FileInfo[]		fileInfoList	= dirInfo.GetFiles();

    //    foreach (FileInfo fileInfo in fileInfoList)
    //    {
    //        string file = string.Format("{0}/{1}", path, fileInfo.Name);
    //        if (!XBuildTools.CheckNeedBuild(file))
    //            continue;

    //        XBuildTools.MarkBuildVersion(file);
    //    }

    //    string srcPath	= GetDirectory() + path.Substring(path.LastIndexOf('/'));
    //    string destPath = XBuildTools.MakeSureExportPath(srcPath, EditorUserBuildSettings.activeBuildTarget);
    //    XBuildTools.CopyFolder(path, destPath);
    //}
}
