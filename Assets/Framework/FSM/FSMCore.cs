using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 有限状态机
/// </summary>
public class FSMCore
{
    Dictionary<Type, FSMState> stateCache = new Dictionary<Type, FSMState>();

    FSMState currentState;

    public FSMState CurState
    {
        get { return currentState; }
    }

    public object customData { get; private set; }

    public void SetCustomData(object data)
    {
        customData = data;
    }

    /// <summary>
    /// 添加状态
    /// </summary>
    /// <param name="state"></param>
    public void AddState(FSMState state)
    {
        if (!stateCache.ContainsKey(state.GetType()))
        {
            stateCache.Add(state.GetType(), state);
        }
        else
        {
            Debug.LogError("FSMCore Add Error : Repeat State by Type " + state.GetType());
        }
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <typeparam name="T">状态类型</typeparam>
    public void SwitchState<T>()
        where T : FSMState
    {
        //Debug.LogError(GetHashCode() + " Switch State = " + typeof(T));
        if (stateCache.ContainsKey(typeof(T)))
        {
            var nextState = stateCache[typeof(T)];

            if (currentState != null)
                currentState.Exit(this);

            nextState.Enter(this);

            currentState = nextState;
        }
        else
        {
            Debug.LogError(string.Format("Can't Find FSM State by Type = {0}", typeof(T)));
        }
    }

    public void SwitchState(Type stateType)
    {
        if (stateCache.ContainsKey(stateType))
        {
            //Debug.Log("Switch State = " + stateType);

            var nextState = stateCache[stateType];

            if (currentState != null)
                currentState.Exit(this);

            nextState.Enter(this);

            currentState = nextState;
        }
        else
        {
            Debug.LogError(string.Format("Can't Find FSM State by Type = {0}", stateType));
        }
    }

    /// <summary>
    /// 状态更新
    /// </summary>
    /// <param name="timeHeap"></param>
    public virtual void TickTime(float timeHeap)
    {
        if (currentState != null)
        {
            currentState.Stay(this, timeHeap);
            //Debug.Log("update cur state = " + currentState);
        }
        else
        {
            //Debug.LogError("cur state is Null");
        }
    }
}