using UnityEngine;
using System.Collections;
using KEngine;
public class KParticleSystemDep : KAssetDep
{

    protected override void DoProcess(string resourcePath)
    {
        ProcessParticleSystem(resourcePath);
    }

    /// <summary>
    /// 加载粒子系统以来的材质
    /// </summary>
    /// <param name="path"></param>
    protected void ProcessParticleSystem(string path)
    {
        var p = (ParticleSystem)DependencyComponent;
        p.Stop();  // 先暂停粒子播放
        var pRenderer = (ParticleSystemRenderer)p.renderer;

        pRenderer.enabled = false;
        LoadMaterial(path, (mat) =>
        {
            if (IsDestroy)
            {
                Logger.LogError("[ProcessParticleSystem]Material loaded, but Destroyed Dep: {0}, Material: {1}", path, mat);
            }

            if (!IsDestroy)
            {
                pRenderer.sharedMaterial = mat;
                pRenderer.enabled = true;
            }

            OnFinishLoadDependencies(mat);

            if (!IsDestroy)
            {
                // callback后再play
                p.Play();
            }
        });
    }

}
