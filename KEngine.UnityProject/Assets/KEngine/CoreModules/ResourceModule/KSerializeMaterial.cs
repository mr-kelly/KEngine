using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class KSerializeMaterial : ScriptableObject
{
    public string MaterialName;
    public string ShaderName;

    public List<KSerializeMaterialProperty> Props = new List<KSerializeMaterialProperty>();
}
