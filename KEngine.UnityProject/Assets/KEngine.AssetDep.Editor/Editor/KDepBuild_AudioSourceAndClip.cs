using UnityEditor;
using UnityEngine;
using System.Collections;
using KEngine;
using KEngine.Editor;

public partial class KDependencyBuild
{
    [DepBuild(typeof(AudioSource))]
    static void ProcessAudioSource(AudioSource com)
    {
        var audioSource = com;
        if (audioSource.clip != null)
        {
            string audioPath = BuildAudioClip(audioSource.clip);
            KAssetDep.Create<KAudioSourceDep>(audioSource, audioPath);
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
        bool needBuild = KAssetVersionControl.TryCheckNeedBuildWithMeta(assetPath);
        if (needBuild)
            KAssetVersionControl.TryMarkBuildVersion(assetPath);

        var result = DoBuildAssetBundle("Audio/Audio_" + audioClip.name, audioClip, needBuild);

        return result.Path;
    }

}