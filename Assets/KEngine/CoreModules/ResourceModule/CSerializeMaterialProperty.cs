using UnityEngine;
using System.Collections;

[System.Serializable]
public class CSerializeMaterialProperty
{
    public enum ShaderType
    {
        Texture,
        Color,
        Range, // Float
        Vector,
        RenderTexture,
    }
    public ShaderType Type; // 0 纹理， 1 Color， 2 Range, 3 Vector, 4 RenderTexture
    public string PropName;
    public string PropValue;
}
