#region Copyright(c) Kingsoft Xishanju 

// Company: Kingsoft Xishanju
// Filename: KEditorUtils.cs
// Date:     2015/11/07
// Author:   Kelly / chenpeilin1
// Email: chenpeilin1@kingsoft.com / 23110388@qq.com

#endregion

using System;
using System.IO;
using System.Reflection;
using System.Text;
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
        /// <param name="workingDirectory"></param>
        public static void ExecuteCommand(string command, string workingDirectory = null)
        {
            var fProgress = .1f;
            EditorUtility.DisplayProgressBar("KEditorUtils.ExecuteCommand", command, fProgress);

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
                var allOutput = new StringBuilder();
                using (var process = new System.Diagnostics.Process())
                {
                    if (workingDirectory != null)
                        process.StartInfo.WorkingDirectory = workingDirectory;
                    process.StartInfo.FileName = cmdName;
                    process.StartInfo.Arguments = preArg + "\"" + command + "\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();

                    while (true)
                    {
                        var line = process.StandardOutput.ReadLine();
                        if (line == null)
                            break;
                        allOutput.AppendLine(line);
                        EditorUtility.DisplayProgressBar("[ExecuteCommand] " + command, line, fProgress);
                        fProgress += .001f;
                    }

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