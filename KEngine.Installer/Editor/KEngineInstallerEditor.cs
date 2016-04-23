#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KSettingModuleEditor.cs
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
using System.IO;
using System.Runtime.InteropServices;
using KUnityEditorTools;
using UnityEditor;
using UnityEngine;

namespace KEngine.Installer
{
    public enum KEngineInstallType
    {
        Dll,
        SourceCode,
    }

    public enum KEngineCopyType
    {
        Hardlink,
        SymbolLink,
        CopyFile,
    }

    /// <summary>
    /// KEngine Installer --- Tool install from KEngine.git
    /// </summary>
    public class KEngineInstallerEditor : EditorWindow
    {
        [DllImport("kernel32.dll")]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName,
            SymbolicLink dwFlags);

        [DllImport("kernel32.dll", EntryPoint = "CreateHardLinkW", CharSet = CharSet.Unicode)]
        public static extern bool CreateHardLink(string lpFileName,
            string lpExistingFileName,
            IntPtr mustBeNull);

        private enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        private static KEngineInstallerEditor Instance;

        private static string KEngineInstallDirPath
        {
            get
            {
                return "Assets/KEngine";
            }
        }

        private static string KEngineEditorInstallDirPath
        {
            get
            {
                return "Assets/KEngine/Editor";
            }
        }

        private static KEngineInstallType InstallType = KEngineInstallType.Dll;
        private static KEngineCopyType CopyType = KEngineCopyType.Hardlink;

        private bool _addonTools = false;
        private bool _addonUI= false;
        private bool _addonAssetDep = false;
        private bool _addonNGUI = true;
        private bool _addonResourceDep = true;
        private bool _deleteKEngineConfigTxt = false;

        [MenuItem("KEngine/KEngine Installer")]
        public static void OpenWindow()
        {
            if (Instance == null)
            {
                Instance = EditorWindow.GetWindow<KEngineInstallerEditor>(true, "KEngine Installer");
            }
            Instance.Show();
        }

        private GUIStyle headerStyle = new GUIStyle();

        private KEngineInstallerEditor()
        {
            headerStyle.fontSize = 30;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = Color.white;
        }


        private void OnGUI()
        {
            GUILayout.Label("KEngine Installer", headerStyle);
            EditorGUILayout.Separator();
            InstallType = (KEngineInstallType)EditorGUILayout.EnumPopup("Install Type", InstallType);
            CopyType = (KEngineCopyType)EditorGUILayout.EnumPopup("Copy File Type", CopyType);

            _addonTools = EditorGUILayout.Toggle("[Addon]Tools", _addonTools);
            _addonUI = EditorGUILayout.Toggle("[Addon]UI Module", _addonUI);
            _addonAssetDep = EditorGUILayout.Toggle("[Addon]Asset Dep", _addonAssetDep);
            if (_addonAssetDep)
                _addonNGUI = EditorGUILayout.Toggle("[Addon]NGUI AssetDep", _addonNGUI);

            if (_addonResourceDep)
                _addonResourceDep = EditorGUILayout.Toggle("[Addon]Resource Dep", _addonResourceDep);

            EditorGUILayout.HelpBox("Select KEngine git source project to install", MessageType.Info);

            if (GUILayout.Button("Select Git Project to Install"))
            {
                DynamicInstall();
            }

            EditorGUILayout.Space();
            GUILayout.Label("=== UnInstall ==");
            _deleteKEngineConfigTxt = EditorGUILayout.Toggle("Uninstall with KEngineConfig.txt", _deleteKEngineConfigTxt);

            if (GUILayout.Button("UnInstall"))
            {
                UnInstall();
                if (_deleteKEngineConfigTxt)
                {
                    AssetDatabase.DeleteAsset("Assets/Resources/KEngineConfig.txt");
                    _deleteKEngineConfigTxt = true;
                }
            }
        }

        public static void UnInstall()
        {
            if (Directory.Exists(KEngineEditorInstallDirPath))
            {
                Directory.Delete(KEngineEditorInstallDirPath, true);
            }
            AssetDatabase.DeleteAsset(KEngineEditorInstallDirPath);

            Debug.Log("UnInstall dir: " + KEngineEditorInstallDirPath);

            if (Directory.Exists(KEngineInstallDirPath))
            {
                Directory.Delete(KEngineInstallDirPath, true);
            }
            AssetDatabase.DeleteAsset(KEngineInstallDirPath);
            Debug.Log("UnInstall dir: " + KEngineInstallDirPath);
        }

