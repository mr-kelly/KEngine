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
#if !UNITY_5
using KEngine.ResourceDep;
#endif
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
            null,
            new IModuleInitable[]
            {
//                UIModule.Instance,
                //KGameSettings.Instance,

            });
        while (!app.IsInited)
            yield return null;

        //TestLoadLevelAdditiveAsync();

        UIModule.Instance.OpenWindow("Test");

        UIModule.Instance.CallUI("Test", (ui, _) =>
        {
            // Do some UI stuff
        });

        yield return new WaitForSeconds(2f);
        Log.Info("Opening KUITestSubWindow");
        UIModule.Instance.OpenWindow("TestSub");

    }

    void TestLoadLevelAdditiveAsync()
    {
#if !UNITY_5
        Log.Info("Load Scene");
        ResourceDepUtils.LoadLevelAdditiveAsync("BundleResources/NGUI/TestNGUI.unity");
#else
        Log.LogError("Not support on Unity 5.x");
#endif
    }

    // Update is called once per frame
    private void Update()
    {
    }
}
