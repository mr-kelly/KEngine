#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KActionRecord.cs
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
using KEngine;
using UnityEngine;

/// <summary>
/// 客户端版的行为记录仪, 类似服务器的records.js
/// 本质是一个事件驱动管理器, 但是拥有传入计数参数功能
/// 为了性能，5秒保存一次(或特殊情况)
/// </summary>
public class KActionRecords : KBehaviour
{
    private static readonly KPrefs Prefs = new KPrefs(123456789);

    private bool _isDirty = false;

    private static KActionRecords _instance;

    protected static KActionRecords Instance
    {
        get
        {
            if (_instance == null)
            {
                if (IsApplicationQuited || !Application.isPlaying)
                {
                    Log.LogErrorWithStack("[错误埋点]Error Instance Action Recods!  请查看堆栈, 尽快修复！！！");
                    return null;
                }
                _instance = (new GameObject("__ActionRecorder__")).AddComponent<KActionRecords>();
                DontDestroyOnLoad(_instance.CachedGameObject);
                _instance.Init();
            }
            return _instance;
        }
    }

    /// <summary>
    /// 回调返回的参数
    /// </summary>
    public class ActionRecordArg
    {
        public Enum Type;
        public string SubType;
        //public object ExtraArg; // 附加的函数参数
        public int RecordCount; // 用于Mark和Count事件，计数
    }

    private Dictionary<string, int> _records;

    private readonly Dictionary<string, HashSet<GenCoroutineDelegate>> _actionRecordCoroutine =
        new Dictionary<string, HashSet<GenCoroutineDelegate>>(); // 委托不会重复！

    private readonly Dictionary<string, HashSet<WaitCallbackDelegate>> _actionRecordWaitCallback =
        new Dictionary<string, HashSet<WaitCallbackDelegate>>(); // 委托不会重复！

    public delegate IEnumerator GenCoroutineDelegate(Enum type, string subType, int count);

    public delegate void WaitCallbackDelegate(Enum type, string subType, int count, Action next);

    public static Coroutine WaitCallback(Enum type, string subType, int count, WaitCallbackDelegate func)
    {
        return KEngine.AppEngine.EngineInstance.StartCoroutine(CoWaitCallback(type, subType, count, func));
    }

    private static IEnumerator CoWaitCallback(Enum type, string subType, int count, WaitCallbackDelegate func)
    {
        bool wait = true;
        func(type, subType, count, () => { wait = false; });
        // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
        while (wait)
        {
            yield return null;
        }
    }

    private void Init()
    {
        var str = Prefs.GetKey("ActionRecords") ?? "";
        _records = KTool.SplitToDict<string, int>(str);
    }

    private void Save()
    {
        Prefs.SetKey("ActionRecords", KTool.DictToSplitStr(_records));
        _isDirty = false;
    }

    public static void Reset()
    {
        Prefs.SetKey("ActionRecords", "");
    }

    private void Update()
    {
        if (_isDirty && Time.frameCount%500 == 0) // 每300帧保存一次
        {
            Save();
        }
    }

    #region Event/Callback Add or Remove

    public static void UnBind(Enum type, string subType, WaitCallbackDelegate waitCallback)
    {
        var key = MakeKey(type, subType);
        HashSet<WaitCallbackDelegate> emGentors;
        if (!Instance._actionRecordWaitCallback.TryGetValue(key, out emGentors))
        {
            emGentors = Instance._actionRecordWaitCallback[key] = new HashSet<WaitCallbackDelegate>();
        }
        emGentors.Remove(waitCallback);
    }

    public static void AddListener(Enum type, WaitCallbackDelegate waitCallback)
    {
        AddListener(type, null, waitCallback);
    }

    public static void AddListener(Enum type, string subType, WaitCallbackDelegate waitCallback)
    {
        if (waitCallback == null)
        {
            Log.Error("[AddListener]waitCallback Cannnot null!");
            return;
        }
        var key = MakeKey(type, subType);
        HashSet<WaitCallbackDelegate> emGentors;
        if (!Instance._actionRecordWaitCallback.TryGetValue(key, out emGentors))
        {
            emGentors = Instance._actionRecordWaitCallback[key] = new HashSet<WaitCallbackDelegate>();
        }
        emGentors.Add(waitCallback);
    }

    public static void AddListener(Enum type, GenCoroutineDelegate emGentor)
    {
        AddListener(type, null, emGentor);
    }

    public static void AddListener(Enum type, string subType, GenCoroutineDelegate emGentor)
    {
        if (emGentor == null)
        {
            Log.Error("[AddListener]emGentor Cannnot null!");
            return;
        }
        var key = MakeKey(type, subType);
        HashSet<GenCoroutineDelegate> emGentors;
        if (!Instance._actionRecordCoroutine.TryGetValue(key, out emGentors))
        {
            emGentors = Instance._actionRecordCoroutine[key] = new HashSet<GenCoroutineDelegate>();
        }
        emGentors.Add(emGentor);
    }

