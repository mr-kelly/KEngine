using System;
using CosmosEngine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
        return CCosmosEngine.EngineInstance.StartCoroutine((IEnumerator)CoWaitCallback(func));
    }

    private static IEnumerator CoWaitCallback(WaitCallbackDelegate func)
    {
        bool wait = true;
        func(() =>
        {
            wait = false;
        });
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
        CCosmosEngine.EngineInstance.StartCoroutine(CoWaitTrue(waits, okCallback));
    }

    public static void Wait(IWaitable wait, Action okCallback)
    {
        CCosmosEngine.EngineInstance.StartCoroutine(CoWaitTrue(new[] { wait }, okCallback));
    }

    static IEnumerator CoWaitTrue(IEnumerable<IWaitable> waits, Action okCallback)
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
        return CCosmosEngine.EngineInstance.StartCoroutine((IEnumerator)CoTimeCallback(time, callback));
    }

    private static IEnumerator CoTimeCallback(float time, Action callback)
    {
        yield return new WaitForSeconds(time);
        callback();
    }
}
