using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

public partial class CBuildTools
{
	struct XVersionControlInfo
	{
		public string Server;
		public int Port;
		public string Database;
		public string User;
		public string Pass;
	};

	static int PushedAssetCount = 0;

    // 鉤子函數, 動態改變某些打包行為
    static void HookFunc(string funcName, params object[] args)
    {
        MethodInfo methodInfo = typeof(CBuildTools).GetMethod(funcName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (methodInfo == null)
        {
            CBase.LogWarning("Not Found HookFunc: {0}", funcName);
            return;
        }
        methodInfo.Invoke(null, args);
    }

    #region 打包功能
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
                path = basePath + CResourceManager.GetBuildPlatformName() + "/";
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

	public static void ShowDialog(string msg, string title = "提示", string button = "确定")
	{
		EditorUtility.DisplayDialog(title, msg, button);
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
		Debug.LogWarning(string.Format(fmt, args));
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

		if ((prefabType == PrefabType.None && AssetDatabase.GetAssetPath(asset) == string.Empty) ||
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


        HookFunc("BeforeBuildAssetBundle", asset, path, relativePath);

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

		CBase.Log("生成文件： {0}", path);


        HookFunc("AfterBuildAssetBundle", asset, path, relativePath);

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

}
