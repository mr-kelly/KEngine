using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KEngine.Editor
{
    /// <summary>
    /// 基于Tab表格的纯文本简单资源差异管理器
    /// </summary>
    public class KAssetVersionControl : IDisposable
    {
        public static KAssetVersionControl Current;

        private bool _isRebuild = false;
        /// <summary>
        /// 资源打包周期版本管理
        /// </summary>
        /// <param name="rebuild">如果是rebuild，将无视之前的差异打包信息</param>
        public KAssetVersionControl(bool rebuild = false)
        {
            if (Current != null)
            {
                Logger.LogError("New a KAssetVersionControl, but already has annother instance using! Be careful!");
            }

            Current = this;
            
            _isRebuild = rebuild;

            Logger.LogWarning("================== KAssetVersionControl Begin ======================");
            
            SetupHistory();

            KDependencyBuild.Clear();

            KBuildTools.AfterBuildAssetBundleEvent += OnAfterBuildAssetBundleEvent;
        }

        private void OnAfterBuildAssetBundleEvent(Object arg1, string arg2, string arg3)
        {
            BuildCount++;
        }

        public void Dispose()
        {
            WriteVersion();
            if (BuildCount > 0)
            {
                //ProductMd5_CurPlatform();
                Logger.Log("一共打包了{0}個資源", BuildCount);
            }
            else
                Logger.Log("没有任何需要打包的资源！");

            KDependencyBuild.SaveBuildAction();

            Current = null;
            KBuildTools.AfterBuildAssetBundleEvent -= OnAfterBuildAssetBundleEvent;
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

        public static void WriteVersion()
        {
            string path = GetBuildVersionTab();// MakeSureExportPath(VerCtrlInfo.VerFile, EditorUserBuildSettings.activeBuildTarget);
            KTabFile tabFile = new KTabFile();
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

        static void SetupHistory()
        {
            BuildCount = 0;
            BuildVersion = new Dictionary<string, BuildRecord>();

            BuildVersion.Clear();

            string verFile = GetBuildVersionTab(); //MakeSureExportPath(VerCtrlInfo.VerFile, EditorUserBuildSettings.activeBuildTarget);
            KTabFile tabFile;
            if (File.Exists(verFile))
            {
                tabFile = KTabFile.LoadFromFile(verFile);

                foreach (KTabFile.RowInterator row in tabFile)
                {
                    BuildVersion[row.GetString("AssetPath")] =
                        new BuildRecord(
                            row.GetString("AssetMD5"),
                            row.GetString("AssetDateTime"),
                            row.GetInteger("ChangeCount"));
                }
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

        public static int BuildCount = 0;  // 累计Build了多少次，用于版本控制时用的

        // Prefab Asset打包版本號記錄
        public static string GetBuildVersionTab()
        {
            return Application.dataPath + "/" + KEngineDef.ResourcesBuildInfosDir + "/ArtBuildResource_" + KResourceModule.BuildPlatformName + ".txt";
        }

        public static bool TryCheckNeedBuildWithMeta(params string[] sourceFiles)
        {
            if (Current == null)
                return true;

            return Current.CheckNeedBuildWithMeta(sourceFiles);
        }

        ///// <summary>
        ///// 判断路径，并且尝试判断meta文件
        ///// </summary>
        public bool CheckNeedBuildWithMeta(params string[] sourceFiles)
        {
            foreach (string file in sourceFiles)
            {
                if (DoCheckNeedBuild(file, true) || DoCheckNeedBuild(file + ".meta"))
                    return true;
            }

            return false;
        }

        private bool DoCheckNeedBuild(string filePath, bool log = false)
        {
            BuildRecord assetMd5;
            if (!File.Exists(filePath))
            {
                if (log)
                    Logger.LogError("[DoCheckNeedBuild]Not Found 无法找到文件 {0}", filePath);

                if (filePath.Contains("unity_builtin_extra"))
                {
                    Logger.LogError("[DoCheckNeedBuild]Find unity_builtin_extra resource to build!! Please check it! current scene: {0}", EditorApplication.currentScene);
                }
                return false;
            }
            
            if (_isRebuild) // 所有rebuild，不用判断，直接需要build, 保留change count的正确性
                return true;

            if (!BuildVersion.TryGetValue(filePath, out assetMd5))
                return true;

            if (KTool.MD5_File(filePath) != assetMd5.MD5)
                return true;  // different

            return false;
        }
        /// <summary>
        /// 标记一个路径为打包
        /// </summary>
        public void MarkBuildVersion(params string[] sourceFiles)
        {
            if (sourceFiles == null || sourceFiles.Length == 0)
                return;

            foreach (string file in sourceFiles)
            {
                //BuildVersion[file] = GetAssetVersion(file);
                BuildRecord theRecord;
                var nowMd5 = KTool.MD5_File(file);
                if (!BuildVersion.TryGetValue(file, out theRecord))
                {
                    theRecord = BuildVersion[file] = new BuildRecord();
                    theRecord.Mark(nowMd5);
                }
                else
                {
                    if (nowMd5 != theRecord.MD5) // 只有改变时才会mark，所以可能会出现情况，rebuild时，change count不改变
                    {
                        theRecord.Mark(nowMd5);
                    }
                }


                string metaFile = file + ".meta";
                if (File.Exists(metaFile))
                {
                    BuildRecord theMetaRecord;
                    var nowMetaMd5 = KTool.MD5_File(metaFile);
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
        public static void TryMarkBuildVersion(params string[] sourceFiles)
        {
            if (Current == null)
                return;

            Current.MarkBuildVersion(sourceFiles);
        }
        #endregion
    }

}
