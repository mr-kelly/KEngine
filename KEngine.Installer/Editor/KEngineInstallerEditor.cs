using System;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
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

        private bool _withAssetDep = true;

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

            _withAssetDep = EditorGUILayout.Toggle("[Addon]Asset Dep", _withAssetDep);

            EditorGUILayout.HelpBox("Select KEngine git source project to install", MessageType.Info);

            if (GUILayout.Button("Select Git Project to Install"))
            {
                DynamicInstall();
            }
            if (GUILayout.Button("UnInstall"))
            {
                UnInstall();
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
            var path = EditorUtility.OpenFolderPanel("Select KEngine Build Folder", "./", "");

            Debug.Log("Using KEngine project: " + path);

            var srcEngineCodePath = Path.Combine(path, @"KEngine.UnityProject\Assets\KEngine");
            var srcEngineDllPath = Path.Combine(path, "Build/Release/KEngine.dll");
            var srcEngineEditorDllPath = Path.Combine(path, "Build/Release/KEngine.Editor.dll");
            var srcEngineEditorCodePath = Path.Combine(path, @"KEngine.UnityProject\Assets\KEngine.Editor\Editor");

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


            if (_withAssetDep)
            {
                var srcAssetDepDirPath = Path.Combine(path, @"KEngine.UnityProject\Assets\KEngine.AssetDep");
                var srcAssetDepDllPath = Path.Combine(path, "Build/Release/KEngine.AssetDep.dll");
                var srcAssetDepEditorDirPath = Path.Combine(path, @"KEngine.UnityProject\Assets\KEngine.AssetDep.Editor\Editor");

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
