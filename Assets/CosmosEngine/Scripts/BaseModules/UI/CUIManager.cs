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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// UI Module
/// </summary>
[CDependencyClass(typeof(CResourceManager))]
public class CUIManager : ICModule
{
    class _InstanceClass {public static CUIManager _Instance = new CUIManager();}
    public static CUIManager Instance { get { return _InstanceClass._Instance; } }

    /// <summary>
    /// A bridge for different UI System, for instance, you can use NGUI or EZGUI or etc.. UI Plugin through UIBridge
    /// </summary>
    public ICUIBridge UiBridge;
    public Dictionary<string, CUILoadState> UIWindows = new Dictionary<string, CUILoadState>();
    public bool UIRootLoaded = false;

    public event System.Action<string> OpenWindowEvent;
    public event System.Action<string> CloseWindowEvent;

    private CUIManager()
    {
    }

    public IEnumerator Init()
    {
        Type bridgeType = Type.GetType(string.Format("C{0}Bridge", CCosmosEngine.GetConfig("UIBridgeType")));
        if (bridgeType != null)
        {
            UiBridge = Activator.CreateInstance(bridgeType) as ICUIBridge;
            UiBridge.InitBridge();
        }
        else
            CBase.Log("No UI Bridge in Use.");
        
        yield break;
    }

    public IEnumerator UnInit()
    {
        yield break;
    }

    public void OpenWindow(Type type, params object[] args)
    {
        string uiName = type.Name.Remove(0, 3); // 去掉"CUI"
        OpenWindow(uiName, args);
    }

    public void OpenWindow<T>(params object[] args) where T : CUIController
    {
        OpenWindow(typeof(T), args);
    }

    // 打开窗口（非复制）
    public void OpenWindow(string name, params object[] args)
    {
        CUILoadState uiState;
        if (!UIWindows.TryGetValue(name, out uiState))
        {
            LoadWindow(name, true, args);
            return;
        }

        if (uiState.IsLoading)
        {
            uiState.OpenWhenFinish = true;
            return;
        }

        OnOpen(uiState, args);
    }

    // 隐藏时打开，打开时隐藏
    public void ToggleWindow<T>(params object[] args)
    {
        string uiName = typeof(T).Name.Remove(0, 3); // 去掉"XUI"
        ToggleWindow(uiName, args);
    }
    public void ToggleWindow(string name, params object[] args)
    {
        if (IsOpen(name))
        {
            CloseWindow(name);
        }
        else
        {
            OpenWindow(name, args);
        }
    }

    
    /// <summary>
    /// // Dynamic动态窗口，复制基准面板
    /// </summary>
    public void OpenDynamicWindow(string template, string name, params object[] args)
    {
        CUILoadState uiState = _GetUIState(name);
        if (uiState != null)
        {
            OnOpen(uiState, args);
            return;
        }

        CallUI(template, (_ui, _args) => {  // _args useless
            CUILoadState uiInstanceState;
            CUILoadState uiTemplateState = _GetUIState(template);
            if (!UIWindows.TryGetValue(name, out uiInstanceState)) // 实例创建
            {
                uiInstanceState = new CUILoadState(template);
                uiInstanceState.IsStaticUI = false;
                uiInstanceState.IsLoading = true;
                uiInstanceState.UIWindow = null;
                UIWindows[name] = uiInstanceState;
            }

            // 组合template和name的参数 和args外部参数
            object[] totalArgs = new object[args.Length + 2];
            totalArgs[0] = template;
            totalArgs[1] = name;
            args.CopyTo(totalArgs, 2);

            OnDynamicWindowCallback(uiTemplateState.UIWindow, totalArgs);
        });
    }

