using UnityEngine;
using System.Collections;

/// <summary>
/// Unity原生UI桥接器 TODO:
/// </summary>
public class CUGUIBridge : ICUIBridge
{
    // Init the UI Bridge, necessary
    public void InitBridge()
    {
    }

    // Get a component inside the UI Bridge
    public object GetUIComponent(string comName)
    {
        return null;
    }

    public void UIObjectFilter(CUIConfig uiConfig, GameObject uiObject) { }
}
