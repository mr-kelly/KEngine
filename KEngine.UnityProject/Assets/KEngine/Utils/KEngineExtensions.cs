#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KEngineExtensions.cs
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
using KEngine;
using UnityEngine;

/// <summary>
/// Extension Unity's function, to be more convinient
/// </summary>
public static class KEngineExtensions
{
    public static void SetWidth(this RectTransform rectTrans, float width)
    {
        var size = rectTrans.sizeDelta;
        size.x = width;
        rectTrans.sizeDelta = size;
    }

    public static void SetHeight(this RectTransform rectTrans, float height)
    {
        var size = rectTrans.sizeDelta;
        size.y = height;
        rectTrans.sizeDelta = size;
    }

    public static void SetPositionX(this Transform t, float newX)
    {
        t.position = new Vector3(newX, t.position.y, t.position.z);
    }

    public static void SetPositionY(this Transform t, float newY)
    {
        t.position = new Vector3(t.position.x, newY, t.position.z);
    }

    public static void SetLocalPositionX(this Transform t, float newX)
    {
        t.localPosition = new Vector3(newX, t.localPosition.y, t.localPosition.z);
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

    public static void SetLocalScale(this Transform t, Vector3 newScale)
    {
        t.localScale = newScale;
    }

    public static void SetLocalScaleZero(this Transform t)
    {
        t.localScale = Vector3.zero;
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

    public static float GetLocalPositionX(this Transform t)
    {
        return t.localPosition.x;
    }

    public static float GetLocalPositionY(this Transform t)
    {
        return t.localPosition.y;
    }

    public static float GetLocalPositionZ(this Transform t)
    {
        return t.localPosition.z;
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

    public static byte ToByte(this string val)
    {
        return string.IsNullOrEmpty(val) ? (byte)0 : Convert.ToByte(val);
    }

    public static int ToInt32(this string val)
    {
        return string.IsNullOrEmpty(val) ? 0 : Convert.ToInt32(val);
    }

    public static long ToInt64(this string val)
    {
        return string.IsNullOrEmpty(val) ? 0 : Convert.ToInt64(val);
    }

    public static float ToFloat(this string val)
    {
        return string.IsNullOrEmpty(val) ? 0f : Convert.ToSingle(val);
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
            {
                try
                {
                    ret = (T)Convert.ChangeType(arrElement, typeof(T));
                }
                catch (Exception)
                {
                    if (arrElement is string && string.IsNullOrEmpty(arrElement as string))
                        ret = default(T);
                    else
                    {
                        Logger.LogError("[Error get from object[],  '{0}' change to type {1}", arrElement, typeof(T));
                        ret = default(T);
                    }
                }
            }
        }
        else
        {
            ret = default(T);
            if (isLog)
                Logger.LogError("[GetArg] {0} args - offset: {1}", openArgs, offset);
        }

        return ret;
    }
}