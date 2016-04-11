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
            // Default C# Code Templates
            //CodeTemplates = new Dictionary<string, string>()
            //{
            //    {File.ReadAllText("./GenCode.cs.tpl"), "TabConfigs.cs"}, // code template -> CodePath
            //};
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

        private Hash DoCompiler(string path, SimpleExcelFile excelFile, string compileToFilePath = null, string compileBaseDir = null)
        {
            //var fileExt = Path.GetExtension(path);
            //IExcelDataReader excelReader = null;
            //if (fileExt == ".xlsx" || fileExt == ".xml")
            //{
            //    try
            //    {
            //        //2. Reading from a OpenXml Excel file (2007 format; *.xlsx)
            //        excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
            //    }
            //    catch (Exception e2)
            //    {
            //        throw new InvalidExcelException("Cannot read Excel 2007 File : " + path + e2.Message);
            //    }
            //}
            //else
            //{
            //    try
            //    {
            //        //1. Reading from a binary Excel file ('97-2003 format; *.xls)
            //        excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
            //    }
            //    catch (Exception e2)
            //    {
            //        throw new InvalidExcelException("Cannot read Excel 2003 File : " + path + e2.Message);
            //    }
            //}


            //if (excelReader != null)
            //{
            //    using (excelReader)
            //    {

            //    }
            //}
            return DoCompilerExcelReader(path, excelFile, compileToFilePath, compileBaseDir);
        }


        private Hash DoCompilerExcelReader(string path, SimpleExcelFile excelFile, string compileToFilePath = null, string compileBaseDir = null)
        {
            //3. DataSet - The result of each spreadsheet will be created in the result.Tables
            //DataSet result = excelReader.AsDataSet();

            //4. DataSet - Create column names from first row
            //excelFile.IsFirstRowAsColumnNames = true;


            //DataSet result = excelFile.AsDataSet();

            //if (result.Tables.Count <= 0)
            //    throw new InvalidExcelException("No Sheet!");


            var renderVars = new RenderTemplateVars();
            renderVars.FieldsInternal = new List<RenderFieldVars>();

            //var sheet1 = result.Tables[0];

            var strBuilder = new StringBuilder();

            var ignoreColumns = new HashSet<int>();
            //var ignoreRows = new HashSet<int>();

            //// 寻找注释行，1,或2行
            //var hasStatementRow = false;
            //var statementRow = sheet1.Rows[0].ItemArray;
            //var regExCheckStatement = new Regex(@"\[(.*)\]"); // 不要[ xx ]符号了
            //foreach (var cellVal in statementRow)
            //{
            //    if ((cellVal is string))
            //    {
            //        var matches = regExCheckStatement.Matches(cellVal.ToString());
            //        if (matches.Count > 0)
            //        {
            //            hasStatementRow = true;
            //        }
            //    }

            //    break;
            //}

            //// 获取注释行
            //var commentRow = hasStatementRow ? sheet1.Rows[1].ItemArray : sheet1.Rows[0].ItemArray;
            //var commentsOfColumns = new List<string>();
            //foreach (var cellVal in commentRow)
            //{
            //    commentsOfColumns.Add(cellVal.ToString());
            //}

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

            // Data Rows
            //var rowIndex = 1;
            //foreach (DataRow dRow in sheet1.Rows)
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
                        //        // 如果单元格是字符串，换行符改成\\n
                        //        if (item is string)
                        //        {
                        //            var sItme = item as string;
                        //            cloneItem = sItme.Replace("\n", "\\n");
                        //        }
                        //        strBuilder.Append(cloneItem);
                        //        colIndex++;

                        // 如果单元格是字符串，换行符改成\\n
                        cellStr = cellStr.Replace("\n", "\\n");
                        strBuilder.Append(cellStr);

                    }

                }
            }
            //if (hasStatementRow)
            //{
            //    // 有声明行，忽略第2行
            //    if (rowIndex == 2)
            //    {
            //            rowIndex++;
            //            continue;

            //        }
            //    }
            //    else
            //    {
            //        // 无声明行，忽略第1行
            //        if (rowIndex == 1)
            //        {
            //            rowIndex++;
            //            continue;
            //        }
            //    }

            //    colIndex = 0;
            //foreach (var item in dRow.ItemArray)
            //    {
            //        if (ignoreColumns.Contains(colIndex)) // comment column, ignore
            //            continue;

            //        if (colIndex > 0)
            //            strBuilder.Append("\t");

            //        var cloneItem = item;
            //        // 如果单元格是字符串，换行符改成\\n
            //        if (item is string)
            //        {
            //            var sItme = item as string;
            //            cloneItem = sItme.Replace("\n", "\\n");
            //        }
            //        strBuilder.Append(cloneItem);
            //        colIndex++;
            //    }
            //    strBuilder.Append("\n");
            //    rowIndex++;
            //}

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
            File.WriteAllText(exportPath, strBuilder.ToString());


            // 基于base dir路径
            var tabFilePath = exportPath; // without extension
            if (!string.IsNullOrEmpty(compileBaseDir))
            {
                tabFilePath = tabFilePath.Replace(compileBaseDir, ""); // 保留后戳
            }
            if (tabFilePath.StartsWith("/"))
                tabFilePath = tabFilePath.Substring(1);

            var classNameOrigin = Path.GetDirectoryName(tabFilePath) + "/" + Path.GetFileNameWithoutExtension(tabFilePath);// 未处理路径的类名, 去掉后缀扩展名

            renderVars.ClassName = string.Join("",
                (from name in classNameOrigin.Replace("/", "_").Replace("\\", "_").Replace(" ", "").Split(new char[]{'_'}, StringSplitOptions.RemoveEmptyEntries)
                 select System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name)).ToArray());
            renderVars.TabFilePath = tabFilePath;

            return Hash.FromAnonymousObject(renderVars);
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
                if (colNameStr.StartsWith(commentStartsWith))
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
        /// <returns></returns>
        public Hash Compile(string path, string compileToFilePath = null, string compileBaseDir = null)
        {
            // 确保目录存在
            var compileToFileDirPath = Path.GetDirectoryName(compileToFilePath);

            if (!Directory.Exists(compileToFileDirPath))
                Directory.CreateDirectory(compileToFileDirPath);

            var excelFile = new SimpleExcelFile(path);
            var hash = DoCompiler(path, excelFile, compileToFilePath, compileBaseDir);
            return hash;

        }
    }
}