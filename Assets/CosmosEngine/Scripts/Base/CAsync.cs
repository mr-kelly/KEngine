using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// 链式操作，结合协程和DOTween, 并且支持真线程（用于密集运算，无法调用Unity大部分函数）
/// 
/// 适合做动画、结合协程、回调一堆的情况
/// 
/// CAsync.Start(doSomething)
///         .WaitForSeconds(1f)
///         .Coroutine(CGame.Instance.StartCoroutine(enumtor))
///         .Then(logSomething)
///         .Then((next)=>{
///             DOTween.DO(tween).OnComplete(next);
///         });
///         .When(()=> booleanVarTrue)
///         .Then(()=>{
///             // Over!
///         });
/// 
/// </summary>
public class CAsync
{
    #region 管理器~用于开启协程，执行主线程回调等
    private class CAsyncManager : MonoBehaviour
    {
        private static CAsyncManager _instance;
        public static CAsyncManager Instance
        {
            get { return _instance ?? (_instance = new GameObject("[AsyncManager]").AddComponent<CAsyncManager>()); }
        }
        public readonly List<Action> _mainThreadCallbacks = new List<Action>();  // 主線程調用Unity類，如StartCoroutine

        void Update()
        {
            foreach (var i in _mainThreadCallbacks)
            {
                i();
            }
            _mainThreadCallbacks.Clear();
        }
    }
    #endregion

    #region 核心调度
    private Queue<AsyncWaitNextDelegate> _cacheCallbacks;
    private bool _canNext;

    private CAsync()
    {
        _canNext = true;
    }

    private delegate void AsyncWaitNextDelegate(Action nextFunc);

    private void WaitNext(AsyncWaitNextDelegate callback)
    {
        if (!_canNext)
        {
            if (_cacheCallbacks == null)
                _cacheCallbacks = new Queue<AsyncWaitNextDelegate>();
            _cacheCallbacks.Enqueue(callback);
        }
        else
        {
            _canNext = false;
            callback(Next);
        }

    }

    private void Next()
    {
        _canNext = true;
        if (_cacheCallbacks != null && _cacheCallbacks.Count > 0)
            WaitNext(_cacheCallbacks.Dequeue());
    }
    #endregion

    /// <summary>
    /// 在子线程执行一个函数，让其回到主线程再执行的
    /// </summary>
    /// <param name="call"></param>
    public static void AddMainThreadCall(Action call)
    {
        CAsyncManager.Instance._mainThreadCallbacks.Add(call);
    }

    public static CAsync Start()
    {
        var async = new CAsync();
        return async;
    }
    public static CAsync Start(Action callback)
    {
        var async = new CAsync();

        return async.Then(callback);
    }

    public static CAsync Start(AsyncThenDelegateEasy callback)
    {
        var async = new CAsync();
        return async.Then(callback);
    }

    public CAsync Then(Action callback)
    {
        WaitNext((next) =>
        {
            callback();
            next();
        });
        return this;
    }

    public delegate void AsyncThreadDelegate(Action next);
    public delegate void AsyncThenDelegateEasy(Action next);
    public delegate void AsyncThenDelegate(Action next, Action kill);
    public CAsync Then(AsyncThenDelegate thenFunc)
    {
        WaitNext((next) =>
        {
            thenFunc(next, () =>
            {
                Debug.LogError("TODO: kill!");
            });
        });
        return this;
    }

    /// <summary>
    /// 等待条件成立
    /// </summary>
    /// <param name="retBool"></param>
    /// <returns></returns>
    public CAsync When(Func<bool> retBool)
    {
        WaitNext((next) =>
        {
            CAsyncManager.Instance.StartCoroutine(_CoWhen(retBool, next));
        });
        return this;
    }

    private IEnumerator _CoWhen(Func<bool> retBool, Action next)
    {
        while (!(retBool()))
            yield return null;
        next();
    }

    public CAsync Then(AsyncThenDelegateEasy thenFunc)
    {
        WaitNext((next) =>
        {
            thenFunc(next);
        });
        return this;
    }

    /// <summary>
    /// 线程。注意大部分Unity函数不能使用！ 借用协程配合~
    /// </summary>
    /// <param name="threadCalAction"></param>
    /// <returns></returns>
    public CAsync Thread(AsyncThreadDelegate threadCalAction)
    {
        return Coroutine(_Thread(threadCalAction));
    }
    public CAsync Thread(Action threadCalAction)
    {
        return Coroutine(_Thread((next) =>
        {
            threadCalAction();
            next();
        }));
    }
    
    public IEnumerator _Thread(AsyncThreadDelegate threadCalAction)
    {
        bool waitThreadFinish = false;
        var thread = new Thread(() =>
        {
            Action customNext = () =>
            {
                waitThreadFinish = true;
            };
            threadCalAction(customNext);

        });
        thread.Start();

        while (!waitThreadFinish)
            yield return null;
    }
    /// <summary>
    /// 开启并等待一个协程
    /// </summary>
    /// <param name="enumtor"></param>
    /// <returns></returns>
    public CAsync Coroutine(IEnumerator enumtor)
    {
        WaitNext((next) =>
        {
            CAsyncManager.Instance.StartCoroutine(_StartCoroutine(enumtor, next));
        });
        return this;
    }

    private IEnumerator _StartCoroutine(IEnumerator enumtor, Action next)
    {
        yield return CAsyncManager.Instance.StartCoroutine(enumtor);
        next();
    }
    /// <summary>
    /// 等待一个已经被其它MonoBehaviour开启的协程
    /// </summary>
    /// <param name="co"></param>
    /// <returns></returns>
    public CAsync Coroutine(Coroutine co)
    {
        WaitNext((next) =>
        {
            CAsyncManager.Instance.StartCoroutine(_Coroutine(co, next));
        });
        return this;
    }

    private IEnumerator _Coroutine(Coroutine co, Action next)
    {
        yield return co;
        next();
    }

    /// <summary>
    /// 等待一定帧数
    /// </summary>
    /// <param name="frameCount"></param>
    /// <returns></returns>
    public CAsync WaitForFrames(int frameCount)
    {
        WaitNext((next) =>
        {
            CAsyncManager.Instance.StartCoroutine(_WaitForFrames(frameCount, next));
        });
        return this;
    }
    private IEnumerator _WaitForFrames(int frameCount, Action next)
    {
        for (var i = 0; i < frameCount; i++)
            yield return null;
        next();
    }

    /// <summary>
    /// 等待秒数
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public CAsync WaitForSeconds(float time)
    {
        WaitNext((next) =>
        {
            CAsyncManager.Instance.StartCoroutine(_CoWaitForSeconds(time, next));
        });
        return this;
    }

    private IEnumerator _CoWaitForSeconds(float time, Action next)
    {
        yield return new WaitForSeconds(time);
        next();
    }

    /// <summary>
    /// 等到本帧结束
    /// </summary>
    /// <returns></returns>
    public CAsync WaitForEndOfFrame()
    {
        WaitNext((next) =>
        {
            CAsyncManager.Instance.StartCoroutine(_WaitForEndOfFrame(next));
        });
        return this;
    }

    private IEnumerator _WaitForEndOfFrame(Action next)
    {
        yield return new WaitForEndOfFrame();
        next();
    }
}