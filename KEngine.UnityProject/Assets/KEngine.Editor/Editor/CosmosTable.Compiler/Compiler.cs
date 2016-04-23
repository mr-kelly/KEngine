using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DotLiquid;

namespace CosmosTable
{
    /// <summary>
    /// Invali Excel Exception
    /// </summary>
    public class InvalidExcelException : Exception
    {
        public InvalidExcelException(string msg)
            : base(msg)
        {
        }
    }

    /// <summary>
    /// 用来进行模板渲染
    /// </summary>
    public class RenderTemplateVars
    {
        public string TabFilePath { get; set; }
        public string ClassName { get; set; }
        public List<RenderFieldVars> FieldsInternal { get; set; } // column + type

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

        public List<Hash> Columns2DefaultValus { get; set; } // column + Default Values
        public string PrimaryKey { get; set; }

        public RenderTemplateVars()
        {
            FieldsInternal = new List<RenderFieldVars>();
            Columns2DefaultValus = new List<Hash>();
        }
    }

    public class RenderFieldVars
    {
        public int Index { get; set; }
        public string Type { get; set; }

        /// <summary>
        /// 经过格式化，去掉[]的类型字符串，支持数组(int[] -> int_array), 字典(map[string]int) -> map_string_int
        /// </summary>
        public string TypeMethod
        {
            get { return Type.Replace(@"[]", "_array").Replace("<", "_").Replace(">", "").Replace(",", "_"); }
        }

        /// <summary>
        /// 类型
        /// </summary>
        public string FormatType
        {
            get
            {
                return Type;
            }
        }

        public string Name { get; set; }
        public string DefaultValue { get; set; }
        public string Comment { get; set; }
    }

    public class CompilerConfig
    {
        /// <summary>
        /// 编译后的扩展名
        /// </summary>
        public string ExportTabExt = ".bytes";
        // 被认为是注释的表头
        public string[] CommentColumnStartsWith = { "Comment", "#" };


        public CompilerConfig()
        {
        }
    }

    /// <summary>
    /// Compile Excel to TSV
    /// </summary>
    public class Compiler
    {
        private readonly CompilerConfig _config;

        public Compiler()
            : this(new CompilerConfig()
            {
            })
        {
        }

        public Compiler(CompilerConfig cfg)
        {
            _config = cfg;
        }

