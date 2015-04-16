using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CosmosEngine;
/// <summary>
/// 带有异步处理功能的组件
/// 
/// 
/// 有一些组件，如果武器、英雄球、UI窗等，它们涉及资源的异步加载.
/// 要在加载完成后，才能做某些事情
/// 
/// 用AsyncCall(()=>xxx)
/// 
/// 注意资源加载完，要把CanAsyncCall标记到true!
/// </summary>
public abstract class CAsyncBehaviour : CBehaviour
{
    private List<System.Action> _callbacks;

    private bool _canAsyncCall = false;
    public virtual bool CanAsyncCall
    {
        get { return _canAsyncCall; }
        set
        {
            _canAsyncCall = value;
            if (_canAsyncCall && _callbacks != null)
            {
                foreach (var call in _callbacks)
                {
                    call();
                }
            }
        }
    }

    /// <summary>
    /// 异步调用，等待完成
    /// </summary>
    /// <param name="call"></param>
    public void AsyncCall(Action call)
    {
        if (call == null)
            return;

        if (!CanAsyncCall)
        {
            if (_callbacks == null)
                _callbacks = new List<Action>();
            _callbacks.Add(call);
        }
        else
        {
            call();
        }

    }

    protected virtual void Awake()
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            // 调试模式下，防止永远没设置AsyncCall的程序bug
            CCosmosEngine.EngineInstance.StartCoroutine(DebuggerForCheckAsyncCall());
        }
    }
    

    IEnumerator DebuggerForCheckAsyncCall()
    {
        if (CanAsyncCall)
            yield break;

        if (!CanAsyncCall)
        {
            yield return new WaitForSeconds(10f); // 10秒检测
            if (!CanAsyncCall)
            {
                CDebug.LogError("[CAsyncBehaviour]超过10秒，组件还是不能CanAsyncCall!是否程序有错？");
            }
        }
    }


    /// <summary>
    ///  多个等待并callback~
    /// </summary>
    /// <param name="ayncs"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static Coroutine AsyncCalls(IEnumerable<CAsyncBehaviour> ayncs, Action callback)
    {
        return CCosmosEngine.EngineInstance.StartCoroutine(CoAsyncCalls(ayncs, callback));
    }

    public static IEnumerator CoAsyncCalls(IEnumerable<CAsyncBehaviour> asyncs, Action callback)
    {
        while (true)
        {
            var anyCannot = false;
            foreach (var a in asyncs)
            {
                if (a != null && !a.CanAsyncCall)
                {
                    anyCannot = true;
                }
            }
            if (!anyCannot)
                break;

            yield return null;
        }

        callback();
    }

}
