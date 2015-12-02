using System;
using UnityEngine;
using System.Collections;
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
            if (!typeof(T).IsSubclassOf(typeof(Delegate)))
            {
                throw new InvalidOperationException(typeof(T).Name + " is not a delegate type");
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