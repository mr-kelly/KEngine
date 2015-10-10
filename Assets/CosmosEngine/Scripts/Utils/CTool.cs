//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                     Version 0.8 (20140904)
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SimpleJson;
using UnityEngine;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

/// <summary>
/// Some tool function for time, bytes, MD5, or something...
/// </summary>
public class CTool
{
    static readonly Dictionary<string, Shader> CacheShaders = new Dictionary<string, Shader>(); // Shader.Find是一个非常消耗的函数，因此尽量缓存起来

    /// <summary>
    /// Whether In Wifi or Cable Network
    /// </summary>
    /// <returns></returns>
    public static bool IsWifi()
    {
        return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
    }


    /// <summary>
    /// 获取最近的2次方
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    public static int GetNearestPower2(int num)
    {
        return (int)(Mathf.Pow(2, Mathf.Ceil(Mathf.Log(num) / Mathf.Log(2))));
    }
    /// <summary>
    /// 判断一个数是否2的次方
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    public static bool CheckPow2(int num)
    {
        int i = 1;
        while (true)
        {
            if (i > num)
                return false;
            if (i == num)
                return true;
            i = i * 2;
        }
    }

    /// <summary>
    /// 模仿 NGUISelectionTool的同名方法，将位置旋转缩放清零
    /// </summary>
    /// <param name="t"></param>
    public static void ResetLocalTransform(Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    // 最大公约数
    public static int GetGCD(int a, int b)
    {
        if (a < b) { int t = a; a = b; b = t; }
        while (b > 0)
        {
            int t = a % b;
            a = b;
            b = t;
        }
        return a;
    }

    public static void DestroyGameObjectChildren(GameObject go)
    {
        var tran = go.transform;

        while (tran.childCount > 0)
        {
            var child = tran.GetChild(0);

            if (Application.isEditor && !Application.isPlaying)
            {
                child.parent = null; // 清空父, 因为.Destroy非同步的
                GameObject.DestroyImmediate(child.gameObject);
            }
            else
            {
                GameObject.Destroy(child.gameObject);
                // 预防触发对象的OnEnable，先Destroy
                child.parent = null; // 清空父, 因为.Destroy非同步的
            }
            
        }
    }

    /// <summary>
    /// 字典转到字符串A:1|B:2|C:3这类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="K"></typeparam>
    /// <param name="dict"></param>
    /// <param name="delimeter1"></param>
    /// <param name="delimeter2"></param>
    /// <returns></returns>
    public static string DictToSplitStr<T, K>(Dictionary<T, K> dict, char delimeter1 = '|', char delimeter2 = ':')
    {
        var sb = new StringBuilder();
        foreach (var kv in dict)
        {
            sb.AppendFormat("{0}{1}{2}{3}", kv.Key, delimeter2, kv.Value, delimeter1);
        }
        return sb.ToString();
    }

    /// <summary>
    /// A:1|B:2|C:3这类字符串转成字典
    /// </summary>
    /// <typeparam name="T">string</typeparam>
    /// <typeparam name="K">string</typeparam>
    /// <param name="str">原始字符串</param>
    /// <param name="delimeter1">分隔符1</param>
    /// <param name="delimeter2">分隔符2</param>
    /// <returns></returns>
    public static Dictionary<T, K> SplitToDict<T, K>(string str, char delimeter1 = '|', char delimeter2 = ':')
    {
        var dict = new Dictionary<T, K>();
        if (!string.IsNullOrEmpty(str))
        {
            string[] strs = str.Split(delimeter1);
            foreach (string s in strs)
            {
                string trimS = s.Trim();
                if (!string.IsNullOrEmpty(trimS))
                {
                    string[] strs2 = trimS.Split(delimeter2);
                    K valK = default(K);
                    T valT = default(T);
                    if (strs2.Length > 0)
                    {
                        valT = (T)Convert.ChangeType(strs2[0], typeof(T));
                    }
                    if (strs2.Length == 2)
                    {

                        valK = (K)Convert.ChangeType(strs2[1], typeof(K));

                    }
                    dict[valT] = valK;
                }
            }
        }
        return dict;
    }

    public static JsonObject SplitToJson(string str, char delimeter1 = '|', char delimeter2 = ':')
    {
        var json = new JsonObject();
        var dic = SplitToDict<string, object>(str, delimeter1, delimeter2);
        foreach (KeyValuePair<string, object> pair in dic)
        {
            json[pair.Key] = pair.Value;
        }
        return json;
    }

    /// <summary>
    /// 截断字符串变成数组
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="str"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static List<T> Split<T>(string str, params char[] args)
    {
        if (args.Length == 0)
        {
            args = new[] { '|' }; // 默认
        }

        var retList = new List<T>();
        if (!string.IsNullOrEmpty(str))
        {
            string[] strs = str.Split(args);

            foreach (string s in strs)
            {
                string trimS = s.Trim();
                if (!string.IsNullOrEmpty(trimS))
                {
                    T val = (T)Convert.ChangeType(trimS, typeof(T));
                    if (val != null)
                    {
                        retList.Add(val);
                    }
                }

            }
        }
        return retList;
    }

    /// <summary>
    /// 从一个List中随机获取
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static T GetRandomItemFromList<T>(IList<T> list)
    {
        if (list.Count == 0)
            return default(T);

        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    /// <summary>
    /// 波浪随机数整数版
    /// </summary>
    /// <param name="waveNumberStr"></param>
    /// <returns></returns>
    public static int GetWaveRandomNumberInt(string waveNumberStr)
    {
        return Mathf.RoundToInt(GetWaveRandomNumber(waveNumberStr));
    }

    /// <summary>
    /// 获取波浪随机数,   即填“1”或填“1~2”这样的字符串中返回一个数！
    /// 
    /// 如填"1"，直接返回1
    /// 如果填"1~10"这样的，那么随机返回1~10中间一个数
    /// </summary>
    /// <param name="waveNumberStr"></param>
    /// <returns></returns>
    public static float GetWaveRandomNumber(string waveNumberStr)
    {
        if (string.IsNullOrEmpty(waveNumberStr))
            return 0;

        var strs = waveNumberStr.Split('-', '~');
        if (strs.Length == 1)
        {
            return waveNumberStr.ToFloat();
        }

        return UnityEngine.Random.Range(strs[0].ToFloat(), strs[1].ToFloat());
    }


    public struct FromToNumber
    {
        public float From;
        public float To;
    }

    /// <summary>
    /// 获取波浪随机数的最大最小
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static FromToNumber ParseMinMaxNumber(string str)
    {
        var rangeArr = CTool.Split<float>(str, '~', '-');
        var number = new FromToNumber();
        if (rangeArr.Count > 0)
        {
            number.From = rangeArr[0];
        }
        if (rangeArr.Count > 1)
        {
            number.To = rangeArr[1];
        }
        return number;
    }
    /// <summary>
    /// 是否在波浪数之间
    /// </summary>
    /// <param name="waveNumberStr"></param>
    /// <param name="testNumber"></param>
    /// <returns></returns>
    public static bool IsBetweenWave(string waveNumberStr, int testNumber)
    {
        if (string.IsNullOrEmpty(waveNumberStr))
            return false;

        var strs = waveNumberStr.Split('~');
        if (strs.Length == 1)
        {
            return strs[0].ToInt32() == testNumber;
        }
        var min = strs[0].ToInt32();
        var max = strs[1].ToInt32();
        return testNumber >= min && testNumber <= max;
    }

    /// <summary>
    /// 是否包含在逗号数组内
    /// </summary>
    /// <param name="numberStr">数组字符串</param>
    /// <param name="testValue">被测数值</param>
    /// <param name="sp">数组分隔符</param>
    /// <returns></returns>
    public static bool IsContains(string numberStr, string testValue, char sp = ',')
    {
        if (string.IsNullOrEmpty(numberStr))
            return false;

        var strs = numberStr.Split(sp);
        return  strs.CContains(testValue);
    }

    public static Shader FindShader(string shaderName)
    {
        Shader shader;
        if (!CacheShaders.TryGetValue(shaderName, out shader))
        {
            shader = Shader.Find(shaderName);
            CacheShaders[shaderName] = shader;
            if (shader == null)
                CDebug.LogError("缺少Shader：{0}  ， 检查Graphics Settings的预置shader", shaderName);
        }

        return shader;
    }

    public static byte[] StructToBytes(object structObject)
    {
        int structSize = Marshal.SizeOf(structObject);
        byte[] bytes = new byte[structSize];
        GCHandle bytesHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        IntPtr bytesPtr = bytesHandle.AddrOfPinnedObject();
        Marshal.StructureToPtr(structObject, bytesPtr, false);

        if (bytesHandle.IsAllocated)
            bytesHandle.Free();

        return bytes;
    }

    public static T BytesToStruct<T>(byte[] bytes, int offset = 0)
    {
        GCHandle bytesHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        IntPtr bytesPtr = (IntPtr)(bytesHandle.AddrOfPinnedObject().ToInt32() + offset);

        T structObject = (T)Marshal.PtrToStructure(bytesPtr, typeof(T));

        if (bytesHandle.IsAllocated)
            bytesHandle.Free();

        return structObject;
    }

    // 将字符串转成指定类型的数组 , 单元测试在Test_StrBytesToArray
    public static T[] StrBytesToArray<T>(string str, int arraySize)
    {
        int typeSize = Marshal.SizeOf(typeof(T));
        byte[] strBytes = Encoding.Unicode.GetBytes(str);
        byte[] bytes = new byte[typeSize * arraySize];  // 强制数组大小

        for (int k = 0; k < strBytes.Length; k++)
            bytes[k] = strBytes[k];  // copy

        T[] tArray = new T[bytes.Length / typeSize];  // 总字节 除以 类型长度 = 有多少个类型对象

        int offset = 0;
        for (int i = 0; i < tArray.Length; i++)
        {
            object convertedObj = null;
            TypeCode typeCode = Type.GetTypeCode(typeof(T));
            switch (typeCode)
            {
                case TypeCode.Byte:
                    convertedObj = bytes[offset];
                    break;
                case TypeCode.Int16:
                    convertedObj = BitConverter.ToInt16(bytes, offset);
                    break;
                case TypeCode.Int32:
                    convertedObj = BitConverter.ToInt32(bytes, offset);
                    break;
                case TypeCode.Int64:
                    convertedObj = BitConverter.ToInt64(bytes, offset);
                    break;
                case TypeCode.UInt16:
                    convertedObj = BitConverter.ToUInt16(bytes, offset);
                    break;
                case TypeCode.UInt32:
                    convertedObj = BitConverter.ToUInt32(bytes, offset);
                    break;
                case TypeCode.UInt64:
                    convertedObj = BitConverter.ToUInt64(bytes, offset);
                    break;
                default:
                    CDebug.LogError("Unsupport Type {0} in StrBytesToArray(), You can custom this.", typeCode);
                    CDebug.Assert(false);
                    break;
            }

            tArray[i] = (T)(convertedObj);
            offset += typeSize;
        }

        return tArray;
    }

    static public uint MakeDword(ushort high, ushort low)
    {
        return ((uint)(((ushort)(((uint)(low)) & 0xffff)) | ((uint)((ushort)(((uint)(high)) & 0xffff))) << 16));
    }
    static public ushort LoWord(uint low)
    {
        return ((ushort)(((uint)(low)) & 0xffff));
    }
    static public ushort HiWord(uint high)
    {
        return ((ushort)((((uint)(high)) >> 16) & 0xffff));
    }
    #region Int + Int = Long
    static public ulong MakeLong(uint high, uint low)
    {
        return ((ulong)high) << 32 | low;
    }

    static public uint HiInt(ulong l)
    {
        return (uint)(l >> 32);
    }

    static public uint LowInt(ulong l)
    {
        return (uint)l;
    }
    #endregion

    public static string HumanizeTimeString(int seconds)
    {
        TimeSpan ts = TimeSpan.FromSeconds(seconds);
        string timeStr = string.Format("{0}{1}{2}{3}",
            ts.Days == 0 ? "" : ts.Days + "天",
            ts.Hours == 0 ? "" : ts.Hours + "小时",
            ts.Minutes == 0 ? "" : ts.Minutes + "分钟",
            ts.Seconds == 0 ? "" : ts.Seconds + "秒");

        return timeStr;
    }

    // 同Lua, Lib:GetUtcDay
    public static int GetUtcDay()
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        DateTime now = DateTime.UtcNow;
        var span = now - origin;

        return span.Days;
    }

    public static int GetDeltaMinutes(DateTime origin)
    {
        DateTime now = DateTime.UtcNow;
        var span = now - origin;

        return span.Minutes;
    }

    public static int GetDeltaHours(DateTime origin)
    {
        DateTime now = DateTime.UtcNow;
        var span = now - origin;

        return span.Hours;
    }

    public static int GetDeltaDay(DateTime origin)
    {
        DateTime now = DateTime.UtcNow;
        var span = now - origin;

        return span.Days;
    }

    /// <summary>
    /// Utc毫秒转Utc时间
    /// </summary>
    /// <param name="utcTime"></param>
    /// <param name="zone">默认0时区</param>
    /// <returns></returns>
    public static DateTime GetDateTimeByUtcMilliseconds(long utcTime, int zone = 0)
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return origin.AddMilliseconds(utcTime).AddHours(zone);
    }
    /// <summary>
    /// Utc秒转Utc时间
    /// </summary>
    /// <param name="unixTimeStamp"></param>
    /// <param name="zone">默认0时区</param>
    /// <returns></returns>
    public static DateTime GetDateTimeByUtcSeconds(double unixTimeStamp, int zone = 0)
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return origin.AddSeconds(unixTimeStamp).AddHours(zone);
    }
    /// <summary>
    /// Unix時間總毫秒數
    /// </summary>
    /// <returns></returns>
    public static double GetUtcMilliseconds()
    {
        return GetUtcMilliseconds(DateTime.UtcNow);
    }
    /// <summary>
    /// Unix時間總毫秒數
    /// </summary>
    /// <returns></returns>
    public static double GetUtcMilliseconds(DateTime date)
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        TimeSpan diff = date - origin;
        return diff.TotalMilliseconds;
    }

    /// <summary>
    /// Unix時間總秒數
    /// </summary>
    /// <returns></returns>
    public static double GetUtcSeconds()
    {
        return GetUtcSeconds(DateTime.UtcNow);
    }

    /// <summary>
    /// Unix時間總秒數
    /// </summary>
    /// <returns></returns>
    public static double GetUtcSeconds(DateTime date)
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        TimeSpan diff = date - origin;
        return diff.TotalSeconds;
    }
    /// <summary>
    /// 测试cron是否触发(精确到分钟)
    /// </summary>
    /// <returns></returns>
    public static bool TestCron(string cron)
    {
        var nCron = NCrontab.CrontabSchedule.Parse(cron);
        var now = DateTime.Now; // 这里涉及到手机本地时间, 不能使用UtcNow
        var next = nCron.GetNextOccurrence(now, DateTime.Now.AddDays(1));
        CDebug.Log("Cron:{0}, now: {1}, next: {2}", cron, now, next);
        var span = next - now;
        CDebug.Log(span.TotalMinutes.ToString());
        return span.TotalMinutes < 1;
    }

    /// <summary>
    /// 人性化数字显示，百万，千万，亿
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static string HumanizeNumber(int number)
    {
        if (number > 100000000)
        {
            return string.Format("{0}{1}", number / 100000000, "亿");
        }
        else if (number > 10000000)
        {
            return string.Format("{0}{1}", number / 10000000, "千万");
        }
        else if (number > 1000000)
        {
            return string.Format("{0}{1}", number / 1000000, "百万");
        }
        else if (number > 10000)
        {
            return string.Format("{0}{1}", number / 10000, "万");
        }

        return number.ToString();
    }

    // 仅用于捕获
    public static string[] Match(string find, string pattern)
    {
        int resultCount = 0;
        string[] result;
        Regex regex;
        Match match;

        regex = new Regex(pattern);
        match = regex.Match(find);

        resultCount = match.Groups.Count - 1;
        if (resultCount < 1)
            return null;

        result = new string[resultCount];
        for (int i = 1; match.Groups[i].Value != ""; i++)
            result[i - 1] = match.Groups[i].Value;

        return result;
    }
    /// <summary>
    /// 模板生成
    /// </summary>
    /// <param name="source">模板内容</param>
    /// <param name="data">数据来源[对象]</param>
    /// <returns></returns>
    public static string Template(string source, object data)
    {
        if (data == null) return source;
        var isJson = data is SimpleJson.JsonObject;
        var result = source;
        var regex = new Regex(@"\{.+?\}");
        var matches = regex.Matches(source);
        foreach (Match match in matches)
        {
            var paramRex = match.Value;
            var paramKey = paramRex.Substring(1, paramRex.Length - 2).Trim(); // 截头截尾
            try
            {
                if (isJson)
                {
                    object paramValue;
                    (data as SimpleJson.JsonObject).TryGetValue(paramKey, out paramValue);
                    if (paramValue != null)
                    {
                        result = result.Replace(paramRex, paramValue.ToString());
                    }
                }
                else
                {
                    var field = data.GetType().GetField(paramKey);
                    if (field != null)
                    {
                        var paramValue = field.GetValue(data).ToString();
                        result = result.Replace(paramRex, paramValue);
                    }
                }
            }
            catch (Exception)
            {
                CDebug.LogError("not find field \"{0}\" for {1}", paramKey, data.GetType());
            }
        }

        return result;
    }
    /// <summary>
    /// 模板生成
    /// </summary>
    /// <param name="source">模板内容</param>
    /// <param name="datas">模板键-值对应数组[key1,value1,key2,value2,...]</param>
    /// <returns></returns>
    public static string Template(string source, params object[] datas)
    {
        // 最少需要两个元素
        if (datas.Length == 0 || datas.Length % 2 != 0)
        {
            CDebug.LogError("传入datas数量必须为偶数个!");
            return source;
        }
        var json = new SimpleJson.JsonObject();
        for (var i = 0; i < datas.Length; i += 2)
        {
            json[datas[i].ToString()] = datas[i + 1];
        }
        return Template(source, json);
    }
    /// <summary>
    /// 混合模板
    /// </summary>
    /// <param name="source">模板内容</param>
    /// <param name="data">数据来源[对象]</param>
    /// <param name="args">数据来源[数组]</param>
    /// <returns></returns>
    public static string Template(string source, object data, object[] args)
    {
        return FormatArgs(Template(source, data), args);
    }

    public static string Format(string source, params object[] args)
    {
        return FormatArgs(source, args);
    }

    /// <summary>
    /// 模板获取
    /// </summary>
    /// <param name="source">模板内容</param>
    /// <param name="args">数据来源[数组]</param>
    /// <returns></returns>
    public static string FormatArgs(string source, object[] args)
    {
        if (args == null) return source;
        var result = source;
        var regex = new Regex(@"\{\d+\}");
        var matches = regex.Matches(source);
        foreach (Match match in matches)
        {
            var paramRex = match.Value;
            var paramKey = paramRex.Substring(1, paramRex.Length - 2).Trim(); // 截头截尾
            try
            {
                var index = paramKey.ToInt32();
                if (args.Length > index)
                {
                    var paramValue = args[index].ToString();
                    result = result.Replace(paramRex, paramValue);
                }
            }
            catch (Exception)
            {
                CDebug.LogError("not find argument index: \"{0}\" in array: {1}", paramKey, args);
            }
        }

        return result;
    }

    /// <summary>
    /// Recursively set the game object's layer.
    /// </summary>
    static public void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;

        var t = go.transform;

        for (int i = 0, imax = t.childCount; i < imax; ++i)
        {
            var child = t.GetChild(i);
            SetLayer(child.gameObject, layer);
        }
    }

    /// <summary>
    /// 传入uri寻找指定控件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="findTrans"></param>
    /// <param name="uri"></param>
    /// <param name="isLog"></param>
    /// <returns></returns>
    public static T GetChildComponent<T>(Transform findTrans, string uri, bool isLog = true) where T : Component
    {
        Transform trans = findTrans.Find(uri);
        if (trans == null)
        {
            if (isLog)
                CDebug.LogError("Get Child Error: " + uri);
            return default(T);
        }

        return (T)trans.GetComponent(typeof(T));
    }

    public static T GetChildComponent<T>(string uri, Transform findTrans, bool isLog = true) where T : Component
    {
        if (findTrans == null)
            return default(T);
        Transform trans = findTrans.Find(uri);
        if (trans == null)
        {
            if (isLog)
                CDebug.LogError("Get Child Error: " + uri);
            return default(T);
        }

        return (T)trans.GetComponent(typeof(T));
    }

    public static GameObject DFSFindObject(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; ++i)
        {
            Transform node = parent.GetChild(i);
            if (node.name == name)
                return node.gameObject;

            GameObject target = DFSFindObject(node, name);
            if (target != null)
                return target;
        }

        return null;
    }
    public static GameObject GetGameObject(string name, Transform findTrans, bool isLog = true)
    {
        GameObject obj = DFSFindObject(findTrans, name);
        if (obj == null)
        {
            CDebug.LogError("Find GemeObject Error: " + name);
            return null;
        }

        return obj;
    }

    public static void SetChild(GameObject child, GameObject parent, bool selfRotation = false, bool selfScale = false)
    {
        SetChild(child.transform, parent.transform, selfRotation, selfScale);
    }
    public static void SetChild(Transform child, Transform parent, bool selfRotation = false, bool selfScale = false)
    {
        child.parent = parent;
        ResetTransform(child, selfRotation, selfScale);
    }
    public static void ResetTransform(UnityEngine.Transform transform, bool selfRotation = false, bool selfScale = false)
    {
        transform.localPosition = UnityEngine.Vector3.zero;
        if (!selfRotation)
            transform.localEulerAngles = UnityEngine.Vector3.zero;

        if (!selfScale)
            transform.localScale = UnityEngine.Vector3.one;
    }

    //获取从父节点到自己的完整路径
    public static string GetRootPathName(UnityEngine.Transform transform)
    {
        var pathName="/"+transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            pathName +="/"+ transform.name;
        }
        return pathName;
    }
    // 获取指定流的MD5
    public static string MD5_Stream(Stream stream)
    {
        System.Security.Cryptography.MD5CryptoServiceProvider get_md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

        byte[] hash_byte = get_md5.ComputeHash(stream);

        string resule = System.BitConverter.ToString(hash_byte);

        resule = resule.Replace("-", "");

        return resule;
    }

    public static string MD5_File(string filePath)
    {
        try
        {
            using (FileStream get_file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return MD5_Stream(get_file);
            }
        }
        catch (Exception e)
        {
            return e.ToString();

        }
    }
    public static byte[] MD5_bytes(string str)
    {
        // MD5 文件名
        var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        return md5.ComputeHash(System.Text.Encoding.Unicode.GetBytes(str));
    }

    // 字符串16位 MD5
    public static string MD5_16bit(string str)
    {
        byte[] md5bytes = MD5_bytes(str);
        str = System.BitConverter.ToString(md5bytes, 4, 8);
        str = str.Replace("-", "");
        return str;
    }

    public static T ToEnum<T>(string e)
    {
        return (T)Enum.Parse(typeof(T), e);
    }


    // 整形渐近，+1或-1 , kk
    public static int IntLerp(int from, int to)
    {
        if (from > to)
            return from - 1;
        else if (from < to)
            return from + 1;
        else
            return from;
    }

    // 粒子特效比例缩放
    public static void ScaleParticleSystem(GameObject gameObj, float scale)
    {
        var notFind = true;
        foreach (ParticleSystem p in gameObj.GetComponentsInChildren<ParticleSystem>(true))
        {
            notFind = false;
            p.startSize *= scale;
            p.startSpeed *= scale;
            p.startRotation *= scale;
            p.transform.localScale *= scale;
        }
        if (notFind)
        {
            gameObj.transform.localScale = new Vector3(scale, scale, 1);
        }
    }

    //设置粒子系统的RenderQueue
    public static void SetParticleSystemRenderQueue(Transform parent, int renderQueue = 3900)
    {
        int childCount = parent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.childCount > 0)
                SetParticleSystemRenderQueue(child, renderQueue);
            if (child.GetComponent<ParticleSystem>() != null)
            {
                var particleSystem = child.GetComponent<ParticleSystem>();
                if (particleSystem.renderer.sharedMaterial != null)
                    particleSystem.renderer.sharedMaterial.renderQueue = renderQueue;
            }
        }
        if (parent.GetComponent<ParticleSystem>() != null)
        {
            var particleSystem = parent.GetComponent<ParticleSystem>();
            //bug 当同一个窗口有多个使用相同的Material时，其它组件的Material在关闭后会被释放
            if (particleSystem.renderer.sharedMaterial != null)
                particleSystem.renderer.sharedMaterial.renderQueue = renderQueue;
        }
    }
    public static void MoveAllCollidersToGameObject(GameObject srcGameObject, GameObject targetGameObject)
    {
        CDebug.Assert(srcGameObject != targetGameObject);
        foreach (Collider collider2d in srcGameObject.GetComponentsInChildren<Collider>())
        {
            CopyColliderToGameObject(collider2d, targetGameObject);
            GameObject.Destroy(collider2d);
        }

        foreach (Collider2D collider2d in srcGameObject.GetComponentsInChildren<Collider2D>())
        {
            CopyCollider2DToGameObject(collider2d, targetGameObject);
            GameObject.Destroy(collider2d);
        }
    }

    /// <summary>
    /// return new Copy
    /// </summary>
    /// <param name="collider2d"></param>
    /// <param name="targetGameObject"></param>
    /// <returns></returns>
    public static Collider2D CopyCollider2DToGameObject(Collider2D collider2d, GameObject targetGameObject)
    {
        if (collider2d is CircleCollider2D)
        {
            CircleCollider2D oldCircle = collider2d as CircleCollider2D;
            CircleCollider2D newCircle = targetGameObject.AddComponent<CircleCollider2D>();
            newCircle.isTrigger = oldCircle.isTrigger;
            newCircle.radius = oldCircle.radius;

            Vector3 realLocalPos = targetGameObject.transform.InverseTransformPoint(oldCircle.gameObject.transform.position);
            newCircle.center = oldCircle.center + (Vector2)realLocalPos;
            return newCircle;
        }
        else if (collider2d is BoxCollider2D)
        {
            BoxCollider2D oldBox = collider2d as BoxCollider2D;
            BoxCollider2D newBox = targetGameObject.AddComponent<BoxCollider2D>();
            newBox.isTrigger = oldBox.isTrigger;
            newBox.size = oldBox.size;
            //newBox.center = oldBox.center;
            Vector3 realLocalPos = targetGameObject.transform.InverseTransformPoint(oldBox.gameObject.transform.position);
            newBox.center = oldBox.center + (Vector2)realLocalPos;
            return newBox;
        }
        CDebug.LogError("Error Collider: {0}", collider2d);
        return null;
    }
    // 将3D碰撞强转2D
    public static void CopyColliderToGameObject(Collider collider, GameObject targetGameObject)
    {
        if (collider is SphereCollider)
        {
            SphereCollider oldCircle = collider as SphereCollider;
            CircleCollider2D newCircle = targetGameObject.AddComponent<CircleCollider2D>();
            newCircle.isTrigger = oldCircle.isTrigger;
            newCircle.radius = oldCircle.radius;
            newCircle.center = oldCircle.center;
        }
        else if (collider is BoxCollider)
        {
            BoxCollider oldBox = collider as BoxCollider;
            BoxCollider2D newBox = targetGameObject.AddComponent<BoxCollider2D>();
            newBox.isTrigger = oldBox.isTrigger;
            newBox.size = oldBox.size;
            newBox.center = oldBox.center;
        }
    }

    public static void CopyRigidbody2DToGameObject(Rigidbody2D rigidbody2d, GameObject targetGameObject)
    {
        Rigidbody2D oldRigidbody = rigidbody2d;
        Rigidbody2D newRigidbody = targetGameObject.AddComponent<Rigidbody2D>();
        newRigidbody.mass = oldRigidbody.mass;
        newRigidbody.gravityScale = oldRigidbody.gravityScale;
        newRigidbody.isKinematic = oldRigidbody.isKinematic;
    }

    public static void CopyTransformToTarget(Transform sourceTrans, Transform targetTrans)
    {
        targetTrans.localPosition = sourceTrans.localPosition;
        targetTrans.localRotation = sourceTrans.localRotation;
        targetTrans.localScale = sourceTrans.localScale;
    }
    // 测试有无写权限
    public static bool HasWriteAccessToFolder(string folderPath)
    {
        try
        {
            string tmpFilePath = Path.Combine(folderPath, Path.GetRandomFileName());
            using (FileStream fs = new FileStream(tmpFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                StreamWriter writer = new StreamWriter(fs);
                writer.Write("1");
            }
            File.Delete(tmpFilePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void SetStaticRecursively(GameObject obj)
    {
        LinkedList<Transform> transList = new LinkedList<Transform>();
        transList.AddLast(obj.transform);

        while (transList.Count > 0)
        {
            Transform trans = transList.First.Value;
            transList.RemoveFirst();

            trans.gameObject.isStatic = true;

            for (int i = 0; i < trans.childCount; ++i)
            {
                transList.AddLast(trans.GetChild(i));
            }
        }
    }

    // 标准角度，角度负数会转正
    public static float GetNormalizedAngle(float _anyAngle)
    {
        _anyAngle = _anyAngle % 360;
        if (_anyAngle < 0)
        {
            _anyAngle += 360;
        }

        return _anyAngle;
    }

    // 调整对象的collider到指定全局深度
    public static void AdjustCollidersCenterZ(GameObject gameObj, float z = 0)
    {
        foreach (Collider collider in gameObj.GetComponentsInChildren<Collider>())
        {
            if (collider is BoxCollider)
            {
                BoxCollider boxC = (BoxCollider)collider;
                float gameObjectZ = boxC.center.z + collider.gameObject.transform.position.z - z;
                boxC.center = new Vector3(boxC.center.x, boxC.center.y, -gameObjectZ);  // 对象深度-1，collider弄成1就平衡了
            }
            else if (collider is SphereCollider)
            {
                SphereCollider sphereC = (SphereCollider)collider;
                float globalZ = sphereC.center.z + collider.gameObject.transform.position.z - z;
                sphereC.center = new Vector3(sphereC.center.x, sphereC.center.y, -globalZ);
            }
        }
    }

    // 首字母大写
    public static string ToTitleCase(string word)
    {
        return word.Substring(0, 1).ToUpper() + (word.Length > 1 ? word.Substring(1).ToLower() : "");
    }

    // 首字母大写变下划线
    public static string ToSentenceCase(string str)
    {
        str = char.ToLower(str[0]) + str.Substring(1);
        return Regex.Replace(str, "[a-z][A-Z]", (m) =>
        {
            return char.ToLower(m.Value[0]) + "_" + char.ToLower(m.Value[1]);
        });
    }

    // 概率，百分比, // 注意，0的时候当是100%
    public static bool Probability(float chancePercent)
    {
        var chance = UnityEngine.Random.Range(0f, 100f);

        if (chance <= chancePercent)  // 概率
        {
            return true;
        }

        return false;
    }

    public static bool Probability(byte chancePercent)
    {
        int chance = UnityEngine.Random.Range(1, 101);

        if (chance <= chancePercent)  // 概率
        {
            return true;
        }

        return false;
    }

    // 數組值比較
    public static bool ArraysEqual<T>(T[] a1, T[] a2)
    {
        if (ReferenceEquals(a1, a2))
            return true;

        if (a1 == null || a2 == null)
            return false;

        if (a1.Length != a2.Length)
            return false;

        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < a1.Length; i++)
        {
            if (!comparer.Equals(a1[i], a2[i])) return false;
        }
        return true;
    }

    /// <summary>
    /// 呈弧形，传入一个参考点根据角度和半径计算出其它位置的坐标
    /// </summary>
    /// <param name="nNum">需要的数量</param>
    /// <param name="pAnchorPos">锚定点/参考点</param>
    /// <param name="fAngle">角度</param>
    /// <param name="nRadius">半径</param>
    /// <returns></returns>
    public static Vector3[] GetSmartNpcPoints(Vector3 startDirection, int nNum, Vector3 pAnchorPos, float fAngle, float nRadius)
    {
        bool bPlural = nNum % 2 == 0 ? true : false; // 是否复数模式
        Vector3 vDir = startDirection;
        int nMidNum = bPlural ? nNum / 2 : nNum / 2 + 1; // 中间数, 循环过了中间数后，另一个方向起排布
        Vector3 vRPos = vDir * nRadius; //// 计算直线在圆形上的顶点 半径是表现距离
        Vector3[] targetPos = new Vector3[nNum];
        for (int i = 1; i <= nNum; i++)
        {
            float nAddAngle = 0;

            if (bPlural) // 复数模式
            {
                if (i > nMidNum)
                    nAddAngle = fAngle * ((i % nMidNum) + 1) - fAngle / 2;
                else
                    nAddAngle = -fAngle * ((i % nMidNum) + 1) + fAngle / 2; // 除以2，是为了顶端NPC均匀排布 by KK
            }
            else // 单数模式
            {
                // 判断是否过了中间数
                if (i > nMidNum)
                {
                    nAddAngle = fAngle * (i % nMidNum); // 添加NPC的角度
                }
                else if (i < nMidNum) // 非复数模式， 中间数NPC 放在正方向
                {
                    nAddAngle = -fAngle * (i % nMidNum); // 反方向角度
                }
                else
                    nAddAngle = 0; // 正方向
            }

            Vector3 vTargetPos = pAnchorPos + Quaternion.AngleAxis(nAddAngle, Vector3.forward) * vRPos;
            targetPos[i - 1] = vTargetPos;
        }
        return targetPos;
    }

    /// 返回localPoint
    public static Vector3[] GetParallelPoints(Vector3 startPos, Vector3 startDirection, int nNum, float meterInterval)
    {
        bool bPlural = nNum % 2 == 0 ? true : false; // 是否复数模式
        int nMidNum = bPlural ? nNum / 2 : nNum / 2 + 1; // 中间数, 循环过了中间数后，另一个方向起排布
        Vector3[] targetPos = new Vector3[nNum];
        for (int i = 1; i <= nNum; i++)
        {
            float fAddInterval = 0;

            if (bPlural) // 复数模式
            {
                if (i > nMidNum)
                    fAddInterval = meterInterval * ((i % nMidNum) + 1) - meterInterval / 2;
                else
                    fAddInterval = -meterInterval * ((i % nMidNum) + 1) + meterInterval / 2; // 除以2，是为了顶端NPC均匀排布 by KK
            }
            else // 单数模式
            {
                // 判断是否过了中间数
                if (i > nMidNum)
                {
                    fAddInterval = meterInterval * ((i % nMidNum) + 1); // 添加NPC的角度
                }
                else if (i < nMidNum) // 非复数模式， 中间数NPC 放在正方向
                {
                    fAddInterval = -meterInterval * ((i % nMidNum) + 1); // 反方向角度
                }
                else
                    fAddInterval = 0; // 正方向
            }

            // 90度旋转求垂直
            Vector3 vTargetPos = startPos + Quaternion.AngleAxis(90, Vector3.forward) * startDirection * fAddInterval;
            //Vector3 vTargetPos = direction*fAddInterval;
            targetPos[i - 1] = vTargetPos;
        }
        return targetPos;
    }

    // 两线交点（忽略长度）
    public static bool LineIntersectionPoint(out Vector2 intersectPoint, Vector2 ps1, Vector2 pe1, Vector2 ps2,
        Vector2 pe2)
    {
        intersectPoint = Vector2.zero;

        // Get A,B,C of first line - points : ps1 to pe1
        float A1 = pe1.y - ps1.y;
        float B1 = ps1.x - pe1.x;
        float C1 = A1 * ps1.x + B1 * ps1.y;

        // Get A,B,C of second line - points : ps2 to pe2
        float A2 = pe2.y - ps2.y;
        float B2 = ps2.x - pe2.x;
        float C2 = A2 * ps2.x + B2 * ps2.y;

        // Get delta and check if the lines are parallel
        float delta = A1 * B2 - A2 * B1;
        if (delta == 0)
            return false;

        // now return the Vector2 intersection point
        intersectPoint = new Vector2(
            (B2 * C1 - B1 * C2) / delta,
            (A1 * C2 - A2 * C1) / delta
        );
        return true;
    }
    /// <summary>
    /// 判断字符串是否是数字
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool IsNumber(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            CDebug.LogWarning("传入的值为空！请检查");
            return false;
        }
        var pattern = @"^\d*$";
        return Regex.IsMatch(str, pattern);
    }


    /// <summary>
    /// 获取椭圆上的某一点，相对坐标
    /// </summary>
    /// <param name="长半轴即目标距离"></param>
    /// <param name="短半轴"></param>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static Vector2 GetRelativePositionOfEllipse(float 长半轴即目标距离, float 短半轴, float angle)
    {
        var rad = angle * Mathf.Deg2Rad; // 弧度
        var newPos = new Vector2(长半轴即目标距离 * Mathf.Cos(rad), 短半轴 * Mathf.Sin(rad));
        return newPos;
    }

    public static float Angle(Vector2 from, Vector2 to)
    {
        return Quaternion.FromToRotation(from.normalized, to.normalized).eulerAngles.z;
    }

    /// <summary>
    /// 把数字格式化成三位 , 分隔
    /// </summary>
    public static string NumberFormatTo3(Int64 num, string sp = ",")
    {
        return num.ToString("##" + sp + "###", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Get IPAdress from IpHostEntry,  配合GetIpAddress
    /// </summary>
    /// <param name="ipHostEntry"></param>
    /// <returns></returns>
    public static IPAddress GetIpAddressFromIpHostEntry(IPHostEntry ipHostEntry)
    {

        var addresses = ipHostEntry.AddressList;

        foreach (var item in addresses)
        {
            if (item.AddressFamily == AddressFamily.InterNetwork)
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// Async Get IPAdress
    /// </summary>
    /// <param name="host"></param>
    /// <param name="callback"></param>
    public static void GetIpAddress(string host, Action<IPAddress> callback = null)
    {
        IPAddress ipAddress = null;
        if (!IPAddress.TryParse(host, out ipAddress))
        {
            Dns.BeginGetHostAddresses(host, new AsyncCallback((asyncResult) =>
            {
                IPAddress[] addrs = Dns.EndGetHostAddresses(asyncResult);
                if (callback != null)
                {
                    if (addrs.Length > 0)
                        ipAddress = addrs[0];
                    callback(ipAddress);
                }
            }), null);

        }
        else
        {
            if (callback != null)
                callback(ipAddress);
        }
    }
}

public class XMemoryParser<T>
{
    readonly int MaxCount;
    readonly byte[] SourceBytes;
    readonly GCHandle SourceBytesHandle;
    readonly IntPtr SourceBytesPtr;

    public XMemoryParser(byte[] bytes, int maxCount)
    {
        SourceBytes = bytes;
        MaxCount = maxCount;
        SourceBytesHandle = GCHandle.Alloc(SourceBytes, GCHandleType.Pinned);
        SourceBytesPtr = SourceBytesHandle.AddrOfPinnedObject();
    }

    public XMemoryParser(IntPtr bytesPtr, int maxCount)
    {
        MaxCount = maxCount;
        SourceBytesPtr = bytesPtr;
    }

    ~XMemoryParser()
    {
        if (SourceBytesHandle.IsAllocated)
            SourceBytesHandle.Free();
    }

    public T this[int index]
    {
        get
        {
            CDebug.Assert(index < MaxCount);
            IntPtr p = (IntPtr)(SourceBytesPtr.ToInt32() + Marshal.SizeOf(typeof(T)) * index);
            return (T)Marshal.PtrToStructure(p, typeof(T));
        }
    }
}

// C# 扩展, 扩充C#类的功能
public static class XExtensions
{
    // 扩展List/  
    public static void Shuffle<T>(this IList<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    public static T CFirstOrDefault<T>(this IEnumerable<T> source)
    {
        if (source != null)
        {
            foreach (T item in source)
            {
                return item;
            }
        }

        return default(T);
    }
    public static List<T> CFirst<T>(this IEnumerable<T> source, int num)
    {
        var count = 0;
        var items = new List<T>();
        if (source != null)
        {
            foreach (T item in source)
            {
                if (++count > num)
                {
                    break;
                }
                items.Add(item);
            }
        }

        return items;
    }
    public delegate bool CFilterAction<T>(T t);
    public static List<T> CFilter<T>(this IEnumerable<T> source, CFilterAction<T> testAction)
    {
        var items = new List<T>();
        if (source != null)
        {
            foreach (T item in source)
            {
                if (testAction(item))
                {
                    items.Add(item);
                }
            }
        }

        return items;
    }
    public delegate bool CFilterAction<T, K>(T t, K k);
    public static Dictionary<T, K> CFilter<T, K>(this IEnumerable<KeyValuePair<T, K>> source, CFilterAction<T, K> testAction)
    {
        var items = new Dictionary<T, K>();
        if (source != null)
        {
            foreach (KeyValuePair<T, K> pair in source)
            {
                if (testAction(pair.Key, pair.Value))
                {
                    items.Add(pair.Key, pair.Value);
                }
            }
        }

        return items;
    }
    public static T CLastOrDefault<T>(this IEnumerable<T> source)
    {
        var result = default(T);
        foreach (T item in source)
        {
            result = item;
        }
        return result;
    }
    public static List<T> CLast<T>(this IEnumerable<T> source, int num)
    {
        // 开始读取的位置
        var startIndex = Math.Max(0, source.CToList().Count - num);
        var index = 0;
        var items = new List<T>();
        if (source != null)
        {
            foreach (T item in source)
            {
                if (index < startIndex)
                {
                    continue;
                }
                items.Add(item);
            }
        }

        return items;
    }
    public static T[] CToArray<T>(this IEnumerable<T> source)
    {
        var list = new List<T>();
        foreach (T item in source)
        {
            list.Add(item);
        }
        return list.ToArray();
    }
    public static List<T> CToList<T>(this IEnumerable<T> source)
    {
        var list = new List<T>();
        foreach (T item in source)
        {
            list.Add(item);
        }
        return list;
    }
    public static List<T> CUnion<T>(this List<T> first, List<T> second, IEqualityComparer<T> comparer)
    {
        var results = new List<T>();
        var list = first.CToList();
        list.AddRange(second);
        foreach (T item in list)
        {
            var include = false;
            foreach (T result in results)
            {
                if (comparer.Equals(result, item))
                {
                    include = true;
                    break;
                }
            }
            if (!include)
            {
                results.Add(item);
            }
        }
        return results;
    }
    public static string CJoin<T>(this IEnumerable<T> source, string sp)
    {
        var result = new StringBuilder();
        foreach (T item in source)
        {
            if (result.Length == 0)
            {
                result.Append(item);
            }
            else
            {
                result.Append(sp).Append(item);
            }
        }
        return result.ToString();
    }
    public static bool CContains<TSource>(this IEnumerable<TSource> source, TSource value)
    {
        foreach (TSource item in source)
        {
            if (Equals(item, value))
            {
                return true;
            }
        }
        return false;
    }

    // by KK, 获取自动判断JSONObject的str，n
    //public static object Value(this JSONObject jsonObj)
    //{
    //    switch (jsonObj.type)
    //    {
    //        case JSONObject.Type.NUMBER:  // 暂时返回整形！不管浮点了, lua目前少用浮点
    //            return (int)jsonObj.n;
    //        case JSONObject.Type.STRING:
    //            return jsonObj.str;
    //        case JSONObject.Type.NULL:
    //            return null;
    //        case JSONObject.Type.ARRAY:
    //        case JSONObject.Type.OBJECT:
    //            return jsonObj;
    //        case JSONObject.Type.BOOL:
    //            return jsonObj.b;
    //    }

    //    return null;
    //}


}