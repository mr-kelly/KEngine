using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using KEngine;
public partial class KDependencyBuild
{
    [DepBuild(typeof(ParticleSystem))]
    static void ProcessParticleSystem(ParticleSystem particleCom)
    {
        var particle = particleCom;
        if (particle.renderer.sharedMaterial != null)
        {
            string matPath = BuildDepMaterial(particle.renderer.sharedMaterial);
            //CResourceDependencies.Create(particle, CResourceDependencyType.PARTICLE_SYSTEM, matPath);
            KAssetDep.Create<KParticleSystemDep>(particle, matPath);

            particle.renderer.sharedMaterial = null;
        }
        else
        {
            Logger.LogWarning("没有Material的粒子: {0}", particle.gameObject.name);
        }
    }

}