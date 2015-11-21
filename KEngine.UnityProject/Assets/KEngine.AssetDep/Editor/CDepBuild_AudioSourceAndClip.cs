using UnityEditor;
using UnityEngine;
using System.Collections;
using KEngine;
public partial class CDependencyBuild
{
    [DepBuild(typeof(AudioSource))]
    static void ProcessAudioSource(AudioSource com)
    {
        var audioSource = com;
        if (audioSource.clip != null)
        {
            string audioPath = BuildAudioClip(audioSource.clip);
            CAssetDep.Create<CAudioSourceDep>(audioSource, audioPath);
            audioSource.clip = null;
        }
        else
        {
            Logger.LogWarning("找不到AudioClip在AudioSource... {0}", audioSource.name);
        }
    }

    static string BuildAudioClip(AudioClip audioClip)
    {
        string assetPath = AssetDatabase.GetAssetPath(audioClip);
        bool needBuild = CBuildTools.CheckNeedBuild(assetPath);
        if (needBuild)
            CBuildTools.MarkBuildVersion(assetPath);

        var result = DoBuildAssetBundle("Audio/Audio_" + audioClip.name, audioClip, needBuild);

        return result.Path;
    }

}