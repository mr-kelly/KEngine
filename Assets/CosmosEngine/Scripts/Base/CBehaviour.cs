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

/// <summary>
/// Without Update, With some cache
/// </summary>
public class CBehaviour : MonoBehaviour
{
    private Transform _cachedTransform;
    public Transform CachedTransform { get { return _cachedTransform ?? (_cachedTransform = transform); } }

    private GameObject _cacheGameObject;
    public GameObject CachedGameObject { get { return _cacheGameObject ?? (_cacheGameObject = gameObject); } }

    public Transform _Transform {get { return CachedTransform; }}
    public GameObject _GameObject {get { return CachedGameObject; }}

    protected bool IsDestroyed = false;

    static bool _IsApplicationQuited = false;  // 全局标记, 程序是否退出状态
    public static bool IsApplicationQuited {get { return _IsApplicationQuited; }}

    public static System.Action ApplicationQuitEvent;

    private float _TimeScale = 1f;  // TODO: In Actor, Bullet,....
    public virtual float TimeScale
    {
        get { return _TimeScale; }
        set { _TimeScale = value; }
    }

    /// <summary>
    /// GameObject.Destory对象
    /// </summary>
    public virtual void Delete()
    {
        if (!IsApplicationQuited)
            UnityEngine.Object.Destroy(gameObject);
    }

    // 只删除自己这个Component
    public virtual void DeleteSelf()
    {
        UnityEngine.Object.Destroy(this);
    }

    // 继承CBehaivour必须通过Delete删除
    // 程序退出时会强行Destroy所有，这里做了个标记
    protected virtual void OnDestroy()
    {
        IsDestroyed = true;
    }

    private void OnApplicationQuit()
    {
        if (!_IsApplicationQuited)
            CDebug.Log("OnApplicationQuit!");

        _IsApplicationQuited = true;

        if (ApplicationQuitEvent != null)
            ApplicationQuitEvent();
    }
}
