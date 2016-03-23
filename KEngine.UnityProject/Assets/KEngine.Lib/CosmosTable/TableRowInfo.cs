#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - AssetBundle framework for Unity3D
// ===================================
// 
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

namespace CosmosTable
{
    public abstract class TableRowInfo
    {
        /// <summary>
        /// When true, will use reflection to map the Tab File
        /// </summary>
        public virtual bool IsAutoParse
        {
            get { return true; }
        }

        /// <summary>
        /// TableRowInfo's row number of table
        /// </summary>
        public int RowNumber { get; internal set; }

        /// <summary>
        /// Table Header, name and type definition
        /// </summary>
        public Dictionary<string, HeaderInfo> HeaderInfos { get; internal set; }

        protected TableRowInfo()
        {
        }

        public virtual void Parse(string[] cellStrs)
        {
        }

        public virtual object PrimaryKey
        {
            get { return null; }
        }

        protected string Get_String(string value, string defaultValue)
        {
            return Get_string(value, defaultValue);
        }

        protected string Get_string(string value, string defaultValue)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;
            return value;
        }

        protected int Get_Int32(string value, string defaultValue)
        {
            return Get_int(value, defaultValue);
        }
        protected bool Get_Boolean(string value, string defaultValue)
        {
            var str = Get_string(value, defaultValue);
            bool result;
            if (bool.TryParse(str, out result))
            {
                return result;
            }
            return Get_int(value, defaultValue) != 0;
        }
        protected int Get_int(string value, string defaultValue)
        {
            var str = Get_string(value, defaultValue);
            return string.IsNullOrEmpty(str) ? default(int) : int.Parse(str);
        }

        protected uint Get_uint(string value, string defaultValue)
        {
            var str = Get_string(value, defaultValue);
            return string.IsNullOrEmpty(str) ? default(int) : uint.Parse(str);
        }

        protected string[] Get_string_array(string value, string defaultValue)
        {
            var str = Get_string(value, defaultValue);
            return str.Split(',');
        }

        protected Dictionary<string, int> Get_Dictionary_string_int(string value, string defaultValue)
        {
            return GetDictionary<string, int>(value, defaultValue);
        }

        protected Dictionary<TKey, TValue> GetDictionary<TKey, TValue>(string value, string defaultValue)
        {
            var dict = new Dictionary<TKey, TValue>();
            var str = Get_String(value, defaultValue);
            var arr = str.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in arr)
            {
                var kv = item.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                var itemKey = ConvertString<TKey>(kv[0]);
                var itemValue = ConvertString<TValue>(kv[1]);
                dict[itemKey] = itemValue;
            }
            return dict;
        }

        protected T ConvertString<T>(string value)
        {
            return (T)Convert.ChangeType(value, typeof (T));
        }

    }

    /// <summary>
    /// Default Tab Row
    /// Store All column Values
    /// </summary>
    public class DefaultTableRowInfo : TableRowInfo
    {
        /// <summary>
        /// No need to auto parse, use string as key!
        /// </summary>
        public override bool IsAutoParse
        {
            get { return false; }
        }

        /// <summary>
        /// Store values of this row
        /// </summary>
        public string[] Values;

        /// <summary>
        /// Cache save the row values
        /// </summary>
        /// <param name="cellStrs"></param>
        public override void Parse(string[] cellStrs)
        {
            Values = cellStrs;
        }

        /// <summary>
        /// Use first object of array as primary key!
        /// </summary>
        public override object PrimaryKey
        {
            get { return this[0]; }
        }

        public object Get(int index)
        {
            return this[index];
        }

        public object Get(string headerName)
        {
            return this[headerName];
        }

        /// <summary>
        /// Get Value by Indexer
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public object this[int index]
        {
            get
            {
                if (index > Values.Length || index < 0)
                {
                    throw new Exception(string.Format("Overflow index `{0}`", index));
                }

                return Values[index];
            }
        }

        /// <summary>
        /// Get Value by Indexer
        /// </summary>
        /// <param name="headerName"></param>
        /// <returns></returns>
        public object this[string headerName]
        {
            get
            {
                HeaderInfo headerInfo;
                if (!HeaderInfos.TryGetValue(headerName, out headerInfo))
                {
                    throw new Exception("not found header: " + headerName);
                }

                return this[headerInfo.ColumnIndex];
            }
        }
    }

}
