//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                          Version 0.8
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;


[CDependencyClass(typeof(CResourceModule))]
[CDependencyClass(typeof(CSettingManager))]
public class CGameSettings : ICModule
{
    class _InstanceClass { public static CGameSettings _Instance = new CGameSettings();}
    public static CGameSettings Instance { get { return _InstanceClass._Instance; } }

    public Dictionary<Type, Dictionary<string, CBaseInfo>> SettingInfos = new Dictionary<Type, Dictionary<string, CBaseInfo>>();

    private Dictionary<Type, string[]> LazyLoad = new Dictionary<Type, string[]>();

    public Action InitAction; // Init時調用的委託、函數指針
    
    public static event Action<string, string> FoundDuplicatedIdEvent;

    public IEnumerator Init()
    {
        if (this.InitAction == null)
            CBase.LogError("GameSettings沒有定義初始化行為！！！");
        else
            this.InitAction();
        yield break;
    }

    public IEnumerator UnInit()
    {
        yield break;
    }

    public void LoadTab<T>(params string[] tabPaths) where T : CBaseInfo
    {
        LazyLoad[typeof(T)] = tabPaths;
    }

    private void EnsureLoad<T>() where T : CBaseInfo
    {
        Type type = typeof (T);
        string[] loadFilePaths;
        if (LazyLoad.TryGetValue(type, out loadFilePaths))
        {
            DoLoadTab<T>(loadFilePaths);
            LazyLoad.Remove(type);
        }
    }

    private void DoLoadTab<T>(string[] tabPaths) where T : CBaseInfo
    {
        foreach (string tabPath in tabPaths)
        {
#if GAME_CLIENT
            using (CTabFile tabFile = CTabFile.LoadFromString(CSettingManager.Instance.LoadSetting(tabPath)))
#else 
    // Editor Only
        string p1 = System.IO.Path.GetFullPath("Assets/" + CCosmosEngine.GetConfig("ProductRelPath") + "/") + tabPath;
        using (CTabFile tabFile = CTabFile.LoadFromString(System.IO.File.ReadAllText(p1)))
#endif
            {
                
                Dictionary<string, CBaseInfo> dict;
                if (!SettingInfos.TryGetValue(typeof(T), out dict))  // 如果没有才添加
                    dict = new Dictionary<string, CBaseInfo>();

                const int rowStart = 1;
                for (int i = rowStart; i < tabFile.GetHeight(); i++)
                {
                    // 先读取ID， 获取是否之前已经读取过配置，
                    // 如果已经读取过，那么获取原配置对象，并重新赋值 (因为游戏中其它地方已经存在它的引用了，直接替换内存泄露)
                    string id = tabFile.GetString(i, "Id"); // 获取ID是否存在, 如果已经存在，那么替换其属性，不new一个
                    CBaseInfo existOne;
                    if (dict.TryGetValue(id, out existOne))
                    {
                        if (FoundDuplicatedIdEvent != null)
                            FoundDuplicatedIdEvent(tabPath, id);

                        CBaseInfo existT = existOne;
                        CBaseInfo.LoadFromTab(typeof (T), ref existT, tabFile, i); // 修改原对象，不new
                        (existT as CBaseInfo).Parse();
                    }
                    else
                    {
                        T pInfo = CBaseInfo.LoadFromTab(typeof (T), tabFile, i) as T;
                        pInfo.Parse();
                        dict[pInfo.Id] = pInfo; // 不存在，直接new
                    }
                }

                SettingInfos[typeof (T)] = dict;
            }
        }
    }

    public List<T> GetInfos<T>() where T : CBaseInfo
    {
        EnsureLoad<T>();

        Dictionary<string, CBaseInfo> dict;
        if (SettingInfos.TryGetValue(typeof(T), out dict))
        {
            //CBase.Log(dict.Count+"");
            List<T> list = new List<T>();
            foreach (CBaseInfo item in dict.Values)
            {
                list.Add((T)item);
            }
            return list;
        }
        else
            CBase.LogError("找不到类型配置{0}, 总类型数{1}", typeof(T).Name, SettingInfos.Count);

        return null;
    }
    public T GetInfo<T>(string id) where T : CBaseInfo
    {
        EnsureLoad<T>();

        Dictionary<string, CBaseInfo> dict;
        if (SettingInfos.TryGetValue(typeof(T), out dict))
        {
            CBaseInfo tabInfo;
            if (dict.TryGetValue(id, out tabInfo))
            {
                return (T)tabInfo;
            }
            else
                CBase.LogError("找不到类型{0} Id为{1}的配置对象, 类型表里共有对象{2}", typeof(T).Name, id, dict.Count);
        }
        else
            CBase.LogError("嘗試Id {0}, 找不到类型配置{1}, 总类型数{2}", id, typeof(T).Name, SettingInfos.Count);

        return null;
    }
    // 数字ID是索引
    public T GetInfo<T>(int id) where T : CBaseInfo
    {
        return GetInfo<T>(id.ToString());
    }

}

