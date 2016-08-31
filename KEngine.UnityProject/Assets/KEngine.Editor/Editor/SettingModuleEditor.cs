#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: SettingModuleEditor.cs
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
using System.Linq;
using System.Text.RegularExpressions;
using CosmosTable;
using DotLiquid;
using KUnityEditorTools;
using UnityEditor;
using UnityEngine;

namespace KEngine.Editor
{
    /// <summary>
    /// For SettingModule
    /// </summary>
    [InitializeOnLoad]
    public class SettingModuleEditor
    {
        /// <summary>
        /// 是否自动在编译配置表时生成静态代码，如果不需要，外部设置false
        /// </summary>
        public static bool AutoGenerateCode = true;

        /// <summary>
        /// 当生成的类名，包含数组中字符时，不生成代码
        /// </summary>
        /// <example>
        /// GenerateCodeFilesFilter = new []
        /// {
        ///     "SubdirSubSubDirExample3",
        /// };
        /// </example>
        public static string[] GenerateCodeFilesFilter = null;

        /// <summary>
        /// 条件编译变量
        /// </summary>
        public static string[] CompileSettingConditionVars;

        /// <summary>
        /// 可以为模板提供额外生成代码块！返回string即可！
        /// 自定义[InitializeOnLoad]的类并设置这个委托
        /// </summary>
        public static CustomExtraStringDelegate CustomExtraString;
        public delegate string CustomExtraStringDelegate(TableCompileResult tableCompileResult);

        /// <summary>
        /// 编译出的后缀名, 可修改
        /// </summary>
        public static string SettingExtension = ".bytes";

        /// <summary>
        /// 生成代码吗？它的路径配置
        /// </summary>
        public static string SettingCodePath
        {
            get
            {
                var compilePath = AppEngine.GetConfig("KEngine.Setting", "SettingCompileCodePath", false);
                if (string.IsNullOrEmpty(compilePath))
                {
                    return "Assets/AppSettings.cs"; // default value
                }
                return compilePath;
            }
        }

        /// <summary>
        /// 标记，是否正在打开提示配置变更对话框
        /// </summary>
        private static bool _isPopUpConfirm = false;

        static SettingModuleEditor()
        {
            var path = SettingSourcePath;
            if (Directory.Exists(path))
            {
                new KDirectoryWatcher(path, (o, args) =>
                {
                    if (_isPopUpConfirm) return;

                    _isPopUpConfirm = true;
                    KEditorUtils.CallMainThread(() =>
                    {
                        EditorUtility.DisplayDialog("Excel Setting Changed!", "Ready to Recompile All!", "OK");
                        DoCompileSettings(false);
                        _isPopUpConfirm = false;
                    });
                });
                Debug.Log("[SettingModuleEditor]Watching directory: " + SettingSourcePath);
            }
        }

