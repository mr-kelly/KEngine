using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 序列化到AssetBundle的对象
/// 
/// 需要和打包工程保持一致的目录，
/// 当脚本所在目录改变，原打包的AssetBundle将失效，需要重新打包
/// </summary>
public class KSerializeMaterial : ScriptableObject
{
    public string MaterialName;
    public string ShaderName;
    public string ShaderPath;

    public List<KSerializeMaterialProperty> Props = new List<KSerializeMaterialProperty>();
}
