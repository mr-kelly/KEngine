#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KSettingManager.cs
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
using System.Collections.Generic;
using KEngine;
using UnityEngine;

/// <summary>
/// Load from Local hard disk or
/// Load from a AssetBundles..
/// </summary>
public class KSettingManager : KEngine.IModuleInitable
{
    private static KSettingManager _Instance;

    public static KSettingManager Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = new KSettingManager();
            return _Instance;
        }
    }

    private KSettingManager()
    {
    }

    public Dictionary<string, string> GameSettings = new Dictionary<string, string>();
    private bool SettingOutPackage = true;

    public bool LoadFinished = false;

    public IEnumerator Init()
    {
        if (LoadFinished)
        {
            if (Debug.isDebugBuild)
                Log.LogWarning("[CSettingMananger]重新加载Settings");
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

        Log.Info("Load setting out of package = {0}", SettingOutPackage.ToString());

        yield return KResourceModule.Instance.StartCoroutine(InitSetting());
    }

    public double InitProgress { get; private set; }

    public IEnumerator UnInit()
    {
        yield break;
    }

    private IEnumerator InitSetting()
    {
        var assetLoader = StaticAssetLoader.Load("GameSetting" + KEngine.AppEngine.GetConfig("KEngine", "AssetBundleExt"), null);
        while (!assetLoader.IsCompleted)
            yield return null;

        CGameSettingFiles gameSetting = (CGameSettingFiles) assetLoader.TheAsset;

        for (int i = 0; i < gameSetting.SettingFiles.Length; ++i)
        {
            GameSettings[gameSetting.SettingFiles[i]] = gameSetting.SettingContents[i];
        }

        Log.Info("{0} setting files loaded.", GameSettings.Count);

        Object.Destroy(gameSetting);
        assetLoader.Release();
        LoadFinished = true;
    }

    public string LoadSetting(string path)
    {
        if (SettingOutPackage)
            return LoadSettingOutPackage(path); // WWW读取模式
        else
            return LoadSettingInPackage(path); // scriptableObject获取
    }

    private string LoadSettingInPackage(string path)
    {
        string content;
        bool result = GameSettings.TryGetValue(path, out content);
        if (!result)
        {
            Log.LogError("Setting not fount, {0}", path);
            return null;
        }

        return content;
    }

    // 仅在PC版可用
    private string LoadSettingOutPackage(string path)
    {
        string fullPath = KResourceModule.ApplicationPath + path;
        fullPath = fullPath.Replace(KResourceModule.GetFileProtocol(), "");

        System.Text.Encoding encoding = System.Text.Encoding.UTF8;

        return System.IO.File.ReadAllText(fullPath, encoding);
    }
}