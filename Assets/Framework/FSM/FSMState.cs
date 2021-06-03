using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 状态基类
/// </summary>
public class FSMState
{
    public virtual void Enter(FSMCore fsm) { }

    public virtual void Stay(FSMCore fsm, float timeHeap) { }

    public virtual void Exit(FSMCore fsm) { }
}
