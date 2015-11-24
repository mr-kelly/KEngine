using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Collections;
using KEngine;
using KEngine.Editor;

public partial class KDependencyBuild
{

    static string BuildDepMaterial(Material mat, float scaleTexture = 1f)
    {
        KSerializeMaterial sMat = __DoExportMaterial(mat, DepBuildToFolder, scaleTexture);

        if (sMat != null)
        {
            string path = AssetDatabase.GetAssetPath(mat);

            bool needBuild = KAssetVersionControl.TryCheckNeedBuildWithMeta(path);
            if (needBuild)
                KAssetVersionControl.TryMarkBuildVersion(path);

            path = __GetPrefabBuildPath(path);
            var buildResult = __DoBuildScriptableObject(DepBuildToFolder + "/Mat_" + path, sMat, needBuild);

            return buildResult.Path;
        }

        return "";
    }

    // 只导出材质信息，不实际导出材质
    static KSerializeMaterial __DoExportMaterial(Material mat, string buildToFolder, float scaleTexture = 1f)
    {
        string matPath = AssetDatabase.GetAssetPath(mat).Replace("Assets/", "");
        if (string.IsNullOrEmpty(matPath))
        {
            Logger.LogWarning("没有路径材质Material: {0}   可能是动态创建的？", mat.name);
            return null;
        }

        var props = new List<KSerializeMaterialProperty>();
        IEnumerator<KSerializeMaterialProperty> shaderPropEnumtor = _ShaderPropEnumtor(mat, buildToFolder, scaleTexture);
        while (shaderPropEnumtor.MoveNext())
        {
            KSerializeMaterialProperty shaderProp = shaderPropEnumtor.Current;
            if (shaderProp != null)
            {
                props.Add(shaderProp);
            }
        }

        KSerializeMaterial xMat = ScriptableObject.CreateInstance<KSerializeMaterial>();
        xMat.MaterialName = matPath;
        xMat.ShaderName = mat.shader.name;
        xMat.Props = props;

        return xMat;
    }


    static IEnumerator<KSerializeMaterialProperty> _ShaderPropEnumtor(Material mat, string buildToFolder, float scaleTexture = 1f)
    {
        var shader = mat.shader;
        if (shader != null)
        {
            for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var shaderType = ShaderUtil.GetPropertyType(shader, i);
                var shaderPropName  = ShaderUtil.GetPropertyName(shader, i);
                switch (shaderType)
                {
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        yield return _GetShaderTexProp(mat, shaderPropName, buildToFolder, scaleTexture);
                        break;
                    case ShaderUtil.ShaderPropertyType.Color:
                        yield return _GetMatColor(mat, shaderPropName);
                        break;
                    case ShaderUtil.ShaderPropertyType.Range:
                    case ShaderUtil.ShaderPropertyType.Float:  // TODO: 未确定Mat.GetInt是float还是range
                        yield return _GetMatRange(mat, shaderPropName);
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        yield return  _GetShaderVectorProp(mat, shaderPropName);
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

    static KSerializeMaterialProperty _GetShaderTexProp(Material mm, string texProp, string buildToFolder, float scaleTexture = 1f)
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
                    var texTiling = mm.GetTextureScale(texProp);  // 纹理+tiling+offset
                    var texOffset = mm.GetTextureOffset(texProp);
                    var texPath = BuildDepTexture(tex, scaleTexture);
                    shaderProp.Type = KSerializeMaterialProperty.ShaderType.Texture;

                    shaderProp.PropValue = string.Format("{0}|{1}|{2}|{3}|{4}", texPath, texTiling.x, texTiling.y,
                        texOffset.x, texOffset.y);
                }
                else
                {
                    Logger.LogWarning("找到一个非Texture2D, Type:{0} Mat:{1} PropName:{2}", tex.GetType(), mm.name, texProp);
                    shaderProp.Type = KSerializeMaterialProperty.ShaderType.RenderTexture; // Shader的RenderTexture不打包，一般由脚本动态生成
                    shaderProp.PropValue = null;
                }
                return shaderProp;
            }
            else
            {
                Logger.Log("[_GetShaderTexProp]处理纹理时发现获取不到纹理, 材质{0}  Shader属性{1}", mm.name, texProp);
                return null;
            }
        }
        return null;
    }

    static KSerializeMaterialProperty _GetShaderVectorProp(Material mm, string texProp)
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

    static KSerializeMaterialProperty _GetMatColor(Material mm, string colorProp)
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
    static KSerializeMaterialProperty _GetMatRange(Material mm, string propName)
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