    void OnDynamicWindowCallback(CUIController _ui, object[] _args)
    {
        string template = (string)_args[0];
        string name = (string)_args[1];

        GameObject uiObj = (GameObject)UnityEngine.Object.Instantiate(_ui.gameObject);

        uiObj.name = name;

        UiBridge.UIObjectFilter(uiObj);

        CUIController uiBase = uiObj.GetComponent<CUIController>();
        uiBase.UITemplateName = template;
        uiBase.UIName = name;

        CUILoadState _instanceUIState = UIWindows[name];

        _instanceUIState.IsLoading = false;
        _instanceUIState.UIWindow = uiBase;

        object[] originArgs = new object[_args.Length - 2];  // 去除前2个参数
        for (int i = 2; i < _args.Length; i++)
            originArgs[i - 2] = _args[i];

        InitWindow(_instanceUIState, uiBase, true, originArgs);
        OnUIWindowLoaded(_instanceUIState, uiBase);
    }

    public void CloseWindow(Type t)
    {
        CloseWindow(t.Name.Remove(0, 3)); // XUI remove
    }

    public void CloseWindow<T>()
    {
        CloseWindow(typeof(T));
    }

    public void CloseWindow(string name)
    {
        if (CloseWindowEvent != null)
            CloseWindowEvent(name);

        CUILoadState uiState;
        if (!UIWindows.TryGetValue(name, out uiState))
        {
            return; // 未开始Load
        }

        if (uiState.IsLoading)  // Loading中
        {
            uiState.OpenWhenFinish = false;
            return;
        }

        uiState.UIWindow.gameObject.SetActive(false);
        uiState.UIWindow.OnClose();

        if (!uiState.IsStaticUI)
            DestroyWindow(name);
    }

    /// <summary>
    /// Destroy all windows that has LoadState.
    /// Be careful to use.
    /// </summary>
    public void DestroyAllWindows()
    {
        List<string> LoadList = new List<string>();

        foreach (KeyValuePair<string, CUILoadState> uiWindow in UIWindows)
        {
            if (IsLoad(uiWindow.Key))
            {
                LoadList.Add(uiWindow.Key);
            }
        }

        foreach (string item in LoadList)
            DestroyWindow(item);

    }

    public void CloseAllWindows()
    {
        foreach (KeyValuePair<string, CUILoadState> uiWindow in UIWindows)
        {
            if (IsOpen(uiWindow.Key))
            {
                CloseWindow(uiWindow.Key);
            }
        }
    }

    CUILoadState _GetUIState(string name)
    {
        CUILoadState uiState;
        UIWindows.TryGetValue(name, out uiState);
        if (uiState != null)
            return uiState;

        return null;
    }

    CUIController GetUIBase(string name)
    {
        CUILoadState uiState;
        UIWindows.TryGetValue(name, out uiState);
        if (uiState != null && uiState.UIWindow != null)
            return uiState.UIWindow;

        return null;
    }

    public bool IsOpen(string name)
    {
        CUIController uiBase = GetUIBase(name);
        return uiBase == null ? false : uiBase.gameObject.activeSelf;
    }

    public bool IsLoad(string name)
    {
        if (UIWindows.ContainsKey(name))
            return true;
        return false;
    }

    public CUILoadState LoadWindow(string name, bool openWhenFinish, params object[] args)
    {
        if (UIWindows.ContainsKey(name))
            CBase.LogError("[LoadWindow]多次重复LoadWindow: {0}", name);
        CBase.Assert(!UIWindows.ContainsKey(name));

        string path = string.Format("UI/{0}_UI{1}", name, CCosmosEngine.GetConfig("AssetBundleExt"));

        CUILoadState openState = new CUILoadState(name);
        openState.IsStaticUI = true;
        openState.OpenArgs = args;
        openState.OpenWhenFinish = openWhenFinish;

        CResourceManager.Instance.StartCoroutine(LoadUIAssetBundle(path, name, openState));

        UIWindows.Add(name, openState);

        return openState;
    }

    IEnumerator LoadUIAssetBundle(string path, string name, CUILoadState openState)
    {
        CAssetLoader assetLoader = new CAssetLoader(path);
        while (!assetLoader.IsFinished)
            yield return null;

        GameObject uiObj = (GameObject)assetLoader.Asset;

        openState.IsLoading = false;

        uiObj.SetActive(false);
        uiObj.name = openState.Name;

        UiBridge.UIObjectFilter(uiObj);
        
        CUIController uiBase = (CUIController)uiObj.AddComponent(openState.UIType);

        openState.UIWindow = uiBase;

        uiBase.UIName = uiBase.UITemplateName = openState.Name;
        InitWindow(openState, uiBase, openState.OpenWhenFinish, openState.OpenArgs);
        OnUIWindowLoaded(openState, uiBase);

    }

