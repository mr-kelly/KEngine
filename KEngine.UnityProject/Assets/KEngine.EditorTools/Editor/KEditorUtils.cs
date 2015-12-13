#region Copyright(c) Kingsoft Xishanju

// Company: Kingsoft Xishanju
// Filename: KEditorUtils.cs
// Date:     2015/11/07
// Author:   Kelly / chenpeilin1
// Email: chenpeilin1@kingsoft.com / 23110388@qq.com

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
            Assembly assembly = Assembly.GetAssembly(typeof(ActiveEditorTracker));
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
                string cmd;
                string preArg;
                var os = Environment.OSVersion;

                Debug.Log(string.Format("[ExecuteCommand]Command on OS: {0}", os.ToString()));
                if (os.ToString().Contains("Windows"))
                {
                    cmd = "cmd.exe";
                    preArg = "/C ";
                }
                else
                {
                    cmd = "sh";
                    preArg = "-c ";
                }
                Debug.Log("[ExecuteCommand]" + command);
                var allOutput = new StringBuilder();
                using (var process = new System.Diagnostics.Process())
                {
                    if (workingDirectory != null)
                        process.StartInfo.WorkingDirectory = workingDirectory;
                    process.StartInfo.FileName = cmd;
                    process.StartInfo.Arguments = preArg + "\"" + command + "\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
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

                    var err = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(err))
                    {
                        Debug.LogError(string.Format("[ExecuteCommand] {0}", err));
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

        public delegate void EachDirectoryDelegate(string fileFullPath, string fileRelativePath);

        /// <summary>
        /// 递归一个目录所有文件，callback
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="eachCallback"></param>
        public static void EachDirectoryFiles(string dirPath, EachDirectoryDelegate eachCallback)
        {
            foreach (var filePath in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
            {
                var fileRelativePath = filePath.Replace(dirPath, "");
                if (fileRelativePath.StartsWith("/") || fileRelativePath.StartsWith("\\"))
                    fileRelativePath = fileRelativePath.Substring(1, fileRelativePath.Length - 1);

                var cleanFilePath = filePath.Replace("\\", "/");
                fileRelativePath = fileRelativePath.Replace("\\", "/");
                eachCallback(cleanFilePath, fileRelativePath);
            }
        }
        /// <summary>
        /// 将丑陋的windows路径，替换掉\字符
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetCleanPath(string path)
        {
            return path.Replace("\\", "/");
        }

        /// <summary>
        /// 监视一个目录，如果有修改则触发事件函数, 包含其子目录！
        /// <para>使用更大的buffer size确保及时触发事件</para>
        /// <para>不用includesubdirect参数，使用自己的子目录扫描，更稳健</para>
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="handler"></param>
        /// <param name="includeSubdirectories">是否包含子目录</param>
        /// <returns></returns>
        public static FileSystemWatcher DirectoryWatch(string dirPath, FileSystemEventHandler handler, bool includeSubdirectories = false)
        {
            var watcher = new FileSystemWatcher();
            watcher.IncludeSubdirectories = includeSubdirectories;
            watcher.Path = dirPath;
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
            watcher.Filter = "*";
            watcher.Changed += handler;
            watcher.EnableRaisingEvents = true;
            watcher.InternalBufferSize = 10240;
            return watcher;
        }

        /// <summary>
        /// 在指定目录中搜寻字符串并返回匹配}
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="searchWord"></param>
        /// <param name="fileFilter"></param>
        /// <returns></returns>
        public static Dictionary<string, List<Match>> FindStrMatchesInFolderTexts(string sourceFolder, Regex searchWord,
            Func<string, bool> fileFilter = null)
        {
            var retMatches = new Dictionary<string, List<Match>>();
            var allFiles = new List<string>();
            AddFileNamesToList(sourceFolder, allFiles);
            foreach (string fileName in allFiles)
            {
                if (fileFilter != null && !fileFilter(fileName))
                    continue;

                retMatches[fileName] = new List<Match>();
                string contents = File.ReadAllText(fileName);
                var matches = searchWord.Matches(contents);
                if (matches.Count > 0)
                {
                    for (int i = 0; i < matches.Count; i++)
                    {
                        retMatches[fileName].Add(matches[i]);
                    }

                }
            }
            return retMatches;
        }

        static void AddFileNamesToList(string sourceDir, List<string> allFiles)
        {

            string[] fileEntries = Directory.GetFiles(sourceDir);
            foreach (string fileName in fileEntries)
            {
                allFiles.Add(fileName);
            }

            //Recursion    
            string[] subdirectoryEntries = Directory.GetDirectories(sourceDir);
            foreach (string item in subdirectoryEntries)
            {
                // Avoid "reparse points"
                if ((File.GetAttributes(item) & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                {
                    AddFileNamesToList(item, allFiles);
                }
            }

        }
    }

}