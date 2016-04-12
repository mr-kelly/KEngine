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
            new IModule[]
            {
                //KGameSettings.Instance,
                KUIModule.Instance,
            },
            null,
            null);

        while (!engine.IsInited)
            yield return null;

        var uiName = "DemoHome";
        KUIModule.Instance.OpenWindow(uiName);

        KUIModule.Instance.CallUI(uiName, (ui, args) =>
        {
            // Do some UI stuff
        });

        Debug.Log("[SettingModule]Table: " + ExampleInfos.TabFilePath);
        foreach (ExampleInfo exampleInfo in ExampleInfos.GetAll())
        {
            Debug.Log(string.Format("Name: {0}", exampleInfo.Name));
            Debug.Log(string.Format("Number: {0}", exampleInfo.Number));
        }
        var info = ExampleInfos.GetByPrimaryKey("A_1024");
        Debuger.Assert(info.Name == "Test1");
        var info2 = SubdirExample2Infos.GetByPrimaryKey(2);
        Debuger.Assert(info2.Name == "Test2");
    }
    //private void OnGameSettingsInit()
    //{
    //    KGameSettings _ = KGameSettings.Instance;

    //    KLogger.Log("Begin Load tab file...");

    //    var tabContent =
    //        File.ReadAllText(Application.dataPath + "/" + KEngine.AppEngine.GetConfig("ProductRelPath") +
    //                         "/Setting/test_tab.bytes");
    //    _.LoadTab<CTestTabInfo>(tabContent);
    //    KLogger.Log("Output the tab file...");
    //    foreach (CTestTabInfo info in _.GetInfos<CTestTabInfo>())
    //    {
    //        KLogger.Log("Id:{0}, Name: {1}", info.Id, info.Name);
    //    }
    //}
}

public class CTestTabInfo : CBaseInfo
{
    // Id auto inherit
    public string Name;
}