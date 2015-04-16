//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                     Version 0.8 (20140904)
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------

using System;
using UnityEngine;
using System.Collections;
using CosmosEngine;
using System.IO;

public class CMyGame : MonoBehaviour
{
    void Awake()
    {
        CGameSettings.Instance.InitAction += OnGameSettingsInit;

        CCosmosEngine.New(
            gameObject,
            new ICModule[] {
                CGameSettings.Instance,
            },
            null,
            null);

        CUIModule.Instance.OpenWindow<CUIDemoHome>();
    }

    void OnGameSettingsInit()
    {
        CGameSettings _ = CGameSettings.Instance;

        CDebug.Log("Begin Load tab file...");

        //var tabContent = File.ReadAllText("Assets/" + CCosmosEngine.GetConfig("ProductRelPath") + "/setting/test_tab.bytes");
        //var path = CResourceModule.GetResourceFullPath("/setting/test_tab.bytes");
        var tabContent = File.ReadAllText(Application.dataPath + "/" + CCosmosEngine.GetConfig("ProductRelPath") + "/setting/test_tab.bytes");
        _.LoadTab<CTestTabInfo>(tabContent);
        CDebug.Log("Output the tab file...");
        foreach (CTestTabInfo info in _.GetInfos<CTestTabInfo>())
        {
            CDebug.Log("Id:{0}, Name: {1}", info.Id, info.Name);
        }

    }
}

public class CTestTabInfo : CBaseInfo
{
    // Id auto inherit
    public string Name;
}
