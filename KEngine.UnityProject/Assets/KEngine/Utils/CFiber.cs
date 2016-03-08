#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CFiber.cs
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

using System.Collections;
using System.Collections.Generic;
using KEngine;
using UnityEngine;

public class CFiber : MonoBehaviour
{
    private static CFiber _Instance;

    public static CFiber Instance
    {
        get
        {
            if (_Instance == null)
            {
                var f = new GameObject("CFiber").AddComponent<CFiber>();
                GameObject.DontDestroyOnLoad(f.gameObject);
            }

            return _Instance;
        }
    }

    private Queue<CCoroutineWrapper> AddQueue = new Queue<CCoroutineWrapper>();
    private Queue<CCoroutineWrapper> DeleteQueue = new Queue<CCoroutineWrapper>();

    private Dictionary<int, CCoroutineWrapper> Coroutines = new Dictionary<int, CCoroutineWrapper>();
    private static int IdGen = 0;

    private int UpdateCount = 0;
    public float TimeScale = 1; // 越小越慢, 越大越快

    private void Awake()
    {
        _Instance = this;
    }

    public CCoroutineWrapper _NewCoroutine(IEnumerator coFunc)
    {
        var co = new CCoroutineWrapper() {CoroutineId = IdGen, CoroutineFunc = coFunc};
        IdGen++;

        co.UpdateMove(); // 開始立刻移動一下
        AddQueue.Enqueue(co);

        return co;
    }

    public int PlayCoroutine(IEnumerator coFunc)
    {
        return _NewCoroutine(coFunc).CoroutineId;
    }

    private void Update()
    {
        UpdateCount++;

        int exeCount = 1;
        int interval = 0;

        Debuger.Assert(TimeScale > 0);

        if (TimeScale > 1)
            exeCount = Mathf.RoundToInt(TimeScale);
        else if (TimeScale < 1)
            interval = Mathf.RoundToInt(1f/TimeScale);

        if (interval == 0 || UpdateCount%interval == 0)
        {
            for (int count = 0; count < exeCount; count++)
            {
                while (AddQueue.Count > 0)
                {
                    var d = AddQueue.Dequeue();
                    Coroutines.Add(d.CoroutineId, d);
                }
                foreach (KeyValuePair<int, CCoroutineWrapper> kv in Coroutines)
                {
                    if (kv.Value.Suspend)
                    {
                        continue;
                    }

                    kv.Value.UpdateMove();
                }

                while (DeleteQueue.Count > 0)
                {
                    var d = DeleteQueue.Dequeue();
                    Coroutines.Remove(d.CoroutineId);
                }
            }
        }
    }

    private IEnumerator _HandleUnityCoroutine(CCoroutineWrapper coWrapper, Coroutine co)
    {
        coWrapper.Suspend = true;
        yield return co;
        coWrapper.Suspend = false;
    }

    private IEnumerator _HandleCustomRoutine(int coId, CCoroutineWrapper coWrapper, CFiberBase wait)
    {
        coWrapper.Suspend = true;
        _NewCoroutine(wait.DoRun()); // var co = 
        while (!wait.IsFinish)
            yield return null;
        coWrapper.Suspend = false;
    }

    /// for instance: Unity's WaitForSeconds...WaitForFixedFrame... so on....
    private IEnumerator _HandleUnityYieldInstruction(int coId, CCoroutineWrapper coWrapper, YieldInstruction wait)
    {
        coWrapper.Suspend = true; // Suspend coroutine, wait for seconds push it back
        yield return wait;
        coWrapper.Suspend = false;
    }

    public bool PauseCoroutine(int coId)
    {
        return true;
    }

    public bool ResumeCoroutine(int coId)
    {
        return true;
    }

    public bool KillCoroutine(int coId)
    {
        Coroutines[coId].Suspend = true;
        DeleteQueue.Enqueue(Coroutines[coId]);
        return true;
    }

    public class CCoroutineWrapper
    {
        public bool Suspend = false;
        public int CoroutineId;
        public IEnumerator CoroutineFunc;

        public void UpdateMove()
        {
            if (CoroutineFunc.MoveNext())
            {
                object yieldVal = CoroutineFunc.Current;
                if (yieldVal is YieldInstruction) // Unity Coroutine
                {
                    CFiber.Instance.StartCoroutine(CFiber.Instance._HandleUnityYieldInstruction(CoroutineId, this,
                        yieldVal as YieldInstruction));
                }
                if (yieldVal is Coroutine) // StartCoroutine(xxx)
                {
                    CFiber.Instance.StartCoroutine(CFiber.Instance._HandleUnityCoroutine(this, yieldVal as Coroutine));
                }
                else if (yieldVal is CFiberBase)
                {
                    CFiberBase customRoutine = yieldVal as CFiberBase;
                    CFiber.Instance.PlayCoroutine(CFiber.Instance._HandleCustomRoutine(CoroutineId, this, customRoutine));
                } // else do nothing

                // TODO: WWW, WaitForxxx, execute once at create  
            }
            else
            {
                CFiber.Instance.DeleteQueue.Enqueue(this);
            }
        }
    }
}

/// <summary>
/// use for custom a croutine for CRoutine
/// </summary>
public abstract class CFiberBase
{
    public bool IsFinish = false;

    public IEnumerator DoRun()
    {
        IsFinish = false;

        IEnumerator runEnum = Wait();
        while (runEnum.MoveNext())
            yield return runEnum.Current;

        IsFinish = true;
    }

    public abstract IEnumerator Wait(); // cannot execute yield return StartCoroutine(xxx) in it

    protected Coroutine StartCoroutine(IEnumerator coFunc)
    {
        return CFiber.Instance.StartCoroutine(coFunc);
    }
}