        /// <summary>
        /// Generate static code from settings
        /// </summary>
        /// <param name="templateVars"></param>
        public static void GenerateCode(string genCodeFilePath, string nameSpace, List<Hash> files)
        {

            var codeTemplates = new Dictionary<string, string>()
            {
                {SettingModuleTemplate.GenCodeTemplate, genCodeFilePath},
            };

            foreach (var kv in codeTemplates)
            {
                var templateStr = kv.Key;
                var exportPath = kv.Value;

                // 生成代码
                var template = Template.Parse(templateStr);
                var topHash = new Hash();
                topHash["NameSpace"] = nameSpace;
                topHash["Files"] = files;

                if (!string.IsNullOrEmpty(exportPath))
                {
                    var genCode = template.Render(topHash);
                    if (File.Exists(exportPath)) // 存在，比较是否相同
                    {
                        if (File.ReadAllText(exportPath) != genCode)
                        {
                            EditorUtility.ClearProgressBar();
                            // 不同，会触发编译，强制停止Unity后再继续写入
                            if (EditorApplication.isPlaying)
                            {
                                Log.Error("[CAUTION]AppSettings code modified! Force stop Unity playing");
                                EditorApplication.isPlaying = false;
                            }
                            File.WriteAllText(exportPath, genCode);
                        }
                    }
                    else
                        File.WriteAllText(exportPath, genCode);

                }
            }
            // make unity compile
            AssetDatabase.Refresh();
        }
        /// <summary>
        /// Compile one directory 's all settings, and return behaivour results
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="compilePath"></param>
        /// <param name="genCodeFilePath"></param>
        /// <param name="changeExtension"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public static List<TableCompileResult> CompileTabConfigs(string sourcePath, string compilePath, string genCodeFilePath, string changeExtension = ".bytes", bool force = false)
        {
            var results = new List<TableCompileResult>();
            var compileBaseDir = compilePath;
            // excel compiler
            var compiler = new Compiler(new CompilerConfig() {ConditionVars = CompileSettingConditionVars});

            var excelExt = new HashSet<string>() { ".xls", ".xlsx", ".tsv" };
            var findDir = sourcePath;
            try
            {
                var allFiles = Directory.GetFiles(findDir, "*.*", SearchOption.AllDirectories);
                var allFilesCount = allFiles.Length;
                var nowFileIndex = -1; // 开头+1， 起始为0
                foreach (var excelPath in allFiles)
                {
                    nowFileIndex++;
                    var ext = Path.GetExtension(excelPath);
                    var fileName = Path.GetFileNameWithoutExtension(excelPath);
                    if (excelExt.Contains(ext) && !fileName.StartsWith("~")) // ~开头为excel临时文件，不要读
                    {
                        // it's an excel file
                        var relativePath = excelPath.Replace(findDir, "").Replace("\\", "/");
                        if (relativePath.StartsWith("/"))
                            relativePath = relativePath.Substring(1);


                        var compileToPath = string.Format("{0}/{1}", compileBaseDir,
                            Path.ChangeExtension(relativePath, changeExtension));
                        var srcFileInfo = new FileInfo(excelPath);

                        EditorUtility.DisplayProgressBar("Compiling Excel to Tab...",
                            string.Format("{0} -> {1}", excelPath, compileToPath), nowFileIndex / (float)allFilesCount);

                        // 如果已经存在，判断修改时间是否一致，用此来判断是否无需compile，节省时间
                        bool doCompile = true;
                        if (File.Exists(compileToPath))
                        {
                            var toFileInfo = new FileInfo(compileToPath);

                            if (!force && srcFileInfo.LastWriteTime == toFileInfo.LastWriteTime)
                            {
                                //Log.DoLog("Pass!SameTime! From {0} to {1}", excelPath, compileToPath);
                                doCompile = false;
                            }
                        }
                        if (doCompile)
                        {
                            Log.Warning("[SettingModule]Compile from {0} to {1}", excelPath, compileToPath);

                            var compileResult = compiler.Compile(excelPath, compileToPath, compileBaseDir, doCompile);

                            // 添加模板值
                            results.Add(compileResult);

                            var compiledFileInfo = new FileInfo(compileToPath);
                            compiledFileInfo.LastWriteTime = srcFileInfo.LastWriteTime;

                        }
                    }
                }

                // 根据模板生成所有代码,  如果不是强制重建，无需进行代码编译
                if (!AutoGenerateCode)
                {
                    Log.Warning("Ignore Gen Settings code");
                }
                else if (!force)
                {
                    Log.Warning("Ignore Gen Settings Code, not a forcing compiling");
                }
                else
                {

                    // 根据编译结果，构建vars，同class名字的，进行合并
                    var templateVars = new Dictionary<string, TableTemplateVars>();
                    foreach (var compileResult in results)
                    {
                        // 判断本文件是否忽略代码生成，用正则表达式
                        var settingCodeIgnorePattern = AppEngine.GetConfig("KEngine.Setting", "SettingCodeIgnorePattern", false);
                        if (!string.IsNullOrEmpty(settingCodeIgnorePattern))
                        {
                            var ignoreRegex = new Regex(settingCodeIgnorePattern);
                            if (ignoreRegex.IsMatch(compileResult.TabFilePath))
                                continue; // ignore this 
                        }

                        var customExtraStr = CustomExtraString != null ? CustomExtraString(compileResult) : null;

                        var templateVar = new TableTemplateVars(compileResult, customExtraStr);

                        // 尝试类过滤
                        var ignoreThisClassName = false;
                        if (GenerateCodeFilesFilter != null)
                        {
                            for (var i = 0; i < GenerateCodeFilesFilter.Length; i++)
                            {
                                var filterClass = GenerateCodeFilesFilter[i];
                                if (templateVar.ClassName.Contains(filterClass))
                                {
                                    ignoreThisClassName = true;
                                    break;
                                }

                            }
                        }
                        if (!ignoreThisClassName)
                        {
                            if (!templateVars.ContainsKey(templateVar.ClassName))
                                templateVars.Add(templateVar.ClassName, templateVar);
                            else
                            {
                                templateVars[templateVar.ClassName].Paths.Add(compileResult.TabFilePath);
                            }
                        }

                    }

                    // 整合成字符串模版使用的List
                    var templateHashes = new List<Hash>();
                    foreach (var kv in templateVars)
                    {
                        var templateVar = kv.Value;
                        var renderTemplateHash = Hash.FromAnonymousObject(templateVar);
                        templateHashes.Add(renderTemplateHash);
                    }


                    var nameSpace = "AppSettings";
                    GenerateCode(genCodeFilePath, nameSpace, templateHashes);
                }

            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            return results;
        }

        static string SettingSourcePath
        {
            get
            {
                var sourcePath = AppEngine.GetConfig("KEngine.Setting", "SettingSourcePath");
                return sourcePath;
            }
        }

        [MenuItem("KEngine/Settings/Force Compile Settings + Code")]
        public static void CompileSettings()
        {
            DoCompileSettings(true);
        }
        [MenuItem("KEngine/Settings/Quick Compile Settings")]
        public static void QuickCompileSettings()
        {
            DoCompileSettings(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="force">Whether or not,check diff.  false will be faster!</param>
        /// <param name="genCode">Generate static code?</param>
        public static void DoCompileSettings(bool force = true)
        {
            var sourcePath = SettingSourcePath;//AppEngine.GetConfig("SettingSourcePath");
            if (string.IsNullOrEmpty(sourcePath))
            {
                Log.Error("Need to KEngineConfig: SettingSourcePath");
                return;
            }
            var compilePath = AppEngine.GetConfig("KEngine.Setting", "SettingCompiledPath");
            if (string.IsNullOrEmpty(compilePath))
            {
                Log.Error("Need to KEngineConfig: SettingCompiledPath");
                return;
            }
            CompileTabConfigs(sourcePath, compilePath, SettingCodePath, SettingExtension, force);
        }
    }

    /// <summary>
    /// 用于liquid模板
    /// </summary>
    public class TableTemplateVars
    {
        public delegate string CustomClassNameDelegate(string originClassName, string filePath);

        /// <summary>
        /// You can custom class name
        /// </summary>
        public static TableTemplateVars.CustomClassNameDelegate CustomClassNameFunc;

        public List<string> Paths = new List<string>();

        /// <summary>
        ///  构建成一个数组"aaa", "bbb"
        /// </summary>
        public string TabFilePaths
        {
            get
            {
                var paths = "\"" + string.Join("\", \"", Paths.ToArray()) + "\"";
                return paths;
            }
        }

        public string ClassName { get; set; }
        public List<TableColumnVars> FieldsInternal { get; set; } // column + type

        public string PrimaryKey { get; set; }

        public List<Hash> Fields
        {
            get { return (from f in FieldsInternal select Hash.FromAnonymousObject(f)).ToList(); }
        }

        /// <summary>
        /// Get primary key, the first column field
        /// </summary>
        public Hash PrimaryKeyField
        {
            get { return Fields[0]; }
        }

        /// <summary>
        /// Custom extra strings
        /// </summary>
        public string Extra { get; private set; }

        public List<Hash> Columns2DefaultValus { get; set; } // column + Default Values

        public TableTemplateVars(TableCompileResult compileResult, string extraString)
            : base()
        {
            var tabFilePath = compileResult.TabFilePath;
            Paths.Add(compileResult.TabFilePath);

            ClassName = DefaultClassNameParse(tabFilePath);
            // 可自定义Class Name
            if (CustomClassNameFunc != null)
                ClassName = CustomClassNameFunc(ClassName, tabFilePath);

            FieldsInternal = compileResult.FieldsInternal;
            PrimaryKey = compileResult.PrimaryKey;
            Columns2DefaultValus = new List<Hash>();

            Extra = extraString;
        }

        /// <summary>
        /// get a class name from tab file path, default strategy
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        string DefaultClassNameParse(string tabFilePath)
        {
            // 未处理路径的类名, 去掉后缀扩展名
            var classNameOrigin = Path.ChangeExtension(tabFilePath, null);

            // 子目录合并，首字母大写, 组成class name
            var className = classNameOrigin.Replace("/", "_").Replace("\\", "_");
            className = className.Replace(" ", "");
            className = string.Join("", (from name
                in className.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                                         select (name[0].ToString().ToUpper() + name.Substring(1, name.Length - 1)))
                .ToArray());

            // 去掉+或#号后面的字符
            var plusSignIndex = className.IndexOf("+");
            className = className.Substring(0, plusSignIndex == -1 ? className.Length : plusSignIndex);
            plusSignIndex = className.IndexOf("#");
            className = className.Substring(0, plusSignIndex == -1 ? className.Length : plusSignIndex);

            return className;

        }
    }
}