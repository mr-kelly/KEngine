#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
//
// Filename: KUGUIDemoMain.cs
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

using System.Collections;
using System.IO;
using System.Text;
using AppSettings;
using KEngine;
using KEngine.CoreModules;
using KEngine.UI;
using UnityEngine;

public class KUGUIDemoMain : MonoBehaviour
{
    private void Awake()
    {
    }

    IEnumerator Start()
    {
        //KGameSettings.Instance.InitAction += OnGameSettingsInit;

        var engine = KEngine.AppEngine.New(
            gameObject,
            null,
            new IModuleInitable[]
            {
                //KGameSettings.Instance,
//                UIModule.Instance,
            });

        while (!engine.IsInited)
            yield return null;

        var uiName = "DemoHome";
        UIModule.Instance.OpenWindow(uiName);

        Debug.Log("[SettingModule]Table: " + string.Join(",", ExampleSettings.TabFilePaths));

        foreach (ExampleSetting exampleInfo in ExampleSettings.GetAll())
        {
            Debug.Log(string.Format("Name: {0}", exampleInfo.Name));
            Debug.Log(string.Format("Number: {0}", exampleInfo.Number));
        }
        var info = ExampleSettings.Get("A_1024");
        Debuger.Assert(info.Name == "Test1");
        var info2 = SubdirExample2Settings.Get(2);
        Debuger.Assert(info2.Name == "Test2");

        var info3 = AppConfigSettings.Get("Test.Cat1");
        Debuger.Assert(info3.Value == "Cat1");

        ExampleSettings.OnReload = () =>
        {
            var reloadedInfo = ExampleSettings.Get("C_9888");
            Log.Info("Reload ExampleInfos! Now info: {0} -> {1}", "C_9888", reloadedInfo.Name);
        };




        Log.Info("Start reading streamingAssets Test...");
        UIModule.Instance.CallUI(uiName, (ui, args) =>
        {
            var tip = string.Format("Reading from streamingAssets, content: {0}", Encoding.UTF8.GetString(KResourceModule.LoadSyncFromStreamingAssets("TestFile.txt")));
            var demoHome = ui as KUIDemoHome;
            demoHome.TipLabel.text += tip;
            // Do some UI stuff
        });

    }
    //private void OnGameSettingsInit()
    //{
    //    KGameSettings _ = KGameSettings.Instance;

    //    Log.Info("Begin Load tab file...");

    //    var tabContent =
    //        File.ReadAllText(Application.dataPath + "/" + KEngine.AppEngine.GetConfig("ProductRelPath") +
    //                         "/Setting/test_tab.bytes");
    //    _.LoadTab<CTestTabInfo>(tabContent);
    //    Log.Info("Output the tab file...");
    //    foreach (CTestTabInfo info in _.GetInfos<CTestTabInfo>())
    //    {
    //        Log.Info("Id:{0}, Name: {1}", info.Id, info.Name);
    //    }
    //}
}

public class CTestTabInfo : CBaseInfo
{
    // Id auto inherit
    public string Name;
}
