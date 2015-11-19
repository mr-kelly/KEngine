using System;
using System.Collections;
using System.Collections.Generic;
using KEngine;
using UnityEngine;

/// <summary>
/// 基础依赖组件, 由AssetDepBuilder派发
/// </summary>
public abstract class CAssetDep : MonoBehaviour
{
    // 依赖加载出来的对象容器
    private static GameObject _DependenciesContainer;

    public static GameObject DependenciesContainer
    {
        get { return _DependenciesContainer ?? (_DependenciesContainer = new GameObject("[AssetDep]")); }
    }

    private GameObject _cacheGameObject;
    protected bool _NewIsInit = false;

    public Component DependencyComponent;  // 依赖的脚本控件，完整依赖加载后会用到它
    public string ResourcePath;
    public string AssetName;  // 没用了！保留的序列化字段
    public object[] Args;

    public bool IsFinishDependency = false;  // 默认不完成
    public UnityEngine.Object DependencyObject;

    protected static bool IsQuitApplication = false;
    protected bool IsDestroy = false;

    public static event Action<CAssetDep> BeforeEvent; // 前置依赖加载时调用的事件，用于修改它的依赖哦!!!
    public static event Action<CAssetDep> FinishEvent; // 完成依赖加载时调用的事件，只执行一次哦

    [System.NonSerialized]
    protected int TexturesWaitLoadCount = 0;
    [System.NonSerialized] protected readonly List<Action> TexturesLoadedCallback = new List<Action>();
    [System.NonSerialized] protected readonly Queue<Action<CAssetDep, UnityEngine.Object>> LoadedDependenciesCallback = new Queue<Action<CAssetDep, UnityEngine.Object>>(); // 所有依赖加载完毕后的回调， 暂时用在SpriteCollection、UIAtlas加载完、Sprite加载完后, 会多次被用，跟FinishEvent不同
    [System.NonSerialized]
    protected readonly List<KAbstractResourceLoader> ResourceLoaders = new List<KAbstractResourceLoader>();

    protected CAssetDep()
    {
    }

    // TODO: 现在只支持MainAsset
    public static T Create<T>(Component dependencyComponent, string path, string assetName = null) where T : CAssetDep
    {

        var dep = dependencyComponent.gameObject.AddComponent<T>();
        dep.DependencyComponent = dependencyComponent;
        dep.ResourcePath = path;

        //
        // dep.Arg  // AssetName

        return (T)dep;
    }

    void Awake()
    {
        _cacheGameObject = gameObject;

        if (!_NewIsInit)
            Init();
    }

    void OnDisable()
    {
        if (!_NewIsInit)
            Init();
    }
    void OnEnable()
    {
        if (!_NewIsInit)
            Init();
    }

    private IEnumerator ExistCustomUpdate;

    // 让外部主动调，他妈的有一些inactive状态的，awake都不会激活
    public void Init()
    {
        _NewIsInit = true;

        if (IsFinishDependency)
            return;

        if (BeforeEvent != null)
            BeforeEvent(this);

        IsFinishDependency = false;

        DoProcess(ResourcePath);

        if (ExistCustomUpdate != null)
        {
            AppEngine.EngineInstance.StopCoroutine(ExistCustomUpdate);
            ExistCustomUpdate = null;
            Logger.LogWarning("[ExistCustomUpdate != null");
        }
        ExistCustomUpdate = CustomUpdate();
        AppEngine.EngineInstance.StartCoroutine(ExistCustomUpdate);  // 可以无视对象是否处于inactive状态，避免Update不反应的问题

    }

    IEnumerator CustomUpdate()
    {
        float startTime = Time.time;
        while (true)
        {
            if (IsFinishDependency)
            {
                if (LoadedDependenciesCallback.Count > 0)  // 处理多依赖，要多回调
                {
                    while (LoadedDependenciesCallback.Count > 0)
                    {
                        var action = LoadedDependenciesCallback.Dequeue();
                        action(this, DependencyObject);
                        //yield return null;
                    }
                }
                ExistCustomUpdate = null;
                yield break;
            }

            if (Time.time - startTime > 10)
            {
                Debug.Log("超过10秒未完成的依赖资源: " + this.name, this);
            }

            yield return null;
        }
    }
#if UNITY_EDITOR
    private int _logErrorCount = 0;
    void Update()
    {
        const int maxShowCount = 30;
        if (IsFinishDependency)
        {
            if (DependencyObject == null)
            {
                if (_logErrorCount < maxShowCount)
                {
                    Debug.LogError(string.Format("加载完的依赖对象: {0}, 加载完成对象 {1} 却被销毁", this.ResourcePath, this.name), this);
                    _logErrorCount++;
                }
                
            }
        }
    }
#endif
    // 完成依赖加载后的回调
    public void AddFinishCallback(Action<CAssetDep, UnityEngine.Object> callback)
    {
        if (!IsFinishDependency)
            LoadedDependenciesCallback.Enqueue(callback);
        else
            callback(this, DependencyObject); // 立即执行
        //if (IsFinishDependency)  
        //    OnFinishLoadDependencies(DependencyObject);
    }

