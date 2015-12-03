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

using System.IO;
using KEngine;
using KEngine.CoreModules;
using UnityEngine;

public class KUGUIDemoMain : MonoBehaviour
{
    private void Awake()
    {
        KGameSettings.Instance.InitAction += OnGameSettingsInit;

        KEngine.AppEngine.New(
            gameObject,
            new ICModule[]
            {
                //KGameSettings.Instance,
            },
            null,
            null);

        KUIModule.Instance.OpenWindow<KUIDemoHome>();

        KUIModule.Instance.CallUI<KUIDemoHome>(ui =>
        {
            // Do some UI stuff
        });
    }

    private void OnGameSettingsInit()
    {
        KGameSettings _ = KGameSettings.Instance;

        Logger.Log("Begin Load tab file...");

        //var tabContent = File.ReadAllText("Assets/" + Engine.GetConfig("ProductRelPath") + "/Setting/test_tab.bytes");
        //var path = KResourceModule.GetResourceFullPath("/Setting/test_tab.bytes");
        var tabContent =
            File.ReadAllText(Application.dataPath + "/" + KEngine.AppEngine.GetConfig("ProductRelPath") +
                             "/Setting/test_tab.bytes");
        _.LoadTab<CTestTabInfo>(tabContent);
        Logger.Log("Output the tab file...");
        foreach (CTestTabInfo info in _.GetInfos<CTestTabInfo>())
        {
            Logger.Log("Id:{0}, Name: {1}", info.Id, info.Name);
        }
    }
}

public class CTestTabInfo : CBaseInfo
{
    // Id auto inherit
    public string Name;
}