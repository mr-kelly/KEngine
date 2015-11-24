
using CosmosTable;

namespace AppSettings
{

	/// <summary>
	/// Auto Generate for Tab File: Example.bytes
	/// </summary>
	public partial class ExampleInfo : TableRowInfo
	{
		public static readonly string TabFilePath = "Example.bytes";
		
		public override bool IsAutoParse { get { return false; } }

		
		public int Id { get; internal set; }  // ID Column/编号
		
		public string Name { get; internal set; }  // Name/名字
		
		public string[] StrArray { get; internal set; }  // ArrayTest/测试数组
		

		public override void Parse(string[] values)
		{
		
			// ID Column/编号
			Id = Get_int(values[0], "0");
		
			// Name/名字
			Name = Get_string(values[1], "");
		
			// ArrayTest/测试数组
			StrArray = Get_string_array(values[2], "");
		
		}

		public override object PrimaryKey
		{
			get
			{
				return Id;
			}
		}
	}

}
