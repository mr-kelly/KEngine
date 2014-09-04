//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                         version 0.8
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
using UnityEngine;
using System.IO;


/// <summary>
/// Some tool function for time, bytes, MD5, or something...
/// </summary>
public class CTool
{
    static float[] RecordTime = new float[10];
    static string[] RecordKey = new string[10];
    static int RecordPos = 0;

    static Dictionary<string, Shader> CacheShaders = new Dictionary<string, Shader>(); // Shader.Find是一个非常消耗的函数，因此尽量缓存起来

    // 需要StartCoroutine
    public static IEnumerator TimeCallback(float time, Action callback)
    {
        yield return new WaitForSeconds(time);
        callback();
    }
    public static void DestroyGameObjectChildren(GameObject go)
    {
        foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
        {
            if (trans == go.transform) continue;  // 不干自己
            trans.parent = null; // 清空父, 因为.Destroy非同步的
            GameObject.Destroy(trans.gameObject);
        }
    }

    public static List<T> Split<T>(string str, params char[] args)
    {
        List<T> retList = new List<T>();
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
    public static Shader FindShader(string shaderName)
    {
        Shader shader;
        if (!CacheShaders.TryGetValue(shaderName, out shader))
        {
            shader = Shader.Find(shaderName);
            CacheShaders[shaderName] = shader;
            if (shader == null)
                CBase.LogError("缺少Shader：{0}  ， 检查Graphics Settings的预置shader", shaderName);
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
                    CBase.LogError("Unsupport Type {0} in StrBytesToArray(), You can custom this.", typeCode);
                    CBase.Assert(false);
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
        TimeSpan ts = new TimeSpan(0, 0, seconds);
        string timeStr = string.Format("{0}{1}{2}{3}",
            ts.Days == 0 ? "" : ts.Days + "天",
            ts.Hours == 0 ? "" : ts.Hours + "小时",
            ts.Minutes == 0 ? "" : ts.Minutes + "分",
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

    public static DateTime GetDateTime(double unixTimeStamp)
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return origin.AddSeconds(unixTimeStamp);
    }

    /// <summary>
    /// Unix時間總秒數
    /// </summary>
    /// <returns></returns>
    public static uint GetUnixSeconds()
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        TimeSpan diff = DateTime.Now - origin;
        return (uint)diff.TotalSeconds;
    }

    public static UInt64 GetTimeEx()
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        TimeSpan diff = DateTime.Now - origin;
        return (UInt64)diff.TotalMilliseconds;
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

    public static void BeginRecordTime(string key)
    {
        RecordTime[RecordPos] = UnityEngine.Time.realtimeSinceStartup;
        RecordKey[RecordPos] = key;
        RecordPos++;
    }

    public static string EndRecordTime(bool printLog = true)
    {
        RecordPos--;
        double s = (UnityEngine.Time.realtimeSinceStartup - RecordTime[RecordPos]);
        if (printLog)
        {
            CBase.Log("[RecordTime] {0} use {1}s", RecordKey[RecordPos], s);
        }
        return string.Format("[RecordTime] {0} use {1}s.", RecordKey[RecordPos], s);
    }

    // 添加性能观察, 使用C#内置
    public static void AddWatch(Action del)
    {
        AddWatch("执行耗费时间: {0}s", del);
    }

    public static void AddWatch(string outputStr, Action del)
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start(); //  开始监视代码运行时间

        if (del != null)
        {
            del();
        }

        stopwatch.Stop(); //  停止监视
        TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
        //double seconds = timespan.TotalSeconds;  //  总秒数
        double millseconds = timespan.TotalMilliseconds;
        decimal seconds = (decimal)millseconds / 1000m;

        CBase.LogWarning(outputStr, seconds.ToString("F7")); // 7位精度
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

    public static void SetChild(Transform child, Transform parent)
    {
        child.parent = parent;
        ResetTransform(child);
    }
    public static void ResetTransform(UnityEngine.Transform transform)
    {
        transform.localPosition = UnityEngine.Vector3.zero;
        transform.localScale = UnityEngine.Vector3.one;
        transform.localEulerAngles = UnityEngine.Vector3.zero;
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

    public static void ResetUITween(UnityEngine.GameObject gameObj)
    {
        // 重置出现 移动动画
        foreach (UITweener tween in gameObj.GetComponentsInChildren<UITweener>())
        {
            tween.ResetToBeginning();
            tween.PlayForward();
        }
    }

    // 粒子特效比例缩放
    public static void ScaleParticleSystem(GameObject gameObj, float scale)
    {
        var notFind = true;
        foreach (ParticleSystem p in gameObj.GetComponentsInChildren<ParticleSystem>())
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

    public static void MoveAllCollidersToGameObject(GameObject srcGameObject, GameObject targetGameObject)
    {
        CBase.Assert(srcGameObject != targetGameObject);
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

    public static void CopyCollider2DToGameObject(Collider2D collider2d, GameObject targetGameObject)
    {
        if (collider2d is CircleCollider2D)
        {
            CircleCollider2D oldCircle = collider2d as CircleCollider2D;
            CircleCollider2D newCircle = targetGameObject.AddComponent<CircleCollider2D>();
            newCircle.isTrigger = oldCircle.isTrigger;
            newCircle.radius = oldCircle.radius;

            Vector3 realLocalPos = targetGameObject.transform.InverseTransformPoint(oldCircle.gameObject.transform.position);
            newCircle.center = oldCircle.center + (Vector2)realLocalPos;
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
        }
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
    public static bool Probability(int chancePercent)
    {
        int chance = UnityEngine.Random.Range(1, 101);

        if (chancePercent <= 0)
            chancePercent = 100; // 必中概率

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
	/// <param name="nAngle">角度</param>
	/// <param name="nRadius">半径</param>
	/// <returns></returns>
	public static Vector3[] GetSmartNpcPoints(int nNum, Vector3 pAnchorPos, int nAngle, float nRadius)
	{
		bool bPlural = nNum % 2 == 0 ? true : false; // 是否复数模式
		Vector3 vDir = Vector3.down.normalized;
		int nMidNum = bPlural ? nNum / 2 : nNum / 2 + 1; // 中间数, 循环过了中间数后，另一个方向起排布
		Vector3 vRPos = vDir * nRadius; //// 计算直线在圆形上的顶点 半径是表现距离
		Vector3[] targetPos = new Vector3[nNum];
		for (int i = 1; i <= nNum; i++)
		{
			int nAddAngle = 0;

			if (bPlural) // 复数模式
			{
				if (i > nMidNum)
					nAddAngle = nAngle * ((i % nMidNum) + 1) - nAngle / 2;
				else
					nAddAngle = -nAngle * ((i % nMidNum) + 1) + nAngle / 2; // 除以2，是为了顶端NPC均匀排布 by KK
			}
			else // 单数模式
			{
				// 判断是否过了中间数
				if (i > nMidNum)
				{
					nAddAngle = nAngle * ((i % nMidNum) + 1); // 添加NPC的角度
				}
				else if (i < nMidNum) // 非复数模式， 中间数NPC 放在正方向
				{
					nAddAngle = -nAngle * ((i % nMidNum) + 1); // 反方向角度
				}
				else
					nAddAngle = 0; // 正方向
			}

			Vector3 vTargetPos = pAnchorPos + Quaternion.AngleAxis(nAddAngle, Vector3.forward) * vRPos;
			targetPos[i - 1] = vTargetPos;
		}
		return targetPos;
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
            CBase.Assert(index < MaxCount);
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