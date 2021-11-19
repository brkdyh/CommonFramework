using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SampleECS;


[System(systemMode = SystemMode.Action, context = "game")]
public class TestSystem : ECS_System
{
    public override ECS_Match GetSystemMatch()
    {
        return new ECS_Match(typeof(TestComp));
    }

    public override bool GetTrigger(ECS_Entity entity)
    {
        return true;
    }

    public override void Excute(ECS_Entity[] entities)
    {
        base.Excute(entities);
    }
}
