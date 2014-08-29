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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[CModuleDependency(typeof(CResourceManager))]
public class CUIManager : ICModule
{
    class _InstanceClass {public static CUIManager _Instance = new CUIManager();}
    public static CUIManager Instance { get { return _InstanceClass._Instance; } }

    public class CUIState
    {
        public string Name;
        public CUIConfig UISetting;
        public CUIController UIWindow;
        public string UIType;
        public bool IsLoading;
        public bool IsStaticUI; // 非复制出来的, 静态UI

        public bool OpenWhenFinish;
        public object[] OpenArgs;

        public Dictionary<string, Texture> TextureDict = new Dictionary<string, Texture>();

        public bool DestroyAfterClose;
        public bool NotDestroyAfterLeave = false;

        public Queue<Action<CUIController, object[]>> CallbacksWhenFinish;
        public Queue<object[]> CallbacksArgsWhenFinish;

        public CUIState(string _UITypeName)
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

    public ICUIBridge UiBridge;

    public Dictionary<string, CUIState> UIWindows = new Dictionary<string, CUIState>();
    Dictionary<int, CUIState> MutexUI = new Dictionary<int, CUIState>(); // 静态（非复制）UI界面的互斥状态

    public bool UIRootLoaded = false;

    private CUIManager()
    {
    }

    public IEnumerator Init()
    {
        Type bridgeType = Type.GetType(CCosmosEngine.GetConfig("UIBridgeType"));
        UiBridge = Activator.CreateInstance(bridgeType) as ICUIBridge;
        UiBridge.InitBridge();
        yield break;
    }

    public IEnumerator UnInit()
    {
        yield break;
    }

    public void OpenWindow(Type type, params object[] args)
    {
        string uiName = type.Name.Remove(0, 3); // 去掉"XUI"
        OpenWindow(uiName, args);
    }

    public void OpenWindow<T>(params object[] args) where T : CUIController
    {
        OpenWindow(typeof(T), args);
    }

