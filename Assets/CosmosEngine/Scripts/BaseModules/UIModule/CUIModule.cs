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

using CosmosEngine;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI Module
/// </summary>
[CDependencyClass(typeof(CResourceModule))]
public class CUIModule : ICModule
{
    class _InstanceClass { public static CUIModule _Instance = new CUIModule();}
    public static CUIModule Instance { get { return _InstanceClass._Instance; } }

    /// <summary>
    /// 正在加载的UI统计
    /// </summary>
    private int _loadingUICount = 0;
    public int LoadingUICount
    {
        get { return _loadingUICount; }
        set
        {
            _loadingUICount = value;
            if (_loadingUICount < 0) CDebug.LogError("Error ---- LoadingUICount < 0");
        }
    }

    /// <summary>
    /// A bridge for different UI System, for instance, you can use NGUI or EZGUI or etc.. UI Plugin through UIBridge
    /// </summary>
    public ICUIBridge UiBridge = new CUGUIBridge();
    public Dictionary<string, CUILoadState> UIWindows = new Dictionary<string, CUILoadState>();
    public bool UIRootLoaded = false;

    public static event Action<CUIController> OnInitEvent;

    public static event Action<CUIController> OnOpenEvent;
    public static event Action<CUIController> OnCloseEvent;

    private CUIModule()
    {
    }

    public void SetupUIBridge(ICUIBridge bridge)
    {
        UiBridge = bridge;
    }

    public IEnumerator Init()
    {
        if (UiBridge == null)
            UiBridge = new CUGUIBridge();

        UiBridge.InitBridge();

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
        Action doOpen = () =>
        {
            CUILoadState uiState;
            if (!UIWindows.TryGetValue(name, out uiState))
            {
                LoadWindow(name, true, args);
                return;
            }

            OnOpen(uiState, args);
        };

        doOpen();
    }

