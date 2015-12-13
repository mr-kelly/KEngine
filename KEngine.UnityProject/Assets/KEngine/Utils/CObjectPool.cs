#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CObjectPool.cs
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

// TODO: 尚未做对象内存优化
public class CObjectPool
{
    private static Dictionary<int, object> Objects = new Dictionary<int, object>();

    private static int ObjectIdGenerator = 100; // Unique ID, 唯一的ID

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
        Objects.Remove(uid); // TODO: 存起来
    }
}