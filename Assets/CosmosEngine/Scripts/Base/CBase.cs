//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                          Version 0.8
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using System;
using System.IO;
using System.Diagnostics;

/// Frequent Used,
/// A File logger + Debug Tools
public class CBase
{
    static bool IsLogFile = false; // 是否輸出到日誌

    static bool IsDebugBuild;

    static CBase()
    {
        // isDebugBuild先预存起来，因为它是一个get_属性, 在非Unity主线程里不能用，导致多线程网络打印log时报错
        IsDebugBuild = UnityEngine.Debug.isDebugBuild;
    }

    enum XLogType
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
		if (result != 0)
			return;
		LogErrorWithStack("Assertion Failed!", 2);
		throw new Exception("Assert"); // 中断当前调用
	}

	public static void Assert(Int64 result)
	{
		if (result != 0)
			return;
		LogErrorWithStack("Assertion Failed!", 2);
		throw new Exception("Assert"); // 中断当前调用
	}

	public static void Assert(object obj)
	{
		if (obj != null)
			return;
		LogErrorWithStack("Assertion Failed!", 2);
		throw new Exception("Assert"); // 中断当前调用
	}

    // 这个使用系统的log，这个很特别，它可以再多线程里用，其它都不能再多线程内用！！！
    public static void LogConsole_MultiThread(string log, params object[] args)
    {
#if UNITY_EDITOR || UNITY_STANDLONE
        Log(log, args);
#else
        Console.WriteLine(log, args);
#endif
    }

	public static void Log(string log)
	{
		log = string.Format("[{0}] {1}\n\n===============================================================================\n\n", DateTime.Now.ToString("HH:mm:ss"), log);
        DoLog(log, XLogType.NORMAL);
	}

	public static void Log(string log, params object[] args)
	{
		Log(string.Format(log, args));
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

	public static void LogErrorWithStack(string err = "", int stack = 1)
	{
		StackFrame[] stackFrames = new StackTrace(true).GetFrames(); ;
		StackFrame sf = stackFrames[stack];
		string log = string.Format("[ERROR]{0}\n\n{1}:{2}\t{3}", err, sf.GetFileName(), sf.GetFileLineNumber(), sf.GetMethod());
		Console.Write(log);
        DoLog(log, XLogType.ERROR);
	}

	public static void LogError(string err, params object[] args)
	{
		LogErrorWithStack(string.Format(err, args), 2);
	}

	public static void LogWarning(string err, params object[] args)
	{
		string log = string.Format(err, args);
        DoLog(log, XLogType.WARNING);
	}

	public static void Pause()
	{
		UnityEngine.Debug.Break();
	}

    private static void DoLog(string szMsg, XLogType emType)
    {
        switch (emType)
        {
            case XLogType.NORMAL:
                UnityEngine.Debug.Log(szMsg);
                break;
            case XLogType.WARNING:
                UnityEngine.Debug.LogWarning(szMsg);
                break;
            case XLogType.ERROR:
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
        if (IsDebugBuild && IsLogFile)
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
        if (IsDebugBuild && IsLogFile)  //  开发者模式true:写log IO文件+响应服务器log
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
#if UNITY_EDITOR || UNITY_STANDALONE
        string logPath = "logs/";
#else
		string logPath = UnityEngine.Application.persistentDataPath + "/" + "logs/";
#endif
        return logPath;
    }
}
