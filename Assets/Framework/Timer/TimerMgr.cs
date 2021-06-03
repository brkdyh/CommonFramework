using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 计时器管理器
/// </summary>
public class TimerMgr
{
    static List<MonoTimer> timerCache = new List<MonoTimer>();//所有计时器

    /// <summary>
    /// 注销计时器
    /// </summary>
    public static void UnRegisterTimer(string timerName)
    {
        List<MonoTimer> tempList = new List<MonoTimer>();
        foreach (var timer in timerCache)
        {
            if (timer.timerName == timerName)
            {
                tempList.Add(timer);
            }
        }
        foreach (var timer in tempList)
        {
            RemoveTimer(timer);
        }
    }

    /// <summary>
    /// 添加计时器
    /// </summary>
    public static void AddTimer(MonoTimer timer)
    {
        timerCache.Add(timer);
    }
    /// <summary>
    /// 移除计时器
    /// </summary>
    public static void RemoveTimer(MonoTimer timer)
    {
        GameObject.Destroy(timer);
        timerCache.Remove(timer);
    }
}