    protected void OnFinishLoadDependencies(UnityEngine.Object obj)
    {
        IsFinishDependency = true;

        if (!IsDestroy)  // 删除的东西，不回调
        {
            //if (LoadedDependenciesCallback.Count > 0)
            //{
            //    var action = LoadedDependenciesCallback.Dequeue();
            //    action(obj);
            //}

            {
                DependencyObject = obj;

                if (DependencyObject == null)
                {
                    Debug.LogError(string.Format("[OnFinishLoadDependencies]Null ResultObject: {0}", this.ResourcePath), this);
                }
                if (FinishEvent != null)
                    FinishEvent(this);
            }

        }

    }

    protected void LoadMaterial(string path, Action<Material> callback)
    {
        //var matLoader = new CStaticAssetLoader(path, OnLoadMaterialScript, path, matCallback);
        //ResourceLoaders.Add(matLoader); 
        var mLoader = KMaterialLoader.Load(path, (isOk, getMat) =>
        {
            if (isOk)
                callback(getMat);
            else
            {
                callback(getMat);
            }
        });
        ResourceLoaders.Add(mLoader);
    }

    // 清理Textures, 下次加载时，重新执行依赖加载 TODO: 目前暂时只测试支持UITextures的
    public void ClearAndReset()
    {
        _NewIsInit = false;
        IsFinishDependency = false;
        TexturesWaitLoadCount = 0;

        DisposeAllLoaders();
    }

    protected void DisposeAllLoaders()
    {
        foreach (var loader in ResourceLoaders)
        {
            loader.Release();
        }
        ResourceLoaders.Clear();
    }
    //private static readonly Dictionary<string, UIAtlas> CachedUIAtlas = new Dictionary<string, UIAtlas>();  // UIAtlas + StaticAssetLoader

    protected void OnApplicationQuit()
    {
        IsQuitApplication = true;
    }
    protected virtual void OnDestroy()
    {
        IsDestroy = true;
        if (!IsQuitApplication)
        {
            if (ExistCustomUpdate != null)
            {
                AppEngine.EngineInstance.StopCoroutine(ExistCustomUpdate);
                ExistCustomUpdate = null;    
            }

            DisposeAllLoaders();
        }
    }

    public static Coroutine WaitDep(GameObject obj)
    {
        return KResourceModule.Instance.StartCoroutine(CoWaitDep(obj));
    }
    public static IEnumerator CoWaitDep(GameObject obj)
    {
        var wait = true;
        WaitDep(obj, () =>
        {
            wait = false;
        });

        while (wait)
            yield return null;
    } 

    // 等待器
    public static CAssetDep[] WaitDep(GameObject obj, Action c)
    {
        CAssetDepWaiter newWaiter = new CAssetDepWaiter();
        newWaiter.DepCallback = c;
        newWaiter.WaitObject = obj;

        CAssetDep[] deps = obj.GetComponentsInChildren<CAssetDep>(true);
        newWaiter.AssetDeps = deps;

        if (deps.Length > 0)
        {
            newWaiter.count = deps.Length;
            foreach (CAssetDep dep in deps)
            {
                if (!dep._NewIsInit)
                    dep.Init();
                dep.AddFinishCallback(newWaiter.OnLoadedDepCallback);
            }
            newWaiter.StartDebug();
        }
        else
        {
            if (newWaiter.DepCallback != null)
                newWaiter.DepCallback();
        }
        
        return deps;
    }

    internal class CAssetDepWaiter
    {
        public IList<CAssetDep> AssetDeps;
        public int count = 0;
        public Action DepCallback = null;
        public GameObject WaitObject;

        public void OnLoadedDepCallback(CAssetDep assetDep, UnityEngine.Object obj)
        {
            count--;

//            AssetDeps.Remove(assetDep);
//#if UNITY_EDITOR
//            Logger.Assert(count == AssetDeps.Count);
//#endif
            if (count <= 0)
            {
                if (DepCallback != null)
                    DepCallback();
            }
        }

        public void StartDebug()
        {
//#if UNITY_EDITOR
//            CGame.StartCo(CoDebug());
//#endif
        }

        //IEnumerator CoDebug()
        //{
        //    yield return new WaitForSeconds(5f);
        //    foreach (var dep in AssetDeps)
        //    {
        //        //if (!dep.IsFinishDependency)
        //        {
        //            Debug.LogError(string.Format("超过时间AssetDep未加载完: {0}", dep.ResourcePath));
        //            Debug.LogError("target: ", dep.gameObject);
        //        }
        //    }
        //} 

    }

    /// <summary>
    /// 处理加载事宜
    /// </summary>
    /// <param name="resourcePath"></param>
    protected abstract void DoProcess(string resourcePath);
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(CAssetDep))]
public class CBaseAssetDepInspector : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        bool isFinish = ((CAssetDep)target).IsFinishDependency;
        if (isFinish)
            UnityEditor.EditorGUILayout.LabelField("依赖已经加载完毕！");
    }
}
#endif