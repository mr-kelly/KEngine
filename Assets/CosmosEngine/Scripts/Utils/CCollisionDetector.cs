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

// 碰撞侦测器， 添加组件并绑定事件，用来外部回调一些碰撞检测， 只限2D
public class CCollisionDetector : CBehaviour
{
    public event Action<Collider2D, Vector2> OnCollisionEnterEvent;

    /// <summary>
    /// 所有的碰撞器
    /// </summary>
    Collider2D[] Colliders;

    public int LayerMask_;
    const bool IgnoreSelf = true;  // 不對自己碰撞哦
    /// <summary>
    /// 忽略的Transform
    /// </summary>
    Transform[] IgnoreTransforms;

    List<Collider2D> _HitsCached = new List<Collider2D>();  // 緩存碰撞到的物體
    public List<Collider2D> HitsCached
    {
        get { return _HitsCached; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gameObj"></param>
    /// <param name="ignoreTransforms">忽略的Transform</param>
    /// <returns></returns>
    public static CCollisionDetector Create(GameObject gameObj, int layerMask, params Transform[] ignoreTransforms)
    {
        CCollisionDetector det = gameObj.GetComponent<CCollisionDetector>() ?? gameObj.AddComponent<CCollisionDetector>();
        det.IgnoreTransforms = ignoreTransforms;
        det.LayerMask_ = layerMask;
        return det;
    }

    override public void Start()
    {
        base.Start();
        Colliders = GetComponents<Collider2D>();

    }

    // 即時刷新一下碰撞檢測
    public bool RefreshCollsion()
    {
        bool result = false; // No Collide
        _HitsCached.Clear();
        Physics2D.raycastsHitTriggers = true;
        foreach (Collider2D col in Colliders)
        {
            if (col is BoxCollider2D)
            {
                BoxCollider2D boxCol = col as BoxCollider2D;

                Vector2 leftTopPoint = (Vector2)_Transform.position + boxCol.center + new Vector2(-boxCol.size.x / 2, boxCol.size.y / 2);
                Vector2 rightBottomPoint = (Vector2)_Transform.position + boxCol.center + new Vector2(boxCol.size.x / 2, -boxCol.size.y / 2);

                Debug.DrawLine(leftTopPoint, rightBottomPoint, Color.red, 0.1f);

                foreach (Collider2D hit in Physics2D.OverlapAreaAll(leftTopPoint, rightBottomPoint, LayerMask_))
                {
                    OnHit(col, hit);
                    result = true;
                }
            }
            else if (col is CircleCollider2D)
            {
                CircleCollider2D circleCol = col as CircleCollider2D;

                foreach (Collider2D hit in Physics2D.OverlapCircleAll((Vector2)_Transform.position + circleCol.center, circleCol.radius, LayerMask_))
                {
                    OnHit(col, hit);
                    result = true;;
                }
            }
            else
                CBase.LogWarning("[CCollisionDetector]未知类型: {0}", col.GetType().Name);
        }

        return result;
    }
    
    void LateUpdate()
    {
        if (IsDeleted) return;

        RefreshCollsion();
    }

    /// <summary>
    /// 进入碰撞
    /// </summary>
    /// <param name="selfCol">碰撞者本身</param>
    /// <param name="hitCollider">碰到的对象</param>
    void OnHit(Collider2D selfCol, Collider2D hitCollider)
    {
        if (IgnoreSelf && selfCol == hitCollider)
            return;

        _HitsCached.Add(hitCollider);

        Transform hitTrans = hitCollider.transform;
        if (IgnoreTransforms != null)
        {
            foreach (Transform loopTrans in IgnoreTransforms)
            {
                try
                {
                    if (loopTrans && hitTrans && hitTrans.IsChildOf(loopTrans))//被忽略的碰撞
                    {
                        return;
                    }
                }
                catch (MissingReferenceException e)
                {
                    CBase.LogError("{0} hit {1} error: {2}", selfCol, hitCollider.gameObject.name, e.Message);
                }
            }
        }

        Vector2 touchPoint = GetTouchPoint(selfCol, hitCollider);

        if (OnCollisionEnterEvent != null)
            OnCollisionEnterEvent(hitCollider, touchPoint);

    }

    public static Vector2 GetTouchPoint(Collider2D selfCol, Collider2D hitCollider)
    {
        Bounds selfBounds = selfCol.bounds;
        Bounds hitBounds = hitCollider.bounds;
        // 计算碰撞点，取对方离我最近的点
        Vector2 selfColCenter = Vector2.zero;
        float selfColRadius = 0;
        Vector2 hitColCenter = Vector2.zero;
        if (hitCollider is BoxCollider2D)
        {
            selfColCenter = selfBounds.center;
            hitColCenter = hitBounds.center;
            Vector2 xReverseForce = Vector2.zero;// 反作用力
            Vector2 yReverseForce = Vector2.zero;

            if (selfColCenter.y < (hitColCenter.y - hitBounds.size.y / 2f))
                xReverseForce = -Vector2.up;
            else if (selfColCenter.y > (hitColCenter.y + hitBounds.size.y / 2f))
                xReverseForce = Vector2.up;

            if (selfColCenter.x < (hitColCenter.x - hitBounds.size.x / 2f))
                yReverseForce = -Vector2.right;
            else if (selfColCenter.x > (hitColCenter.x + hitBounds.size.x / 2f))
                yReverseForce = Vector2.right;

            return (xReverseForce + yReverseForce) * -1 + (Vector2)selfCol.transform.position;
        }
        else if (hitCollider is CircleCollider2D)
        {
            CircleCollider2D hitCircle = (hitCollider as CircleCollider2D);
            selfColCenter = selfBounds.center;
            selfColRadius = hitCircle.radius;
            hitColCenter = hitBounds.center;

            Vector2 touchPos = (hitColCenter - selfColCenter).normalized * selfColRadius + hitColCenter;

            return touchPos;
        }
        else
            CBase.Assert(false);

        if (hitCollider is BoxCollider2D)
            hitColCenter = (Vector2)hitCollider.transform.position + (hitCollider as BoxCollider2D).center;
        else if (hitCollider is CircleCollider2D)
            hitColCenter = (Vector2)hitCollider.transform.position + (hitCollider as CircleCollider2D).center;
        else
            CBase.Assert(false);


        Vector2 touchPos2 = (hitColCenter - selfColCenter).normalized * selfColRadius + selfColCenter;

        return touchPos2;
    }
}
