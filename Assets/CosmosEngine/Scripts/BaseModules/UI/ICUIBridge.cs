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

public interface ICUIBridge
{
    // Init the UI Bridge, necessary
    void InitBridge();

    // Get a component inside the UI Bridge
    object GetUIComponent(string comName);

    void UIObjectFilter(GameObject uiObject);
}