    // 隐藏时打开，打开时隐藏
    public void ToggleWindow<T>(params object[] args)
    {
        string uiName = typeof(T).Name.Remove(0, 3); // 去掉"CUI"
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
    public CUILoadState OpenDynamicWindow(string template, string instanceName, params object[] args)
    {
        CUILoadState uiState = _GetUIState(instanceName);
        if (uiState != null)
        {
            OnOpen(uiState, args);
            return uiState;
        }

        CUILoadState uiInstanceState;
        if (!UIWindows.TryGetValue(instanceName, out uiInstanceState)) // 实例创建
        {
            uiInstanceState = new CUILoadState(template, instanceName);
            uiInstanceState.IsStaticUI = false;
            uiInstanceState.IsLoading = true;
            uiInstanceState.UIWindow = null;
            uiInstanceState.OpenWhenFinish = true;
            UIWindows[instanceName] = uiInstanceState;
        }

        CallUI(template, (_ui, _args) =>
        {  // _args useless

            CUILoadState uiTemplateState = _GetUIState(template);

            // 组合template和name的参数 和args外部参数
            object[] totalArgs = new object[args.Length + 2];
            totalArgs[0] = template;
            totalArgs[1] = instanceName;
            args.CopyTo(totalArgs, 2);

            OnDynamicWindowCallback(uiTemplateState.UIWindow, totalArgs);
        });

        return uiInstanceState;
    }

    void OnDynamicWindowCallback(CUIController _ui, object[] _args)
    {
        string template = (string)_args[0];
        string name = (string)_args[1];

        GameObject uiObj = (GameObject)UnityEngine.Object.Instantiate(_ui.gameObject);

        uiObj.name = name;

        UiBridge.UIObjectFilter(_ui, uiObj);

        CUILoadState instanceUIState = UIWindows[name];
        instanceUIState.IsLoading = false;

        CUIController uiBase = uiObj.GetComponent<CUIController>();
        uiBase.UITemplateName = template;
        uiBase.UIName = name;

        instanceUIState.UIWindow = uiBase;

        object[] originArgs = new object[_args.Length - 2];  // 去除前2个参数
        for (int i = 2; i < _args.Length; i++)
            originArgs[i - 2] = _args[i];
        InitWindow(instanceUIState, uiBase, instanceUIState.OpenWhenFinish, originArgs);
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
        CUILoadState uiState;
        if (!UIWindows.TryGetValue(name, out uiState))
        {
            if (Debug.isDebugBuild)
                CDebug.LogWarning("[CloseWindow]没有加载的UIWindow: {0}", name);
            return; // 未开始Load
        }

        if (uiState.IsLoading)  // Loading中
        {
            if (Debug.isDebugBuild)
                CDebug.Log("[CloseWindow]IsLoading的{0}", name);
            uiState.OpenWhenFinish = false;
            return;
        }

        Action doCloseAction = () =>
        {
            uiState.UIWindow.gameObject.SetActive(false);

            uiState.UIWindow.OnClose();

            if (OnCloseEvent != null)
                OnCloseEvent(uiState.UIWindow);

            if (!uiState.IsStaticUI)
            {
                DestroyWindow(name);
            }
        };

        doCloseAction();
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

    [Obsolete("Deprecated: Please don't use this")]
    public void CloseAllWindows()
    {
        List<string> toCloses = new List<string>();

        foreach (KeyValuePair<string, CUILoadState> uiWindow in UIWindows)
        {
            if (IsOpen(uiWindow.Key))
            {
                toCloses.Add(uiWindow.Key);
            }
        }

        for (int i = toCloses.Count - 1; i >= 0; i--)
        {
            CloseWindow(toCloses[i]);
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

    public bool IsOpen<T>() where T : CUIController
    {
        string uiName = typeof(T).Name.Remove(0, 3); // 去掉"CUI"
        return IsOpen(uiName);
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

    public CUILoadState LoadWindow(string windowTemplateName, bool openWhenFinish, params object[] args)
    {
        if (UIWindows.ContainsKey(windowTemplateName))
        {
            CDebug.LogError("[LoadWindow]多次重复LoadWindow: {0}", windowTemplateName);
        }
        CDebug.Assert(!UIWindows.ContainsKey(windowTemplateName));

        string path = string.Format("UI/{0}_UI{1}", windowTemplateName, CCosmosEngine.GetConfig("AssetBundleExt"));

        CUILoadState openState = new CUILoadState(windowTemplateName, windowTemplateName);
        openState.IsStaticUI = true;
        openState.OpenArgs = args;

        //if (openState.IsLoading)
        openState.OpenWhenFinish = openWhenFinish;

        CResourceModule.Instance.StartCoroutine(LoadUIAssetBundle(path, windowTemplateName, openState));

        UIWindows.Add(windowTemplateName, openState);

        return openState;
    }

    IEnumerator LoadUIAssetBundle(string path, string name, CUILoadState openState)
    {
        LoadingUICount++;
        var assetLoader = CStaticAssetLoader.Load(path);
        openState.UIResourceLoader = assetLoader;  // 基本不用手工释放的
        while (!assetLoader.IsFinished)
            yield return null;

        GameObject uiObj = (GameObject)assetLoader.TheAsset;

        uiObj.SetActive(false);
        uiObj.name = openState.TemplateName;

        CUIController uiBase = (CUIController)uiObj.AddComponent(openState.UIType);

        openState.UIWindow = uiBase;

        uiBase.UIName = uiBase.UITemplateName = openState.TemplateName;

        UiBridge.UIObjectFilter(uiBase, uiObj);

        openState.IsLoading = false;  // Load完
        InitWindow(openState, uiBase, openState.OpenWhenFinish, openState.OpenArgs);
        LoadingUICount--;
    }

    public void DestroyWindow(string name)
    {
        CUILoadState uiState;
        UIWindows.TryGetValue(name, out uiState);
        if (uiState == null || uiState.UIWindow == null)
        {
            CDebug.Log("{0} has been destroyed", name);
            return;
        }

        UnityEngine.Object.Destroy(uiState.UIWindow.gameObject);

        uiState.UIWindow = null;

        UIWindows.Remove(name);
    }

    /// <summary>
    /// 等待并获取UI实例，执行callback
    /// 源起Loadindg UI， 在加载过程中，进度条设置方法会失效
    /// 
    /// 如果是DynamicWindow,，使用前务必先要Open!
    /// </summary>
    /// <param name="uiTemplateName"></param>
    /// <param name="callback"></param>
    /// <param name="args"></param>
    public void CallUI(string uiTemplateName, Action<CUIController, object[]> callback, params object[] args)
    {
        CDebug.Assert(callback);

        CUILoadState uiState;
        if (!UIWindows.TryGetValue(uiTemplateName, out uiState))
        {
            uiState = LoadWindow(uiTemplateName, false);  // 加载，这样就有UIState了, 但注意因为没参数，不要随意执行OnOpen
        }

        uiState.DoCallback(callback, args);
    }

    /// <summary>
    /// DynamicWindow专用, 不会自动加载，会提示报错
    /// </summary>
    /// <param name="uiName"></param>
    /// <param name="callback"></param>
    /// <param name="args"></param>
    public void CallDynamicUI(string uiName, Action<CUIController, object[]> callback, params object[] args)
    {
        CDebug.Assert(callback);

        CUILoadState uiState;
        if (!UIWindows.TryGetValue(uiName, out uiState))
        {
            CDebug.LogError("找不到UIState: {0}", uiName);
            return;
        }

        CUILoadState openState = UIWindows[uiName];
        openState.DoCallback(callback, args);
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
        if (uiState.IsLoading)
        {
            uiState.OpenWhenFinish = true;
            uiState.OpenArgs = args;
            return;
        }

        CUIController uiBase = uiState.UIWindow;

        Action doOpenAction = () =>
        {
            if (uiBase.gameObject.activeSelf)
            {
                uiBase.OnClose();
            }

            uiBase.BeforeOpen(() =>
            {
                uiBase.gameObject.SetActive(true);

                uiBase.OnOpen(args);

                if (OnOpenEvent != null)
                    OnOpenEvent(uiBase);
            });
        };

        doOpenAction();
    }


    void InitWindow(CUILoadState uiState, CUIController uiBase, bool open, params object[] args)
    {
        uiBase.OnInit();
        if (OnInitEvent != null)
            OnInitEvent(uiBase);
        if (open)
        {
            OnOpen(uiState, args);

        }

        if (!open)
        {
            if (!uiState.IsStaticUI)
            {
                CloseWindow(uiBase.UIName); // Destroy
                return;
            }
            else
            {
                uiBase.gameObject.SetActive(false);
            }
        }

        uiState.OnUIWindowLoadedCallbacks(uiState, uiBase);


    }
}

/// <summary>
/// UI Async Load State class
/// </summary>
public class CUILoadState
{
    public string TemplateName;
    public string InstanceName;
    public CUIController UIWindow;
    public string UIType;
    public bool IsLoading;
    public bool IsStaticUI; // 非复制出来的, 静态UI

    public bool OpenWhenFinish;
    public object[] OpenArgs;

    internal Queue<Action<CUIController, object[]>> CallbacksWhenFinish;
    internal Queue<object[]> CallbacksArgsWhenFinish;
    public CBaseResourceLoader UIResourceLoader; // 加载器，用于手动释放资源

    public CUILoadState(string uiTypeTemplateName, string uiInstanceName)
    {
        TemplateName = uiTypeTemplateName;
        InstanceName = uiInstanceName;
        UIWindow = null;
        UIType = "CUI" + uiTypeTemplateName;

        IsLoading = true;
        OpenWhenFinish = false;
        OpenArgs = null;

        CallbacksWhenFinish = new Queue<Action<CUIController, object[]>>();
        CallbacksArgsWhenFinish = new Queue<object[]>();
    }

    

    /// <summary>
    /// 确保加载完成后的回调
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="args"></param>
    public void DoCallback(Action<CUIController, object[]> callback, object[] args = null)
    {
        if (args == null)
            args = new object[0];

        if (IsLoading) // Loading
        {
            CallbacksWhenFinish.Enqueue(callback);
            CallbacksArgsWhenFinish.Enqueue(args);
            return;
        }

        // 立即执行即可
        callback(UIWindow, args);
    }

    internal void OnUIWindowLoadedCallbacks(CUILoadState uiState, CUIController uiObject)
    {
        //if (openState.OpenWhenFinish)  // 加载完打开 模式下，打开时执行回调
        {
            while (uiState.CallbacksWhenFinish.Count > 0)
            {
                Action<CUIController, object[]> callback = uiState.CallbacksWhenFinish.Dequeue();
                object[] _args = uiState.CallbacksArgsWhenFinish.Dequeue();
                //callback(uiBase, _args);

                DoCallback(callback, _args);
            }
        }
    }
}
