//-------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//-------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Load from Local hard disk or 
/// Load from a AssetBundles..
/// </summary>
[CDependencyClass(typeof(CResourceManager))]
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
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
            case RuntimePlatform.IPhonePlayer:
            case RuntimePlatform.WP8Player:
                SettingOutPackage = false;
                break;
        }

		CBase.Log("Load setting out of package = {0}", SettingOutPackage.ToString());
		yield return CResourceManager.Instance.StartCoroutine(InitSetting());
	}

    public IEnumerator UnInit()
    {
        yield break;
    }

	IEnumerator InitSetting()
	{
		XAssetLoader assetLoader = new XAssetLoader("GameSetting" + CCosmosEngine.GetConfig("AssetBundleExt"), null);
		while (!assetLoader.IsFinished)
			yield return null;

        CGameSettingFiles gameSetting = (CGameSettingFiles)assetLoader.Asset;

		for (int i = 0; i < gameSetting.SettingFiles.Length; ++i)
		{
			GameSettings.Add(gameSetting.SettingFiles[i], gameSetting.SettingContents[i]);
		}

		CBase.Log("{0} setting files loaded.", GameSettings.Count);

		Object.Destroy(gameSetting);
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
			CBase.LogError("Setting not fount, {0}", path);
			return null;
		}

		return content;
	}

    // 仅在PC版可用
    string LoadSettingOutPackage(string path)
	{
		string fullPath = CResourceManager.ApplicationPath + path;
        fullPath = fullPath.Replace(CResourceManager.GetFileProtocol(), "");

        System.Text.Encoding encoding = System.Text.Encoding.UTF8;

        return System.IO.File.ReadAllText(fullPath, encoding);
	}
}
