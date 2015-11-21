using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using KEngine;
public partial class KBuildTools
{
    static KBuildTools()
    {
        BeforeBuildAssetBundleEvent -= BeforeBuildAssetBundle;
        BeforeBuildAssetBundleEvent += BeforeBuildAssetBundle;
        AfterBuildAssetBundleEvent -= AfterBuildAssetBundle;
        AfterBuildAssetBundleEvent += AfterBuildAssetBundle;
    }


    // svn版本記錄, 記錄版本號，MD5, 相对于Assets
    public static string GetSvnRevMd5Tab()
    {
        return KEngineDef.ResourcesBuildInfosDir + "/ResourcesMD5_" + KResourceModule.BuildPlatformName+ ".txt";
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
    static void EncryptAssetBundle(string abPath, string relativePath)
    {
        if (!File.Exists(abPath))
        {
            Logger.LogError("[EncryptAssetBundle]Cannot Find File: {0}", abPath);
            return;
        }

        using (FileStream stream = File.Open(abPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {

            if (stream.Length <= 2)
            {
                Logger.LogError("[EncryptAssetBundle]Stream大小过短！ ： {0}", relativePath);
                return;
            }

            //stream.Position = stream.Length;  // 去到最后
            //byte[] md5Str = CTool.MD5_bytes(relativePath);
            //stream.Write(md5Str, 0, md5Str.Length);  // append 16bit md5 of relativePath

            // 第二位修改
            stream.Position = 1;

            byte[] getSome = new byte[1];
            stream.Read(getSome, 0, 1);
            if (getSome[0] == 0)
                getSome[0] = 255;  // 第二位-1
            else
                getSome[0]--;

            stream.Position = 1;
            stream.Write(getSome, 0, 1);// re write in
        }
    }


    #region Hook函數
    static void BeforeBuildAssetBundle(Object asset, string path, string relativePath)
    {
        //Logger.Log("No Func in BeforeBuildAssetBundle");
    }
    static void AfterBuildAssetBundle(Object asset, string path, string relativePath)
    {
        //EncryptAssetBundle(path, relativePath);
    }
    #endregion
}
