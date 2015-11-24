using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CosmosTable;

namespace KEngine.CoreModules
{
    /// <summary>
    /// 使用CosmosTable的数据表加载器
    /// </summary>
    public class SettingModule
    {
        public static TableFile<T> Get<T>(string path) where T : TableRowInfo, new()
        {
            return TableFile<T>.LoadFromString(path);
        }
    }
}