public class CBaseInfo
{
    public string Id;

    private int? _CacheIntId;

    /// <summary>
    /// Id是一個字符串, 嘗試Id轉成Int
    /// </summary>
    public int IntId
    {
        get
        {
            if (_CacheIntId != null)
                return _CacheIntId.Value;

            int tryInt;
            if (!int.TryParse(Id, out tryInt))
                CBase.LogError("錯誤解析Int Id");
            
            _CacheIntId = tryInt;

            return _CacheIntId.Value;
        }
    }
    
    public virtual void Parse()
    {

    }

    public static void LoadFromTab(Type type, ref CBaseInfo newT, ICTabReadble tabFile, int row)
    {
        CBase.Assert(typeof(CBaseInfo).IsAssignableFrom(type));

        FieldInfo[] fields = type.GetFields();
        foreach (FieldInfo field in fields)
        {
            if (!tabFile.HasColumn(field.Name))
            {
                CBase.LogError("表{0} 找不到表头{1}", type.Name, field.Name);
                continue;
            }
            object value;
            if (field.FieldType == typeof(int))
            {
                value = tabFile.GetInteger(row, field.Name);
            }
            else if (field.FieldType == typeof(long))
            {
                value = (long)tabFile.GetInteger(row, field.Name);
            }
            else if (field.FieldType == typeof(string))
            {
                value = tabFile.GetString(row, field.Name);
            }
            else if (field.FieldType == typeof(float))
            {
                value = tabFile.GetFloat(row, field.Name);
            }
            else if (field.FieldType == typeof(bool))
            {
                value = tabFile.GetBool(row, field.Name);
            }
            else if (field.FieldType == typeof(double))
            {
                value = tabFile.GetDouble(row, field.Name);
            }
            else if (field.FieldType == typeof(uint))
            {
                value = tabFile.GetUInteger(row, field.Name);
            }
            else if (field.FieldType == typeof(List<string>))
            {
                string sz = tabFile.GetString(row, field.Name);
                value = CTool.Split<string>(sz, '|');
            }
            else if (field.FieldType == typeof(List<int>))
            {
                List<int> retInt = new List<int>();
                string szArr = tabFile.GetString(row, field.Name);
                if (!string.IsNullOrEmpty(szArr))
                {
                    string[] szIntArr = szArr.Split('|');
                    foreach (string szInt in szIntArr)
                    {
                        float parseFloat;
                        float.TryParse(szInt, out parseFloat);
                        int parseInt_ = (int)parseFloat;
                        retInt.Add(parseInt_);
                    }
                    value = retInt;
                }
                else
                    value = new List<int>();
            }
            else if (field.FieldType == typeof(List<List<string>>))
            {
                string sz = tabFile.GetString(row, field.Name);
                if (!string.IsNullOrEmpty(sz))
                {
                    var szOneList = new List<List<string>>();
                    string[] szArr = sz.Split('|');
                    foreach (string szOne in szArr)
                    {
                        string[] szOneArr = szOne.Split('-');
                        szOneList.Add(new List<string>(szOneArr));
                    }
                    value = szOneList;
                }
                else
                    value = new List<List<string>>();
            }
            else if (field.FieldType == typeof(List<List<int>>))
            {
                string sz = tabFile.GetString(row, field.Name);
                if (!string.IsNullOrEmpty(sz))
                {
                    var zsOneIntList = new List<List<int>>();
                    string[] szArr = sz.Split('|');
                    foreach (string szOne in szArr)
                    {
                        List<int> retInts = new List<int>();
                        string[] szOneArr = szOne.Split('-');
                        foreach (string szOneInt in szOneArr)
                        {
                            float parseFloat;
                            float.TryParse(szOneInt, out parseFloat);
                            int parseInt_ = (int)parseFloat;
                            retInts.Add(parseInt_);
                        }
                        zsOneIntList.Add(retInts);
                    }
                    value = zsOneIntList;
                }
                else
                    value = new List<List<int>>();
            }
            else
            {
                CBase.LogWarning("未知类型: {0}", field.Name);
                value = null;
            }

            if (field.Name == "Id")  // 如果是Id主键，确保数字成整数！不是浮点数  因为excel转tab可能转成浮点
            {
                float fValue;
                if (float.TryParse((string)value, out fValue))
                {
                    try
                    {
                        value = ((int)fValue).ToString();
                    }
                    catch
                    {
                        CBase.LogError("转型错误...{0}", value.ToString());
                    }

                }
            }

            field.SetValue(newT, value);
        }

    }

    public static CBaseInfo LoadFromTab(Type type, ICTabReadble tabFile, int row)
    {
        CBaseInfo newT = Activator.CreateInstance(type) as CBaseInfo;
        LoadFromTab(type, ref newT, tabFile, row);
        return newT;
    }
}