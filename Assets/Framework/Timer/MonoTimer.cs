using UnityEngine;
using System;

/// <summary>
/// 计时器
/// </summary>
public class MonoTimer : MonoBehaviour
{
    float startTime;//开始的时间

    bool beginTimer;//是否开始计时
    bool isDestroy;//是否销毁
    float countdown;//倒计时时间
    Action onFinish;//倒计时结束的回调
    [HideInInspector] public string timerName;//倒计时名称

    /// <summary>
    /// 设置计时器
    /// </summary>
    public void SetTimer(float countdown, Action onFinish
        , string timerName = "", bool isDestroy = true)
    {
        this.countdown = countdown;
        this.onFinish = onFinish;
        this.timerName = timerName;
        this.isDestroy = isDestroy;
        startTime = Time.realtimeSinceStartup;
        beginTimer = true;
    }

    private void Update()
    {
        if (!beginTimer) return;

        if (Time.realtimeSinceStartup - startTime >= countdown)
        {
            onFinish?.Invoke();
            TimerMgr.RemoveTimer(this);
            beginTimer = false;
            if (isDestroy)
            {
                Destroy(gameObject);
            }
        }
    }
}
