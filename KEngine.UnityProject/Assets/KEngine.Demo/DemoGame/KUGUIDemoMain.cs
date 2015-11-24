//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Version 0.9.1 (20151010)
//                     Copyright © 2011-2015
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using System;
using UnityEngine;
using System.Collections;
using KEngine;
using System.IO;
using KEngine.CoreModules;

public class KUGUIDemoMain : MonoBehaviour
{
    void Awake()
    {
        KGameSettings.Instance.InitAction += OnGameSettingsInit;

        KEngine.AppEngine.New(
            gameObject, 
            new ICModule[] {
                //KGameSettings.Instance,
            },
            null,
            null);

        KUIModule.Instance.OpenWindow<KUIDemoHome>();

        KUIModule.Instance.CallUI<KUIDemoHome>(ui => {
            
            // Do some UI stuff

        });
    }

    void OnGameSettingsInit()
    {
        KGameSettings _ = KGameSettings.Instance;

        Logger.Log("Begin Load tab file...");

        //var tabContent = File.ReadAllText("Assets/" + Engine.GetConfig("ProductRelPath") + "/Setting/test_tab.bytes");
        //var path = KResourceModule.GetResourceFullPath("/Setting/test_tab.bytes");
        var tabContent = File.ReadAllText(Application.dataPath + "/" + KEngine.AppEngine.GetConfig("ProductRelPath") + "/Setting/test_tab.bytes");
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
