//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                          Version 0.8
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// TODO: 尚未做对象内存优化
public class CObjectPool
{
    static Dictionary<int, object> Objects = new Dictionary<int, object>();

    static int ObjectIdGenerator = 100;  // Unique ID, 唯一的ID

    public static int GetObjectId()
    {
        return ObjectIdGenerator++;
    }

    public static void SetObject(int uid, object obj)
    {
        Objects[uid] = obj;
    }

    public static T CreateObject<T>(out int objectId) where T : new()
    {
        T obj = new T();

        int _object_id = GetObjectId();

        objectId = _object_id;

        Objects[objectId] = obj;

        return obj;
    }

    public static void RemoveObject(int uid)
    {
        Objects.Remove(uid);  // TODO: 存起来
    }
}