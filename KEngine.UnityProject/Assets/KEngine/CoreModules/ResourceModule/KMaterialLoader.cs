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
using System.Collections.Generic;
using System.Security.Policy;
using KEngine;

/// <summary>
/// 加载材质，通过CSerializeMaterial
/// </summary>
[CDependencyClass(typeof(KAssetFileLoader))]
public class KMaterialLoader : KAbstractResourceLoader
{
    public delegate void CCMaterialLoaderDelegate(bool isOk, Material mat);
    public Material Mat { get; private set; }

    private List<KTextureLoader> TextureLoaders;

    public static KMaterialLoader Load(string path, CCMaterialLoaderDelegate callback = null)
    {
        KAbstractResourceLoader.CLoaderDelgate newCallback = null;
        if (callback != null)
        {
            newCallback = (isOk, obj) => callback(isOk, obj as Material);
        }
        return AutoNew<KMaterialLoader>(path, newCallback);
    }

    protected override void Init(string url, params object[] args)
    {
        base.Init(url, args);
        KResourceModule.Instance.StartCoroutine(CoLoadSerializeMaterial());
    }

    IEnumerator CoLoadSerializeMaterial()
    {
        var matLoadBridge = KAssetFileLoader.Load(Url);
        while (!matLoadBridge.IsFinished)
        {
            Progress = matLoadBridge.Progress;
            yield return null;
        }

        var sMat = matLoadBridge.ResultObject as KSerializeMaterial;
        Logger.Assert(sMat);

        Desc = sMat.ShaderName;

        Logger.Assert(Mat == null);
        yield return KResourceModule.Instance.StartCoroutine(CoGenerateMaterial(Url, sMat));
        Logger.Assert(Mat);

        matLoadBridge.Release(); //不需要它了

#if UNITY_EDITOR
        KResoourceLoadedAssetDebugger.Create("Material", Url, Mat);
#endif
        OnFinish(Mat);
    }

    protected override void OnFinish(object resultObj)
    {
        if (resultObj == null)
        {
            Logger.LogError("[KMaterialLoader:OnFinishe] Null Material: {0}", Url);
        }
        base.OnFinish(resultObj);
    }

    protected override void DoDispose()
    {
        base.DoDispose();

        if (TextureLoaders != null)
        {
            foreach (var texLoader in TextureLoaders)
            {
                texLoader.Release();
            }
            TextureLoaders.Clear();
        }
        GameObject.Destroy(Mat);
    }

