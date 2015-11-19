using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CSerializeMaterial : ScriptableObject
{
    public string MaterialName;
    public string ShaderName;

    public List<CSerializeMaterialProperty> Props = new List<CSerializeMaterialProperty>();
}
