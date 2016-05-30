#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KDepBuild_Material.cs
// Date:     2015/12/03
// Author:  Kelly
// Email: 23110388@qq.com
// Github: https://github.com/mr-kelly/KEngine
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.

#endregion

#if !UNITY_5
using System.Collections.Generic;
using KEngine;
using KEngine.Editor;
using UnityEditor;
using UnityEngine;

public class KDepBuild_Material
{
    public static string BuildDepMaterial(Material mat, float scaleTexture = 1f)
    {
        CDepCollectInfo buildResult = KDepCollectInfoCaching.GetCache(mat);
        if (buildResult != null)
        {
            return buildResult.Path;
        }

        KSerializeMaterial sMat = __DoExportMaterial(mat, scaleTexture);

        if (sMat != null)
        {
            string path = AssetDatabase.GetAssetPath(mat);

            bool needBuild = AssetVersionControl.TryCheckNeedBuildWithMeta(path);
            if (needBuild)
                AssetVersionControl.TryMarkBuildVersion(path);

            path = KDependencyBuild.__GetPrefabBuildPath(path);
            buildResult = KDependencyBuild.__DoBuildScriptableObject("Material/Material_" + path, sMat, needBuild);

            KDepCollectInfoCaching.SetCache(mat, buildResult);

            return buildResult.Path;
        }

        // 可能没路径的材质，直接忽略
        return "";
    }

    // 只导出材质信息，不实际导出材质
    private static KSerializeMaterial __DoExportMaterial(Material mat, float scaleTexture = 1f)
    {
        string matPath = AssetDatabase.GetAssetPath(mat).Replace("Assets/", "");
        if (string.IsNullOrEmpty(matPath))
        {
            KLogger.LogWarning("没有路径材质Material: {0}   可能是动态创建的？", mat.name);
            return null;
        }

        var props = new List<KSerializeMaterialProperty>();
        IEnumerator<KSerializeMaterialProperty> shaderPropEnumtor = _ShaderPropEnumtor(mat, scaleTexture);
        while (shaderPropEnumtor.MoveNext())
        {
            KSerializeMaterialProperty shaderProp = shaderPropEnumtor.Current;
            if (shaderProp != null)
            {
                props.Add(shaderProp);
            }
        }

        var shaderBuildPath = _BuildShader(mat.shader);
        KSerializeMaterial xMat = ScriptableObject.CreateInstance<KSerializeMaterial>();
        xMat.MaterialName = matPath;
        xMat.ShaderName = mat.shader.name;
        xMat.ShaderPath = shaderBuildPath;
        xMat.Props = props;

        return xMat;
    }

    private static string _BuildShader(Shader shader)
    {
        Shader fileShader;
        string shaderAssetPath = AssetDatabase.GetAssetPath(shader);
        if (shaderAssetPath.Contains("unity_builtin_extra"))
        {
            shaderAssetPath = "Assets/" + KEngineDef.ResourcesBuildCacheDir + "/BuiltinShader";

            fileShader = AssetDatabase.LoadAssetAtPath(shaderAssetPath, typeof (Shader)) as Shader;
            if (fileShader == null)
            {
                AssetDatabase.CreateAsset(shader, shaderAssetPath);
                AssetDatabase.ImportAsset(shaderAssetPath);
                fileShader = AssetDatabase.LoadAssetAtPath(shaderAssetPath, typeof (Shader)) as Shader;

                if (fileShader == null)
                {
                    KLogger.LogError("Cannot Build Builtin Shader: {0}", shader.name);
                }
            }
        }
        else
        {
            fileShader = shader;
        }

        //var shaderFlag = string.Format("Shader:{0}:{1}", fileShader.name, shaderAssetPath);  // 构造一个标记

        bool needBuild = AssetVersionControl.TryCheckFileBuild(shaderAssetPath);
        if (needBuild)
            AssetVersionControl.TryMarkBuildVersion(shaderAssetPath);

        var cleanShaderName = GetShaderNameToBuild(fileShader);
        var result = KDependencyBuild.DoBuildAssetBundle("Shader/Shader_" + cleanShaderName, fileShader, needBuild);
        return result.Path;
    }

    private static string GetShaderNameToBuild(Shader shader)
    {
        var cleanShaderName = shader.name.Replace(" ", "_").Replace("/", "_"); // 去空格，去斜杠
        return cleanShaderName;
    }

