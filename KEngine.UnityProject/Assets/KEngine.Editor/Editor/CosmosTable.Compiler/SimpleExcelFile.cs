using System;
using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;

namespace CosmosTable
{
    /// <summary>
    /// 简单的NPOI Excel封装
    /// </summary>
    public class SimpleExcelFile
    {
        //private Workbook Workbook_;
        //private Worksheet Worksheet_;
        public Dictionary<string, int> ColName2Index;
        public Dictionary<int, string> Index2ColName;
        public Dictionary<string, string> ColName2Statement; //  string,or something
        public Dictionary<string, string> ColName2Comment; // string comment

        /// <summary>
        /// Header, Statement, Comment, at lease 3 rows
        /// 预留行数
        /// </summary>
        private const int PreserverRowCount = 3;

        //private DataTable DataTable_;
        private string Path;
        private IWorkbook Workbook;
        private ISheet Worksheet;
        public bool IsLoadSuccess = true;
        private int _columnCount;

        public SimpleExcelFile(string excelPath)
        {
            Path = excelPath;

            using (var file = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try
                {
                    Workbook = WorkbookFactory.Create(file);
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("无法打开Excel: {0}, 可能原因：正在打开？或是Office2007格式（尝试另存为）？ {1}", excelPath,
                        e.Message));
                    //IsLoadSuccess = false;
                }
            }
            if (IsLoadSuccess)
            {
                if (Workbook == null)
                    throw new Exception("Null Workbook");

                //var dt = new DataTable();

                Worksheet = Workbook.GetSheetAt(0);
                if (Worksheet == null)
                    throw new Exception("Null Worksheet");

                var sheetRowCount = GetWorksheetCount();
                if (sheetRowCount < PreserverRowCount)
                    throw new Exception(string.Format("At lease {0} rows of this excel", sheetRowCount));

                ColName2Index = new Dictionary<string, int>();
                Index2ColName = new Dictionary<int, string>();
                ColName2Statement = new Dictionary<string, string>();
                ColName2Comment = new Dictionary<string, string>();

                // 列头名
                var headerRow = Worksheet.GetRow(0);
                // 列总数保存
                int columnCount = _columnCount = headerRow.LastCellNum;

                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    var cell = headerRow.GetCell(columnIndex);
                    var headerName = cell != null ? cell.ToString().Trim() : ""; // trim!
                    ColName2Index[headerName] = columnIndex;
                    Index2ColName[columnIndex] = headerName;
                }
                // 表头声明
                var statementRow = Worksheet.GetRow(1);
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    var colName = Index2ColName[columnIndex];
                    var statementCell = statementRow.GetCell(columnIndex);
                    var statementString = statementCell != null ? statementCell.ToString() : "";
                    ColName2Statement[colName] = statementString;
                }
                // 表头注释
                var commentRow = Worksheet.GetRow(2);
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    var colName = Index2ColName[columnIndex];
                    var commentCell = commentRow.GetCell(columnIndex);
                    var commentString = commentCell != null ? commentCell.ToString() : "";
                    ColName2Comment[colName] = commentString;
                }
            }
        }

        /// <summary>
        /// 是否存在列名
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public bool HasColumn(string columnName)
        {
            return ColName2Index.ContainsKey(columnName);
        }

        /// <summary>
        /// 清除行内容
        /// </summary>
        /// <param name="row"></param>
        public void ClearRow(int row)
        {
            var theRow = Worksheet.GetRow(row);
            Worksheet.RemoveRow(theRow);
        }

        public float GetFloat(string columnName, int row)
        {
            return float.Parse(GetString(columnName, row));
        }

        public int GetInt(string columnName, int row)
        {
            return int.Parse(GetString(columnName, row));
        }

        /// <summary>
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="dataRow">无计算表头的数据行数</param>
        /// <returns></returns>
        public string GetString(string columnName, int dataRow)
        {
            dataRow += PreserverRowCount;

            var theRow = Worksheet.GetRow(dataRow);
            if (theRow == null)
                theRow = Worksheet.CreateRow(dataRow);

            var colIndex = ColName2Index[columnName];
            var cell = theRow.GetCell(colIndex);
            if (cell == null)
                cell = theRow.CreateCell(colIndex);
            return cell.ToString();
        }

        /// <summary>
        /// 不带3个预留头的数据总行数
        /// </summary>
        /// <returns></returns>
        public int GetRowsCount()
        {
            return GetWorksheetCount() - PreserverRowCount;
        }

        /// <summary>
        /// 工作表的总行数
        /// </summary>
        /// <returns></returns>
        private int GetWorksheetCount()
        {
            return Worksheet.LastRowNum + 1;
        }

        private ICellStyle GreyCellStyleCache;

        public void SetRowGrey(int row)
        {
            var theRow = Worksheet.GetRow(row);
            foreach (var cell in theRow.Cells)
            {
                if (GreyCellStyleCache == null)
                {
                    var newStyle = Workbook.CreateCellStyle();
                    newStyle.CloneStyleFrom(cell.CellStyle);
                    //newStyle.FillBackgroundColor = colorIndex;
                    newStyle.FillPattern = FillPattern.Diamonds;
                    GreyCellStyleCache = newStyle;
                }

                cell.CellStyle = GreyCellStyleCache;
            }
        }

        public void SetRow(string columnName, int row, string value)
        {
            if (!ColName2Index.ContainsKey(columnName))
            {
                throw new Exception(string.Format("No Column: {0} of File: {1}", columnName, Path));
            }
            var theRow = Worksheet.GetRow(row);
            if (theRow == null)
                theRow = Worksheet.CreateRow(row);
            var cell = theRow.GetCell(ColName2Index[columnName]);
            if (cell == null)
                cell = theRow.CreateCell(ColName2Index[columnName]);

            if (value.Length > (1 << 14)) // if too long
            {
                value = value.Substring(0, 1 << 14);
            }
            cell.SetCellValue(value);
        }

        public void Save()
        {
            /*for (var loopRow = Worksheet.FirstRowNum; loopRow <= Worksheet.LastRowNum; loopRow++)
        {
            var row = Worksheet.GetRow(loopRow);
            bool emptyRow = true;
            foreach (var cell in row.Cells)
            {
                if (!string.IsNullOrEmpty(cell.ToString()))
                    emptyRow = false;
            }
            if (emptyRow)
                Worksheet.RemoveRow(row);
        }*/
            //try
            {
                using (var memStream = new MemoryStream())
                {
                    Workbook.Write(memStream);
                    memStream.Flush();
                    memStream.Position = 0;

                    using (var fileStream = new FileStream(Path, FileMode.Create, FileAccess.Write))
                    {
                        var data = memStream.ToArray();
                        fileStream.Write(data, 0, data.Length);
                        fileStream.Flush();
                    }
                }
            }
            //catch (Exception e)
            //{
            //    CDebug.LogError(e.Message);
            //    CDebug.LogError("是否打开了Excel表？");
            //}
        }

        /// <summary>
        /// 获取列总数
        /// </summary>
        /// <returns></returns>
        public int GetColumnCount()
        {
            return _columnCount;
        }
    }
}