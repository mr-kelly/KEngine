using UnityEngine;
using System.Collections;
using KEngine;
public class CAudioSourceDep : CAssetDep {

    protected override void DoProcess(string resourcePath)
    {
        ProcessAudioSource(resourcePath);
    }

    protected void ProcessAudioSource(string path)
    {
        KAudioLoader.Load(path, (isOk, clip) =>
        {
            if (!IsDestroy)
            {
                AudioSource src = DependencyComponent as AudioSource;

                Logger.Assert(src);
                src.clip = clip;
                //src.Play(); // 特效进行Play, 不主动播放
                src.Stop();
            }
            OnFinishLoadDependencies(DependencyComponent);  // 返回GameObject而已哦
        });
    }
}
