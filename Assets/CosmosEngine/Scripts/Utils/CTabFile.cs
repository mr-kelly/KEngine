//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                     Version 0.8 (20140904)
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CTabFileDef
{
    public static char[] Separators = new char[] { '\t' };
}

public class CTabFile : IDisposable, ICTabReadble, IEnumerable<CTabFile.CTabRow>
{
    CTabRow tabRowCache;
    public CTabFile()
        : base()
    {
        tabRowCache = new CTabRow(this);  // 用來迭代的
    }

    private int ColCount;  // 列数
    protected Dictionary<string, int> ColIndex = new Dictionary<string, int>();
    protected Dictionary<int, List<string>> TabInfo = new Dictionary<int, List<string>>();

    // 直接从字符串分析
    public static CTabFile LoadFromString(string content)
    {
        CTabFile tabFile = new CTabFile();
        tabFile.ParseString(content);

        return tabFile;
    }

    // 直接从文件, 传入完整目录，跟通过资源管理器自动生成完整目录不一样，给art库用的
    public static CTabFile LoadFromFile(string fileFullPath)
    {
        CTabFile tabFile = new CTabFile();
        if (tabFile.LoadByIO(fileFullPath))
            return tabFile;
        else
            return null;
    }

    public bool LoadByIO(string fileName)
    {
        using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            // 不会锁死, 允许其它程序打开
        {

            StreamReader oReader;
            try
            {
                oReader = new StreamReader(fileStream, System.Text.Encoding.UTF8);
            }
            catch
            {
                return false;
            }

            ParseReader(oReader);
        }

        return true;
    }

    protected bool ParseReader(TextReader oReader)
    {
        string sLine = "";
        int indexLine = 0; // 0 是行头
        sLine = oReader.ReadLine(); // 首行
        if (sLine == null)
        {
            return true;
        }

        string[] splitString = sLine.Split(CTabFileDef.Separators, StringSplitOptions.None);  // don't remove RemoveEmptyEntries!
        for (int i = 1; i <= splitString.Length; i++)
        {
            ColIndex[splitString[i - 1].Trim()] = i;
        }
        ColCount = splitString.Length;  // 標題

        List<string> arrlist = new List<string>(splitString);

        TabInfo[indexLine] = arrlist;
        indexLine++;
        while (sLine != null)
        {
            sLine = oReader.ReadLine();
            if (sLine != null)
            {
                string[] splitString1 = sLine.Split(CTabFileDef.Separators, StringSplitOptions.None);
                List<string> arrlist1 = new List<string>(splitString1);
                while (arrlist1.Count < ColCount)
                    arrlist1.Add("");  // 小于header列数, 补空

                TabInfo[indexLine] = arrlist1;
                indexLine++;
            }
        }
        return true;
    }

    protected bool ParseString(string content)
    {
        using (StringReader oReader = new StringReader(content))
        {
            ParseReader(oReader);
        }

        return true;
    }

