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
using KEngine;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

public class CAutoBuilder
{
    static string GetProjectName()
    {
        string[] s = Application.dataPath.Split('/');
        return s[s.Length - 2];
    }

    static string[] GetScenePaths()
    {
        string[] scenes = new string[EditorBuildSettings.scenes.Length];

        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }

        return scenes;
    }

    static void ParseArgs(ref BuildOptions opt, ref string outputpath)
    {
        string[] args = System.Environment.GetCommandLineArgs();

        string productPath = Path.Combine(Application.dataPath, KEngine.KEngine.GetConfig("ProductRelPath"));

        if (!Directory.Exists(productPath))
        {
            Directory.CreateDirectory(productPath);
        }

        if (args.Length >= 2)
        {
            CommandArgs commandArg = CommandLine.Parse(args);
            //List<string> lparams = commandArg.Params;
            Dictionary<string, string> argPairs = commandArg.ArgPairs;

            foreach (KeyValuePair<string, string> item in argPairs)
            {
                switch (item.Key)
                {
                    case "BundleVersion":
                        PlayerSettings.bundleVersion = item.Value;
                        break;
                    case "AndroidVersionCode":
                        PlayerSettings.Android.bundleVersionCode = System.Int32.Parse(item.Value);
                        break;
                    case "AndroidKeyStoreName":
                        PlayerSettings.Android.keystoreName = item.Value;
                        break;
                    case "AndroidKeyStorePass":
                        PlayerSettings.Android.keystorePass = item.Value;
                        break;
                    case "AndroidkeyAliasName":
                        PlayerSettings.Android.keyaliasName = item.Value;
                        break;
                    case "AndroidKeyAliasPass":
                        PlayerSettings.Android.keyaliasPass = item.Value;
                        break;
                    case "BuildOptions":
                        {
                            opt = BuildOptions.None;
                            string[] opts = item.Value.Split('|');
                            foreach (string o in opts)
                            {
                                opt = opt | (BuildOptions)System.Enum.Parse(typeof(BuildOptions), o);
                            }
                        }
                        break;
                    case "Outputpath":
                        outputpath = item.Value;
                        break;
                }
                UnityEngine.Debug.Log(item.Key + " : " + item.Value);
            }
        }
    }

    /// <summary>
    /// return full path or build
    /// </summary>
    /// <param name="outputpath"></param>
    /// <param name="tag"></param>
    /// <param name="opt"></param>
    /// <returns></returns>
    static string PerformBuild(string outputpath, BuildTarget tag, BuildOptions opt)
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(tag);

        ParseArgs(ref opt, ref outputpath);

        string fullPath = System.IO.Path.Combine(Application.dataPath, System.IO.Path.Combine(KEngine.KEngine.GetConfig("ProductRelPath"), outputpath));

        string fullDir = System.IO.Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(fullDir))
            Directory.CreateDirectory(fullDir);

        CDebug.Log("Build Client {0} to: {1}", tag, fullPath);
        BuildPipeline.BuildPlayer(GetScenePaths(), fullPath, tag, opt);

        return fullPath;
    }

    //public static int GetProgramVersion()
    //{
    //    var oldVersion = 0;
    //    if (File.Exists(GetProgramVersionFullPath()))
    //        oldVersion = File.ReadAllText(GetProgramVersionFullPath()).ToInt32();

    //    return oldVersion;
    //}

    //public static string GetProgramVersionFullPath()
    //{
    //    string programVersionFile = string.Format("{0}/Resources/ProgramVersion.txt", Application.dataPath);
    //    return programVersionFile;
    //}

    [MenuItem("CosmosEngine/AutoBuilder/WindowsX86D")]  // 注意，PC版本放在不一样的目录的！
    public static void PerformWinBuild()
    {
        PerformBuild("ClientX86D.exe", BuildTarget.StandaloneWindows, BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler);
    }

    //[MenuItem("File/AutoBuilder/WindowsX86")]
    //static void PerformWinReleaseBuild()
    //{
    //	PerformBuild(GetProjectName() + "X86.exe", BuildTarget.StandaloneWindows, BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler);
    //}

    [MenuItem("CosmosEngine/AutoBuilder/iOS")]
    public static void PerformiOSBuild()
    {
        PerformiOSBuild("App");        
    }

    public static string PerformiOSBuild(string ipaName, bool isDevelopment = true)
    {
        BuildOptions opt = isDevelopment
            ? (BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler)
            : BuildOptions.None;
        return PerformBuild("Apps/IOSProjects/" + ipaName, BuildTarget.iPhone, opt);
    }

    [MenuItem("CosmosEngine/AutoBuilder/Android")]
    public static void PerformAndroidBuild()
    {
        PerformAndroidBuild("StrikeHero");
    }

    public static string PerformAndroidBuild(string apkName, bool isDevelopment = true)
    {
        BuildOptions opt = isDevelopment
            ? (BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler)
            : BuildOptions.None;
        var path = string.Format("Apps/{0}/{1}.apk", "Android", apkName);
        return PerformBuild(path, BuildTarget.Android, opt);
    }

    [MenuItem("CosmosEngine/Clear PC PersitentDataPath")]
    public static void ClearPersistentDataPath()
    {
        foreach (string dir in Directory.GetDirectories(CResourceModule.GetAppDataPath()))
        {
            Directory.Delete(dir, true);
        }
        foreach (string file in Directory.GetFiles(CResourceModule.GetAppDataPath()))
        {
            File.Delete(file);
        }
    }
    [MenuItem("CosmosEngine/Open PC PersitentDataPath Folder")]
    public static void OpenPersistentDataPath()
    {
        System.Diagnostics.Process.Start(CResourceModule.GetAppDataPath());
    }

    [MenuItem("CosmosEngine/Clear Prefs")]
    public static void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        CBuildTools.ShowDialog("Prefs Cleared!");
    }
}

