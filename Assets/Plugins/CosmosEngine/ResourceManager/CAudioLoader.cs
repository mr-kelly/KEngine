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

public class CAudioLoader
{
	class XAssetBundleCache
	{
		public string URL;
		public AudioClip Clip;
		public float UpdateTime;
		public XAssetBundleCache(string url)
		{
			URL = url;
		}
	}

	const int MaxCacheCount = 20;
	static Dictionary<string, XAssetBundleCache> AudioDict = new Dictionary<string, XAssetBundleCache>();
	AudioClip ResultAudioClip;

	public bool IsFinished { get { return ResultAudioClip != null; } }

	public AudioClip Clip { get { return ResultAudioClip; } }
    string Url;

	public CAudioLoader(string url)
	{
        Url = url;
        if (CCosmosEngine.GetConfig("IsLoadAssetBundle").ToInt32() == 0)
            CResourceManager.Instance.StartCoroutine(LoadFromResourcesFolder(url));
        else
            CResourceManager.Instance.StartCoroutine(Load(url));
	}

    IEnumerator LoadFromResourcesFolder(string url)
    {
        yield return null;
        AudioClip clip = Resources.Load<AudioClip>(url);
        OnLoadAudioClip(clip);
    }

	public void ReleaseCache()
	{
		if (AudioDict.Count < MaxCacheCount)
			return;

		LinkedList<string> audioKey = new LinkedList<string>();
		foreach (var node in AudioDict)
		{
			if (node.Value.Clip != null && Time.time - node.Value.UpdateTime > 30)
			{
				audioKey.AddLast(node.Key);
				Object.Destroy(node.Value.Clip);
			}
		}

		foreach (string key in audioKey)
		{
			AudioDict.Remove(key);
		}
	}

	public IEnumerator Load(string url)
	{
		XAssetBundleCache cache = null;
		CResourceManager.LogRequest("Audio", url);

		if (AudioDict.TryGetValue(url, out cache) && cache.Clip != null)
		{
			cache.UpdateTime = Time.time;
            ResultAudioClip = cache.Clip;
			yield break;
		}

		if (cache == null)
		{
			ReleaseCache();
			cache = new XAssetBundleCache(url);
			cache.UpdateTime = Time.time;
			AudioDict.Add(url, cache);

			CWWWLoader wwwLoader = new CWWWLoader(url);
			while (!wwwLoader.IsFinished)
				yield return null;
			cache.Clip = wwwLoader.Www.GetAudioClip(true, false);
		}
		else
		{
			while (cache.Clip == null)
				yield return null;
		}

        OnLoadAudioClip(cache.Clip);
	}

    void OnLoadAudioClip(AudioClip clip)
    {
        if (clip == null)
            CBase.LogError("Null Audio Clip!!!: {0}", this.Url);

        ResultAudioClip = clip;
    }
}