    private static IEnumerator<KSerializeMaterialProperty> _ShaderPropEnumtor(Material mat, float scaleTexture = 1f)
    {
        var shader = mat.shader;
        if (shader != null)
        {
            for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var shaderType = ShaderUtil.GetPropertyType(shader, i);
                var shaderPropName = ShaderUtil.GetPropertyName(shader, i);
                switch (shaderType)
                {
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        yield return _GetShaderTexProp(mat, shaderPropName, scaleTexture);
                        break;
                    case ShaderUtil.ShaderPropertyType.Color:
                        yield return _GetMatColor(mat, shaderPropName);
                        break;
                    case ShaderUtil.ShaderPropertyType.Range:
                    case ShaderUtil.ShaderPropertyType.Float: // TODO: 未确定Mat.GetInt是float还是range
                        yield return _GetMatRange(mat, shaderPropName);
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        yield return _GetShaderVectorProp(mat, shaderPropName);
                        break;
                }
            }
        }
        // Unity Builtin Shaders
        //yield return _GetShaderTexProp(mat, "_MainTex", buildToFolder, scaleTexture);
        //yield return _GetShaderTexProp(mat, "_BumpMap", buildToFolder, scaleTexture);
        //yield return _GetShaderTexProp(mat, "_Cube", buildToFolder, scaleTexture);
        //yield return _GetMatColor(mat, "_Color");
        //yield return _GetMatColor(mat, "_SpecColor");
        //yield return _GetMatColor(mat, "_Emission");
        //yield return _GetMatColor(mat, "_ReflectColor");

        //// FX/Water 
        //yield return _GetMatRange(mat, "_WaveScale");
        //yield return _GetMatRange(mat, "_ReflDistort");
        //yield return _GetMatRange(mat, "_RefrDistort");
        //yield return _GetMatColor(mat, "_RefrColor");
        //yield return _GetShaderTexProp(mat, "_Fresnel", buildToFolder, scaleTexture);
        //yield return _GetShaderVectorProp(mat, "WaveSpeed");
        //yield return _GetShaderTexProp(mat, "_ReflectiveColor", buildToFolder, scaleTexture);
        //yield return _GetShaderTexProp(mat, "_ReflectiveColorCube", buildToFolder, scaleTexture);
        //yield return _GetMatColor(mat, "_HorizonColor");
        //yield return _GetShaderTexProp(mat, "_ReflectionTex", buildToFolder, scaleTexture);
        //yield return _GetShaderTexProp(mat, "_RefractionTex", buildToFolder, scaleTexture);
    }

    private static KSerializeMaterialProperty _GetShaderTexProp(Material mm, string texProp, float scaleTexture = 1f)
    {
        if (mm.HasProperty(texProp))
        {
            Texture tex = mm.GetTexture(texProp);
            if (tex != null)
            {
                KSerializeMaterialProperty shaderProp = new KSerializeMaterialProperty();

                shaderProp.PropName = texProp;
                if (tex is Texture2D)
                {
                    var texTiling = mm.GetTextureScale(texProp); // 纹理+tiling+offset
                    var texOffset = mm.GetTextureOffset(texProp);
                    var texPath = KDependencyBuild.BuildDepTexture(tex, scaleTexture);
                    shaderProp.Type = KSerializeMaterialProperty.ShaderType.Texture;

                    shaderProp.PropValue = string.Format("{0}|{1}|{2}|{3}|{4}", texPath, texTiling.x, texTiling.y,
                        texOffset.x, texOffset.y);
                }
                else
                {
                    KLogger.LogWarning("找到一个非Texture2D, Type:{0} Mat:{1} PropName:{2}", tex.GetType(), mm.name, texProp);
                    shaderProp.Type = KSerializeMaterialProperty.ShaderType.RenderTexture;
                        // Shader的RenderTexture不打包，一般由脚本动态生成
                    shaderProp.PropValue = null;
                }
                return shaderProp;
            }
            else
            {
                KLogger.Log("[_GetShaderTexProp]处理纹理时发现获取不到纹理, 材质{0}  Shader属性{1}", mm.name, texProp);
                return null;
            }
        }
        return null;
    }

    private static KSerializeMaterialProperty _GetShaderVectorProp(Material mm, string texProp)
    {
        if (mm.HasProperty(texProp))
        {
            KSerializeMaterialProperty shaderProp = new KSerializeMaterialProperty();
            shaderProp.Type = KSerializeMaterialProperty.ShaderType.Vector;
            shaderProp.PropName = texProp;
            Vector4 tex = mm.GetVector(texProp);
            shaderProp.PropValue = tex.ToString();

            return shaderProp;
        }
        return null;
    }

    private static KSerializeMaterialProperty _GetMatColor(Material mm, string colorProp)
    {
        if (mm.HasProperty(colorProp))
        {
            KSerializeMaterialProperty shaderProp = new KSerializeMaterialProperty();
            shaderProp.Type = KSerializeMaterialProperty.ShaderType.Color;
            shaderProp.PropName = colorProp;

            Color color = mm.GetColor(colorProp);
            shaderProp.PropValue = color.ToString();
            return shaderProp;
        }

        return null; // 默认给个白
    }

    private static KSerializeMaterialProperty _GetMatRange(Material mm, string propName)
    {
        if (mm.HasProperty(propName))
        {
            KSerializeMaterialProperty shaderProp = new KSerializeMaterialProperty();
            shaderProp.Type = KSerializeMaterialProperty.ShaderType.Range;
            shaderProp.PropName = propName;
            float propValue = mm.GetFloat(propName);
            shaderProp.PropValue = propValue.ToString();
            return shaderProp;
        }

        return null;
    }
}
#endif