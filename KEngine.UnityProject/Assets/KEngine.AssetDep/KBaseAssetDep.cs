using UnityEngine;
using System.Collections;

/// <summary>
/// 基础依赖组件
/// </summary>
public abstract class KBaseAssetDep : MonoBehaviour
{
    // 依赖加载出来的对象容器
    private static GameObject _DepContainer;

    public static GameObject DepContainer
    {
        get { return _DepContainer ?? (_DepContainer = new GameObject("_DepContainer")); }
    }


    protected abstract void DoProcess(string resourcePath);
}
