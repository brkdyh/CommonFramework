using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 计时结构
/// </summary>
public class TickData
{
    float startRealTime;

    public float TickTime { get; private set; } = 0f;
    public bool ignoreTimeScale { get; set; } = false;
    public bool disposed { get; private set; } = false;
    public bool ticking { get; private set; } = false;

    public float currentTimer { get; private set; }

    Action<TickData> onTick;
    Action<TickData> onFinish;

    public void BeginTick(float tickTime, Action<TickData> onTick, Action<TickData> onFinish, bool ignoreTimeScale = false)
    {
        this.onTick = onTick;
        this.onFinish = onFinish;
        TickTime = tickTime;
        this.ignoreTimeScale = ignoreTimeScale;
        startRealTime = Time.realtimeSinceStartup;
        currentTimer = 0f;
        disposed = false;
        ticking = true;
    }

    public void Dispose()
    {
        disposed = true;
        currentTimer = TickTime;
    }

    public void Tick(float timeHeap)
    {
        if (!ticking)
            return;

        if (!ignoreTimeScale)
            currentTimer += timeHeap;
        else
            currentTimer = Time.realtimeSinceStartup - startRealTime;

        if (currentTimer < TickTime)
        {
            if (!disposed)
                onTick?.Invoke(this);
        }
        else
        {
            ticking = false;
            if (!disposed)
                onFinish?.Invoke(this);
        }
    }
}
