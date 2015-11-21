using UnityEngine;
using System.Collections;

/// <summary>
/// 基础依赖组件
/// </summary>
public abstract class CBaseAssetDep : MonoBehaviour
{
    // 依赖加载出来的对象容器
    private static GameObject _DependenciesContainer;

    public static GameObject DependenciesContainer
    {
        get { return _DependenciesContainer ?? (_DependenciesContainer = new GameObject("_DependenciesContainer_")); }
    }


    protected abstract void DoProcess(string resourcePath);
}