    // 打开窗口（非复制）
    public void OpenWindow(string name, params object[] args)
    {
        CUIState uiState;
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

    // Dynamic动态窗口，复制基准面板，需预加载
    public void OpenDynamicWindow(string template, string name, params object[] args)
    {
        CUIState uiState = _GetUIState(name);
        if (uiState != null)
        {
            OnOpen(uiState, args);
            return;
        }

        CallUI(template, (_ui, _args) => {  // _args useless
            CUIState uiInstanceState;
            CUIState uiTemplateState = _GetUIState(template);
            if (!UIWindows.TryGetValue(name, out uiInstanceState)) // 实例创建
            {
                uiInstanceState = new CUIState(template);
                uiInstanceState.UISetting = uiTemplateState.UISetting;
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
        CUIConfig uiConfig = uiObj.GetComponent<CUIConfig>();

        uiObj.name = name;


        UiBridge.UIObjectFilter(uiConfig, uiObj);

        CUIController uiBase = uiObj.GetComponent<CUIController>();
        uiBase.UITemplateName = template;
        uiBase.UIName = name;

        CUIState _instanceUIState = UIWindows[name];

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
        CUIState uiState;
        if (!UIWindows.TryGetValue(name, out uiState))
        {
            return; // 未开始Load
        }

        if (uiState.IsLoading)  // Loading中
        {
            uiState.OpenWhenFinish = false;
            return;
        }

        int mutexId = uiState.UISetting.MutexId;
        if (mutexId > 0)
        {
            if (Debug.isDebugBuild)
            {
                CUIState mutexState;
                if (MutexUI.TryGetValue(mutexId, out mutexState) && mutexState != null && mutexState != uiState)
                {
                    CBase.LogError("[CloseWindow]MutexUI互斥有问题，关闭时发现已经有另一个互斥Ui被打开了: {0}", mutexState.Name);
                }
            }
            MutexUI[mutexId] = null;
        }

        uiState.UIWindow.gameObject.SetActive(false);
        uiState.UIWindow.OnClose();

        if (uiState.DestroyAfterClose)
            DestroyWindow(name);
    }

    public void DestroyAllWindows()
    {
        List<string> LoadList = new List<string>();

        foreach (KeyValuePair<string, CUIState> uiWindow in UIWindows)
        {
            if (IsLoad(uiWindow.Key))
            {
                LoadList.Add(uiWindow.Key);
            }
        }

        foreach (string item in LoadList)
            DestroyWindow(item);

    }

    public void DestroyWindowsAfterLeave()
    {
        List<string> LoadList = new List<string>();

        foreach (KeyValuePair<string, CUIState> uiWindow in UIWindows)
        {
            if (IsLoad(uiWindow.Key) && (!uiWindow.Value.NotDestroyAfterLeave))
            {
                LoadList.Add(uiWindow.Key);
            }
        }

        foreach (string item in LoadList)
            DestroyWindow(item);
    }

    public void CloseAllWindows()
    {
        foreach (KeyValuePair<string, CUIState> uiWindow in UIWindows)
        {
            if (IsOpen(uiWindow.Key))
            {
                CloseWindow(uiWindow.Key);
            }
        }
    }

    CUIState _GetUIState(string name)
    {
        CUIState uiState;
        UIWindows.TryGetValue(name, out uiState);
        if (uiState != null)
            return uiState;

        return null;
    }

    CUIController GetUIBase(string name)
    {
        CUIState uiState;
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

    public CUIState LoadWindow(string name, bool openWhenFinish, params object[] args)
    {
        if (UIWindows.ContainsKey(name))
            CBase.LogError("[LoadWindow]多次重复LoadWindow: {0}", name);
        CBase.Assert(!UIWindows.ContainsKey(name));

        string path = string.Format("UI/{0}_UI{1}", name, CCosmosEngine.GetConfig("AssetBundleExt"));

        CUIState openState = new CUIState(name);
        openState.IsStaticUI = true;
        openState.OpenArgs = args;
        openState.OpenWhenFinish = openWhenFinish;
        openState.TextureDict.Clear();
        //openState.DestroyAfterClose = uiSetting.DestroyAfterClose;
        //openState.NotDestroyAfterLeave = uiSetting.NotDestroyAfterLeave;
        //openState.UISetting = uiSetting;

        CResourceManager.Instance.StartCoroutine(LoadUIAssetBundle(path, name, openState));

        UIWindows.Add(name, openState);

        return openState;
    }

    IEnumerator LoadUIAssetBundle(string path, string name, CUIState openState)
    {
        XAssetLoader assetLoader = new XAssetLoader(path);
        while (!assetLoader.IsFinished)
            yield return null;

        GameObject uiObj = (GameObject)assetLoader.Asset;

        CUIConfig uiConfig = uiObj.GetComponent<CUIConfig>();
        CBase.Assert(uiConfig);
        openState.DestroyAfterClose = uiConfig.DestroyAfterClose;
        openState.NotDestroyAfterLeave = uiConfig.NotDestroyAfterLeave;
        openState.UISetting = uiConfig;
        openState.IsLoading = false;

        uiObj.SetActive(false);
        uiObj.name = openState.Name;

        UiBridge.UIObjectFilter(uiConfig, uiObj);
        
        CUIController uiBase = (CUIController)uiObj.AddComponent(openState.UIType);

        openState.UIWindow = uiBase;

        uiBase.UIName = uiBase.UITemplateName = openState.Name;
        InitWindow(openState, uiBase, openState.OpenWhenFinish, openState.OpenArgs);
        OnUIWindowLoaded(openState, uiBase);

    }

    public void DestroyWindow(string name)
    {
        CUIState uiState;
        UIWindows.TryGetValue(name, out uiState);
        if (uiState == null || uiState.UIWindow == null)
        {
            CBase.Log("{0} has been destroyed", name);
            return;
        }

        if (!uiState.UIWindow.IsDynamicWindow)
        {
            //foreach (var item in uiState.TextureDict)
            //{
            //    //CResourceManager.DestroyTexture(item.Value);
            //}
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

        CUIState uiState;
        if (!UIWindows.TryGetValue(uiName, out uiState))
        {
            uiState = LoadWindow(uiName, false);  // 加载，这样就有UIState了
        }

        if (uiState.IsLoading) // Loading
        {
            CUIState openState = UIWindows[uiName];
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

    [System.Obsolete("[警告]CallUIMethod过时了~ 请尽快改成CallUI()")]
    public void CallUIMethod(string uiName, string methodName, params object[] args)
    {
        CBase.LogError("[警告]CallUIMethod过时了~ 请尽快改成CallUI()");

        CUIController uiBase = GetUIBase(uiName);
        if (uiBase == null)
        {
            CBase.LogWarning("CallUIMethod Failed, UI not load, UI={0}, Method={1}", uiName, methodName);
            return;
        }

        MethodInfo method = uiBase.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            CBase.LogError("CallUIMethod Failed, invalid method, UI={0}, Method={1}", uiName, methodName);
            return;
        }

        try
        {
            method.Invoke(uiBase, args);
        }
        catch (System.ArgumentException ex)
        {
            CBase.LogError("CallUIMethod Failed, runtime error, {0}, UI={1}, Method={2}", ex.Message, uiName, methodName);
        }
    }

    void OnOpen(CUIState uiState, params object[] args)
    {

        if (uiState.UISetting == null)
        {
            CBase.LogError("無UiConfig {0}", uiState.Name);
        }

        int mutexId = uiState.UISetting.MutexId;

        CUIState mutexState;
        if (mutexId > 0 && MutexUI.TryGetValue(mutexId, out mutexState) && mutexState != null
            && mutexState != uiState) // 不是重复打开同一个
        {
            CloseWindow(mutexState.Name);
        }
        MutexUI[mutexId] = uiState;

        CUIController uiBase = uiState.UIWindow;
        uiBase.OnPreOpen();
        if (uiBase.gameObject.activeSelf)
            uiBase.OnClose();
        else
            uiBase.gameObject.SetActive(true);

        uiBase.OnOpen(args);
    }


    void InitWindow(CUIState uiState, CUIController uiBase, bool open, params object[] args)
    {
        uiBase.OnInit();

        if (open)
        {
            OnOpen(uiState, args);
            uiBase.gameObject.SetActive(true);
        }

    }
    void OnUIWindowLoaded(CUIState uiState, CUIController uiBase)
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
