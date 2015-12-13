#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CWaitCo.cs
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
using UnityEngine;

public interface IWaitable
{
    bool ShouldWait { get; }
}

public delegate void CWaitCoBreak();

public delegate void WaitCallbackDelegate(CWaitCoBreak breakCo);

/// <summary>
/// C Wait Coroutine...
/// 一个用异步匿名函数来处理协程的工具。。。减少繁杂的代码
/// </summary>
public class CWaitCo
{
    public static Coroutine WaitCallback(WaitCallbackDelegate func)
    {
        return KEngine.AppEngine.EngineInstance.StartCoroutine((IEnumerator) CoWaitCallback(func));
    }

    private static IEnumerator CoWaitCallback(WaitCallbackDelegate func)
    {
        bool wait = true;
        func(() => { wait = false; });
        // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
        while (wait)
        {
            yield return null;
        }
    }

    public static Action After(int count, Action callback)
    {
        var callTimes = 0;
        return () =>
        {
            callTimes++;
            if (callTimes >= count)
            {
                callback();
            }
        };
    }

    /// <summary>
    /// 等待回调返回true，再执行第二个回调
    /// </summary>
    /// <param name="waits"></param>
    /// <param name="okCallback"></param>
    public static void Wait(IEnumerable<IWaitable> waits, Action okCallback)
    {
        KEngine.AppEngine.EngineInstance.StartCoroutine(CoWaitTrue(waits, okCallback));
    }

    public static void Wait(IWaitable wait, Action okCallback)
    {
        KEngine.AppEngine.EngineInstance.StartCoroutine(CoWaitTrue(new[] {wait}, okCallback));
    }

    private static IEnumerator CoWaitTrue(IEnumerable<IWaitable> waits, Action okCallback)
    {
        while (true)
        {
            var bHasWait = false;
            foreach (var wait in waits)
            {
                if (wait.ShouldWait)
                {
                    bHasWait = true;
                    break;
                }
            }
            if (!bHasWait)
                break;

            yield return null;
        }

        okCallback();
    }

    // 需要StartCoroutine

    public static Coroutine TimeCallback(float time, Action callback)
    {
        return KEngine.AppEngine.EngineInstance.StartCoroutine((IEnumerator) CoTimeCallback(time, callback));
    }

    private static IEnumerator CoTimeCallback(float time, Action callback)
    {
        yield return new WaitForSeconds(time);
        callback();
    }
}