using UnityEngine;
using System.Collections;

/// <summary>
/// 材质所包含的Shader属性，如_MainTexture等的记录
/// </summary>
[System.Serializable]
public class KSerializeMaterialProperty
{
    public enum ShaderType
    {
        Texture,
        Color,
        Range, // equal Float
        Vector,
        RenderTexture,
    }
    public ShaderType Type; // 0 纹理， 1 Color， 2 Range, 3 Vector, 4 RenderTexture
    public string PropName;
    public string PropValue;
}
