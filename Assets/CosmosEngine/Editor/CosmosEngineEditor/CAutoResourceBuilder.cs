//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                     Version 0.8 (20140904)
//                     Copyright © 2011-2014
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
        string ext = export.GetExtention();
        string[] itemArray;

        if (ext.StartsWith("dir:"))  // 目錄下的所有文件，包括子文件夾
        {
            string newExt = ext.Replace("dir:", "");
            itemArray = Directory.GetFiles("Assets/_ResourcesBuild_/" + export.GetDirectory(), newExt, SearchOption.AllDirectories);
        }
        else if (ext == "dir")
            itemArray = Directory.GetDirectories("Assets/_ResourcesBuild_/" + export.GetDirectory());
        else if (ext == "")
            itemArray = new string[0];
        else
            itemArray = Directory.GetFiles("Assets/_ResourcesBuild_/" + export.GetDirectory(), export.GetExtention());  // 不包括子文件夾

        export.BeforeExport();
        foreach (string item in itemArray)
        {
            export.Export(item.Replace('\\', '/'));
        }
        export.AfterExport();
    }

}
