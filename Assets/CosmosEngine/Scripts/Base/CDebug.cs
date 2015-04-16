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
using UnityEngine;

/// Frequent Used,
/// A File logger + Debug Tools
public class CDebug
{
    public static bool IsLogFile = false; // 是否輸出到日誌

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
        LogErrorWithStack(e.Message + " , " + e.StackTrace);
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
        szMsg = string.Format("[{0}]{1}\n\n=================================================================", DateTime.Now.ToString("HH:mm:ss.ffff"), szMsg);

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

        LogToFile("game.log", szMsg);
    }

    public static void LogToFile(string logPath, string szMsg)
    {
        LogToFile(logPath, szMsg, true); // 默认追加模式
    }

    // 是否写过log file
    public static bool HasLogFile(string logFile)
    {
        if (IsLogFile)
        {
            string fullPath = GetLogPath() + logFile;
            if (File.Exists(fullPath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        return false;
    }

    // 写log文件
    public static void LogToFile(string logFile, string szMsg, bool append)
    {
        if (IsLogFile)  //  开发者模式true:写log IO文件+响应服务器log
        {
            string fullPath = GetLogPath() + logFile;
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
    }

    // 用于写日志的可写目录
    public static string GetLogPath()
    {
        string logPath;

        if (IsEditor)
            logPath = "logs/";
        else
		    logPath = UnityEngine.Application.persistentDataPath + "/" + "logs/";

        return logPath;
    }
}
