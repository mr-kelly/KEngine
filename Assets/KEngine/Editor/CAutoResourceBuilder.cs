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
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using KEngine;

public abstract class CBuild_Base
{
    public virtual void BeforeExport() { }
    public abstract void Export(string path);

    public virtual void AfterExport() { }

    public abstract string GetDirectory();
    public abstract string GetExtention();
}

public partial class CAutoResourceBuilder
{
    public static void ProductExport(CBuild_Base export)
    {
        Logger.Log("Start Auto Build... {0}", export.GetType().Name);

        var time = DateTime.Now;
        try
        {
            string ext = export.GetExtention();
            string[] itemArray;

            if (ext.StartsWith("dir:"))  // 目錄下的所有文件，包括子文件夾
            {
                string newExt = ext.Replace("dir:", "");
                itemArray = Directory.GetFiles("Assets/" + CCosmosEngineDef.ResourcesBuildDir + "/" + export.GetDirectory(), newExt, SearchOption.AllDirectories);
            }
            else if (ext == "dir")
                itemArray = Directory.GetDirectories("Assets/" + CCosmosEngineDef.ResourcesBuildDir + "/" + export.GetDirectory());
            else if (ext == "")
                itemArray = new string[0];
            else
                itemArray = Directory.GetFiles("Assets/" + CCosmosEngineDef.ResourcesBuildDir + "/" + export.GetDirectory(), export.GetExtention());  // 不包括子文件夾

            export.BeforeExport();
            foreach (string item in itemArray)
            {
                EditorUtility.DisplayCancelableProgressBar("[ProductExport]", item, .5f);
                export.Export(item.Replace('\\', '/'));
                EditorUtility.ClearProgressBar();

                GC.Collect();
                Resources.UnloadUnusedAssets();
            }
            export.AfterExport();

        }
        catch (Exception e)
        {
            Logger.LogError("[Fail] Auto Build... {0}, Exception: {1}, Used Time: {2}, CurrentScene: {3}", 
                export.GetType().Name, 
                e.Message + "," + (e.InnerException != null ? e.InnerException.Message : ""), DateTime.Now - time, EditorApplication.currentScene);
        }

        GC.Collect();

        Logger.Log("Finish Auto Build... {0}, Used Time: {1}", export.GetType().Name, DateTime.Now - time);
    }

}