    // 将当前保存成文件
    public bool Save(string fileName)
    {
        bool result = false;
        StringBuilder sb = new StringBuilder();
        foreach (KeyValuePair<int, List<string>> item in TabInfo)
        {
            int i = 0;
            foreach (string str in item.Value)
            {
                i++;
                sb.Append(str);
                if (i != item.Value.Count)
                {
                    sb.Append('\t');
                }
                else
                {
                    sb.Append('\r');
                    sb.Append('\n');
                }
            }
        }

        try
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
                {
                    sw.Write(sb);

                    result = true;
                }
            }
        }
        catch (IOException e)
        {
            Debug.LogError("可能文件正在被Excel打开?" + e.Message);
            result = false;
        }

        return result;
    }

    // 主要的解析函數
    private string _GetString(int row, int column)
    {
        if (column == 0) // 没有此列
            return string.Empty;
        return TabInfo[row][column - 1].ToString();
    }

    public string GetString(int row, int column)
    {
        return _GetString(row, column);
    }

    public string GetString(int row, string columnName)
    {
        int column;
        if (!ColIndex.TryGetValue(columnName, out column))
            return string.Empty;

        return GetString(row, column);
    }

    public int GetInteger(int row, int column)
    {
        try
        {
            string field = GetString(row, column);
            return (int)float.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public int GetInteger(int row, string columnName)
    {
        try
        {
            string field = GetString(row, columnName);
            return (int)float.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public uint GetUInteger(int row, int column)
    {
        try
        {
            string field = GetString(row, column);
            return (uint)float.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public uint GetUInteger(int row, string columnName)
    {
        try
        {
            string field = GetString(row, columnName);
            return (uint)float.Parse(field);
        }
        catch
        {
            return 0;
        }
    }
    public double GetDouble(int row, int column)
    {
        try
        {
            string field = GetString(row, column);
            return double.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public double GetDouble(int row, string columnName)
    {
        try
        {
            string field = GetString(row, columnName);
            return double.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public float GetFloat(int row, int column)
    {
        try
        {
            string field = GetString(row, column);
            return float.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public float GetFloat(int row, string columnName)
    {
        try
        {
            string field = GetString(row, columnName);
            return float.Parse(field);
        }
        catch
        {
            return 0;
        }
    }

    public bool GetBool(int row, int column)
    {
        int field = GetInteger(row, column);
        return field != 0;
    }

    public bool GetBool(int row, string columnName)
    {
        int field = GetInteger(row, columnName);
        return field != 0;
    }

    public bool HasColumn(string colName)
    {
        return ColIndex.ContainsKey(colName);
    }

    public int NewColumn(string colName = "")
    {
        if (!string.IsNullOrEmpty(colName))  // 无列命，不保存字符索引
            ColIndex.Add(colName, ColIndex.Count + 1);
        ColCount++;

        if (!TabInfo.ContainsKey(0))
            TabInfo[0] = new List<string>();  // 0 行是行头
        TabInfo[0].Add(colName);

        for (int i = 1; i < TabInfo.Count; i++)
        {
            TabInfo[i].Add("");
        }

        return ColCount;
    }

    public int NewRow()
    {
        List<string> list = new List<string>();
        for (int i = 0; i < ColCount; i++)
        {
            list.Add("");
        }
        int rowId = TabInfo.Count;
        TabInfo.Add(rowId, list);
        return rowId;
    }

    public int GetHeight()
    {
        return TabInfo.Count;
    }

    public int GetWidth()
    {
        return ColCount;
    }

    public bool SetValue<T>(int row, int column, T value)
    {
        if (row > TabInfo.Count || column > ColCount || row <= 0 || column <= 0)  //  || column > ColIndex.Count
        {
            return false;
        }
        string content = Convert.ToString(value);
        if (row == 0)
        {
            foreach (KeyValuePair<string, int> item in ColIndex)
            {
                if (item.Value == column)
                {
                    ColIndex.Remove(item.Key);
                    ColIndex[content] = item.Value;
                    break;
                }
            }
        }
        TabInfo[row].RemoveAt(column - 1);
        TabInfo[row].Insert(column - 1, content);
        return true;
    }

    public bool SetValue<T>(int row, string columnName, T value)
    {
        int column;
        if (!ColIndex.TryGetValue(columnName, out column))
            return false;

        return SetValue(row, column, value);
    }

    IEnumerator<CTabRow> IEnumerable<CTabRow>.GetEnumerator()
    {
        int rowStart = 1;
        for (int i = rowStart; i < GetHeight(); i++)
        {
            tabRowCache.Row = i;
            yield return tabRowCache;
        }
    }

    public IEnumerator GetEnumerator()
    {
        int rowStart = 1;
        for (int i = rowStart; i < GetHeight(); i++)
        {
            tabRowCache.Row = i;
            yield return tabRowCache;
        }
    }

    public class CTabRow  // 一行
    {
        internal CTabFile TabFile;

        public int Row { get; internal set; }

        internal CTabRow(CTabFile tabFile)
        {
            TabFile = tabFile;
        }

        public string GetString(string colName)
        {
            return TabFile.GetString(Row, colName);
        }
        public int GetInteger(string colName)
        {
            return TabFile.GetInteger(Row, colName);
        }
    }

    public void Dispose()
    {
        this.ColIndex.Clear();
        this.TabInfo.Clear();
    }

    public void Close()
    {
        Dispose();
    }
}