    private string ParseMaterialStr(string materialTextureStr, out Vector2 tiling, out Vector2 offset)
    {
        var textureStr = materialTextureStr; // 纹理+tiling+offset
        var textureArr = textureStr.Split('|');
        Logger.Assert(textureArr.Length > 0);
        string texturePath = textureArr[0];
        tiling = Vector2.one;
        offset = Vector2.zero;
        if (textureArr.Length > 1)
        {
            tiling = new Vector2(textureArr[1].ToFloat(), textureArr[2].ToFloat());
            offset = new Vector2(textureArr[3].ToFloat(), textureArr[4].ToFloat());
        }

        return texturePath;
    }
    // 加载材质的图片, 协程等待
    IEnumerator CoGenerateMaterial(string matPath, KSerializeMaterial sMat)
    {
        // 纹理全部加载完成后到这里
        //if (!CachedMaterials.TryGetValue(matPath, out mat))
        {
            var shaderLoader = KShaderLoader.Load(sMat.ShaderPath);
            while (!shaderLoader.IsFinished)
            {
                yield return null;
            }
            
            var shader = shaderLoader.ShaderAsset;
            if (shader == null)
            {
                shader = KTool.FindShader(sMat.ShaderName);
                Logger.LogWarning("无法加载Shader资源: {0}, 使用Shaders.Find代替", sMat.ShaderName);
                if (shader == null)
                {
                    Logger.LogWarning("找不到Shader: {0}, 使用Diffuse临时代替", sMat.ShaderName);
                    shader = KTool.FindShader("Diffuse");    
                }
            }
            Logger.Assert(shader);

            Mat = new Material(shader);
            Mat.name = sMat.MaterialName;

            //CachedMaterials[matPath] = mat;

            foreach (KSerializeMaterialProperty shaderProp in sMat.Props)
            {
                switch (shaderProp.Type)
                {
                    case KSerializeMaterialProperty.ShaderType.Texture:
                        Vector2 tiling;
                        Vector2 offset;
                        var texturePath = ParseMaterialStr(shaderProp.PropValue, out tiling, out offset);
                        if (TextureLoaders == null)
                            TextureLoaders = new List<KTextureLoader>();

                        var texLoader = KTextureLoader.Load(texturePath);
                        TextureLoaders.Add(texLoader);
                        while (!texLoader.IsFinished)
                            yield return null;

                        var tex = texLoader.Asset;
                        if (tex == null)
                        {
                            Logger.LogError("找不到纹理: {0}", texturePath);
                        }
                        else
                        {
                            _SetMatTex(Mat, shaderProp.PropName, tex, tiling, offset);
                        }
                        break;
                    case KSerializeMaterialProperty.ShaderType.Color:
                        _SetMatColor(Mat, shaderProp.PropName, shaderProp.PropValue);
                        break;
                    case KSerializeMaterialProperty.ShaderType.Range:
                        _SetMatRange(Mat, shaderProp.PropName, shaderProp.PropValue);
                        break;
                    case KSerializeMaterialProperty.ShaderType.Vector:
                        _SetMatVector(Mat, shaderProp.PropName, shaderProp.PropValue);
                        break;
                    case KSerializeMaterialProperty.ShaderType.RenderTexture:
                        // RenderTextures, 不处理, 一般用在水，Water脚本会自动生成
                        break;

                }
            }
        }
    }

    void _SetMatVector(Material mat, string propName, string propValue)
    {
        if (mat.HasProperty(propName))
        {
            propValue = propValue.Trim('(', ')'); // (1.0, 3.0, 4.0, 5.0)
            string[] vecArr = propValue.Split(',');
            Vector4 vector = new Vector4(float.Parse(vecArr[0]), float.Parse(vecArr[1]), float.Parse(vecArr[2]), float.Parse(vecArr[3]));

            mat.SetVector(propName, vector);
        }
    }
    void _SetMatRange(Material mat, string propName, string propValue)
    {
        if (mat.HasProperty(propName))
        {
            mat.SetFloat(propName, float.Parse(propValue));
        }
        else
            Logger.LogError("[_SetMatRange]Cannot find shader property: {0}", propName);
    }
    void _SetMatColor(Material mat, string colorPropName, string _colorStr) // 设置材质Shader的颜色
    {
        if (mat.HasProperty(colorPropName))
        {
            _colorStr = _colorStr.Replace("RGBA(", "").Replace(")", "");  // RGBA(0.5, 0.5,0.5, 1.0)
            string[] colorArr = _colorStr.Split(',');

            Color color = new Color(float.Parse(colorArr[0]), float.Parse(colorArr[1]), float.Parse(colorArr[2]), float.Parse(colorArr[3]));
            if (mat.HasProperty(colorPropName))
                mat.SetColor(colorPropName, color);
            else
                Logger.LogError("[_SetMatColor]Cannot find shader property: {0}", colorPropName);
        }
    }

    void _SetMatTex(Material mat, string matPropName, Texture tex, Vector2 tiling, Vector2 offset) // 设置材质指定属性的纹理
    {
        //if (!string.IsNullOrEmpty(texturePath))
        //{
        //    Texture tex;
        //if (CachedTextures.TryGetValue(texturePath, out tex))
        {
            mat.SetTexture(matPropName, tex);
            mat.SetTextureScale(matPropName, tiling);
            mat.SetTextureOffset(matPropName, offset);
        }
        //else
        //    Logger.LogError("找不到纹理: {0}", texturePath);
        //}
    }
}
