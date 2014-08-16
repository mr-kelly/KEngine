//-------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//-------------------------------------------------------------------------
using UnityEngine;

// Art Script里的一些定义
public class XArtScriptDefine
{
	// product库，editor or standalone, 手机上不需要
	public static string AppPath
	{
		get
		{
            string productRelPath = System.IO.Path.Combine(Application.dataPath, CCosmosEngine.GetConfig("ProductRelPath"));
            string productDirName = System.IO.Path.GetFileName(productRelPath);
            string productDir = productDirName + "/";
			string appPath = "";

			switch (Application.platform)
			{
				case RuntimePlatform.WindowsEditor:
				case RuntimePlatform.OSXEditor:
					{
                        appPath = productRelPath + "/";
					}
					break;
				case RuntimePlatform.WindowsPlayer:
				case RuntimePlatform.OSXPlayer:
					{
                        string path = Application.dataPath + "/../";
                        appPath = path;
					}
					break;
				case RuntimePlatform.Android:
					{
						appPath = "jar:file://" + Application.dataPath + "!/assets/";
					}
					break;
				case RuntimePlatform.IPhonePlayer:
					{
						appPath = Application.dataPath + "/Raw/";
					}
					break;
				default:
					{
					}
					break;
			}

			return appPath;
		}
	}

	
	public const string PREFIX_SKILL_EDITOR = "__SKILL__";  // 技能编辑器生成的对象都带这个前缀，停止播放时自动删除
	public static string GetNameForSkEditor(string name)
	{
		return PREFIX_SKILL_EDITOR + name;
	}
}
