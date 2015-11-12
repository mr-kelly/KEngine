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
using KFramework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Load from Local hard disk or 
/// Load from a AssetBundles..
/// </summary>
[CDependencyClass(typeof(CResourceModule))]
public class CSettingManager : ICModule
{
    static CSettingManager _Instance;
    public static CSettingManager Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = new CSettingManager();
            return _Instance;
        }
    }
    private CSettingManager() { }
	public Dictionary<string, string> GameSettings = new Dictionary<string, string>();
	bool SettingOutPackage = true;

	public bool LoadFinished = false;
	public IEnumerator Init()
	{
	    if (LoadFinished)
	    {
            if (Debug.isDebugBuild)
	            CDebug.LogWarning("[CSettingMananger]重新加载Settings");
	        LoadFinished = false;
	    }

        switch (Application.platform)
        {
            case RuntimePlatform.Android:
            case RuntimePlatform.IPhonePlayer:
            case RuntimePlatform.WP8Player:
                SettingOutPackage = false;
                break;
        }

		CDebug.Log("Load setting out of package = {0}", SettingOutPackage.ToString());

        yield return CResourceModule.Instance.StartCoroutine(InitSetting());
		
	}

    public IEnumerator UnInit()
    {
        yield break;
    }

	IEnumerator InitSetting()
	{
		var assetLoader = CStaticAssetLoader.Load("GameSetting" + CCosmosEngine.GetConfig("AssetBundleExt"), null);
		while (!assetLoader.IsFinished)
			yield return null;

        CGameSettingFiles gameSetting = (CGameSettingFiles)assetLoader.TheAsset;

		for (int i = 0; i < gameSetting.SettingFiles.Length; ++i)
		{
			GameSettings[gameSetting.SettingFiles[i]] = gameSetting.SettingContents[i];
		}

		CDebug.Log("{0} setting files loaded.", GameSettings.Count);

		Object.Destroy(gameSetting);
	    assetLoader.Release();
		LoadFinished = true;
	}

	public string LoadSetting(string path)
	{
		if (SettingOutPackage)
            return LoadSettingOutPackage(path);   // WWW读取模式
        else
            return LoadSettingInPackage(path);  // scriptableObject获取
	}

    string LoadSettingInPackage(string path)
	{
		string content;
		bool result = GameSettings.TryGetValue(path, out content);
		if (!result)
		{
			CDebug.LogError("Setting not fount, {0}", path);
			return null;
		}

		return content;
	}

    // 仅在PC版可用
    string LoadSettingOutPackage(string path)
	{
		string fullPath = CResourceModule.ApplicationPath + path;
        fullPath = fullPath.Replace(CResourceModule.GetFileProtocol(), "");

        System.Text.Encoding encoding = System.Text.Encoding.UTF8;

        return System.IO.File.ReadAllText(fullPath, encoding);
	}
}
