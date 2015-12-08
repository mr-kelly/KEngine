using System;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using KUnityEditorTools;
using UnityEditor;

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
    /// KEngine Installer
    /// </summary>
    public class KEngineInstallerEditor : EditorWindow
    {
        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);
        [DllImport("kernel32.dll", EntryPoint = "CreateHardLinkW", CharSet = CharSet.Unicode)]
        public static extern bool CreateHardLink(string lpFileName,
                                                 string lpExistingFileName,
                                                 IntPtr mustBeNull);
        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        private static KEngineInstallerEditor Instance;

        private static string KEngineInstallDirPath = "Assets/KEngine";
        private static string KEngineEditorInstallDirPath = "Assets/KEngine/Editor";

        private static KEngineInstallType InstallType = KEngineInstallType.Dll;
        private static KEngineCopyType CopyType = KEngineCopyType.Hardlink;

        private bool _addonAssetDep = true;
        private bool _addonNGUI = true;
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

        GUIStyle headerStyle = new GUIStyle();

        private KEngineInstallerEditor()
        {
            headerStyle.fontSize = 30;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = Color.white;
        }


        void OnGUI()
        {
            GUILayout.Label("KEngine Installer", headerStyle);
            EditorGUILayout.Separator();
            InstallType = (KEngineInstallType)EditorGUILayout.EnumPopup("Install Type", InstallType);
            CopyType = (KEngineCopyType)EditorGUILayout.EnumPopup("Copy File Type", CopyType);

            _addonAssetDep = EditorGUILayout.Toggle("[Addon]Asset Dep", _addonAssetDep);
            if (_addonAssetDep)
                _addonNGUI = EditorGUILayout.Toggle("[Addon]NGUI AssetDep", _addonNGUI);

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
            AssetDatabase.DeleteAsset(KEngineEditorInstallDirPath);
            Debug.Log("UnInstall dir: " + KEngineEditorInstallDirPath);
            AssetDatabase.DeleteAsset(KEngineInstallDirPath);
            Debug.Log("UnInstall dir: " + KEngineInstallDirPath);
        }
        /// <summary>
        /// 选择目录，进行安装
        /// </summary>
        public void DynamicInstall()
        {
            var gitPath = EditorUtility.OpenFolderPanel("Select KEngine Build Folder", "./", "");

            Debug.Log("Using KEngine project: " + gitPath);

            var srcEngineCodePath = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\KEngine");
            var srcEngineDllPath = Path.Combine(gitPath, "Build/Release/KEngine.dll");
            var srcEngineEditorDllPath = Path.Combine(gitPath, "Build/Release/KEngine.Editor.dll");
            var srcEngineEditorCodePath = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\KEngine.Editor\Editor");

            var dlls = new string[]
        {
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
                CopyDll(srcEngineDllPath, KEngineInstallDirPath + "/KEngine.dll");
                CopyFolder(srcEngineEditorCodePath, KEngineEditorInstallDirPath + "/KEngine.Editor");
            }
            else
            {
                CopyFolder(srcEngineCodePath, KEngineInstallDirPath + "/KEngine");
                CopyFolder(srcEngineEditorCodePath, KEngineEditorInstallDirPath + "/KEngine.Editor");
            }

            Debug.Log("Install KEngine Successed!");

            // Asset Dep
            if (_addonAssetDep)
            {
                var srcAssetDepDirPath = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\KEngine.AssetDep");
                var srcAssetDepDllPath = Path.Combine(gitPath, "Build/Release/KEngine.AssetDep.dll");
                var srcAssetDepEditorDirPath = Path.Combine(gitPath, @"KEngine.UnityProject\Assets\KEngine.AssetDep.Editor\Editor");

                if (InstallType == KEngineInstallType.Dll)
                {
                    CopyDll(srcAssetDepDllPath, KEngineInstallDirPath + "/KEngine.AssetDep.dll");
                    CopyFolder(srcAssetDepEditorDirPath, KEngineEditorInstallDirPath + "/KEngine.AssetDep");
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


        void CopyFolder(string src, string target)
        {
            //if (CopyType == KEngineCopyType.CopyFile)
            {
                foreach (var srcFile in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
                {
                    var relativeSrcFilePath = srcFile.Replace(src, "");
                    var targetPath = target + relativeSrcFilePath;
                    CopyFile(srcFile, targetPath);
                }
            }
            //else
            //{
            //    CreateSymbolicLink(target, src, SymbolicLink.Directory);
            //}
            Debug.Log(string.Format("Copy Folder {0} -> {1}", src, target));
        }
        void CopyDll(string src, string target)
        {
            CopyFile(src, target);

            var srcPdb = Path.ChangeExtension(src, ".pdb");
            var targetPdb = Path.ChangeExtension(target, ".pdb");

            if (File.Exists(targetPdb))
                CopyFile(srcPdb, targetPdb);

            Debug.Log(string.Format("Copy Dll {0} -> {1}", src, target));
        }

        void CopyFile(string src, string target)
        {
            if (!Directory.Exists(Path.GetDirectoryName(target)))
                Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (CopyType == KEngineCopyType.CopyFile)
                File.Copy(src, target, true);
            else if (CopyType == KEngineCopyType.Hardlink)
                CreateHardLink(target, src, IntPtr.Zero);
            else
            {
                CreateSymbolicLink(target, src, SymbolicLink.File);
            }
        }
    }

}