        /// <summary>
        /// 拷贝Editor依赖的DLL
        /// </summary>
        private void CopyEditorLib(string gitPath, string toDir)
        {
            CopyDll(Path.Combine(gitPath, "Build/Release/DotLiquid.dll"), toDir);
            CopyDll(Path.Combine(gitPath, "Build/Release/NPOI.dll"), toDir);
            CopyDll(Path.Combine(gitPath, "Build/Release/NPOI.OOXML.dll"), toDir);
            CopyDll(Path.Combine(gitPath, "Build/Release/NPOI.OpenXml4Net.dll"), toDir);
            CopyDll(Path.Combine(gitPath, "Build/Release/NPOI.OpenXmlFormats.dll"), toDir);
        }

        /// <summary>
        /// 选择目录，进行安装
        /// </summary>
        public void DynamicInstall()
        {
            var gitPath = EditorUtility.OpenFolderPanel("Select KEngine Build Folder", "./", "");

            Debug.Log("Using KEngine project: " + gitPath);

            var srcEngineLibCodePath = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\KEngine.Lib");
            var srcEngineLibDllPath = Path.Combine(gitPath, @"Build/Release/KEngine.Lib.dll");
            var srcEngineCodePath = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\KEngine");
            var srcEngineDllPath = Path.Combine(gitPath, "Build/Release/KEngine.dll");
            var srcEngineEditorDllPath = Path.Combine(gitPath, "Build/Release/KEngine.Editor.dll");
            var srcEngineEditorCodePath = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\KEngine.Editor\Editor");
            var srcEngineEditorToolCodePath = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\KEngine.EditorTools\Editor");

            var dlls = new string[]
            {
                srcEngineLibDllPath,
                srcEngineDllPath,
                srcEngineEditorDllPath,
            };

            foreach (var dllPath in dlls)
            {
                if (!File.Exists(dllPath))
                {
                    EditorUtility.DisplayDialog("Error Install", "Not found DLL: " + dllPath, "Ok");
                    return;
                }
            }

            // Start install!
            UnInstall();

            // KEngineConfig.txt
            var selfEngineConfigPath = "Assets/Resources/KEngineConfig.txt";
            if (!File.Exists(selfEngineConfigPath))
            {
                var srcEngineConfig = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\Resources\KEngineConfig.txt");
                File.Copy(srcEngineCodePath, selfEngineConfigPath);
                Debug.Log(string.Format("Copy EngineConfig.txt from {0}, to {1}", srcEngineConfig, selfEngineConfigPath));
            }

            if (InstallType == KEngineInstallType.Dll)
            {
                CopyDll(srcEngineLibDllPath, KEngineInstallDirPath);
                CopyDll(Path.Combine(gitPath, "Build/Release/ICSharpCode.SharpZipLib.dll"), KEngineInstallDirPath); // 3rd lib
                CopyDll(srcEngineDllPath, KEngineInstallDirPath);

                CopyDll(srcEngineEditorDllPath, KEngineEditorInstallDirPath);
                CopyEditorLib(gitPath, KEngineEditorInstallDirPath);// 3rd lib
                //CopyFolder(srcEngineEditorCodePath, KEngineEditorInstallDirPath + "/KEngine.Editor");
            }
            else
            {
                CopyFolder(srcEngineLibCodePath, KEngineInstallDirPath + "/KEngine.Lib");
                CopyFolder(srcEngineCodePath, KEngineInstallDirPath + "/KEngine");
                CopyFolder(srcEngineEditorCodePath, KEngineEditorInstallDirPath + "/KEngine.Editor");
                CopyFolder(srcEngineEditorToolCodePath, KEngineEditorInstallDirPath + "/KEngine.EditorTools");

                CopyEditorLib(gitPath, KEngineEditorInstallDirPath);// 3rd lib
            }

            Debug.Log("Install KEngine Successed!");
            if (_addonTools)
            {
                var srcToolDllPath = Path.Combine(gitPath, "Build/Release/KEngine.Tools.dll");
                CopyDll(srcToolDllPath, KEngineInstallDirPath);
            }
            if (_addonUI)
            {
                var srcUIDLLPath = Path.Combine(gitPath, "Build/Release/KEngine.UI.dll");
                CopyDll(srcUIDLLPath, KEngineInstallDirPath);
            }

            // Resource Dep
            if (_addonResourceDep)
            {
                var srcResourceDepDirPath = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\KEngine.ResourceDep");
                var srcResourceDepEditorDirPath = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\KEngine.ResourceDep.Editor\Editor");

                CopyFolder(srcResourceDepDirPath, KEngineInstallDirPath + "/KEngine.ResourceDep");
                CopyFolder(srcResourceDepEditorDirPath, KEngineEditorInstallDirPath + "/KEngine.ResourceDep");
            }
            // Asset Dep
            if (_addonAssetDep)
            {
                var srcAssetDepDirPath = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\KEngine.AssetDep");
                var srcAssetDepDllPath = Path.Combine(gitPath, "Build/Release/KEngine.AssetDep.dll");
                var srcAssetDepEditorDllPath = Path.Combine(gitPath, "Build/Release/KEngine.AssetDep.Editor.dll");
                var srcAssetDepEditorDirPath = Path.Combine(gitPath,
                    @"KEngine.UnityProject\Assets\KEngine.AssetDep.Editor\Editor");

                if (InstallType == KEngineInstallType.Dll)
                {
                    CopyDll(srcAssetDepDllPath, KEngineInstallDirPath);
                    CopyDll(srcAssetDepEditorDllPath, KEngineEditorInstallDirPath);
                    //CopyFolder(srcAssetDepEditorDirPath, KEngineEditorInstallDirPath + "/KEngine.AssetDep");
                }
                else
                {
                    CopyFolder(srcAssetDepDirPath, KEngineInstallDirPath + "/KEngine.AssetDep");
                    CopyFolder(srcAssetDepEditorDirPath, KEngineEditorInstallDirPath + "/KEngine.AssetDep");
                }

                Debug.Log("Install KEngine.AssetDep Successed!");

                if (_addonNGUI)
                {
                    var srcNGUIDirPath = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\KEngine.NGUI");
                    var srcNGUEditorDirPath = Path.Combine(gitPath,
                        @"KEngine.UnityProject\Assets\KEngine.NGUI.Editor\Editor");
                    var srcNGUIAssetDepDirPath = Path.Combine(gitPath,
                        @"KEngine.UnityProject\Assets\KEngine.NGUI.AssetDep");
                    var srcNGUIAssetDepEditorDirPath = Path.Combine(gitPath,
                        @"KEngine.UnityProject\Assets\KEngine.NGUI.AssetDep.Editor\Editor");
                    // NO Dll for NGUI
                    CopyFolder(srcNGUIDirPath, KEngineInstallDirPath + "/KEngine.NGUI");
                    CopyFolder(srcNGUEditorDirPath, KEngineEditorInstallDirPath + "/KEngine.NGUI.Editor");
                    CopyFolder(srcNGUIAssetDepDirPath, KEngineInstallDirPath + "/KEngine.NGUI.AssetDep");
                    CopyFolder(srcNGUIAssetDepEditorDirPath,
                        KEngineEditorInstallDirPath + "/KEngine.NGUI.AssetDep.Editor");

                    KDefineSymbolsHelper.AddDefineSymbols("NGUI");
                }
                else
                {
                    KDefineSymbolsHelper.RemoveDefineSymbols("NGUI");
                }
            }
            AssetDatabase.Refresh();
        }


