
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
            if (_instance == null)
                _instance = new ExampleInfo();

            _instance._row = row;
            return _instance;
        }

        private TableRow _row;

        private ExampleInfo()
        {
        }

		
        /// ID Column/编号
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
		
        /// Name/名字
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
		
        /// ArrayTest/测试数组
		public string[] StrArray
        {
            get
            {
                return _row.Get_string_array(_row.Values[2], "");
            }
            set
            {
                _row[2] = value.ToString();
            }

        }
		
        /// 字典测试
		public Dictionary<string,int> StrIntMap
        {
            get
            {
                return _row.Get_Dictionary_string_int(_row.Values[3], "");
            }
            set
            {
                _row[3] = value.ToString();
            }

        }
		
	}
 
}
