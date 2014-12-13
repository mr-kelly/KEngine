//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                     Version 0.8 (20140904)
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Extension Unity's function, to be more convinient
/// </summary>
public static class CExtensions
{
	public static void SetPositionX(this Transform t, float newX)
	{
		t.position = new Vector3(newX, t.position.y, t.position.z);
	}

	public static void SetPositionY(this Transform t, float newY)
	{
		t.position = new Vector3(t.position.x, newY, t.position.z);
	}
    public static void SetLocalPositionY(this Transform t, float newY)
    {
        t.localPosition = new Vector3(t.localPosition.x, newY, t.localPosition.z);
    }
	public static void SetPositionZ(this Transform t, float newZ)
	{
		t.position = new Vector3(t.position.x, t.position.y, newZ);
	}
    public static void SetLocalPositionZ(this Transform t, float newZ)
    {
        t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, newZ);
    }

	public static float GetPositionX(this Transform t)
	{
		return t.position.x;
	}

	public static float GetPositionY(this Transform t)
	{
		return t.position.y;
	}

	public static float GetPositionZ(this Transform t)
	{
		return t.position.z;
	}

	public static bool HasRigidbody(this GameObject gobj)
	{
		return (gobj.rigidbody != null);
	}

	public static bool HasAnimation(this GameObject gobj)
	{
		return (gobj.animation != null);
	}

	public static void SetSpeed(this Animation anim, float newSpeed)
	{
		anim[anim.clip.name].speed = newSpeed;
	}

	public static Vector2 ToVector2(this Vector3 vec)
	{
		return new Vector2(vec.x, vec.y);
	}

	public static int ToInt32(this string val)
	{
		return Convert.ToInt32(val);
	}

	public static float ToFloat(this string val)
	{
	    if (val == null || val.Equals("")) return 0f;
		return Convert.ToSingle(val);
	}

    /// <summary>
    /// Get from object Array
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="openArgs"></param>
    /// <param name="offset"></param>
    /// <param name="isLog"></param>
    /// <returns></returns>
    public static T Get<T>(this object[] openArgs, int offset, bool isLog = true)
    {
        T ret;
        if ((openArgs.Length - 1) >= offset)
        {
            var arrElement = openArgs[offset];
            if (arrElement == null)
                ret = default(T);
            else
                ret = (T)Convert.ChangeType(arrElement, typeof(T));
        }
        else
        {
            ret = default(T);
            if (isLog)
                CDebug.LogError("[GetArg] {0} args - offset: {1}", openArgs, offset);
        }

        return ret;
    }

}