        private void CopyFolder(string src, string target)
        {
            foreach (var srcFile in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                var relativeSrcFilePath = srcFile.Replace(src, "");
                var targetPath = target + relativeSrcFilePath;
                CopyFile(srcFile, targetPath);
            }
            Debug.Log(string.Format("Copy Folder {0} -> {1}", src, target));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="targetFolder">注意这是文件夹目录</param>
        private void CopyDll(string src, string targetFolder)
        {
            var targetPath = Path.Combine(targetFolder, Path.GetFileName(src));

            CopyFile(src, targetPath);

            var srcPdb = Path.ChangeExtension(src, ".pdb");
            var targetPdb = Path.ChangeExtension(targetPath, ".pdb");
            var srcXml = Path.ChangeExtension(src, ".xml");
            var targetXml = Path.ChangeExtension(targetPath, ".xml");

            if (File.Exists(srcPdb))
                CopyFile(srcPdb, targetPdb);

            if (File.Exists(srcXml))
                CopyFile(srcXml, targetXml);

        }

        private void CopyFile(string src, string target)
        {
            if (!Directory.Exists(Path.GetDirectoryName(target)))
                Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (CopyType == KEngineCopyType.CopyFile)
            {
                File.Copy(src, target, true);
                Debug.Log(string.Format("Copy {0} -> {1}", src, target));
            }
            else if (CopyType == KEngineCopyType.Hardlink)
            {
                CreateHardLink(target, src, IntPtr.Zero);
                Debug.Log(string.Format("Harlink {0} -> {1}", src, target));
            }
            else
            {
                CreateSymbolicLink(target, src, SymbolicLink.File);
                Debug.Log(string.Format("Symbol {0} -> {1}", src, target));
            }
        }
    }
}