        private RenderTemplateVars DoCompilerExcelReader(string path, SimpleExcelFile excelFile, string compileToFilePath = null, string compileBaseDir = null, bool doCompile = true)
        {
            var renderVars = new RenderTemplateVars();
            renderVars.FieldsInternal = new List<RenderFieldVars>();

            var strBuilder = new StringBuilder();
            var ignoreColumns = new HashSet<int>();
            // Header Column
            foreach (var colNameStr in excelFile.ColName2Index.Keys)
            {
                var colIndex = excelFile.ColName2Index[colNameStr];
                if (!string.IsNullOrEmpty(colNameStr))
                {
                    var isCommentColumn = CheckCommentString(colNameStr);
                    if (isCommentColumn)
                    {
                        ignoreColumns.Add(colIndex);
                    }
                    else
                    {
                        if (colIndex > 0)
                            strBuilder.Append("\t");
                        strBuilder.Append(colNameStr);

                        string typeName = "string";
                        string defaultVal = "";

                        var attrs = excelFile.ColName2Statement[colNameStr].Split(new char[] {'|', '/'}, StringSplitOptions.RemoveEmptyEntries);
                        // Type
                        if (attrs.Length > 0)
                        {
                            typeName = attrs[0];
                        }
                        // Default Value
                        if (attrs.Length > 1)
                        {
                            defaultVal = attrs[1];
                        }
                        if (attrs.Length > 2)
                        {
                            if (attrs[2] == "pk")
                            {
                                renderVars.PrimaryKey = colNameStr;
                            }
                        }

                        renderVars.FieldsInternal.Add(new RenderFieldVars
                        {
                            Index = colIndex,
                            Type = typeName,
                            Name = colNameStr,
                            DefaultValue = defaultVal,
                            Comment = excelFile.ColName2Comment[colNameStr],
                        });
                    }
                }
            }
            strBuilder.Append("\n");

            // Statements rows, keeps
            foreach (var kv in excelFile.ColName2Statement)
            {
                var colName = kv.Key;
                var statementStr = kv.Value;
                var colIndex = excelFile.ColName2Index[colName];

                if (ignoreColumns.Contains(colIndex)) // comment column, ignore
                    continue;
                if (colIndex > 0)
                    strBuilder.Append("\t");
                strBuilder.Append(statementStr);
            }
            strBuilder.Append("\n");

            if (doCompile)
            {
                // 如果不需要真编译，获取头部信息就够了
                // Data Rows
                for (var startRow = 0; startRow < excelFile.GetRowsCount(); startRow++)
                {
                    var columnCount = excelFile.GetColumnCount();
                    for (var loopColumn = 0; loopColumn < columnCount; loopColumn++)
                    {
                        if (!ignoreColumns.Contains(loopColumn)) // comment column, ignore 注释列忽略
                        {
                            var columnName = excelFile.Index2ColName[loopColumn];
                            var cellStr = excelFile.GetString(columnName, startRow);

                            if (loopColumn == 0)
                            {
                                if (CheckCommentString(cellStr)) // 如果行首为#注释字符，忽略这一行)
                                {
                                    break;
                                }
                                if (startRow != 0) // 不是第一行，往添加换行，首列
                                    strBuilder.Append("\n");
                            }

                            if (loopColumn > 0 && loopColumn != (columnCount - 1)) // 最后一列不需加tab
                                strBuilder.Append("\t");

                            // 如果单元格是字符串，换行符改成\\n
                            cellStr = cellStr.Replace("\n", "\\n");
                            strBuilder.Append(cellStr);

                        }

                    }
                }
            }
            
            var fileName = Path.GetFileNameWithoutExtension(path);
            string exportPath;
            if (!string.IsNullOrEmpty(compileToFilePath))
            {
                exportPath = compileToFilePath;
            }
            else
            {
                // use default
                exportPath = fileName + _config.ExportTabExt;
            }

            var exportDirPath = Path.GetDirectoryName(exportPath);
            if (!Directory.Exists(exportDirPath))
                Directory.CreateDirectory(exportDirPath);

            // 是否写入文件
            if (doCompile)
                File.WriteAllText(exportPath, strBuilder.ToString());


            // 基于base dir路径
            var tabFilePath = exportPath; // without extension
            if (!string.IsNullOrEmpty(compileBaseDir))
            {
                tabFilePath = tabFilePath.Replace(compileBaseDir, ""); // 保留后戳
            }
            if (tabFilePath.StartsWith("/"))
                tabFilePath = tabFilePath.Substring(1);

			// 未处理路径的类名, 去掉后缀扩展名
            var classNameOrigin = Path.ChangeExtension(tabFilePath, null);

            var className = classNameOrigin.Replace("/", "_").Replace("\\", "_");
            className = className.Replace(" ", "");
            className = string.Join("", (from name
                in className.Split(new[] {'_'}, StringSplitOptions.RemoveEmptyEntries)
                select (name[0].ToString().ToUpper() + name.Substring(1, name.Length - 1)))
                .ToArray());
            renderVars.ClassName = className;
            renderVars.TabFilePath = tabFilePath;

            return renderVars;
        }

        /// <summary>
        /// 检查一个表头名，是否是可忽略的注释
        /// 或检查一个字符串
        /// </summary>
        /// <param name="colNameStr"></param>
        /// <returns></returns>
        private bool CheckCommentString(string colNameStr)
        {
            foreach (var commentStartsWith in _config.CommentColumnStartsWith)
            {
                if (colNameStr.ToLower().StartsWith(commentStartsWith.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Compile a setting file, return a hash for template
        /// </summary>
        /// <param name="path"></param>
        /// <param name="compileToFilePath"></param>
        /// <param name="compileBaseDir"></param>
        /// <param name="doRealCompile">Real do, or just get the template var?</param>
        /// <returns></returns>
        public RenderTemplateVars Compile(string path, string compileToFilePath = null, string compileBaseDir = null, bool doRealCompile = true)
        {
            // 确保目录存在
            var compileToFileDirPath = Path.GetDirectoryName(compileToFilePath);

            if (!Directory.Exists(compileToFileDirPath))
                Directory.CreateDirectory(compileToFileDirPath);

            var excelFile = new SimpleExcelFile(path);
            var hash = DoCompilerExcelReader(path, excelFile, compileToFilePath, compileBaseDir, doRealCompile);
            return hash;

        }
    }
}