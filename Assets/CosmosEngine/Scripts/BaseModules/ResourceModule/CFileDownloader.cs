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
using System;
using System.Collections;
using System.IO;

public class CWWWDownloader
{
    string _SavePath;
    public string SavePath { get { return _SavePath; } }

    string ToPath;

    CWWWLoader WWWLoader;

    float TIME_OUT_DEF = 5f; // 5秒延遲

    private bool ForceFinished = false;
    public bool IsFinished { get { return WWWLoader.IsError || WWWLoader.IsFinished || ForceFinished; } }
	private bool ForceError = false;
    public bool IsError { get { return WWWLoader.IsError || ForceError; } }
    public WWW Www { get { return WWWLoader.Www; } }
    public float Progress { get {return WWWLoader.Progress;}} // 進度

    public CWWWDownloader(string fullUrl, string toPath)
    {
        ToPath = toPath;
        _SavePath = CResourceModule.GetAppDataPath() + "/" + ToPath;

        WWWLoader = new CWWWLoader(fullUrl);
        CResourceModule.Instance.StartCoroutine(StartDownload(fullUrl));
    }

    IEnumerator StartDownload(string fullUrl)
    {
        float startTime = Time.time;
        
        while (!WWWLoader.IsFinished)
        {
            if (WWWLoader.Progress == 0 && Time.time - startTime > TIME_OUT_DEF)
            {
                CBase.LogError("超時卻無下載 Timeout: {0}", fullUrl);
                break;
            }

            yield return null;
        }
        if (WWWLoader.IsError || !WWWLoader.IsFinished)
        {
            CBase.LogError("Download WWW Error: {0}", fullUrl);
            ForceFinished = true;
	        ForceError = true;
            yield break;
        }

        string dir = Path.GetDirectoryName(_SavePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        System.IO.File.WriteAllBytes(_SavePath, WWWLoader.Www.bytes);
        // WWW没用了
        WWWLoader.Dispose();
    }
}
