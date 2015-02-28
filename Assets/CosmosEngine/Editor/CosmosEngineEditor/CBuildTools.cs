//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                     Version 0.8 (20140904)
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------

using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public partial class CBuildTools
{
	static int PushedAssetCount = 0;

    public static int BuildCount = 0;  // 累计Build了多少次，用于版本控制时用的

    public static event Action<UnityEngine.Object, string, string> BeforeBuildAssetBundleEvent;
    public static event Action<UnityEngine.Object, string, string> AfterBuildAssetBundleEvent;
    
    #region 打包功能
    /// <summary>
    /// 获取完整的打包路径，并确保目录存在
    /// </summary>
    /// <param name="path"></param>
    /// <param name="buildTarget"></param>
    /// <returns></returns>
	public static string MakeSureExportPath(string path, BuildTarget buildTarget)
	{
		path = CBuildTools.GetExportPath(buildTarget) + path;
        
		string exportDirectory = path.Substring(0, path.LastIndexOf('/'));

		if (!System.IO.Directory.Exists(exportDirectory))
			System.IO.Directory.CreateDirectory(exportDirectory);

		return path;
	}

	public static string GetExportPath(BuildTarget platfrom)
	{
        string basePath = Path.GetFullPath(Application.dataPath + "/" + CCosmosEngine.GetConfig("AssetBundleRelPath") + "/");

		if (!Directory.Exists(basePath))
		{
			CBuildTools.ShowDialog("路径配置错误: " + basePath);
			throw new System.Exception("路径配置错误");
		}

		string path = null;
		switch (platfrom)
		{
			case BuildTarget.Android:
			case BuildTarget.iPhone:
			case BuildTarget.StandaloneWindows:
                path = basePath + CResourceModule.GetBuildPlatformName() + "/";
				break;
			default:
				CBuildTools.ShowDialog("构建平台配置错误");
				throw new System.Exception("构建平台配置错误");
		}
		return path;
	}

	public static void ClearConsole()
	{
		Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
		System.Type type = assembly.GetType("UnityEditorInternal.LogEntries");
		MethodInfo method = type.GetMethod("Clear");
		method.Invoke(null, null);
	}

	public static bool ShowDialog(string msg, string title = "提示", string button = "确定")
	{
		return EditorUtility.DisplayDialog(title, msg, button);
	}
    public static void ShowDialogSelection(string msg, Action yesCallback)
    {
        if (EditorUtility.DisplayDialog("确定吗", msg, "是!", "不！"))
        {
            yesCallback();
        }
    }
    public static void PushAssetBundle(Object asset, string path)
	{
		BuildPipeline.PushAssetDependencies();
		BuildAssetBundle(asset, path);
		PushedAssetCount++;
	}

	public static void PopAllAssetBundle()
	{
		for (int i = 0; i < PushedAssetCount; ++i)
		{
			BuildPipeline.PopAssetDependencies();
		}
		PushedAssetCount = 0;
	}

    public static void PopAssetBundle()
    {
        BuildPipeline.PopAssetDependencies();
        PushedAssetCount--;
    }
    #endregion

    public static void BuildError(string fmt, params string[] args)
	{
		fmt = "[BuildError]" + fmt;
		Debug.LogError(string.Format(fmt, args));
	}

	public static uint BuildAssetBundle(Object asset, string path)
	{
		return BuildAssetBundle(asset, path, EditorUserBuildSettings.activeBuildTarget);
	}

	public static uint BuildAssetBundle(Object asset, string path, BuildTarget buildTarget)
	{
		if (asset == null || string.IsNullOrEmpty(path)) 
		{
			BuildError("BuildAssetBundle: {0}", path);
			return 0;
		}
        
		string tmpPrefabPath = string.Format("Assets/{0}.prefab", asset.name);
		PrefabType prefabType = PrefabUtility.GetPrefabType(asset);
		GameObject tmpObj = null;
		Object tmpPrefab = null;

        string relativePath = path;
		path = MakeSureExportPath(path, buildTarget);

	    if (asset is Texture)
	    {
            //asset = asset; // Texutre不复制拷贝一份
	    }
		else if ((prefabType == PrefabType.None && AssetDatabase.GetAssetPath(asset) == string.Empty) ||
			(prefabType == PrefabType.ModelPrefabInstance))
		{
			tmpObj = (GameObject)GameObject.Instantiate(asset);
			tmpPrefab = PrefabUtility.CreatePrefab(tmpPrefabPath, tmpObj, ReplacePrefabOptions.ConnectToPrefab);
			asset = tmpPrefab;
		}
		else if (prefabType == PrefabType.PrefabInstance)
		{
			asset = PrefabUtility.GetPrefabParent(asset);
		}

	    if (BeforeBuildAssetBundleEvent != null)
	        BeforeBuildAssetBundleEvent(asset, path, relativePath);

		uint crc;
		BuildPipeline.BuildAssetBundle(
			asset,
			null,
			path,
			out crc,
			BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle,
			buildTarget);

		if (tmpObj != null)
		{
			GameObject.DestroyImmediate(tmpObj);
			AssetDatabase.DeleteAsset(tmpPrefabPath);
		}

		CDebug.Log("生成文件： {0}", path);
        BuildCount++;
        if (AfterBuildAssetBundleEvent != null)
            AfterBuildAssetBundleEvent(asset, path, relativePath);

		return crc;
	}

	public static uint BuildScriptableObject<T>(T scriptObject, string path) where T : ScriptableObject
	{
		return BuildScriptableObject(scriptObject, path, EditorUserBuildSettings.activeBuildTarget);
	}

	public static uint BuildScriptableObject<T>(T scriptObject, string path, BuildTarget buildTarget) where T : ScriptableObject
	{
		const string tempAssetPath = "Assets/~Temp.asset";
		AssetDatabase.CreateAsset(scriptObject, tempAssetPath);
		T tempObj = (T)AssetDatabase.LoadAssetAtPath(tempAssetPath, typeof(T));

		if (tempObj == null)
		{
			throw new System.Exception();
		}

		uint crc = CBuildTools.BuildAssetBundle(tempObj, path, buildTarget);
		AssetDatabase.DeleteAsset(tempAssetPath);

		return crc;
	}

	public static void CopyFolder(string sPath, string dPath)
	{
		if (!Directory.Exists(dPath))
		{
			Directory.CreateDirectory(dPath);
		}

		DirectoryInfo sDir = new DirectoryInfo(sPath);
		FileInfo[] fileArray = sDir.GetFiles();
		foreach (FileInfo file in fileArray)
		{
			if (file.Extension != ".meta")
				file.CopyTo(dPath + "/" + file.Name, true);
		}

		DirectoryInfo[] subDirArray = sDir.GetDirectories();
		foreach (DirectoryInfo subDir in subDirArray)
		{
			CopyFolder(subDir.FullName, dPath + "/" + subDir.Name);
		}
	}

    /// <summary>
    /// 是否有指定宏呢
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public static bool HasDefineSymbol(string symbol)
    {
        string symbolStrs = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        List<string> symbols = new List<string>(symbolStrs.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));
        return symbols.Contains(symbol);
    }

    /// <summary>
    /// 移除指定宏
    /// </summary>
    /// <param name="symbol"></param>
    public static void RemoveDefineSymbols(string symbol)
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

    /// <summary>
    /// 添加指定宏（不重复）
    /// </summary>
    /// <param name="symbol"></param>
    public static void AddDefineSymbols(string symbol)
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


    // 执行Python文件！获取返回值
    public static string ExecutePyFile(string pyFileFullPath, string arguments)
    {
        var guids = AssetDatabase.FindAssets("py");
        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(assetPath) == "py.exe")
            {
                string pythonExe = assetPath;  // Python地址

                string allOutput = null;
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = pythonExe;
                    process.StartInfo.Arguments = pyFileFullPath + " " + arguments;
                    process.StartInfo.UseShellExecute = false;
                    //process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();

                    allOutput = process.StandardOutput.ReadToEnd();

                    process.WaitForExit();

                }

                return allOutput;
            }

        }
        CDebug.LogError("无法找到py.exe或执行失败");
        return null;
    }

    #region 资源版本管理相关
    class BuildRecord
    {
        public string MD5;
        public int ChangeCount;
        public string DateTime;
        public BuildRecord()
        {
            MD5 = null;
            DateTime = System.DateTime.Now.ToString();
            ChangeCount = 0;
        }
        public BuildRecord(string md5, string dt, int changeCount)
        {
            MD5 = md5;
            DateTime = dt;
            ChangeCount = changeCount;
        }

        public void Mark(string md5)
        {
            MD5 = md5;
            DateTime = System.DateTime.Now.ToString();
            ChangeCount++;
        }
    }

    static Dictionary<string, BuildRecord> BuildVersion;
    public static bool IsCheckMd5 = false;  // 跟AssetServer关联~如果true，函数才有效

    public static void WriteVersion()
    {
        string path = GetBuildVersionTab();// MakeSureExportPath(VerCtrlInfo.VerFile, EditorUserBuildSettings.activeBuildTarget);
        CTabFile tabFile = new CTabFile();
        tabFile.NewRow();
        tabFile.NewColumn("AssetPath");
        tabFile.NewColumn("AssetMD5");
        tabFile.NewColumn("AssetDateTime");
        tabFile.NewColumn("ChangeCount");

        foreach (var node in BuildVersion)
        {
            int row = tabFile.NewRow();
            tabFile.SetValue(row, "AssetPath", node.Key);
            tabFile.SetValue(row, "AssetMD5", node.Value.MD5);
            tabFile.SetValue(row, "AssetDateTime", node.Value.DateTime);
            tabFile.SetValue(row, "ChangeCount", node.Value.ChangeCount);
        }

        tabFile.Save(path);
    }

    public static void SetupHistory(bool rebuild)
    {
        IsCheckMd5 = true;
        BuildVersion.Clear();
        if (!rebuild)
        {
            string verFile = GetBuildVersionTab(); //MakeSureExportPath(VerCtrlInfo.VerFile, EditorUserBuildSettings.activeBuildTarget);
            CTabFile tabFile;
            if (File.Exists(verFile))
            {
                tabFile = CTabFile.LoadFromFile(verFile);

                for (int i = 1; i < tabFile.GetHeight(); ++i)
                {
                    BuildVersion[tabFile.GetString(i, "AssetPath")] =
                        new BuildRecord(
                            tabFile.GetString(i, "AssetMD5"),
                            tabFile.GetString(i, "AssetDateTime"),
                            tabFile.GetInteger(i, "ChangeCount"));
                }
            }
        }
        else
        {

        }
    }

    public static string GetAssetLastBuildMD5(string assetPath)
    {
        BuildRecord md5;
        if (BuildVersion.TryGetValue(assetPath, out md5))
        {
            return md5.MD5;
        }

        return "";
    }

    public static void ClearHistroy()
    {
        if (IsCheckMd5)
            IsCheckMd5 = false;

        BuildCount = 0;
        BuildVersion = new Dictionary<string, BuildRecord>();
    }

    // Prefab Asset打包版本號記錄
    public static string GetBuildVersionTab()
    {
        return Application.dataPath + "/" + CCosmosEngineDef.ResourcesBuildInfosDir + "/ArtBuildResource_" + CResourceModule.GetBuildPlatformName() + ".txt";
    }

    public static bool CheckNeedBuild(params string[] sourceFiles)
    {
        if (!IsCheckMd5)
            return true;

        foreach (string file in sourceFiles)
        {
            if (DoCheckNeedBuild(file) || DoCheckNeedBuild(file + ".meta"))
                return true;
        }

        return false;
    }

    private static bool DoCheckNeedBuild(string filePath)
    {
        BuildRecord assetMd5;
        if (!File.Exists(filePath))
            return false;
        if (!BuildVersion.TryGetValue(filePath, out assetMd5))
            return true;

        if (CTool.MD5_File(filePath) != assetMd5.MD5)
            return true;  // different

        return false;
    }

    public static void MarkBuildVersion(params string[] sourceFiles)
    {
        if (!IsCheckMd5)
            return;

        if (sourceFiles == null || sourceFiles.Length == 0)
            return;

        foreach (string file in sourceFiles)
        {
            //BuildVersion[file] = GetAssetVersion(file);
            BuildRecord theRecord;
            var nowMd5 = CTool.MD5_File(file);
            if (!BuildVersion.TryGetValue(file, out theRecord))
            {
                theRecord = BuildVersion[file] = new BuildRecord();
                theRecord.Mark(nowMd5);
            }
            else
            {
                if (nowMd5 != theRecord.MD5)
                {
                    theRecord.Mark(nowMd5);
                }
            }
            

            string metaFile = file + ".meta";
            if (File.Exists(metaFile))
            {
                BuildRecord theMetaRecord;
                var nowMetaMd5 = CTool.MD5_File(metaFile);
                if (!BuildVersion.TryGetValue(metaFile, out theMetaRecord))
                {
                    theMetaRecord = BuildVersion[metaFile] = new BuildRecord();
                    theMetaRecord.Mark(nowMetaMd5);
                }
                else
                {
                    if (nowMetaMd5 != theMetaRecord.MD5)
                        theMetaRecord.Mark(nowMetaMd5);    
                }
                
            }
        }
    }
    #endregion
}
