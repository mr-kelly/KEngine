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
using UnityEditor;
using UnityEngine;

namespace KEngine.Editor
{
    /// <summary>
    /// For SettingModule
    /// </summary>
    public class KSettingModuleEditor
    {
        public static string GenCodeTemplate = @"
using CosmosTable;

namespace {{ NameSpace }}
{
{% for file in Files %}
	/// <summary>
	/// Auto Generate for Tab File: {{ file.TabFilePath }}
	/// </summary>
	public partial class {{file.ClassName}}Info : TableRowInfo
	{
		public static readonly string TabFilePath = ""{{ file.TabFilePath }}"";
		
		public override bool IsAutoParse { get { return false; } }

		{% for field in file.Fields %}
		public {{ field.Type }} {{ field.Name}} { get; internal set; }  // {{ field.Comment }}
		{% endfor %}

		public override void Parse(string[] values)
		{
		{% for field in file.Fields %}
			// {{ field.Comment }}
			{{ field.Name}} = Get_{{ field.Type | replace:'\[\]','_array' }}(values[{{ field.Index }}], ""{{ field.DefaultValue }}"");
		{% endfor %}
		}

		public override object PrimaryKey
		{
			get
			{
				return {{ file.PrimaryKey }};
			}
		}
	}
{% endfor %}
}
";

        [MenuItem("KEngine/Compile Settings")]
        public static void CompileTabConfigs()
        {
            var sourcePath = AppEngine.GetConfig("SettingSourcePath");
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

            // excel compiler
            var compiler = new Compiler(new CompilerConfig()
            {
                CodeTemplates = new Dictionary<string, string>()
                {
                    {GenCodeTemplate, "Assets/AppSettings.cs"}
                },
                NameSpace = "AppSettings",
            });

            var excelExt = new HashSet<string>() {".xls", ".xlsx"};
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
                            Path.ChangeExtension(relativePath, ".bytes"));
                        var srcFileInfo = new FileInfo(excelPath);

                        EditorUtility.DisplayProgressBar("Compiling Excel to Tab...",
                            string.Format("{0} -> {1}", excelPath, compilePath), nowFileIndex/(float) allFilesCount);

                        // 如果已经存在，判断修改时间是否一致，用此来判断是否无需compile，节省时间
                        if (File.Exists(compileToPath))
                        {
                            var toFileInfo = new FileInfo(compileToPath);

                            if (srcFileInfo.LastWriteTime == toFileInfo.LastWriteTime)
                            {
                                Logger.Log("Pass!SameTime! From {0} to {1}", excelPath, compileToPath);
                                continue;
                            }
                        }
                        Logger.Log("Compile from {0} to {1}", excelPath, compileToPath);
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
    }
}