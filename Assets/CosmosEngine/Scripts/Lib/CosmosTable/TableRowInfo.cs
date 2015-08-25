using System;

namespace CosmosTable
{
    public partial class TableRowInfo
    {
        public virtual bool IsAutoParse
        {
            get { return true; }
        }
        public int RowNumber { get; set; }
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
            return str.Split('|');
        }
    }

    /// <summary>
    /// Default Tab Row
    /// Store All column Values
    /// </summary>
    public class DefaultTableRowInfo : TableRowInfo
    {
        public override bool IsAutoParse
        {
            get { return false; }
        }

        public string[] Values;

        public override void Parse(string[] cellStrs)
        {
            Values = cellStrs;
        }
    }

}
