using System;
using UnityEngine;

/// <summary>
/// Mono扩展
/// </summary>
public static class MonoExt
{
    /// <summary>
    /// 注册倒计时
    /// </summary>
    public static void RegisterTimer(this MonoBehaviour mono, float countdown, Action onFinish
        , string timerName = "DefaultTimer", bool isDestroy = true)
    {
        MonoTimer timer = mono.gameObject.AddComponent<MonoTimer>();
        timer.SetTimer(countdown, onFinish, timerName, isDestroy);
        TimerMgr.AddTimer(timer);
    }
}