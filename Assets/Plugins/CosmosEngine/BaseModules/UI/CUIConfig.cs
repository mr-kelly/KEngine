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
using System.Collections;
using System.Collections.Generic;


// 綁在UI面板上面的
public class CUIConfig : MonoBehaviour
{
    public string UIName;
    public string Side;
    public float OffsetX;
    public float OffsetY;
    public float OffsetZ;

    public List<string> TextureNameList = new List<string>();

    public bool DestroyAfterClose;
    public bool NotDestroyAfterLeave;
    public int MutexId;  // 互斥编号
}
