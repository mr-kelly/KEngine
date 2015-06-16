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
using System.IO;
using System.Diagnostics;
using System.Text;
using UnityEngine;

/// Frequent Used,
/// A File logger + Debug Tools
public class CDebug
{
    private static bool _isLogFile = false; // 是否輸出到日誌，跨线程
    public static bool IsLogFile
    {
        get { return _isLogFile; }
        set
        {
            _isLogFile = value;
            if (_isLogFile)
            {
                Application.RegisterLogCallbackThreaded((szMsg, trace, type) =>
                {
                    LogToFile(szMsg + trace + "\n\n");
                });
            }
            else
            {
                Application.RegisterLogCallbackThreaded(null);
            }

        }
    }

    static readonly bool IsDebugBuild = false;
    public static readonly bool IsEditor = false;

    public static event Action<string> LogErrorEvent;

    static CDebug()
    {
        // isDebugBuild先预存起来，因为它是一个get_属性, 在非Unity主线程里不能用，导致多线程网络打印log时报错

        try
        {
            IsDebugBuild = UnityEngine.Debug.isDebugBuild;
            IsEditor = Application.isEditor;
        }
        catch (Exception e)
        {
            CDebug.LogConsole_MultiThread("CDebug Static Constructor Failed!");
            CDebug.LogConsole_MultiThread(e.Message + " , " + e.StackTrace);
        }
    }

    enum LogType
    {
        NORMAL,
        WARNING,
        ERROR,
    }

    /// <summary>
    /// Check if a object null
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="formatStr"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static bool Check(object obj, string formatStr = null, params object[] args)
    {
        if (obj != null) return true;

        if (string.IsNullOrEmpty(formatStr))
            formatStr = "[Check Null] Failed!";

        LogError("[!!!]" + formatStr, args);
        return false;
    }

    public static void Assert(bool result)
    {
        if (result)
            return;

        LogErrorWithStack("Assertion Failed!", 2);

        throw new Exception("Assert"); // 中断当前调用
    }

    public static void Assert(int result)
    {
        Assert(result != 0);
    }

    public static void Assert(Int64 result)
    {
        Assert(result != 0);
    }

    public static void Assert(object obj)
    {
        Assert(obj != null);
    }

    // 这个使用系统的log，这个很特别，它可以再多线程里用，其它都不能再多线程内用！！！
    public static void LogConsole_MultiThread(string log, params object[] args)
    {
        if (IsEditor)
            Log(log, args);
        else
            Console.WriteLine(log, args);
    }

    public static void DevLog(string log, params object[] args)
    {
        if (IsDebugBuild)
            DoLog(string.Format(log, args), LogType.WARNING);
    }

    public static void Log(string log)
    {
        DoLog(log, LogType.NORMAL);
    }
    public static void Log(string log, params object[] args)
    {
        DoLog(string.Format(log, args), LogType.NORMAL);
    }

    public static void Logs(params object[] logs)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < logs.Length; ++i)
        {
            sb.Append(logs[i].ToString());
            sb.Append(", ");
        }
        Log(sb.ToString());
    }

    public static void LogException(Exception e)
    {
        var sb = new StringBuilder();
        sb.AppendFormat("Exception: {0}", e.Message);
        if (e.InnerException != null)
            sb.AppendFormat(" InnerException: {0}", e.InnerException.Message);

        LogErrorWithStack(sb.ToString() + " , " + e.StackTrace);
    }

    public static void LogErrorWithStack(string err = "", int stack = 1)
    {
        StackFrame[] stackFrames = new StackTrace(true).GetFrames(); ;
        StackFrame sf = stackFrames[stack];
        string log = string.Format("[ERROR]{0}\n\n{1}:{2}\t{3}", err, sf.GetFileName(), sf.GetFileLineNumber(), sf.GetMethod());
        Console.Write(log);
        DoLog(log, LogType.ERROR);

        if (LogErrorEvent != null)
            LogErrorEvent(err);
    }

    public static void LogError(string err, params object[] args)
    {
        LogErrorWithStack(string.Format(err, args), 2);
    }

    public static void LogWarning(string err, params object[] args)
    {
        string log = string.Format(err, args);
        DoLog(log, LogType.WARNING);
    }

    public static void Pause()
    {
        UnityEngine.Debug.Break();
    }

    private static void DoLog(string szMsg, LogType emType)
    {
        szMsg = string.Format("[{0}]{1}\n\n=================================================================\n\n", DateTime.Now.ToString("HH:mm:ss.ffff"), szMsg);

        switch (emType)
        {
            case LogType.NORMAL:
                UnityEngine.Debug.Log(szMsg);
                break;
            case LogType.WARNING:
                UnityEngine.Debug.LogWarning(szMsg);
                break;
            case LogType.ERROR:
                UnityEngine.Debug.LogError(szMsg);
                break;
        }

    }

    public static void LogToFile(string szMsg)
    {
        LogToFile(szMsg, true); // 默认追加模式
    }

    // 是否写过log file
    public static bool HasLogFile()
    {
        string fullPath = GetLogPath();
        return File.Exists(fullPath);
    }

    // 写log文件
    public static void LogToFile(string szMsg, bool append)
    {
        string fullPath = GetLogPath();
        string dir = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using (FileStream fileStream = new FileStream(fullPath, append ? FileMode.Append : FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))  // 不会锁死, 允许其它程序打开
        {
            StreamWriter writer = new StreamWriter(fileStream);  // Append
            writer.Write(szMsg);
            writer.Flush();
            writer.Close();
        }
    }

    // 用于写日志的可写目录
    public static string GetLogPath()
    {
        string logPath;

        if (IsEditor)
            logPath = "logs/";
        else
            logPath = UnityEngine.Application.persistentDataPath + "/" + "logs/";

        var now = DateTime.Now;
        var logName = string.Format("game_{0}_{1}_{2}.log", now.Year, now.Month, now.Day);

        return logPath + logName;
    }

    #region Record Time
    static float[] RecordTime = new float[10];
    static string[] RecordKey = new string[10];
    static int RecordPos = 0;

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
            CDebug.Log("[RecordTime] {0} use {1}s", RecordKey[RecordPos], s);
        }
        return string.Format("[RecordTime] {0} use {1}s.", RecordKey[RecordPos], s);
    }

    // 添加性能观察, 使用C#内置
    public static void WatchPerformance(Action del)
    {
        WatchPerformance("执行耗费时间: {0}s", del);
    }

    public static void WatchPerformance(string outputStr, Action del)
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

        CDebug.LogWarning(outputStr, seconds.ToString("F7")); // 7位精度
    }
    #endregion

}
