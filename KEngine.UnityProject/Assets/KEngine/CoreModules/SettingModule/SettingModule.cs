#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: SettingModule.cs
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
using System.IO;
using System.Text;
using CosmosTable;
using UnityEngine;

namespace KEngine.CoreModules
{
    /// <summary>
    /// 使用CosmosTable的数据表加载器
    /// </summary>
    public class SettingModule
    {
        /// <summary>
        /// table缓存
        /// </summary>
        private static readonly Dictionary<string, object> _tableFilesCache = new Dictionary<string, object>();

        /// <summary>
        /// 通过SettingModule拥有缓存与惰式加载
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="useCache">是否缓存起来？还是单独创建新的</param>
        /// <returns></returns>
        public static TableFile<T> Get<T>(string path, bool useCache = true) where T : TableRowInfo, new()
        {
            object tableFile;
            if (!useCache || !_tableFilesCache.TryGetValue(path, out tableFile))
            {
                var fileContentAsset = Resources.Load("Setting/" + Path.GetFileNameWithoutExtension(path)) as TextAsset;
                var fileContent = Encoding.UTF8.GetString(fileContentAsset.bytes);

                var tab = TableFile<T>.LoadFromString(fileContent);
                _tableFilesCache[path] = tableFile = tab;
                return tab;
            }

            return tableFile as TableFile<T>;
        }
    }
}