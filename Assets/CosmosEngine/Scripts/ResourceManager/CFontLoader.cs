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
using System.Collections;
using System.Collections.Generic;

public class CFontLoader
{
	public CFontLoader(string path, System.Action<Font> callback)
	{
        new CAssetFileBridge(path, (_obj, _args) => {
            Font font = _obj as Font;
            callback(font);
        });
	}

}
