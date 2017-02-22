#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KAssetDep.cs
// Date:     2015/12/03
// Author:  Kelly
// Email: 23110388@qq.com
// Github: https://github.com/mr-kelly/KEngine
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using KEngine;
using UnityEngine;

/// <summary>
/// 基础依赖组件, 由AssetDepBuilder派发
/// </summary>
public abstract class KAssetDep : MonoBehaviour
{
    /// <summary>
    /// 所有AssetDep的引用容器
    /// </summary>
    private static LinkedList<KAssetDep> AssetDepsContainer = new LinkedList<KAssetDep>();

    /// <summary>
    /// AssetDep相对容器的位置
    /// </summary>
    private LinkedListNode<KAssetDep> _assetDepContainerNode;

    // 依赖加载出来的对象容器
    private static GameObject _DependenciesContainer;

    public static GameObject DependenciesContainer
    {
        get
        {
            if (_DependenciesContainer == null)
            {
                _DependenciesContainer = new GameObject("[AssetDep]");
                DontDestroyOnLoad(_DependenciesContainer);
            }
            return _DependenciesContainer;
        }
    }

//    private GameObject _cacheGameObject;
    protected bool _NewIsInit = false;

    public Component DependencyComponent; // 依赖的脚本控件，完整依赖加载后会用到它
    public string ResourcePath;
    public string AssetName; // 没用了！保留的序列化字段
    public object[] Args;

    public bool IsFinishDependency = false; // 默认不完成
    public UnityEngine.Object DependencyObject;

    protected static bool IsQuitApplication = false;
    protected bool IsDestroy = false;

    public static event Action<KAssetDep> BeforeEvent; // 前置依赖加载时调用的事件，用于修改它的依赖哦!!!
    public static event Action<KAssetDep> FinishEvent; // 完成依赖加载时调用的事件，只执行一次哦

    [System.NonSerialized] protected int TexturesWaitLoadCount = 0;
    [System.NonSerialized] protected readonly List<Action> TexturesLoadedCallback = new List<Action>();

    [System.NonSerialized] protected readonly Queue<Action<KAssetDep, UnityEngine.Object>> LoadedDependenciesCallback =
        new Queue<Action<KAssetDep, UnityEngine.Object>>();
        // 所有依赖加载完毕后的回调， 暂时用在SpriteCollection、UIAtlas加载完、Sprite加载完后, 会多次被用，跟FinishEvent不同

    [System.NonSerialized] protected readonly List<AbstractResourceLoader> ResourceLoaders =
        new List<AbstractResourceLoader>();

    protected KAssetDep()
    {
    }

    // TODO: 现在只支持MainAsset
    public static T Create<T>(Component dependencyComponent, string path, string assetName = null) where T : KAssetDep
    {
        var obj = dependencyComponent.gameObject;
        var dep = obj.GetComponent<T>();
        if (dep != null)
        { 
            Debug.LogWarning(string.Format("[KAssetDep.Create]Duplicate Component <{0}> found: {1}, please use `KDepCollectInfoCaching`", typeof(T), dependencyComponent));
            return dep;
        }

        // check exist
        dep = obj.AddComponent<T>();
        dep.DependencyComponent = dependencyComponent;
        dep.ResourcePath = path;
        // dep.Arg  // AssetName

        return (T) dep;
    }

    private void Awake()
    {
//        _cacheGameObject = gameObject;

        if (!_NewIsInit)
            Init();
    }

    private void OnDisable()
    {
        if (!_NewIsInit)
            Init();
    }

    private void OnEnable()
    {
        if (!_NewIsInit)
            Init();
    }

    private IEnumerator ExistCustomUpdate;

    private static IEnumerator CheckErrorCoroutine = null;

