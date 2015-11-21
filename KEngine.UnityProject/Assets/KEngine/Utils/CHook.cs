//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Version 0.9.1 (20151010)
//                     Copyright © 2011-2015
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 钩子系统，类似EventManager事件驱动编程，本质也是Observer观察者模式的一种
// 仿开源系统Wordpress, 叫钩子 
public class CHook
{
	public delegate bool HookDelegate(object[] args);

	static Dictionary<string, HookDelegate> HookActions = new Dictionary<string, HookDelegate>();

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