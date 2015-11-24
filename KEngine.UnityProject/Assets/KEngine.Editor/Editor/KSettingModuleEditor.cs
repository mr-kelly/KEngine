using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
                NameSpace =  "AppSettings",
            });

            var excelExt = new HashSet<string>() {".xls", ".xlsx"};
            var findDir = Path.Combine(Application.dataPath, sourcePath);
            foreach (var excelPath in Directory.GetFiles(findDir, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(excelPath);
                if (excelExt.Contains(ext))
                {
                    // it's an excel file
                    var relativePath = excelPath.Replace(findDir, "").Replace("\\", "/");
                    if (relativePath.StartsWith("/"))
                        relativePath = relativePath.Substring(1);

                    var compileBaseDir = Path.Combine(Application.dataPath, compilePath);
                    var compileToPath = string.Format("{0}/{1}", compileBaseDir, Path.ChangeExtension(relativePath, ".bytes"));
                
                    Logger.Log("Compile from {0} to {1}", excelPath, compileToPath);
                    compiler.Compile(excelPath, compileToPath, compileBaseDir);

                }
            }
        }
    }
}
