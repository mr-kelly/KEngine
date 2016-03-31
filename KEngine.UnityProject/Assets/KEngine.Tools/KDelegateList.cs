#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KDelegateList.cs
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

namespace KEngine
{
    /// <summary>
    /// 比 += delegate拥有更好的性能，更少的GC
    /// </summary>
    public class KDelegateList<T> : List<T> where T : class
    {
        public delegate void DelegateListAction(T item);

        /// <summary>
        /// 因为C#不能使用delegate作为约束(where T : Delegate)，这里做个绕过
        /// </summary>
        static KDelegateList()
        {
            if (!typeof (T).IsSubclassOf(typeof (Delegate)))
            {
                throw new InvalidOperationException(typeof (T).Name + " is not a delegate type");
            }
        }

        /// <summary>
        /// 迭代每一个不为空的元素
        /// </summary>
        /// <param name="doAction"></param>
        public void EachNotNull(DelegateListAction doAction)
        {
            for (var i = 0; i < Count; i++)
            {
                var del = this[i];
                if (del != null)
                    doAction(del);
            }
        }
    }
}