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

public class CMyGame : MonoBehaviour
{
    void Awake()
    {
        CGameSettings.Instance.InitAction += OnGameSettingsInit;

        KEngine.AppEngine.New(
            gameObject,
            new ICModule[] {
                CGameSettings.Instance,
            },
            null,
            null);

        CUIModule.Instance.OpenWindow<CUIDemoHome>();

        CUIModule.Instance.CallUI<CUIDemoHome>(ui => {
            
            // Do some UI stuff

        });
    }

    void OnGameSettingsInit()
    {
        CGameSettings _ = CGameSettings.Instance;

        Logger.Log("Begin Load tab file...");

        //var tabContent = File.ReadAllText("Assets/" + Engine.GetConfig("ProductRelPath") + "/setting/test_tab.bytes");
        //var path = CResourceModule.GetResourceFullPath("/setting/test_tab.bytes");
        var tabContent = File.ReadAllText(Application.dataPath + "/" + KEngine.AppEngine.GetConfig("ProductRelPath") + "/setting/test_tab.bytes");
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
