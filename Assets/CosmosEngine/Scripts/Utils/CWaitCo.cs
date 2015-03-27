using System;
using CosmosEngine;
using UnityEngine;
using System.Collections;

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
        return CCosmosEngine.EngineInstance.StartCoroutine((IEnumerator) CoWaitCallback(func));
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


    // 需要StartCoroutine

    public static Coroutine TimeCallback(float time, Action callback)
    {
        return CCosmosEngine.EngineInstance.StartCoroutine((IEnumerator) CoTimeCallback(time, callback));
    }

    private static IEnumerator CoTimeCallback(float time, Action callback)
    {
        yield return new WaitForSeconds(time);
        callback();
    }
}
