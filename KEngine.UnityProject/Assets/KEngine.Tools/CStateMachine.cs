#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CStateMachine.cs
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
using System.Collections.Generic;
using KEngine;

public abstract class CState<T>
{
    public T ToState;

    public CState(T toState)
    {
        ToState = toState;
    }

    public abstract void OnInit();
    public abstract void OnEnter();
    public abstract void OnExit();
    public abstract void OnBreathe();
}

/// <summary>
/// 有限狀態機, TODO: 未 update (breath)
/// </summary>
public class CStateMachine<OBJ, STATE>
{
    private OBJ Object_;
    private STATE CurState;
    private STATE LastState;

    private bool stateChangedFlag = false;
    private Dictionary<STATE, CState<STATE>> StatesHandlers = new Dictionary<STATE, CState<STATE>>();

    public CStateMachine(OBJ obj, STATE initState, CState<STATE>[] stateMap)
    {
        Object_ = obj;
        LastState = initState;
        CurState = initState;

        Array statesArray = Enum.GetValues(typeof (STATE));
        Debuger.Assert(statesArray.Length == stateMap.Length);

        Debuger.Assert(Object_);

        foreach (CState<STATE> state in stateMap)
        {
            StatesHandlers[state.ToState] = state;
            state.OnInit();
        }
    }

    private void Update()
    {
        if (stateChangedFlag)
        {
            StatesHandlers[LastState].OnExit();

            StatesHandlers[CurState].OnEnter();
            stateChangedFlag = false;
        }

        StatesHandlers[CurState].OnBreathe();
    }

    public void SetState(STATE state)
    {
        LastState = CurState;
        CurState = state;
        stateChangedFlag = true;
    }
}