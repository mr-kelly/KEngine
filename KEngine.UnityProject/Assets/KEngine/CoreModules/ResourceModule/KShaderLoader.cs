using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KEngine
{
    /// <summary>
    /// Shader加载器
    /// </summary>
    public class KShaderLoader : KAbstractResourceLoader
    {
        public delegate void ShaderLoaderDelegate(bool isOk, Shader shader);

        public Shader ShaderAsset
        {
            get { return ResultObject as Shader; }
        }
        
        public static KShaderLoader Load(string path, ShaderLoaderDelegate callback = null)
        {
            CLoaderDelgate newCallback = null;
            if (callback != null)
            {
                newCallback = (isOk, obj) => callback(isOk, obj as Shader);
            }
            return AutoNew<KShaderLoader>(path, newCallback);
        }

        protected override void Init(string url, params object[] args)
        {
            base.Init(url, args);
            KResourceModule.Instance.StartCoroutine(CoLoadShader());
        }

        private IEnumerator CoLoadShader()
        {
            var loader = KAssetBundleLoader.Load(Url);
            while (!loader.IsFinished)
            {
                Progress = loader.Progress;
                yield return null;
            }

            var shader = loader.Bundle.mainAsset as Shader;
            Logger.Assert(shader);

            Desc = shader.name;

#if UNITY_EDITOR
            KResoourceLoadedAssetDebugger.Create("Shader", Url, shader);
#endif
            loader.Release();

            OnFinish(shader);
        }


        protected override void DoDispose()
        {
            base.DoDispose();

            GameObject.Destroy(ShaderAsset);
        }
    }
}
