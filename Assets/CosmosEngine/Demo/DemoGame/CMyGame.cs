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
using UnityEngine;
using System.Collections;

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

        
        CUIManager.Instance.OpenWindow<CUIDemoHome>();
    }

    void OnGameSettingsInit()
    {
        CGameSettings _ = CGameSettings.Instance;

        CBase.Log("Begin Load tab file...");
        _.LoadTab<CTestTabInfo>("setting/test_tab.bytes");
        CBase.Log("Output the tab file...");
        foreach (CTestTabInfo info in _.GetInfos<CTestTabInfo>())
        {
            CBase.Log("Id:{0}, Name: {1}", info.Id, info.Name);
        }

    }
}

public class CTestTabInfo : CBaseInfo
{
    // Id auto inherit
    public string Name;
}
