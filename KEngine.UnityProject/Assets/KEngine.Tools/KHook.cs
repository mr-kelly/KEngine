#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KHook.cs
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

using System.Collections.Generic;

// 钩子系统，类似EventManager事件驱动编程，本质也是Observer观察者模式的一种
// 仿开源系统Wordpress, 叫钩子 
public class KHook
{
    public delegate bool HookDelegate(object[] args);

    private static Dictionary<string, HookDelegate> HookActions = new Dictionary<string, HookDelegate>();

    public static void AddHook(string hookName, HookDelegate hookFunc)
    {
        HookDelegate _delegate;
        if (!HookActions.TryGetValue(hookName, out _delegate))
        {
            HookActions[hookName] = null;
        }

        HookActions[hookName] += hookFunc;
    }

    public static void RemoveHook(string hookName, HookDelegate hookFunc)
    {
        HookActions[hookName] -= hookFunc;
    }

    public static bool DoHook(string hookName, params object[] args)
    {
        HookDelegate _delegate;
        if (HookActions.TryGetValue(hookName, out _delegate))
        {
            return _delegate(args);
        }

        return false;
    }
}