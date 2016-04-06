
using System.Collections.Generic;
using CosmosTable;
namespace AppSettings
{

	/// <summary>
	/// Auto Generate for Tab File: Example.bytes
	/// </summary>
	public partial class ExampleInfo : TableRowParser
	{
		public static readonly string TabFilePath = "Example.bytes";

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
        /// ID Column/编号
        /// </summary>
        public int Id
        {
            get
            {
                return _row.Get_int(_row.Values[0], "0");
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
        /// 数据测试
        /// </summary>
        public int Number
        {
            get
            {
                return _row.Get_int(_row.Values[2], "");
            }
            set
            {
                _row[2] = value.ToString();
            }
        }
		
        /// <summary>
        /// ArrayTest/测试数组
        /// </summary>
        public string[] StrArray
        {
            get
            {
                return _row.Get_string_array(_row.Values[3], "");
            }
            set
            {
                _row[3] = value.ToString();
            }
        }
		
        /// <summary>
        /// 字典测试
        /// </summary>
        public Dictionary<string,int> StrIntMap
        {
            get
            {
                return _row.Get_Dictionary_string_int(_row.Values[4], "");
            }
            set
            {
                _row[4] = value.ToString();
            }
        }
		
	}
 
}
