using System;
using System.Collections.Generic;

namespace CosmosTable
{
    public partial class TableRowInfo
    {
        /// <summary>
        /// When true, will use reflection to map the Tab File
        /// </summary>
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
