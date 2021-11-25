using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SampleECS;
using System.Runtime.CompilerServices;

[System(systemMode = SystemMode.Loop, context = "game")]
public class TestSystem : ECS_System
{
    public override bool GetSystemMatch(ECS_Entity entity)
    {
        return entity.has_TestComp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ECS_Trigger GetTrigger()
    {
        //Debug.Log(entity.is_TestComp_dirty);
        return new ECS_Trigger(ECS_Component_Type.TestComp);
    }

    public override void Excute(ECS_Entity entity)
    {
        base.Excute(entity);
        entity.testcomp.go.position = entity.testcomp.position;
    }
}
