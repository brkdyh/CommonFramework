using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence
{
    public class SequenceEvent
    {
        public float fireTime;
        public List<Action<object>> events;

        public void Invoke(object obj)
        {
            foreach (var e in events)
            {
                e.Invoke(obj);
            }
        }
    }

    //存储原始事件表
    Dictionary<float, SequenceEvent> originalEvents = new Dictionary<float, SequenceEvent>();

    //播放使用
    Dictionary<float, SequenceEvent> sequenceEvents = new Dictionary<float, SequenceEvent>();

    public bool isEnd { get; private set; }
    public bool isPlaying { get; private set; }

    public float totalTime = 0f;
    float timer = 0f;
    public float GetTimerCount() { return timer; }

    public object customData { get; private set; }
    public void SetCustomData(object data) { customData = data; }

    public void AddEvent(float fireTime, Action<object> fireEvent)
    {
        if (!originalEvents.ContainsKey(fireTime))
        {
            SequenceEvent se = new SequenceEvent();
            se.fireTime = fireTime;
            se.events = new List<Action<object>>();
            originalEvents.Add(fireTime, se);
        }

        originalEvents[fireTime].events.Add(fireEvent);
    }

    void CopyEvents()
    {
        //Debug.Log("event count = "+originalEvents.Count);
        sequenceEvents.Clear();
        foreach (var se in originalEvents)
        {
            sequenceEvents.Add(se.Key, se.Value);
        }
    }

    void Reset()
    {
        CopyEvents();
        isEnd = false;
        timer = 0f;
    }

    public void PlaySequence()
    {
        if (isPlaying)
            return;

        Reset();
        isPlaying = true;
    }

    //事件删除列表
    List<float> de_list = new List<float>();

    public void Tick(float timeHeap)
    {
        //Debug.Log("Sequence Tick!  " + isPlaying);
        if (isPlaying)
        {
            timer += timeHeap;

            de_list.Clear();
            foreach (var se in sequenceEvents)
            {
                //Debug.Log("event key = " + se.Key);
                if (timer >= se.Key)
                {
                    se.Value.Invoke(customData);
                    de_list.Add(se.Key);
                }
            }

            foreach (var deKey in de_list)
            {
                sequenceEvents.Remove(deKey);
            }

            if (timer >= totalTime)
            {
                isEnd = true;
                isPlaying = false;
            }
        }
    }

    /// <summary>
    /// 拼装Sequence
    /// </summary>
    public virtual void InitSequence()
    {

    }

    public void ForceEnd()
    {
        timer = totalTime;
    }
}
