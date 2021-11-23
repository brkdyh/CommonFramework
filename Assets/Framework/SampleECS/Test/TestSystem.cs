using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SampleECS;
using System.Runtime.CompilerServices;

[System(systemMode = SystemMode.Action, context = "game")]
public class TestSystem : ECS_System
{
    public override bool GetSystemMatch(ECS_Entity entity)
    {
        return entity.has_TestComp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool GetTrigger(ECS_Entity entity)
    {
        //Debug.Log(entity.is_TestComp_dirty);
        return entity.is_TestComp_dirty;
    }

    public override void Excute(ECS_Entity entity)
    {
        base.Excute(entity);
        entity.testcomp.go.transform.position = entity.testcomp.position;
    }
}
