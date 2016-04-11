#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KBuildTools_AsssetDepExtend.cs
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

using System.IO;
using KEngine;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public partial class KBuildTools_AssetDep
{
    static KBuildTools_AssetDep()
    {
        KBuildTools.BeforeBuildAssetBundleEvent -= BeforeBuildAssetBundle;
        KBuildTools.BeforeBuildAssetBundleEvent += BeforeBuildAssetBundle;
        KBuildTools.AfterBuildAssetBundleEvent -= AfterBuildAssetBundle;
        KBuildTools.AfterBuildAssetBundleEvent += AfterBuildAssetBundle;
    }


    // svn版本記錄, 記錄版本號，MD5, 相对于Assets
    public static string GetSvnRevMd5Tab()
    {
        return KEngineDef.ResourcesBuildInfosDir + "/ResourcesMD5_" + KResourceModule.BuildPlatformName + ".txt";
    }


    public static Material CreateTempIlluminMaterial(Texture tex)
    {
        Shader DefaultShader = Shader.Find("Self-Illumin/Diffuse");
        Material mat = new Material(DefaultShader);
        mat.SetTexture("_MainTex", tex);
        mat.SetColor("_Color", new Color(1, 1, 1));
        AssetDatabase.CreateAsset(mat, "Assets/~TempMaterial.mat");
        return mat;
    }

    public static void DeleteTempIlluminMaterial()
    {
        if (File.Exists("Assets/~TempMaterial.mat"))
        {
            AssetDatabase.DeleteAsset("Assets/~TempMaterial.mat");
        }
    }

    // relativePath是就是客戶端用來讀取的資源名
    private static void EncryptAssetBundle(string abPath, string relativePath)
    {
        if (!File.Exists(abPath))
        {
            KLogger.LogError("[EncryptAssetBundle]Cannot Find File: {0}", abPath);
            return;
        }

        using (FileStream stream = File.Open(abPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            if (stream.Length <= 2)
            {
                KLogger.LogError("[EncryptAssetBundle]Stream大小过短！ ： {0}", relativePath);
                return;
            }

            //stream.Position = stream.Length;  // 去到最后
            //byte[] md5Str = KTool.MD5_bytes(relativePath);
            //stream.Write(md5Str, 0, md5Str.Length);  // append 16bit md5 of relativePath

            // 第二位修改
            stream.Position = 1;

            byte[] getSome = new byte[1];
            stream.Read(getSome, 0, 1);
            if (getSome[0] == 0)
                getSome[0] = 255; // 第二位-1
            else
                getSome[0]--;

            stream.Position = 1;
            stream.Write(getSome, 0, 1); // re write in
        }
    }

    #region Hook函數

    private static void BeforeBuildAssetBundle(Object asset, string path, string relativePath)
    {
        //KLogger.Log("No Func in BeforeBuildAssetBundle");
    }

    private static void AfterBuildAssetBundle(Object asset, string path, string relativePath)
    {
        //EncryptAssetBundle(path, relativePath);
    }

    #endregion
}