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
using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Threading;

public class CHttpDownloader : MonoBehaviour, IDisposable
{
    string _SavePath;
    public string SavePath { get { return _SavePath; } }

    public string Url { get; private set; }
    string ToPath;

    //CWWWLoader WWWLoader;

    float TIME_OUT_DEF;

    private bool FinishedFlag = false;

    public bool IsFinished
    {
        get
        {
            return ErrorFlag || FinishedFlag;
        }
    }

    private bool ErrorFlag = false;
    public bool IsError { get { return ErrorFlag; } }

    private bool UseCache;
    private int ExpireDays = 1; // 过期时间, 默认1天

    //public WWW Www { get { return WWWLoader.Www; } }
    public float Progress = 0; // 進度
    //public float Speed { get { return WWWLoader.LoadSpeed; } } // 速度

    private CHttpDownloader()
    {
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="fullUrl"></param>
    /// <param name="toPath"></param>
    /// <param name="useCache">如果存在则不下载了！</param>
    /// <param name="expireDays"></param>
    /// <param name="timeout"></param>
    public static CHttpDownloader Load(string fullUrl, string toPath, bool useCache = false, int expireDays = 1, int timeout = 5)
    {
        var downloader = new GameObject("HttpDownlaoder+"+fullUrl).AddComponent<CHttpDownloader>();
        downloader.Init(fullUrl, toPath, useCache, expireDays, timeout);

        return downloader;
    }

    public static string GetFullSavePath(string relativePath)
    {
        return CResourceModule.GetAppDataPath() + "/" + relativePath;
    }
    private void Init(string fullUrl, string toPath, bool useCache = false, int expireDays = 1, int timeout = 5)
    {
        Url = fullUrl;
        ToPath = toPath;
        _SavePath = GetFullSavePath(ToPath);
        UseCache = useCache;
        ExpireDays = expireDays;
        TIME_OUT_DEF = timeout; // 5秒延遲
        CResourceModule.Instance.StartCoroutine(StartDownload(fullUrl));

    }

    public static CHttpDownloader Load(string fullUrl, string toPath, int expireDays, int timeout = 5)
    {
        return Load(fullUrl, toPath, expireDays != 0, expireDays, timeout);
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

        string dir = Path.GetDirectoryName(_SavePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var totalSize = int.MaxValue;
        var downloadSize = 0;
        var isThreadError = false;
        var isThreadFinish = false;
        var isThreadStart = false;
        var downloadThread = new Thread(() =>
        {
            ThreadableResumeDownload(fullUrl, (totalSizeNow, downSizeNow) =>
            {
                totalSize = totalSizeNow;
                downloadSize = downSizeNow;
                isThreadStart = true;
            }, () =>
            {
                isThreadError = true;
                isThreadFinish = true;
                isThreadStart = true;
            }, () =>
            {
                isThreadFinish = true;
            });
        });
        downloadThread.Start();

        var timeCounter = 0f;
        var MaxTime = TIME_OUT_DEF;
        while (!isThreadFinish && !isThreadError)
        {
            timeCounter += Time.deltaTime;
            if (timeCounter > MaxTime && !isThreadStart)
            {
//#if !UNITY_IPHONE  // TODO: 新的异步机制去暂停，Iphone 64不支持
//                downloadThread.Abort();
//#endif
                CDebug.LogError("[CHttpDownloader]下载线程超时！: {0}", fullUrl);
                isThreadError = true;
                break;
            }
            Progress = (downloadSize / (float)totalSize);
            yield return null;
        }

        if (isThreadError)
        {
            CDebug.LogError("Download WWW Error: {0}", fullUrl);
            ErrorFlag = true;

            // TODO:
            //try
            //{
            //    if (File.Exists(TmpDownloadPath))
            //        File.Delete(TmpDownloadPath); // delete temporary file
            //}
            //catch (Exception e)
            //{
            //    CDebug.LogError(e.Message);
            //}

            OnFinish();
            yield break;
        }
        OnFinish();
    }

    void OnFinish()
    {
        FinishedFlag = true;
    }

    public byte[] GetDatas()
    {
        CDebug.Assert(IsFinished);
        CDebug.Assert(!IsError);
        return System.IO.File.ReadAllBytes(_SavePath);
    }

    public string TmpDownloadPath
    {
        get { return _SavePath +  ".download"; }
    }
    
    void ThreadableResumeDownload(string url, Action<int, int> stepCallback, Action errorCallback, Action successCallback)
    {
        //string tmpFullPath = TmpDownloadPath; //根据实际情况设置 
        System.IO.FileStream downloadFileStream;
        //打开上次下载的文件或新建文件 
        long lStartPos = 0;
        
        if (System.IO.File.Exists(TmpDownloadPath))
        {
            downloadFileStream = System.IO.File.OpenWrite(TmpDownloadPath);
            lStartPos = downloadFileStream.Length;
            downloadFileStream.Seek(lStartPos, System.IO.SeekOrigin.Current); //移动文件流中的当前指针 

            CDebug.LogConsole_MultiThread("Resume.... from {0}", lStartPos);
        }
        else
        {
            downloadFileStream = new System.IO.FileStream(TmpDownloadPath, System.IO.FileMode.Create);
            lStartPos = 0;
        }
        System.Net.HttpWebRequest request = null;
        //打开网络连接 
        try
        {

            request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            if (lStartPos > 0)
                request.AddRange((int)lStartPos); //设置Range值

            CDebug.LogConsole_MultiThread("Getting Response : {0}", url);

            //向服务器请求，获得服务器回应数据流 
            using (var response = request.GetResponse())  // TODO: Async Timeout
            {
                CDebug.LogConsole_MultiThread("Getted Response : {0}", url);
                if (IsFinished)
                {
                    throw new Exception(string.Format("Get Response ok, but is finished , maybe timeout! : {0}", url));
                }
                else
                {
                    var totalSize = (int)response.ContentLength;
                    if (totalSize <= 0)
                    {
                        totalSize = int.MaxValue;
                    }
                    using (var ns = response.GetResponseStream())
                    {

                        CDebug.LogConsole_MultiThread("Start Stream: {0}", url);
                        int downSize = (int)lStartPos;
                        int chunkSize = 10240;
                        byte[] nbytes = new byte[chunkSize];
                        int nReadSize = (int)lStartPos;
                        while ((nReadSize = ns.Read(nbytes, 0, chunkSize)) > 0)
                        {
                            if (IsFinished)
                                throw new Exception("When Reading Web stream but Downloder Finished!");
                            downloadFileStream.Write(nbytes, 0, nReadSize);
                            downSize += nReadSize;
                            stepCallback(totalSize, downSize);
                        }
                        stepCallback(totalSize, totalSize);

                        request.Abort();
                        downloadFileStream.Close();
                    }
                }
            }

            CDebug.LogConsole_MultiThread("下载完成: {0}", url);
            if (File.Exists(_SavePath))
            {
                File.Delete(_SavePath);
            }
            File.Move(TmpDownloadPath, _SavePath);
        }
        catch (Exception ex)
        {
            CDebug.LogConsole_MultiThread("下载过程中出现错误:" + ex.ToString());
            downloadFileStream.Close();

            if (request != null)
                request.Abort();

            try
            {
                if (File.Exists(TmpDownloadPath))
                    File.Delete(TmpDownloadPath); // delete temporary file
            }
            catch (Exception e)
            {
                CDebug.LogConsole_MultiThread(e.Message);
            }

            errorCallback();
        }
        successCallback();
    }

    void OnDestroy()
    {
        if (!FinishedFlag)
        {
            FinishedFlag = true;
            ErrorFlag = true;
            CDebug.LogError("[HttpDownloader]Not finish but destroy: {0}", Url);
        }
    }

    public void Dispose()
    {
        GameObject.Destroy(gameObject);
    }
}