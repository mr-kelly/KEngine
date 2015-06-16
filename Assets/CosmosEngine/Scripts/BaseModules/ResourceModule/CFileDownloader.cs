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

    float TIME_OUT_DEF = 10f; // 5秒延遲

    private bool FinishedFlag = false;
    public bool IsFinished { get { return ErrorFlag || FinishedFlag; } }
	private bool ErrorFlag = false;
    public bool IsError { get { return ErrorFlag; } }

    private bool UseCache;
    private int ExpireDays = 1; // 过期时间, 默认1天

    public WWW Www { get { return WWWLoader.Www; } }
    public float Progress { get {return WWWLoader.Progress;}} // 進度
    public int Size { get { return WWWLoader.Size; } }
    public int DownloadedSize { get { return WWWLoader.DownloadedSize; } }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fullUrl"></param>
    /// <param name="toPath"></param>
    /// <param name="useCache">如果存在则不下载了！</param>
    /// <param name="expireDays"></param>
    public CWWWDownloader(string fullUrl, string toPath, bool useCache = false, int expireDays = 1)
    {
        ToPath = toPath;
        _SavePath = CResourceModule.GetAppDataPath() + "/" + ToPath;
        UseCache = useCache;
        ExpireDays = expireDays;
        CResourceModule.Instance.StartCoroutine(StartDownload(fullUrl));
    }

    public static CWWWDownloader Load(string fullUrl, string toPath, bool useCache = false)
    {
        return new CWWWDownloader(fullUrl, toPath, useCache);
    }

    public static CWWWDownloader Load(string fullUrl, string toPath, int expireDays)
    {
        return new CWWWDownloader(fullUrl, toPath, true, expireDays);
    }

    IEnumerator StartDownload(string fullUrl)
    {
        float startTime = Time.time;
        if (UseCache && File.Exists(_SavePath))
        {
            var lastWriteTime = File.GetLastWriteTimeUtc(_SavePath);
            CDebug.Log("缓存文件: {0}, 最后修改时间: {1}", _SavePath, lastWriteTime);
            var deltaDays = CTool.GetDeltaDay(lastWriteTime);
            // 文件未过期
            if (deltaDays < ExpireDays)
            {
                CDebug.Log("缓存文件未过期 {0}", _SavePath);
                FinishedFlag = true;
                ErrorFlag = false;
                yield break;
            }
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
