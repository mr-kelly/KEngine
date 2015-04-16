using System;
using System.IO;
using System.Text;

namespace CosmosTable
{
    /// <summary>
    /// Write the TabFile!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TabFileWriter<T> : IDisposable where T : TabRow, new()
    {
        protected readonly TableFile<T> TabFile;

        public TabFileWriter()
        {
            TabFile = new TableFile<T>();
            CheckHeaders();
        }

        void CheckHeaders()
        {
            // 确保表头！
            foreach (var prop in TabFile.TabProperties)
            {
                HeaderInfo headerInfo;
                if (!TabFile.Headers.TryGetValue(prop.Name, out headerInfo))
                {
                    NewColumn(prop.Name);
                }
            }

            foreach (var field in TabFile.TabFields)
            {
                HeaderInfo headerInfo;
                if (!TabFile.Headers.TryGetValue(field.Name, out headerInfo))
                {
                    NewColumn(field.Name);
                }
            }
            
        }
        public TabFileWriter(TableFile<T> tabFile)
        {
            TabFile = tabFile;
        }

        // 将当前保存成文件
        public bool Save(string fileName)
        {
            bool result = false;
            StringBuilder sb = new StringBuilder();

            foreach (var header in TabFile.Headers.Values)
                sb.Append(string.Format("{0}\t", header.HeaderName));
            sb.Append("\r\n");

            foreach (var header in TabFile.Headers.Values)
                sb.Append(string.Format("{0}\t", header.HeaderDef));
            sb.Append("\r\n");

            // 获取所有值
            foreach (var kv in TabFile.Rows)
            {
                var rowT = kv.Value;
                foreach (var prop in TabFile.TabProperties)
                {
                    var retVal = prop.GetGetMethod().Invoke(rowT, new object[]{});
                    sb.Append(retVal);
                    sb.Append('\t');
                }
                foreach (var field in TabFile.TabFields)
                {
                    var retVal = field.GetValue(rowT);
                    sb.Append(retVal);
                    sb.Append('\t');
                }
                sb.Append("\r\n");
            }

            try
            {
                //using (FileStream fs = )
                {
                    using (StreamWriter sw = new StreamWriter(new FileStream(fileName, FileMode.Create), System.Text.Encoding.UTF8))
                    {
                        sw.Write(sb);

                        result = true;
                    }
                }
            }
            catch (IOException e)
            {
				result = false;
                throw new Exception("可能文件正在被Excel打开?" + e.Message);
            }

            return result;
        }

        public T NewRow()
        {
            int rowId = TabFile.Rows.Count + 1;
            var newRow = new T { RowNumber = rowId };

            TabFile.Rows.Add(rowId, newRow);

            return newRow;
        }

        public T GetRow(int row)
        {
            T rowT;
            if (TabFile.Rows.TryGetValue(row, out rowT))
            {
                return rowT;
            }

            return null;
        }
        public int NewColumn(string colName, string defineStr = "")
        {
            if (string.IsNullOrEmpty(colName))
                throw new Exception("Null Col Name : " + colName);

            var newHeader = new HeaderInfo
            {
                ColumnIndex = TabFile.Headers.Count + 1,
                HeaderName = colName,
                HeaderDef = defineStr,
            };

            TabFile.Headers.Add(colName, newHeader);
            TabFile._colCount++;

            return TabFile._colCount;
        }

        public void Dispose()
        {
            TabFile.Dispose();
        }
    }
}
