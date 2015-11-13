#region Copyright(c) Kingsoft Xishanju 

// Company: Kingsoft Xishanju
// Filename: KEditorUtils.cs
// Date:     2015/11/07
// Author:   Kelly / chenpeilin1
// Email: chenpeilin1@kingsoft.com / 23110388@qq.com

#endregion

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace KUnityEditorTools
{
    /// <summary>
    /// Shell / cmd / 等等常用Editor需要用到的工具集
    /// </summary>
    public class KEditorUtils
    {
        /// <summary>
        /// 清除Console log
        /// </summary>
        public static void ClearConsoleLog()
        {
            Assembly assembly = Assembly.GetAssembly(typeof (ActiveEditorTracker));
            Type type = assembly.GetType("UnityEditorInternal.LogEntries");
            MethodInfo method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }

        /// <summary>
        /// 执行批处理命令
        /// </summary>
        /// <param name="command"></param>
        public static void ExecuteCommand(string command)
        {
            EditorUtility.DisplayProgressBar("KEditorUtils.ExecuteCommand", command, .5f);

            try
            {
                var cmdName = "cmd.exe";
                var preArg = "/C ";
                var os = Environment.OSVersion;
                if (os.ToString().Contains("Windows"))
                {
                    cmdName = "cmd.exe";
                    preArg = "/C ";
                }
                else if (os.ToString().Contains("Unix"))
                {
                    cmdName = "sh";
                    preArg = "-c ";
                }
                else
                {
                    Debug.LogError(string.Format("[ExecuteCommand]Error on OS: {0}", os.ToString()));
                }

                Debug.Log("[ExecuteCommand]" + command);
                string allOutput = null;
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = cmdName;
                    process.StartInfo.Arguments = preArg + "\"" + command + "\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();

                    allOutput = process.StandardOutput.ReadToEnd();

                    process.WaitForExit();
                }
                Debug.Log("[ExecuteResult]" + allOutput);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}