    public static void UnBind(Enum type, string subType, GenCoroutineDelegate emGentor)
    {
        var key = MakeKey(type, subType);
        HashSet<GenCoroutineDelegate> emGentors;
        if (!Instance._actionRecordCoroutine.TryGetValue(key, out emGentors))
        {
            emGentors = Instance._actionRecordCoroutine[key] = new HashSet<GenCoroutineDelegate>();
        }
        emGentors.Remove(emGentor);
    }

    #endregion

    /// <summary>
    /// 触发事件，不记录
    /// </summary>
    /// <param name="type"></param>
    /// <param name="subType"></param>
    /// <param name="extraArg"></param>
    /// <returns></returns>
    public static Coroutine Event(Enum type, string subType, int extraArg = -1)
    {
        return IsApplicationQuited ? null : Instance.StartCoroutine(CoTriggerEventFuncs(type, subType, extraArg));
    }

    public static Coroutine Event(Enum type, int extraArg = -1)
    {
        var subType = extraArg == -1 ? null : extraArg.ToString();
        return Event(type, subType, extraArg);
    }

    private static string MakeKey(Enum type, string subType)
    {
        var nType = type.ToString();
        if (!string.IsNullOrEmpty(subType))
            return string.Format("{0}-{1}", nType, subType);
        return nType;
    }


    /// <summary>
    /// 增加次数
    /// </summary>
    /// <param name="type"></param>
    /// <param name="subType"></param>
    /// <param name="addCount"></param>
    /// <returns></returns>
    public static Coroutine AddCount(Enum type, string subType, int addCount)
    {
        int lastCount;
        var key = MakeKey(type, subType);
        if (Instance._records.TryGetValue(key, out lastCount))
        {
            Instance._records[key] = lastCount + addCount;
        }
        else
        {
            Instance._records[key] = addCount;
        }

        Instance._isDirty = true;

        return Instance.StartCoroutine(CoTriggerEventFuncs(type, subType, Instance._records[key]));
    }

    public static Coroutine AddCount(Enum type, int addCount)
    {
        return AddCount(type, null, addCount);
    }

    /// <summary>
    /// 标记1次
    /// </summary>
    /// <param name="type"></param>
    /// <param name="subType"></param>
    /// <returns></returns>
    public static Coroutine Mark(Enum type, string subType = null)
    {
        int count;
        var key = MakeKey(type, subType);
        if (Instance._records.TryGetValue(key, out count))
        {
            Instance._records[key] = count + 1;
        }
        else
        {
            Instance._records[key] = 1;
        }

        Instance._isDirty = true;

        return Instance.StartCoroutine(CoTriggerEventFuncs(type, subType, Instance._records[key]));
    }

    private static IEnumerator CoTriggerEventFuncs(Enum type, string subType, int count)
    {
        var key = MakeKey(type, subType);

        HashSet<GenCoroutineDelegate> emGentors;
        if (!Instance._actionRecordCoroutine.TryGetValue(key, out emGentors))
        {
            emGentors = Instance._actionRecordCoroutine[key] = new HashSet<GenCoroutineDelegate>();
        }

        foreach (var emGentor in new HashSet<GenCoroutineDelegate>(emGentors)) // TODO: Clone不好吧
        {
            var enumtor = emGentor(type, subType, count);
            if (enumtor != null)
                yield return Instance.StartCoroutine(enumtor);
        }

        HashSet<WaitCallbackDelegate> emCallbacks;
        if (!Instance._actionRecordWaitCallback.TryGetValue(key, out emCallbacks))
        {
            emCallbacks = Instance._actionRecordWaitCallback[key] = new HashSet<WaitCallbackDelegate>();
        }
        foreach (var callback in new HashSet<WaitCallbackDelegate>(emCallbacks))
            yield return WaitCallback(type, subType, count, callback);
    }

    /// <summary>
    /// 获取指定行为的发生次数
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static int GetCount(Enum type, string subType = null)
    {
        int count;
        var key = MakeKey(type, subType);
        if (Instance._records.TryGetValue(key, out count))
        {
            return count;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取委托函数数量
    /// </summary>
    /// <param name="type"></param>
    /// <param name="subType"></param>
    /// <returns></returns>
    public static int GetDelegateCount(Enum type, string subType = null)
    {
        var key = MakeKey(type, subType);
        int count = 0;

        HashSet<GenCoroutineDelegate> emGentors;
        if (Instance._actionRecordCoroutine.TryGetValue(key, out emGentors))
        {
            count += emGentors.Count;
        }
        HashSet<WaitCallbackDelegate> emCallbacks;
        if (Instance._actionRecordWaitCallback.TryGetValue(key, out emCallbacks))
        {
            count += emCallbacks.Count;
        }

        return count;
    }
}