    // 让外部主动调，他妈的有一些inactive状态的，awake都不会激活
    public void Init()
    {
        if (Application.isEditor && CheckErrorCoroutine == null)
        {
            CheckErrorCoroutine = CheckErrorUpdate();
            AppEngine.EngineInstance.StartCoroutine(CheckErrorCoroutine); // 一个不停检查状态的Coroutine，为了效率，不放Update函数，单独开辟循环协程
        }

        this._assetDepContainerNode = AssetDepsContainer.AddLast(this);

        //
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
            Log.Warning("[ExistCustomUpdate != null");
        }
        ExistCustomUpdate = CustomUpdate();
        AppEngine.EngineInstance.StartCoroutine(ExistCustomUpdate); // 可以无视对象是否处于inactive状态，避免Update不反应的问题
    }

    private IEnumerator CustomUpdate()
    {
        float startTime = Time.time;
        while (true)
        {
            if (IsFinishDependency)
            {
                if (LoadedDependenciesCallback.Count > 0) // 处理多依赖，要多回调
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

    /// <summary>
    /// 不停的检查AssetDep状态是否正常, Editor Only
    /// </summary>
    /// <returns></returns>
    private static IEnumerator CheckErrorUpdate()
    {
        while (true)
        {
//            const int maxShowCount = 30;
            LinkedListNode<KAssetDep> depNode = AssetDepsContainer.First;
            if (depNode == null)
            {
                yield return null;
                continue;
            }

            do
            {
                KAssetDep dep = null;
                try
                {
                    dep = depNode.Value;
                }
                catch
                {
                    // ignored
					// prevent exeption when stop editor playing
                }

                 if (dep != null && dep.IsFinishDependency)
                {
                    if (dep.DependencyObject == null)
                    {
                        Debug.LogError(string.Format("加载完的依赖对象: {0}, 加载完成对象 {1} 却被销毁", dep.ResourcePath, dep.name),
                            dep);
                    }
                }
               yield return null;
            } while ((depNode = depNode.Next) != null);
        }
    }

    // 完成依赖加载后的回调
    public void AddFinishCallback(Action<KAssetDep, UnityEngine.Object> callback)
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

        if (!IsDestroy) // 删除的东西，不回调
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
                    Debug.LogError(
                        string.Format("[OnFinishLoadDependencies]Null ResultObject: {0}", this.ResourcePath), this);
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
        var mLoader = MaterialLoader.Load(path, (isOk, getMat) =>
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

    // 清理Textures, 下次加载时，重新执行依赖加载
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

            AssetDepsContainer.Remove(_assetDepContainerNode); // 使用node移除更快

            DisposeAllLoaders();
        }
    }

    public static Coroutine WaitDepCoroutine(GameObject obj)
    {
        return KResourceModule.Instance.StartCoroutine(CoWaitDep(obj));
    }

    public static IEnumerator CoWaitDep(GameObject obj)
    {
        var wait = true;
        WaitDep(obj, () => { wait = false; });

        while (wait)
            yield return null;
    }

    /// <summary>
    /// 同步，立刻处理完依赖
    /// </summary>
    /// <param name="obj"></param>
    //public static void WaitDepSync(GameObject obj)
    //{
    //    KAssetDep[] deps = obj.GetComponentsInChildren<KAssetDep>(true); // GetAll
    //    var depsCount = deps.Length;
    //    for (var i = 0; i < depsCount; i++)
    //    {
    //        var dep = deps[i];
    //        if (!dep._NewIsInit)
    //            dep.Init();
    //    }

    //}

    /// <summary>
    /// 等待一个对象完整的依赖加载完毕，包括其孩子的依赖
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static AssetDepWaiter WaitDep(GameObject obj, Action c = null)
    {
        AssetDepWaiter newWaiter = new AssetDepWaiter();
        newWaiter.DepCallback = c;
        newWaiter.WaitObject = obj;

        KAssetDep[] deps = obj.GetComponentsInChildren<KAssetDep>(true); // GetAll
        newWaiter.AssetDeps = deps;

        if (deps.Length > 0)
        {
            newWaiter.count = deps.Length;
            foreach (KAssetDep dep in deps)
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

        return newWaiter;
    }

    public class AssetDepWaiter
    {
        public IList<KAssetDep> AssetDeps;
        public int count = 0;
        public Action DepCallback = null;
        public GameObject WaitObject;

        public bool IsFinished
        {
            get { return count <= 0; }
        }

        public void OnLoadedDepCallback(KAssetDep assetDep, UnityEngine.Object obj)
        {
            count--;

//            AssetDeps.Remove(assetDep);
//if UNITY_EDITOR
            //if (Application.isEditor)
            //    Log.Assert(count == AssetDeps.Count);
//endif
            if (count <= 0)
            {
                if (DepCallback != null)
                    DepCallback();
            }
        }

        public void StartDebug()
        {
            if (Application.isEditor)
                KResourceModule.Instance.StartCoroutine(CoDebug());
        }

        private IEnumerator CoDebug()
        {
            yield return new WaitForSeconds(20f);
            foreach (var dep in AssetDeps)
            {
                if (!dep.IsFinishDependency)
                {
                    Debug.LogError(string.Format("超过20s时间AssetDep未加载完: {0}", dep.ResourcePath));
                    Debug.LogError("target: ", dep.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 处理加载事宜
    /// </summary>
    /// <param name="resourcePath"></param>
    protected abstract void DoProcess(string resourcePath);
}