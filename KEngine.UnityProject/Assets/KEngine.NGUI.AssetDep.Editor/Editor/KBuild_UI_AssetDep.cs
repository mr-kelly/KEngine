#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CBuild_UI_AssetDep.cs
// Date:     2015/12/03
// Author:  Kelly
// Email: 23110388@qq.com
// Github: https://github.com/mr-kelly/KEngine
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.

#endregion

using System.Text.RegularExpressions;
using KEngine.Editor;
using UnityEditor;
using UnityEngine;
#if NGUI
using KEngine.ResourceDep.Builder;
[InitializeOnLoad]
public class KBuild_NGUI_AssetDep
{
    static KBuild_NGUI_AssetDep()
    {
        KBuild_NGUI_ResourceDep.BeginExportEvent -= Custom_BeginExport;
        KBuild_NGUI_ResourceDep.BeginExportEvent += Custom_BeginExport;
        KBuild_NGUI_ResourceDep.EndExportEvent -= Custom_EndExport;
        KBuild_NGUI_ResourceDep.EndExportEvent += Custom_EndExport;

        KBuild_NGUI_ResourceDep.ExportCurrentUIEvent -= Custom_ExportCurrentUI;
        KBuild_NGUI_ResourceDep.ExportCurrentUIEvent += Custom_ExportCurrentUI;

        KBuild_NGUI_ResourceDep.ExportUIMenuEvent -= Custom_ExportUIMenu;
        KBuild_NGUI_ResourceDep.ExportUIMenuEvent += Custom_ExportUIMenu;
        //EndExportEvent -= Custom_
    }

    //private const string UILabelStringsFile = KI18NEditor.UI_LABEL_STRINGS_FILE;
    //private readonly KI18NItems UILabelStrings = new KI18NItems();
    //private readonly KI18NItems UILabelStrings2 = new KI18NItems();


    //readonly HashSet<string> UILabelStrings = new HashSet<string>();
    //readonly HashSet<string> UILabelStrings2 = new HashSet<string>();  // 为什么要弄两个？ 一个用来记录本次打包的字符串，如果全局打包，到最后判断，哪些是已经被删掉不用的！
    private KTabFile UIStringsFile;

    private static readonly Regex TemplateRegex = new Regex(@"\{(.+)\}");

    // 针对当前打开的单个UI
    static void Custom_ExportCurrentUI(KBuild_NGUI_ResourceDep uiBuilder, string uiScenepath, string uiName,
        GameObject objToBuild)
    {
        var info = ResourceDepBuilder.Build(objToBuild);

        //foreach (var path in info.DepAssetPaths)
        //{
        //    Debug.Log(path);
        //}

        //bool reBuildPanel = KAssetVersionControl.TryCheckNeedBuildWithMeta(uiScenepath);

        //KDependencyBuild.BuildGameObject(objToBuild, KBuild_NGUI_ResourceDep.GetBuildRelPath(uiName), reBuildPanel);

        //if (reBuildPanel)
        //    KAssetVersionControl.TryMarkBuildVersion(uiScenepath);

        //_FindLabelLocalization(uiBuilder);
    }


    static void Custom_BeginExport(KBuild_NGUI_ResourceDep uiBuilder)
    {
        // 读取字符串
        //uiBuilder.UILabelStrings.Clear();
        //if (File.Exists(UILabelStringsFile))
        //{
        //    uiBuilder.UIStringsFile = KTabFile.LoadFromFile(UILabelStringsFile);
        //    for (int row = 1; row < uiBuilder.UIStringsFile.GetHeight(); ++row)
        //    {
        //        // 获取当前UI名~
        //        uiBuilder.UILabelStrings.Add(uiBuilder.UIStringsFile.GetString(row, "_String_"), uiBuilder.UIName);
        //    }
        //}
        //else
        //{
        //    uiBuilder.UIStringsFile = new KTabFile();
        //    uiBuilder.UIStringsFile.NewColumn("_String_");
        //}
    }

    // big export
    private static void Custom_EndExport(KBuild_NGUI_ResourceDep uiBuilder)
    {
        ResourceDepBuilder.Clear();
        //KI18NItems exportHashSet;
        //if (uiBuilder.IsBuildAll)
        //{
        //    exportHashSet = uiBuilder.UILabelStrings2;  // 使用全局的，精确的
        //}
        //else
        //    exportHashSet = uiBuilder.UILabelStrings;  // 在原来的基础上

        //int srcFileRowCount = uiBuilder.UIStringsFile.GetHeight();
        //uiBuilder.UIStringsFile = new KTabFile();
        //uiBuilder.UIStringsFile.NewColumn("_String_");
        //uiBuilder.UIStringsFile.NewColumn("_Sources_");

        ////int rowCount = 1;
        //foreach (var kv in exportHashSet.Items)
        //{
        //    var rowCount = uiBuilder.UIStringsFile.NewRow();
        //    uiBuilder.UIStringsFile.SetValue<string>(rowCount, "_String_", kv.Key);
        //    uiBuilder.UIStringsFile.SetValue<string>(rowCount, "_Sources_", string.Join("|", kv.Value.ToArray()));
        //}
        //uiBuilder.UIStringsFile.Save(UILabelStringsFile);
    }

    private static void _FindLabelLocalization(KBuild_NGUI_ResourceDep uiBuilder)
    {
        // 收集UiLabel的字符串
        //if (uiBuilder.WindowObject.GetComponent<UIPanel>() == null)
        {
            // 读取UIPanel的depth, 遍历所有UI控件，将其depth加上UIPanel Depth， 以此设置层级关系
            // 如PanelRoot Depth填10,  子控件填0,1,2   打包后，子控件层级为 10 << 5 | 1 = 320, 321, 322
            //foreach (UIWidget uiWidget in uiBuilder.WindowObject.GetComponentsInChildren<UIWidget>(true))
            //{
            //uiWidget.depth = (uiBuilder.PanelRoot.depth + 15) << 5 | (uiWidget.depth + 15);  // + 15是为了杜绝负数！不要填-15以上的
            //}

            // UILabel 多语言
            //foreach (UILabel uiLabel in uiBuilder.WindowObject.GetComponentsInChildren<UILabel>(true))
            //{
            //    //uiLabel.depth = (uiBuilder.PanelRoot.depth + 15) << 5 | (uiLabel.depth + 15);

            //    // 收集字符串
            //    var match = TemplateRegex.Match(uiLabel.text);
            //    if (match.Groups.Count > 1)  // 捕获组1， 中括号内
            //    {
            //        var val = match.Groups[1].Value.Trim();
            //        if (!string.IsNullOrEmpty(val))
            //        {
            //            uiBuilder.UILabelStrings.Add(val, uiBuilder.UIName);
            //            uiBuilder.UILabelStrings2.Add(val, uiBuilder.UIName);
            //        }
            //    }
            //    //if (!uiLabel.text.StartsWith("_"))

            //}
        }
    }

    static void Custom_ExportUIMenu()
    {
        KDependencyBuild.Clear();
    }
}
#endif