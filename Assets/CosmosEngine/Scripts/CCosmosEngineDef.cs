using UnityEngine;
using System.Collections;
using System.IO;

public class CCosmosEngineDef
{
    // 美术库用到
    public const string ResourcesBuildDir = "_ResourcesBuild_";

    // 打包缓存，一些不同步的资源，在打包时拷到这个目录，并进行打包
    public const string ResourcesBuildCacheDir = "_ResourcesCache_";

    public const string ResourcesBuildInfosDir = "_ResourcesBuildInfos_";

    public const string RedundaciesDir = "_Redundancies_";
}
