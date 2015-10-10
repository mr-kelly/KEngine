//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Version 0.9.1 (20151010)
//                     Copyright © 2011-2015
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------

using System;
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

/// <summary>
/// Version Number
/// </summary>
/// 
/// <example>
/// 
/// <code>
/// var versionNumber = CVersionNumber.Parse("1.0.0.alpha");
/// Console.Write(versionNumber.Major);
/// </code>
/// 
/// Output 1
/// </example>
public struct CVersionNumber
{
    public int Major;
    public int Minor;
    public int Build;
    public string Flag;

    public CVersionNumber(CVersionNumber clone)
    {
        Major = clone.Major;
        Minor = clone.Minor;
        Build = clone.Build;
        Flag = clone.Flag;
    }
    public static CVersionNumber Parse(string verStr)
    {
        var verArgs = CTool.Split<string>(verStr, '.');
        return new CVersionNumber
        {
            Major = verArgs.Count >= 1 ? verArgs[0].ToInt32() : 0,
            Minor = verArgs.Count >= 2 ? verArgs[1].ToInt32() : 0,
            Build = verArgs.Count >= 3 ? verArgs[2].ToInt32() : 0,
            Flag =  verArgs.Count >= 4 ? verArgs[3] : ""
        };
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Major;
            hashCode = (hashCode * 397) ^ Minor;
            hashCode = (hashCode * 397) ^ Build;
            hashCode = (hashCode * 397) ^ (!string.IsNullOrEmpty(Flag) ? Flag.GetHashCode() : 0);
            return hashCode;
        }
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj.GetHashCode() == GetHashCode();
    }

    public static bool operator ==(CVersionNumber a, CVersionNumber b)
    {
        return a.Equals(b);
    }

    public static bool operator >(CVersionNumber a, CVersionNumber b)
    {
        var arr = a.GetNumberArray();
        var brr = b.GetNumberArray();
        for (var i = 0; i < arr.Length; i++)
        {
            if (arr[i] > brr[i])
                return true;
        }
        return false;
    }

    public static bool operator !=(CVersionNumber a, CVersionNumber b)
    {
        return !(a == b);
    }

    public static bool operator <(CVersionNumber a, CVersionNumber b)
    {
        var arr = a.GetNumberArray();
        var brr = b.GetNumberArray();
        for (var i = 0; i < arr.Length; i++)
        {
            if (arr[i] < brr[i])
                return true;
        }
        return false;
    }

    public string Full()
    {
        return ToString();
    }

    /// <summary>
    /// without build
    /// </summary>
    /// <returns></returns>
    public string Short()
    {
        var ver = string.Format("{0}.{1}", Major, Minor);
        if (!string.IsNullOrEmpty(Flag))
        {
            ver += "." + Flag;
        }
        return ver;
    }

    int[] GetNumberArray()
    {
        return new[] {Major, Minor, Build};
    }
    public override string ToString()
    {
        var ver = string.Format("{0}.{1}.{2}", Major, Minor, Build);
        if (!string.IsNullOrEmpty(Flag))
        {
            ver += "." + Flag;
        }
        return ver;
    }
}
