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

using System.Collections.Generic;
using System.IO;
using CosmosTable;
using KUnityEditorTools;
using UnityEditor;
using UnityEngine;

namespace KEngine.Editor
{
    /// <summary>
    /// For SettingModule
    /// </summary>
    [InitializeOnLoad]
    public class KSettingModuleEditor
    {
        /// <summary>
        /// 编译出的后缀名
        /// </summary>
        public static string SettingExtension = ".bytes";

        /// <summary>
        /// 生成代码吗？它的路径配置
        /// </summary>
        public static string SettingCodePath = "Assets/AppSettings.cs";

        public static string GenCodeTemplate = @"
using System.Collections.Generic;
using CosmosTable;
namespace {{ NameSpace }}
{
{% for file in Files %}
	/// <summary>
	/// Auto Generate for Tab File: {{ file.TabFilePath }}
	/// </summary>
	public partial class {{file.ClassName}}Info : TableRowParser
	{
		public static readonly string TabFilePath = ""{{ file.TabFilePath }}"";

		private static {{file.ClassName}}Info _instance;

        public static {{file.ClassName}}Info Wrap(TableRow row)
        {
            if (_instance == null)
                _instance = new {{file.ClassName}}Info();

            _instance._row = row;
            return _instance;
        }

        private TableRow _row;

        private {{file.ClassName}}Info()
        {
        }

		{% for field in file.Fields %}
        /// {{ field.Comment }}
		public {{ field.FormatType }} {{ field.Name}}
        {
            get
            {
                return _row.Get_{{ field.TypeMethod }}(_row.Values[{{ field.Index }}], ""{{ field.DefaultValue }}"");
            }
            set
            {
                _row[{{ field.Index}}] = value.ToString();
            }

        }
		{% endfor %}
	}
{% endfor %} 
}
";
        /// <summary>
        /// 标记，是否正在打开提示配置变更对话框
        /// </summary>
        private static bool _isPopUpConfirm = false;

        static KSettingModuleEditor()
        {
            var path = Path.Combine(Application.dataPath, SettingSourcePath);
            if (Directory.Exists(path))
            {
                new KDirectoryWatcher(path, (o, args) =>
                {
                    if (_isPopUpConfirm) return;

                    _isPopUpConfirm = true;
                    KEditorUtils.CallMainThread(() =>
                    {
                        EditorUtility.DisplayDialog("Excel Setting Changed!", "Ready to Recompile All!", "OK");
                        CompileSettings();
                        _isPopUpConfirm = false;
                    });
                });
                Debug.Log("[KSettingModuleEditor]Watching directory: " + SettingSourcePath);
            }
        }
        public static void CompileTabConfigs(string sourcePath, string compilePath, string genCodeFilePath, string changeExtension = ".bytes", bool force = false)
        {
            // excel compiler
            var compiler = new Compiler(new CompilerConfig()
            {
                CodeTemplates = string.IsNullOrEmpty(genCodeFilePath) ? null : new Dictionary<string, string>()
                {
                    {GenCodeTemplate, "Assets/AppSettings.cs"}
                },
                NameSpace = "AppSettings",
            });

            var excelExt = new HashSet<string>() { ".xls", ".xlsx" };
            var findDir = Path.Combine(Application.dataPath, sourcePath);
            try
            {
                var allFiles = Directory.GetFiles(findDir, "*.*", SearchOption.AllDirectories);
                var allFilesCount = allFiles.Length;
                var nowFileIndex = -1; // 开头+1， 起始为0
                foreach (var excelPath in allFiles)
                {
                    nowFileIndex++;
                    var ext = Path.GetExtension(excelPath);
                    if (excelExt.Contains(ext))
                    {
                        // it's an excel file
                        var relativePath = excelPath.Replace(findDir, "").Replace("\\", "/");
                        if (relativePath.StartsWith("/"))
                            relativePath = relativePath.Substring(1);

                        var compileBaseDir = Path.Combine(Application.dataPath, compilePath);

                        var compileToPath = string.Format("{0}/{1}", compileBaseDir,
                            Path.ChangeExtension(relativePath, changeExtension));
                        var srcFileInfo = new FileInfo(excelPath);

                        EditorUtility.DisplayProgressBar("Compiling Excel to Tab...",
                            string.Format("{0} -> {1}", excelPath, compilePath), nowFileIndex / (float)allFilesCount);

                        // 如果已经存在，判断修改时间是否一致，用此来判断是否无需compile，节省时间
                        if (File.Exists(compileToPath))
                        {
                            var toFileInfo = new FileInfo(compileToPath);

                            if (!force && srcFileInfo.LastWriteTime == toFileInfo.LastWriteTime)
                            {
                                Logger.Log("Pass!SameTime! From {0} to {1}", excelPath, compileToPath);
                                continue;
                            }
                        }
                        Logger.LogWarning("[SettingModule]Compile from {0} to {1}", excelPath, compileToPath);
                        compiler.Compile(excelPath, compileToPath, compileBaseDir);
                        var compiledFileInfo = new FileInfo(compileToPath);
                        compiledFileInfo.LastWriteTime = srcFileInfo.LastWriteTime;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static string SettingSourcePath
        {
            get
            {
                var sourcePath = AppEngine.GetConfig("SettingSourcePath");
                return sourcePath;
            }
        }

        [MenuItem("KEngine/Settings/Force Compile Settings")]
        public static void CompileSettings()
        {
            var sourcePath = SettingSourcePath;//AppEngine.GetConfig("SettingSourcePath");
            if (string.IsNullOrEmpty(sourcePath)) 
            {
                Logger.LogError("Need to KEngineConfig: SettingSourcePath");
                return;
            }
            var compilePath = AppEngine.GetConfig("SettingPath");
            if (string.IsNullOrEmpty(compilePath))
            {
                Logger.LogError("Need to KEngineConfig: SettingPath");
                return;
            }
            CompileTabConfigs(sourcePath, compilePath, SettingCodePath, SettingExtension, true);
        }
    }
}