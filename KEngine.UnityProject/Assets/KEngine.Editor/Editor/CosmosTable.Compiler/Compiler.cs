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
using System.Text;
using System.Text.RegularExpressions;

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
    /// 返回编译结果
    /// </summary>
    public class TableCompileResult
    {
        public string TabFilePath { get; set; }
        public List<TableColumnVars> FieldsInternal { get; set; } // column + type

        public string PrimaryKey { get; set; }
        public ITableSourceFile ExcelFile { get; internal set; }

        public TableCompileResult()
        {
            FieldsInternal = new List<TableColumnVars>();
        }

    }

    public class TableColumnVars
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
        public string[] CommentStartsWith = { "Comment", "#" };

        /// <summary>
        /// 定义条件编译指令
        /// </summary>
        public string[] ConditionVars;

        public CompilerConfig()
        {
        }
    }

    /// <summary>
    /// Compile Excel to TSV
    /// </summary>
    public class Compiler
    {

        /// <summary>
        /// 编译时，判断格子的类型
        /// </summary>
        public enum CellType
        {
            Value,
            Comment,
            If,
            Endif
        }

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


        private TableCompileResult DoCompilerExcelReader(string path, ITableSourceFile excelFile, string compileToFilePath = null, string compileBaseDir = null, bool doCompile = true)
        {
            var renderVars = new TableCompileResult();
            renderVars.ExcelFile = excelFile;
            renderVars.FieldsInternal = new List<TableColumnVars>();

            var tableBuilder = new StringBuilder();
            var rowBuilder = new StringBuilder();
            var ignoreColumns = new HashSet<int>();
            // Header Column
            foreach (var colNameStr in excelFile.ColName2Index.Keys)
            {
                var colIndex = excelFile.ColName2Index[colNameStr];
                if (!string.IsNullOrEmpty(colNameStr))
                {
                    var isCommentColumn = CheckCellType(colNameStr) == CellType.Comment;
                    if (isCommentColumn)
                    {
                        ignoreColumns.Add(colIndex);
                    }
                    else
                    {
                        if (colIndex > 0)
                            tableBuilder.Append("\t");
                        tableBuilder.Append(colNameStr);

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

                        renderVars.FieldsInternal.Add(new TableColumnVars
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
            tableBuilder.Append("\n");

            // Statements rows, keeps
            foreach (var kv in excelFile.ColName2Statement)
            {
                var colName = kv.Key;
                var statementStr = kv.Value;
                var colIndex = excelFile.ColName2Index[colName];

                if (ignoreColumns.Contains(colIndex)) // comment column, ignore
                    continue;
                if (colIndex > 0)
                    tableBuilder.Append("\t");
                tableBuilder.Append(statementStr);
            }
            tableBuilder.Append("\n");

            // #if check, 是否正在if false模式, if false时，行被忽略
            var ifCondtioning = true;
            if (doCompile)
            {
                // 如果不需要真编译，获取头部信息就够了
                // Data Rows
                for (var startRow = 0; startRow < excelFile.GetRowsCount(); startRow++)
                {
                    rowBuilder.Length = 0;
                    rowBuilder.Capacity = 0;
                    var columnCount = excelFile.GetColumnCount();
                    for (var loopColumn = 0; loopColumn < columnCount; loopColumn++)
                    {
                        if (!ignoreColumns.Contains(loopColumn)) // comment column, ignore 注释列忽略
                        {
                            var columnName = excelFile.Index2ColName[loopColumn];
                            var cellStr = excelFile.GetString(columnName, startRow);

                            if (loopColumn == 0)
                            {
                                var cellType = CheckCellType(cellStr);
                                if (cellType == CellType.Comment) // 如果行首为#注释字符，忽略这一行)
                                    break;

                                // 进入#if模式
                                if (cellType == CellType.If)
                                {
                                    var ifVars = GetIfVars(cellStr);
                                    var hasAllVars = true;
                                    foreach (var var in ifVars)
                                    {
                                        if (_config.ConditionVars == null || 
                                            !_config.ConditionVars.Contains(var)) // 定义的变量，需要全部配置妥当,否则if失败
                                        {
                                            hasAllVars = false;
                                            break;
                                        }
                                    }
                                    ifCondtioning = hasAllVars;
                                    break;
                                }
                                if (cellType == CellType.Endif)
                                {
                                    ifCondtioning = true;
                                    break;
                                }

                                if (!ifCondtioning) // 这一行被#if 忽略掉了
                                    break;


                                if (startRow != 0) // 不是第一行，往添加换行，首列
                                    rowBuilder.Append("\n");
                            }

                            if (loopColumn > 0 && loopColumn < columnCount) // 最后一列不需加tab
                                rowBuilder.Append("\t");

                            // 如果单元格是字符串，换行符改成\\n
                            cellStr = cellStr.Replace("\n", "\\n");
                            rowBuilder.Append(cellStr);

                        }
                    }

                    // 如果这行，之后\t或换行符，无其它内容，认为是可以省略的
                    if (!string.IsNullOrEmpty(rowBuilder.ToString().Trim()))
                        tableBuilder.Append(rowBuilder);
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
                File.WriteAllText(exportPath, tableBuilder.ToString());


            // 基于base dir路径
            var tabFilePath = exportPath; // without extension
            if (!string.IsNullOrEmpty(compileBaseDir))
            {
                tabFilePath = tabFilePath.Replace(compileBaseDir, ""); // 保留后戳
            }
            if (tabFilePath.StartsWith("/"))
                tabFilePath = tabFilePath.Substring(1);

            renderVars.TabFilePath = tabFilePath;

            return renderVars;
        }

        /// <summary>
        /// 获取#if A B语法的变量名，返回如A B数组
        /// </summary>
        /// <param name="cellStr"></param>
        /// <returns></returns>
        private string[] GetIfVars(string cellStr)
        {
            return cellStr.Replace("#if", "").Trim().Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// 检查一个表头名，是否是可忽略的注释
        /// 或检查一个字符串
        /// </summary>
        /// <param name="colNameStr"></param>
        /// <returns></returns>
        private CellType CheckCellType(string colNameStr)
        {
            if (colNameStr.StartsWith("#if"))
                return CellType.If;
            if (colNameStr.StartsWith("#endif"))
                return CellType.Endif;
            foreach (var commentStartsWith in _config.CommentStartsWith)
            {
                if (colNameStr.ToLower().Trim().StartsWith(commentStartsWith.ToLower()))
                {
                    return CellType.Comment;
                }
            }

            return CellType.Value;
        }

        /// <summary>
        /// Compile a setting file, return a hash for template
        /// </summary>
        /// <param name="path"></param>
        /// <param name="compileToFilePath"></param>
        /// <param name="compileBaseDir"></param>
        /// <param name="doRealCompile">Real do, or just get the template var?</param>
        /// <returns></returns>
        public TableCompileResult Compile(string path, string compileToFilePath = null, string compileBaseDir = null, bool doRealCompile = true)
        {
            // 确保目录存在
            var compileToFileDirPath = Path.GetDirectoryName(compileToFilePath);

            if (!Directory.Exists(compileToFileDirPath))
                Directory.CreateDirectory(compileToFileDirPath);

            var ext = Path.GetExtension(path);

            ITableSourceFile sourceFile;
            if (ext == ".tsv") sourceFile = new SimpleTSVFile(path);
            else sourceFile = new SimpleExcelFile(path);
            
            var hash = DoCompilerExcelReader(path, sourceFile, compileToFilePath, compileBaseDir, doRealCompile);
            return hash;

        }
    }
}