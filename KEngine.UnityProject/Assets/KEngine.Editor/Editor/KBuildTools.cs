#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KBuildTools.cs
// Date:     2015/12/03
// Author:  Kelly
// Email: 23110388@qq.com
// Github: https://github.com/mr-kelly/KEngine
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using KEngine;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public partial class KBuildTools
{
    private static int PushedAssetCount = 0;

    public static event Action<UnityEngine.Object, string, string> BeforeBuildAssetBundleEvent;
    public static event Action<UnityEngine.Object, string, string> AfterBuildAssetBundleEvent;

    #region 打包功能

    /// <summary>
    /// 获取完整的打包路径，并确保目录存在
    /// </summary>
    /// <param name="path"></param>
    /// <param name="buildTarget"></param>
    /// <returns></returns>
    public static string MakeSureExportPath(string path, BuildTarget buildTarget, KResourceQuality quality)
    {
        path = KBuildTools.GetExportPath(buildTarget, quality) + path;

        string exportDirectory = path.Substring(0, path.LastIndexOf('/'));

        if (!System.IO.Directory.Exists(exportDirectory))
            System.IO.Directory.CreateDirectory(exportDirectory);

        path = path.Replace("/", @"\");

        return path;
    }

    /// <summary>
    /// Extra Flag ->   ex:  Android/  AndroidSD/  AndroidHD/
    /// </summary>
    /// <param name="platfrom"></param>
    /// <param name="quality"></param>
    /// <returns></returns>
    public static string GetExportPath(BuildTarget platfrom, KResourceQuality quality = KResourceQuality.Sd)
    {
        string basePath =
            Path.GetFullPath(Application.dataPath + "/" +
                             KEngine.AppEngine.GetConfig(KEngineDefaultConfigs.AssetBundleBuildRelPath) + "/");

        if (File.Exists(basePath))
        {
            KBuildTools.ShowDialog("路径配置错误: " + basePath);
            throw new System.Exception("路径配置错误");
        }
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        string path = null;
        switch (platfrom)
        {
            case BuildTarget.Android:
            case BuildTarget.iPhone:
            case BuildTarget.StandaloneWindows:
                var platformName = KResourceModule.BuildPlatformName;
                if (quality != KResourceQuality.Sd) // SD no need add
                    platformName += quality.ToString().ToUpper();

                path = basePath + platformName + "/";
                break;
            default:
                KBuildTools.ShowDialog("构建平台配置错误");
                throw new System.Exception("构建平台配置错误");
        }
        return path;
    }

    public static void ClearConsole()
    {
        Assembly assembly = Assembly.GetAssembly(typeof (SceneView));
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
        return BuildAssetBundle(asset, path, EditorUserBuildSettings.activeBuildTarget, KResourceModule.Quality);
    }

    public static uint BuildAssetBundle(Object asset, string path, BuildTarget buildTarget, KResourceQuality quality)
    {
        if (asset == null || string.IsNullOrEmpty(path))
        {
            BuildError("BuildAssetBundle: {0}", path);
            return 0;
        }

        var assetNameWithoutDir = asset.name.Replace("/", "").Replace("\\", ""); // 防止多重目录...
        string tmpPrefabPath = string.Format("Assets/{0}.prefab", assetNameWithoutDir);

        PrefabType prefabType = PrefabUtility.GetPrefabType(asset);

        string relativePath = path;
        path = MakeSureExportPath(path, buildTarget, quality);

        var assetPath = AssetDatabase.GetAssetPath(asset);
        CheckAndLogDependencies(assetPath);

        uint crc = 0;
        if (asset is Texture2D)
        {
            if (!string.IsNullOrEmpty(assetPath)) // Assets内的纹理
            {
// Texutre不复制拷贝一份
                _DoBuild(out crc, asset, null, path, relativePath, buildTarget);
            }
            else
            {
                // 内存的图片~临时创建Asset, 纯正的图片， 使用Sprite吧
                var memoryTexture = asset as Texture2D;
                var memTexName = memoryTexture.name;

                var tmpTexPath = string.Format("Assets/Tex_{0}_{1}.png", memoryTexture.name, Path.GetRandomFileName());

                Logger.LogWarning("【BuildAssetBundle】Build一个非Asset 的Texture: {0}", memoryTexture.name);

                File.WriteAllBytes(tmpTexPath, memoryTexture.EncodeToPNG());
                AssetDatabase.ImportAsset(tmpTexPath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var tmpTex = (Texture2D) AssetDatabase.LoadAssetAtPath(tmpTexPath, typeof (Texture2D));

                asset = tmpTex;
                try
                {
                    asset.name = memTexName;

                    _DoBuild(out crc, asset, null, path, relativePath, buildTarget);
                }
                catch (Exception e)
                {
                    Logger.LogException(e);
                }

                File.Delete(tmpTexPath);
                if (File.Exists(tmpTexPath + ".meta"))
                    File.Delete(tmpTexPath + ".meta");
            }
        }
        else if ((prefabType == PrefabType.None && assetPath == string.Empty) ||
                 (prefabType == PrefabType.ModelPrefabInstance)) // 非prefab对象
        {
            Object tmpInsObj = (GameObject) GameObject.Instantiate(asset); // 拷出来创建Prefab
            Object tmpPrefab = PrefabUtility.CreatePrefab(tmpPrefabPath, (GameObject) tmpInsObj,
                ReplacePrefabOptions.Default);
            CheckAndLogDependencies(tmpPrefabPath);
            asset = tmpPrefab;

            _DoBuild(out crc, asset, null, path, relativePath, buildTarget);

            GameObject.DestroyImmediate(tmpInsObj);
            AssetDatabase.DeleteAsset(tmpPrefabPath);
        }
        else if (prefabType == PrefabType.PrefabInstance)
        {
            var prefabParent = PrefabUtility.GetPrefabParent(asset);
            _DoBuild(out crc, prefabParent, null, path, relativePath, buildTarget);
        }
        else
        {
            //Logger.LogError("[Wrong asse Type] {0}", asset.GetType());
            _DoBuild(out crc, asset, null, path, relativePath, buildTarget);
        }
        return crc;
    }

    /// <summary>
    /// 检查如果有依赖，报出
    /// 检查prefab中存在prefab依赖，进行打散！
    /// </summary>
    /// <param name="assetPath"></param>
    public static void CheckAndLogDependencies(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return;

        // 输出依赖
        var depSb = new StringBuilder();
        var depsArray = AssetDatabase.GetDependencies(new string[] {assetPath});
        if (depsArray != null && depsArray.Length > 0)
        {
            if (depsArray.Length == 1 && depsArray[0] == assetPath)
            {
                // 自己依赖自己的忽略掉
            }
            else
            {
                foreach (var depAssetPath in depsArray)
                {
                    depSb.AppendLine(depAssetPath);
                }
                Logger.Log("[BuildAssetBundle]Asset: {0} has dependencies: {1}", assetPath, depSb.ToString());
            }
        }
    }

    [MenuItem("Assets/Print Aseet Dependencies", false, 100)]
    public static void MenuCheckAndLogDependencies()
    {
        var obj = Selection.activeObject;
        if (obj == null)
        {
            Debug.LogError("No selection object");
            return;
        }
        var assetPath = AssetDatabase.GetAssetPath(obj);
        KBuildTools.CheckAndLogDependencies(assetPath);
    }

    private static void _DoBuild(out uint crc, Object asset, Object[] subAssets, string path, string relativePath,
        BuildTarget buildTarget)
    {
        if (BeforeBuildAssetBundleEvent != null)
            BeforeBuildAssetBundleEvent(asset, path, relativePath);

        if (subAssets == null)
        {
            subAssets = new[] {asset};
        }
        else
        {
            var listSubAsset = new List<Object>(subAssets);
            if (!listSubAsset.Contains(asset))
            {
                listSubAsset.Add(asset);
            }
            subAssets = listSubAsset.ToArray();
        }

        var time = DateTime.Now;
        BuildPipeline.BuildAssetBundle(
            asset,
            subAssets,
            path,
            out crc,
            BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets |
            BuildAssetBundleOptions.DeterministicAssetBundle,
            buildTarget);

        Logger.Log("生成文件： {0}, 耗时: {1:F5}", path, (DateTime.Now - time).TotalSeconds);

        if (AfterBuildAssetBundleEvent != null)
            AfterBuildAssetBundleEvent(asset, path, relativePath);
    }

    public static uint BuildScriptableObject<T>(T scriptObject, string path) where T : ScriptableObject
    {
        return BuildScriptableObject(scriptObject, path, EditorUserBuildSettings.activeBuildTarget,
            KResourceModule.Quality);
    }

    public static uint BuildScriptableObject<T>(T scriptObject, string path, BuildTarget buildTarget,
        KResourceQuality quality) where T : ScriptableObject
    {
        const string tempAssetPath = "Assets/~Temp.asset";
        AssetDatabase.CreateAsset(scriptObject, tempAssetPath);
        T tempObj = (T) AssetDatabase.LoadAssetAtPath(tempAssetPath, typeof (T));

        if (tempObj == null)
        {
            throw new System.Exception();
        }

        uint crc = KBuildTools.BuildAssetBundle(tempObj, path, buildTarget, quality);
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
        string symbolStrs =
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        List<string> symbols =
            new List<string>(symbolStrs.Split(new char[] {';'}, System.StringSplitOptions.RemoveEmptyEntries));
        return symbols.Contains(symbol);
    }

    /// <summary>
    /// 移除指定宏
    /// </summary>
    /// <param name="symbol"></param>
    public static void RemoveDefineSymbols(string symbol)
    {
        foreach (BuildTargetGroup target in System.Enum.GetValues(typeof (BuildTargetGroup)))
        {
            string symbolStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            List<string> symbols =
                new List<string>(symbolStr.Split(new char[] {';'}, System.StringSplitOptions.RemoveEmptyEntries));
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
        foreach (BuildTargetGroup target in System.Enum.GetValues(typeof (BuildTargetGroup)))
        {
            string symbolStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            List<string> symbols =
                new List<string>(symbolStr.Split(new char[] {';'}, System.StringSplitOptions.RemoveEmptyEntries));
            if (!symbols.Contains(symbol))
            {
                symbols.Add(symbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", symbols.ToArray()));
            }
        }
    }

    public static bool IsWin32
    {
        get
        {
            var os = Environment.OSVersion;
            return os.ToString().Contains("Windows");
        }
    }

    // 执行Python文件！获取返回值
    public static string ExecutePyFile(string pyFileFullPath, string arguments)
    {
        string pythonExe = null;
        if (IsWin32)
        {
            var guids = AssetDatabase.FindAssets("py");
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetFileName(assetPath) == "py.exe")
                {
                    pythonExe = assetPath; // Python地址
                    break;
                }
            }
        }
        else
        {
            pythonExe = "python"; // linux or mac
        }


        if (string.IsNullOrEmpty(pythonExe))
        {
            Logger.LogError("无法找到py.exe, 或python指令");
            return "Error: Not found python";
        }

        string allOutput = null;
        using (var process = new System.Diagnostics.Process())
        {
            process.StartInfo.FileName = pythonExe;
            process.StartInfo.Arguments = pyFileFullPath + " " + arguments;
            //process.StartInfo.UseShellExecute = false;
            ////process.StartInfo.CreateNoWindow = true;
            //process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;

            var tips = string.Format("ExecutePython: {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
            Logger.Log(tips);
            EditorUtility.DisplayProgressBar("Python...", tips, .5f);

            process.Start();

            allOutput = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            Logger.Log("PyExecuteResult: {0}", allOutput);

            EditorUtility.ClearProgressBar();

            return allOutput;
        }
    }

    /* TODO: CFolderSyncTool
        public static void DeleteLink(string linkPath)
        {
            var os = Environment.OSVersion;
            if (os.ToString().Contains("Windows"))
            {
                CFolderSyncTool.ExecuteCommand(string.Format("rmdir \"{0}\"", linkPath));
            }
            else if (os.ToString().Contains("Unix"))
            {
                CFolderSyncTool.ExecuteCommand(string.Format("rm -Rf \"{0}\"", linkPath));
            }
            else
            {
                Logger.LogError("[SymbolLinkFolder]Error on OS: {0}", os.ToString());
            }
        }

        public static void SymbolLinkFolder(string srcFolderPath, string targetPath)
        {
            var os = Environment.OSVersion;
            if (os.ToString().Contains("Windows"))
            {
                CFolderSyncTool.ExecuteCommand(string.Format("mklink /J \"{0}\" \"{1}\"", targetPath, srcFolderPath));
            }
            else if (os.ToString().Contains("Unix"))
            {
                var fullPath = Path.GetFullPath(targetPath);
                if (fullPath.EndsWith("/"))
                {
                    fullPath = fullPath.Substring(0, fullPath.Length - 1);
                    fullPath = Path.GetDirectoryName(fullPath);
                }
                CFolderSyncTool.ExecuteCommand(string.Format("ln -s {0} {1}", Path.GetFullPath(srcFolderPath), fullPath));
            }
            else
            {
                Logger.LogError("[SymbolLinkFolder]Error on OS: {0}", os.ToString());
            }
        }
        */

    //[Obsolete("Please use KAssetVersionControl")]
    //public static bool CheckNeedBuildWithMeta(params string[] assetPath)
    //{
    //    return KAssetVersionControl.TryCheckNeedBuildWithMeta(assetPath);
    //}
    //[Obsolete("Please use KAssetVersionControl")]
    //public static void MarkBuildVersion(string assetPath)
    //{
    //    KAssetVersionControl.TryMarkBuildVersion(assetPath);
    //}
}