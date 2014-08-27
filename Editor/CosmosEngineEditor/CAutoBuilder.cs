using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

public static class CAutoBuilder
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

        string productPath = Path.Combine(Application.dataPath, CCosmosEngine.GetConfig("ProductRelPath"));

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

    static void PerformBuild(string outputpath, BuildTarget tag, BuildOptions opt)
    {
        RefreshProgramVersion();

        EditorUserBuildSettings.SwitchActiveBuildTarget(tag);

        ParseArgs(ref opt, ref outputpath);

        string fullPath = System.IO.Path.Combine(Application.dataPath, System.IO.Path.Combine(CCosmosEngine.GetConfig("ProductRelPath"), outputpath));

        string fullDir = System.IO.Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(fullDir))
            Directory.CreateDirectory(fullDir);

        CBase.Log("Build Client {0} to: {1}", tag, outputpath);
        BuildPipeline.BuildPlayer(GetScenePaths(), fullPath, tag, opt);
    }

    /// <summary>
    /// 將svn的版本號寫入Resources目錄
    /// </summary>
    [MenuItem("CosmosEngine/AutoBuilder/Refresh Program Version")]
    static void RefreshProgramVersion()
    {
        string cmd = string.Format("{0}/GetSvnInfo.bat", Application.dataPath);

        var p = new Process();
        var si = new ProcessStartInfo();
        var path = Environment.SystemDirectory;

		if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iPhone)
			path = Path.Combine(path, @"sh");
		else
        	path = Path.Combine(path, @"cmd.exe");

        si.FileName = path;
        if (!cmd.StartsWith(@"/")) cmd = @"/c " + cmd;
        si.Arguments = cmd;
        si.UseShellExecute = false;
        si.CreateNoWindow = true;
        si.RedirectStandardOutput = true;
        si.RedirectStandardError = true;
        p.StartInfo = si;

        p.Start();
        p.WaitForExit();

        var str = p.StandardOutput.ReadToEnd();
        if (!string.IsNullOrEmpty(str))
        {
            string programVersionFile = string.Format("{0}/Resources/ProgramVersion.txt", Application.dataPath);

            Regex regex = new Regex(@"Revision: (\d+)");  // 截取svn版本号
            Match match = regex.Match(str);

            string szRevision = match.Groups[1].ToString();
            int nRevision = szRevision.ToInt32();
            using (FileStream fs = new FileStream(programVersionFile, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
                {
                    sw.Write(nRevision.ToString());
                }
            }


            CBase.Log("Refresh ProgramVersion.txt!! SVN Version: {0}", nRevision);
        }
        else
            CBase.LogError("Error Read svn Revision!");


        str = p.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(str))
            CBase.LogError(str);
    }

    [MenuItem("CosmosEngine/AutoBuilder/WindowsX86D")]  // 注意，PC版本放在不一样的目录的！
    static void PerformWinBuild()
    {
        PerformBuild("ClientX86D.exe", BuildTarget.StandaloneWindows, BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler);
    }

    //[MenuItem("File/AutoBuilder/WindowsX86")]
    //static void PerformWinReleaseBuild()
    //{
    //	PerformBuild(GetProjectName() + "X86.exe", BuildTarget.StandaloneWindows, BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler);
    //}

    [MenuItem("CosmosEngine/AutoBuilder/iOS")]
    static void PerformiOSBuild()
    {
        PerformBuild("Apps/ClientIOS", BuildTarget.iPhone, BuildOptions.Development | BuildOptions.ConnectWithProfiler);
    }

    [MenuItem("CosmosEngine/AutoBuilder/Android")]
    static void PerformAndroidBuild()
    {
        PerformBuild("Apps/Client.apk", BuildTarget.Android, BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler);
    }

    [MenuItem("CosmosEngine/清理PC缓存文件夹")]
    static void ClearPersistentDataPath()
    {
        foreach (string dir in Directory.GetDirectories(CResourceManager.GetAppDataPath()))
        {
            Directory.Delete(dir, true);
        }
        
    }
    [MenuItem("CosmosEngine/打开PC缓存文件夹")]
    static void OpenPersistentDataPath()
    {
        System.Diagnostics.Process.Start(CResourceManager.GetAppDataPath());
    }

    [MenuItem("CosmosEngine/清理Prefs")]
    static void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

}