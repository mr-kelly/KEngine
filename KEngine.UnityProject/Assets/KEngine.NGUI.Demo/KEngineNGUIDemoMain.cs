#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KEngineNGUIDemoMain.cs
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
using KEngine;
using KEngine.CoreModules;
using KEngine.ResourceDep;
using KEngine.UI;
using UnityEngine;

public class KEngineNGUIDemoMain : MonoBehaviour
{
    // Use this for initialization
    private IEnumerator Start()
    {
        //KGameSettings.Instance.InitAction += OnGameSettingsInit;

        var app = KEngine.AppEngine.New(
            gameObject,
            new IModule[]
            {
                KUIModule.Instance,
                //KGameSettings.Instance,
                
            },
            null,
            null);
        while (!app.IsInited)
            yield return null;

        //TestLoadLevelAdditiveAsync();

        KUIModule.Instance.OpenWindow("Test");

        KUIModule.Instance.CallUI("Test", (ui, _) =>
        {
            // Do some UI stuff
        });

        yield return new WaitForSeconds(2f);
        KLogger.Log("Opening KUITestSubWindow"); 
        KUIModule.Instance.OpenWindow("TestSub");

    }

    void TestLoadLevelAdditiveAsync()
    {
        KLogger.Log("Load Scene");
        ResourceDepUtils.LoadLevelAdditiveAsync("BundleResources/NGUI/TestNGUI.unity");
    }

    // Update is called once per frame
    private void Update()
    {
    }
}