public class CSymbolLinkHelper
{

    public static string AssetBundlesLinkPath = "Assets/StreamingAssets/" + KEngine.KEngine.GetConfig(CCosmosEngineDefaultConfig.BundlesFolderName) + "/"; // hold asset bundles
    public static string GetLinkPath()
    {
        if (!Directory.Exists(AssetBundlesLinkPath))
            Directory.CreateDirectory(AssetBundlesLinkPath);
        return AssetBundlesLinkPath + CResourceModule.BuildPlatformName + "/";
    }

    public static string GetResourceExportPath()
    {
        var resourcePath = CBuildTools.GetExportPath(EditorUserBuildSettings.activeBuildTarget, CResourceModule.Quality);
        return resourcePath;
    }

    [MenuItem("CosmosEngine/Symbol Link Builded Resource to StreamingAssets")]
    public static void SymbolLinkResource()
    {
        //CSymbolLinkHelper.DeleteAllLinks(CSymbolLinkHelper.AssetBundlesLinkPath);

        //var exportPath = GetResourceExportPath();
        //var linkPath = GetLinkPath();

        // TODO: 
        //CBuildTools.SymbolLinkFolder(exportPath, linkPath);

        CDebug.Log("TODO:");
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 删除指定目录所有链接
    /// </summary>
    /// <param name="assetBundlesLinkPath"></param>
    private static void DeleteAllLinks(string assetBundlesLinkPath)
    {
        if (Directory.Exists(assetBundlesLinkPath))
        {
            foreach (var dirPath in Directory.GetDirectories(assetBundlesLinkPath))
            {
                // TODO:
                CDebug.Log("TODO: {0}", dirPath);
                //CBuildTools.DeleteLink(dirPath);
            }
        }
  
    }

    /// <summary>
    /// 如果不存在，创建link，并执行callback，完成后删掉link
    /// 如果存在，执行callback，完成后不删
    /// </summary>
    
    public static void SymbolLinkResourceAndDoAction(Action doAction = null)
    {
        var resourcePath = GetResourceExportPath();
        var linkPath = GetLinkPath();
        if (!Directory.Exists(linkPath))
        {
            SymbolLinkResource();

            if (doAction != null)
                doAction();

            // TODO: DeleteLink
            //CBuildTools.DeleteLink(linkPath);
        }
        else
        {
            if (doAction != null)
                doAction();

            CDebug.LogWarning("[SymbolLinkTest]Exist so not link: {0}", resourcePath);
        }
    }
}