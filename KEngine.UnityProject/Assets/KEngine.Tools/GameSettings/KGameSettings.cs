#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KGameSettings.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KEngine.CoreModules
{
    [Obsolete("Use SettingModule instead!")]
    public class KGameSettings : IModuleInitable
    {
        private class _InstanceClass
        {
            public static KGameSettings _Instance = new KGameSettings();
        }

        public static KGameSettings Instance
        {
            get { return _InstanceClass._Instance; }
        }

        public Dictionary<Type, Dictionary<string, CBaseInfo>> SettingInfos =
            new Dictionary<Type, Dictionary<string, CBaseInfo>>();

        public Dictionary<Type, Func<string>[]> LazyLoad = new Dictionary<Type, Func<string>[]>();

        public Action InitAction; // Init時調用的委託、函數指針

        public static event Action<Type, int, string> FoundDuplicatedIdEvent;


        public IEnumerator Init()
        {
            if (this.InitAction == null)
                Log.LogError("GameSettings沒有定義初始化行為！！！");
            else
                InitAction();
            yield return new WaitForEndOfFrame();
        }

        public double InitProgress { get; private set; }

        public IEnumerator UnInit()
        {
            yield break;
        }

        public void LazyLoadTab(Type type, params Func<string>[] getTabContentFuncs)
        {
            Debuger.Assert(typeof(CBaseInfo).IsAssignableFrom(type));

            LazyLoad[type] = getTabContentFuncs;
        }

        public void LoadTab(Type type, params string[] contents)
        {
            Debuger.Assert(typeof(CBaseInfo).IsAssignableFrom(type));
            DoLoadTab(type, contents);
        }

        public void LoadTab<T>(params string[] contents) where T : CBaseInfo
        {
            LoadTab(typeof (T), contents);
        }

        /// <summary>
        /// 确保读取完
        /// </summary>
        /// <param name="type"></param>
        private void EnsureLoad(Type type)
        {
            Debuger.Assert(typeof(CBaseInfo).IsAssignableFrom(type));

            Func<string>[] getContentFuncs;
            if (LazyLoad.TryGetValue(type, out getContentFuncs))
            {
                foreach (var func in getContentFuncs)
                    DoLoadTab(type, new[] {func()});

                LazyLoad.Remove(type);
            }
        }

        private void EnsureLoad<T>() where T : CBaseInfo
        {
            Type type = typeof (T);
            EnsureLoad(type);
        }

        /// <summary>
        /// 外部人工手动读取
        /// </summary>
        /// <param name="type"></param>
        /// <param name="contents"></param>
        public void ForceLoadTab(Type type, params string[] contents)
        {
            DoLoadTab(type, contents);
        }

        /// <summary>
        /// 真正进行读取
        /// </summary>
        /// <param name="type"></param>
        /// <param name="contents"></param>
        private void DoLoadTab(Type type, IEnumerable<string> contents)
        {
            Debuger.Assert(typeof(CBaseInfo).IsAssignableFrom(type));
            foreach (string content in contents)
            {
                using (KTabFile tabFile = KTabFile.LoadFromString(content))
                {
                    Dictionary<string, CBaseInfo> dict;
                    if (!SettingInfos.TryGetValue(type, out dict)) // 如果没有才添加
                        dict = new Dictionary<string, CBaseInfo>();

                    const int rowStart = 1;
                    for (int row = rowStart; row <= tabFile.GetHeight(); row++)
                    {
                        // 先读取ID， 获取是否之前已经读取过配置，
                        // 如果已经读取过，那么获取原配置对象，并重新赋值 (因为游戏中其它地方已经存在它的引用了，直接替换内存泄露)
                        string id = tabFile.GetString(row, "Id"); // 获取ID是否存在, 如果已经存在，那么替换其属性，不new一个
                        CBaseInfo existOne;
                        if (dict.TryGetValue(id, out existOne))
                        {
                            if (FoundDuplicatedIdEvent != null)
                                FoundDuplicatedIdEvent(type, row, id);

                            CBaseInfo existInfo = existOne;
                            existInfo.ReadFromTab(type, ref existInfo, tabFile, row); // 修改原对象，不new
                            existInfo.CustomReadLine(tabFile, row);
                            (existInfo as CBaseInfo).Parse();
                        }
                        else
                        {
                            CBaseInfo pInfo = CBaseInfo.NewFromTab(type, tabFile, row);
                            pInfo.CustomReadLine(tabFile, row);
                            pInfo.Parse();
                            dict[pInfo.Id] = pInfo; // 不存在，直接new
                        }
                    }

                    SettingInfos[type] = dict;
                }
            }
        }

        private void DoLoadTab<T>(string[] contents) where T : CBaseInfo
        {
            DoLoadTab(typeof (T), contents);
        }

        public List<T> GetInfos<T>(Func<T, bool> filter = null) where T : CBaseInfo
        {
            EnsureLoad<T>();

            Dictionary<string, CBaseInfo> dict;
            if (SettingInfos.TryGetValue(typeof (T), out dict))
            {
                //Log.Info(dict.Count+"");
                List<T> list = new List<T>();
                foreach (CBaseInfo item in dict.Values)
                {
                    var getItem = (T) item;
                    if (filter == null || filter(getItem))
                        list.Add(getItem);
                }
                return list;
            }
            else
                Log.LogError("找不到类型配置{0}, 总类型数{1}", typeof (T).Name, SettingInfos.Count);

            return null;
        }

        public T GetInfo<T>(string id, bool printLog = true) where T : CBaseInfo
        {
            EnsureLoad<T>();

            Dictionary<string, CBaseInfo> dict;
            if (SettingInfos.TryGetValue(typeof (T), out dict))
            {
                CBaseInfo tabInfo;
                if (dict.TryGetValue(id, out tabInfo))
                {
                    return (T) tabInfo;
                }
                else
                {
                    if (printLog)
                        Log.LogError("找不到类型{0} Id为{1}的配置对象, 类型表里共有对象{2}", typeof (T).Name, id, dict.Count);
                }
            }
            else
            {
                if (printLog)
                    Log.LogError("嘗試Id {0}, 找不到类型配置{1}, 总类型数{2}", id, typeof (T).Name, SettingInfos.Count);
            }

            return null;
        }

        // 数字ID是索引
        public T GetInfo<T>(int id, bool printLog = true) where T : CBaseInfo
        {
            return GetInfo<T>(id.ToString(), printLog);
        }
    }

    public partial class CBaseInfo : ICloneable
    {
        // 把Fields缓存起来，避开反射的GetFields性能问题  Type => ( FieldName -> Type )
        private static readonly Dictionary<Type, LinkedList<FieldInfo>> CacheTypeFields =
            new Dictionary<Type, LinkedList<FieldInfo>>();

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
                {
                    Log.LogError("錯誤解析Int Id - {0}", GetType());
                }

                _CacheIntId = tryInt;

                return _CacheIntId.Value;
            }
        }

        /// <summary>
        /// 清理一些缓存起来的运算配置
        /// </summary>
        public virtual void ClearCache()
        {
            _CacheIntId = null;
        }

        /// <summary>
        /// 某些值的特殊解析
        /// </summary>
        public virtual void Parse()
        {
        }

        /// <summary>
        /// 可自定义对表进行附加解释.... 在Parse执行前...
        /// tabFile后边会被释放掉
        /// </summary>
        /// <param name="tabFile"></param>
        public virtual void CustomReadLine(IKTabReadble tabFile, int row)
        {
        }

        public void ReadFromTab(Type type, ref CBaseInfo newT, IKTabReadble tabFile, int row)
        {
            if (Debug.isDebugBuild)
                Debuger.Assert(typeof(CBaseInfo).IsAssignableFrom(type));

            // 缓存字段Field, 每个Type只反射一次！
            LinkedList<FieldInfo> okFields;
            if (!CacheTypeFields.TryGetValue(type, out okFields))
            {
                okFields = CacheTypeFields[type] = new LinkedList<FieldInfo>();
                var allFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (FieldInfo field in allFields)
                {
                    if (field.Name.StartsWith("_") || field.IsInitOnly) // 筛掉_ 和 readonly
                        continue;
                    if (!tabFile.HasColumn(field.Name))
                    {
                        if (Debug.isDebugBuild)
                            Log.LogError("表{0} 找不到表头{1}", type.Name, field.Name);
                        continue;
                    }
                    okFields.AddLast(field);
                }
            }

            // 读字段
            foreach (var field in okFields)
            {
                var fieldName = field.Name;
                var fieldType = field.FieldType;

                object value;
                if (fieldType == typeof (int))
                {
                    value = tabFile.GetInteger(row, fieldName);
                }
                else if (fieldType == typeof (long))
                {
                    value = (long) tabFile.GetInteger(row, fieldName);
                }
                else if (fieldType == typeof (string))
                {
                    value = tabFile.GetString(row, fieldName).Replace("\\n", "\n");
                }
                else if (fieldType == typeof (float))
                {
                    value = tabFile.GetFloat(row, fieldName);
                }
                else if (fieldType == typeof (bool))
                {
                    value = tabFile.GetBool(row, fieldName);
                }
                else if (fieldType == typeof (double))
                {
                    value = tabFile.GetDouble(row, fieldName);
                }
                else if (fieldType == typeof (uint))
                {
                    value = tabFile.GetUInteger(row, fieldName);
                }
                else if (fieldType == typeof (Regex))
                {
                    var str = tabFile.GetString(row, fieldName);
                    value = string.IsNullOrEmpty(str) ? null : new Regex(str);
                }
                else if (fieldType == typeof (List<string>))
                {
                    string sz = tabFile.GetString(row, fieldName);
                    value = KTool.Split<string>(sz, '|');
                }
                else if (fieldType == typeof (List<int>))
                {
                    //List<int> retInt = new List<int>();
                    string szArr = tabFile.GetString(row, fieldName);
                    value = KTool.Split<int>(szArr, '|');
                    //if (!string.IsNullOrEmpty(szArr))
                    //{
                    //    string[] szIntArr = szArr.Split('|');
                    //    foreach (string szInt in szIntArr)
                    //    {
                    //        float parseFloat;
                    //        float.TryParse(szInt, out parseFloat);
                    //        int parseInt_ = (int)parseFloat;
                    //        retInt.Add(parseInt_);
                    //    }

                    //}
                    //else
                    //    value = new List<int>();
                }
                else if (fieldType == typeof (List<List<string>>))
                {
                    string sz = tabFile.GetString(row, fieldName);
                    if (!string.IsNullOrEmpty(sz))
                    {
                        var szOneList = new List<List<string>>();
                        string[] szArr = sz.Split('|');
                        foreach (string szOne in szArr)
                        {
                            string[] szOneArr = szOne.Split('-', ':');
                            szOneList.Add(new List<string>(szOneArr));
                        }
                        value = szOneList;
                    }
                    else
                        value = new List<List<string>>();
                }
                else if (fieldType == typeof (List<List<int>>))
                {
                    string sz = tabFile.GetString(row, fieldName);
                    if (!string.IsNullOrEmpty(sz))
                    {
                        var zsOneIntList = new List<List<int>>();
                        string[] szArr = sz.Split('|');
                        foreach (string szOne in szArr)
                        {
                            List<int> retInts = new List<int>();
                            string[] szOneArr = szOne.Split('-', ':');
                            foreach (string szOneInt in szOneArr)
                            {
                                float parseFloat;
                                float.TryParse(szOneInt, out parseFloat);
                                int parseInt_ = (int) parseFloat;
                                retInts.Add(parseInt_);
                            }
                            zsOneIntList.Add(retInts);
                        }
                        value = zsOneIntList;
                    }
                    else
                        value = new List<List<int>>();
                }
                //else if (fieldType == typeof (JsonObject))
                //{
                //    string sz = tabFile.GetString(row, fieldName);
                //    value = string.IsNullOrEmpty(sz) ? new JsonObject() : KTool.SplitToJson(sz);
                //}
                else
                {
                    Log.LogWarning("未知类型: {0}", fieldName);
                    value = null;
                }

                if (fieldName == "Id") // 如果是Id主键，确保数字成整数！
                {
                    int fValue;
                    if (int.TryParse((string) value, out fValue))
                    {
                        try
                        {
                            value = fValue.ToString();
                        }
                        catch
                        {
                            Log.LogError("转型错误...{0}", value.ToString());
                        }
                    }
                }

                field.SetValue(newT, value);
            }
        }

        public static CBaseInfo NewFromTab(Type type, IKTabReadble tabFile, int row)
        {
            var newT = Activator.CreateInstance(type) as CBaseInfo;
            newT.ReadFromTab(type, ref newT, tabFile, row);
            return newT;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}", GetType(), Id);
        }
    }
}