    public void DestroyWindow(string name)
    {
        CUILoadState uiState;
        UIWindows.TryGetValue(name, out uiState);
        if (uiState == null || uiState.UIWindow == null)
        {
            CBase.Log("{0} has been destroyed", name);
            return;
        }

        UnityEngine.Object.Destroy(uiState.UIWindow.gameObject);

        uiState.UIWindow = null;

        UIWindows.Remove(name);
    }

    /// <summary>
    /// 等待并获取UI实例，执行callback
    /// 源起Loadindg UI， 在加载过程中，进度条设置方法会失效
    /// </summary>
    /// <param name="uiName"></param>
    /// <param name="callback"></param>
    /// <param name="args"></param>
    public void CallUI(string uiName, Action<CUIController, object[]> callback, params object[] args)
    {
        CBase.Assert(callback);

        CUILoadState uiState;
        if (!UIWindows.TryGetValue(uiName, out uiState))
        {
            uiState = LoadWindow(uiName, false);  // 加载，这样就有UIState了
        }

        if (uiState.IsLoading) // Loading
        {
            CUILoadState openState = UIWindows[uiName];
            openState.CallbacksWhenFinish.Enqueue(callback);
            openState.CallbacksArgsWhenFinish.Enqueue(args);
            return;
        }

        callback(uiState.UIWindow, args);
    }

    public void CallUI<T>(Action<T> callback) where T : CUIController
    {
        CallUI<T>((_ui, _args) => callback(_ui));
    }

    // 使用泛型方式
    public void CallUI<T>(Action<T, object[]> callback, params object[] args) where T : CUIController
    {
        string uiName = typeof(T).Name.Remove(0, 3); // 去掉 "XUI"

        CallUI(uiName, (CUIController _uibase, object[] _args) =>
        {
            callback((T)_uibase, _args);
        }, args);
    }

    void OnOpen(CUILoadState uiState, params object[] args)
    {
        if (OpenWindowEvent != null)
            OpenWindowEvent(uiState.UIType);

        CUIController uiBase = uiState.UIWindow;
        uiBase.OnPreOpen();
        if (uiBase.gameObject.activeSelf)
            uiBase.OnClose();
        else
            uiBase.gameObject.SetActive(true);

        uiBase.OnOpen(args);
    }


    void InitWindow(CUILoadState uiState, CUIController uiBase, bool open, params object[] args)
    {
        uiBase.OnInit();

        if (open)
        {
            OnOpen(uiState, args);
            uiBase.gameObject.SetActive(true);
        }

    }
    void OnUIWindowLoaded(CUILoadState uiState, CUIController uiBase)
    {
        //if (openState.OpenWhenFinish)  // 加载完打开 模式下，打开时执行回调
        {
            while (uiState.CallbacksWhenFinish.Count > 0)
            {
                Action<CUIController, object[]> callback = uiState.CallbacksWhenFinish.Dequeue();
                object[] _args = uiState.CallbacksArgsWhenFinish.Dequeue();
                callback(uiBase, _args);
            }
        }
    }
}

/// <summary>
/// UI Async Load State class
/// </summary>
public class CUILoadState
{
    public string Name;
    public CUIController UIWindow;
    public string UIType;
    public bool IsLoading;
    public bool IsStaticUI; // 非复制出来的, 静态UI

    public bool OpenWhenFinish;
    public object[] OpenArgs;

    public Queue<Action<CUIController, object[]>> CallbacksWhenFinish;
    public Queue<object[]> CallbacksArgsWhenFinish;

    public CUILoadState(string _UITypeName)
    {
        Name = _UITypeName;
        UIWindow = null;
        UIType = "CUI" + _UITypeName;

        IsLoading = true;
        OpenWhenFinish = false;
        OpenArgs = null;

        CallbacksWhenFinish = new Queue<Action<CUIController, object[]>>();
        CallbacksArgsWhenFinish = new Queue<object[]>();
    }
}
