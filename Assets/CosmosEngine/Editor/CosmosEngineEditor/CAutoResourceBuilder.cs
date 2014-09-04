//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                          Version 0.8
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

public abstract class AutoBuildBase
{
	public virtual void BeginExport() { }
    public abstract void Export(string path);

	public virtual void EndExport() { }

	public abstract string GetDirectory();
	public abstract string GetExtention();
}

public partial class CAutoResourceBuilder
{
    public static void ProductExport(AutoBuildBase export)
    {
        string ext = export.GetExtention();
        string[] itemArray;

        if (ext == "dir")
            itemArray = Directory.GetDirectories("Assets/Product/" + export.GetDirectory());
        else if (ext == "")
            itemArray = new string[0];
        else
            itemArray = Directory.GetFiles("Assets/Product/" + export.GetDirectory(), export.GetExtention());

        export.BeginExport();
        foreach (string item in itemArray)
        {
            export.Export(item.Replace('\\', '/'));
        }
        export.EndExport();
    }

}
