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

    private bool FinishedFlag = false;
    public bool IsFinished { get { return ErrorFlag || FinishedFlag; } }
	private bool ErrorFlag = false;
    public bool IsError { get { return ErrorFlag; } }

    public WWW Www { get { return WWWLoader.Www; } }
    public float Progress { get {return WWWLoader.Progress;}} // 進度

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fullUrl"></param>
    /// <param name="toPath"></param>
    /// <param name="useCache">如果存在则不下载了！</param>
    public CWWWDownloader(string fullUrl, string toPath, bool useCache = false)
    {
        ToPath = toPath;
        _SavePath = CResourceModule.GetAppDataPath() + "/" + ToPath;

        CResourceModule.Instance.StartCoroutine(StartDownload(fullUrl));
    }

    IEnumerator StartDownload(string fullUrl)
    {
        float startTime = Time.time;
        if (File.Exists(_SavePath))
        {
            FinishedFlag = true;
            ErrorFlag = false;
            yield break;
        }

        WWWLoader = CWWWLoader.Load(fullUrl);
        while (!WWWLoader.IsFinished)
        {
            if (WWWLoader.Progress == 0 && Time.time - startTime > TIME_OUT_DEF)
            {
                CDebug.LogError("超時卻無下載 Timeout: {0}", fullUrl);
                break;
            }

            yield return null;
        }

        if (WWWLoader.IsError || !WWWLoader.IsFinished)
        {
            CDebug.LogError("Download WWW Error: {0}", fullUrl);
            FinishedFlag = true;
	        ErrorFlag = true;
            yield break;
        }

        
        string dir = Path.GetDirectoryName(_SavePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        System.IO.File.WriteAllBytes(_SavePath, WWWLoader.Www.bytes);

        FinishedFlag = true;
        // WWW没用了
        WWWLoader.Release();
    }

    public byte[] GetDatas()
    {
        CDebug.Assert(IsFinished);
        return System.IO.File.ReadAllBytes(_SavePath);
    }
}
