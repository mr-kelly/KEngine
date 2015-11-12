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
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// Unity原生UI桥接器
/// </summary>
public class CUGUIBridge : ICUIBridge
{
    public EventSystem eventSystem;
    // Init the UI Bridge, necessary
    public void InitBridge()
    {
        eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
        eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        eventSystem.gameObject.AddComponent<TouchInputModule>(); 
    }

    // Get a component inside the UI Bridge
    public object GetUIComponent(string comName)
    {
        return null;
    }

    public void UIObjectFilter(CUIController ui, GameObject uiObject)
    {
    }
}
