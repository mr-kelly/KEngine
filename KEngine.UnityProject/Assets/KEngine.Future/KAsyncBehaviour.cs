#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KAsyncBehaviour.cs
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

/// <summary>
/// 带有异步处理功能的组件
/// 有一些组件，如果武器、英雄球、UI窗等，它们涉及资源的异步加载.
/// 要在加载完成后，才能做某些事情
/// 用AsyncCall(()=>xxx)
/// 注意资源加载完，要把CanAsyncCall标记到true!
/// </summary>
public abstract class KAsyncBehaviour : KBehaviour
{
    private List<System.Action> _callbacks;

    private bool _firstTouchCanAsyncCalled = false; // 第一次设置CanAsyncCall时，开启超时检查

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
                _callbacks.Clear();
            }

            if (Debug.isDebugBuild)
            {
                if (!_firstTouchCanAsyncCalled)
                {
                    _firstTouchCanAsyncCalled = true;
                    DoCheckAsyncCall();
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

    private void DoCheckAsyncCall()
    {
        // 调试模式下，防止永远没设置AsyncCall的程序bug
        KEngine.AppEngine.EngineInstance.StartCoroutine(DebuggerForCheckAsyncCall());
    }

    private IEnumerator DebuggerForCheckAsyncCall()
    {
        if (CanAsyncCall)
            yield break;

        if (!CanAsyncCall)
        {
            yield return new WaitForSeconds(20f); // 20秒检测
            if (!CanAsyncCall)
            {
                Debug.LogError(
                    string.Format("[KAsyncBehaviour]超过10秒，组件还是不能CanAsyncCall!是否程序有错？ {0}", this.gameObject.name),
                    gameObject);
            }
        }
    }


    /// <summary>
    ///  多个等待并callback~
    /// </summary>
    /// <param name="ayncs"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static Coroutine AsyncCalls(IEnumerable<KAsyncBehaviour> ayncs, Action callback)
    {
        return KEngine.AppEngine.EngineInstance.StartCoroutine(CoAsyncCalls(ayncs, callback));
    }

    public static IEnumerator CoAsyncCalls(IEnumerable<KAsyncBehaviour> asyncs, Action callback)
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