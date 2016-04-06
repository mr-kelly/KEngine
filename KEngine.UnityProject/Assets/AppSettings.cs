
using System.Collections;
using System.Collections.Generic;
using CosmosTable;
using KEngine.CoreModules;
namespace AppSettings
{


	/// <summary>
	/// Auto Generate for Tab File: Example.bytes
	/// </summary>>
    public partial class ExampleInfos
    {
		public static readonly string TabFilePath = "Example.bytes";

        public static TableFile GetTableFile()
        {
            return SettingModule.Get(TabFilePath);
        }

        public static IEnumerable GetAll()
        {
            var tableFile = SettingModule.Get(TabFilePath);
            foreach (var row in tableFile)
            {
                yield return ExampleInfo.Wrap(row);
            }
        }

        public static ExampleInfo GetByPrimaryKey(object primaryKey)
        {
            var tableFile = SettingModule.Get(TabFilePath);
            var row = tableFile.GetByPrimaryKey(primaryKey);
            if (row == null) return null;
            return ExampleInfo.Wrap(row);
        }
    }
	/// <summary>
	/// Auto Generate for Tab File: Example.bytes
    /// Singleton class for less memory use
	/// </summary>
	public partial class ExampleInfo : TableRowParser
	{

		private static ExampleInfo _instance;

        public static ExampleInfo Wrap(TableRow row)
        {
            var inst = _instance ?? (_instance = new ExampleInfo());
            inst._row = row;
            return inst;
        }

        private TableRow _row;

        private ExampleInfo()
        {
        }

		
        /// <summary>
        /// ID Column/编号/主键
        /// </summary>
        public string Id
        {
            get
            {
                return _row.Get_string(_row.Values[0], "");
            }
            set
            {
                _row[0] = value.ToString();
            }
        }
		
        /// <summary>
        /// Name/名字
        /// </summary>
        public string Name
        {
            get
            {
                return _row.Get_string(_row.Values[1], "");
            }
            set
            {
                _row[1] = value.ToString();
            }
        }
		
        /// <summary>
        /// 用于组合成Id主键
        /// </summary>
        public string KeyString
        {
            get
            {
                return _row.Get_string(_row.Values[2], "");
            }
            set
            {
                _row[2] = value.ToString();
            }
        }
		
        /// <summary>
        /// 数据测试
        /// </summary>
        public int Number
        {
            get
            {
                return _row.Get_int(_row.Values[3], "");
            }
            set
            {
                _row[3] = value.ToString();
            }
        }
		
        /// <summary>
        /// ArrayTest/测试数组
        /// </summary>
        public string[] StrArray
        {
            get
            {
                return _row.Get_string_array(_row.Values[4], "");
            }
            set
            {
                _row[4] = value.ToString();
            }
        }
		
        /// <summary>
        /// 字典测试
        /// </summary>
        public Dictionary<string,int> StrIntMap
        {
            get
            {
                return _row.Get_Dictionary_string_int(_row.Values[5], "");
            }
            set
            {
                _row[5] = value.ToString();
            }
        }
		
	}
 
}
