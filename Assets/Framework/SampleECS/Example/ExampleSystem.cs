using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SampleECS;

[System(context = "game", systemMode = SystemMode.Action)]
public class CreateSystem : ECS_System
{
    public override bool GetSystemMatch(ECS_Entity entity)
    {
        return entity.has_CreateComp;
    }

    public override ECS_Trigger GetTrigger()
    {
        return new ECS_Trigger(ECS_Component_Type.CreateComp);
    }

    public override void Excute(ECS_Entity entity)
    {
        var count = entity.createcomp.create_count;
        for (int i = 0; i < count; i++)
        {
            var new_entity = context.CreateEntity();        //新建实体
            IDComp id = new IDComp();
            id.id = i;
            new_entity.AddComponent(id);

            TransformComp trans = new TransformComp();
            trans.transform = Object.Instantiate(Resources.Load<GameObject>("go")).transform;
            trans.transform.name = i.ToString();
            new_entity.AddComponent(trans);                 //添加 TransformComp 组件

            PositionComp pos = new PositionComp();
            pos.position = new Vector3((i % 50) + Random.Range(-0.1f, 0.1f),
                (i / 50) + Random.Range(-0.1f, 0.1f), 0);
            new_entity.AddComponent(pos);                   //添加 PositionComp 组件
        }
    }
}

[System(context = "game",systemMode = SystemMode.Action)]
public class MoveSystem : ECS_System
{
    public override bool GetSystemMatch(ECS_Entity entity)
    {
        return entity.has_TransformComp && entity.has_PositionComp;
    }

    public override ECS_Trigger GetTrigger()
    {
        return new ECS_Trigger(ECS_Component_Type.PositionComp);
    }

    public override void Excute(ECS_Entity entity)
    {
        entity.transformcomp.transform.position = entity.positioncomp.position;
    }
}

[System(context = "game", systemMode = SystemMode.Loop)]
public class ChangePostionSystem : ECS_System
{
    public override bool GetSystemMatch(ECS_Entity entity)
    {
        return entity.has_IDComp && entity.has_PositionComp;
    }

    public override void Excute(ECS_Entity entity)
    {
        //修改位置
        PositionComp pos = new PositionComp();
        pos.position = new Vector3((entity.idcomp.id % 50) + Random.Range(-0.1f, 0.1f),
            (entity.idcomp.id / 50) + Random.Range(-0.1f, 0.1f), 0);
        entity.Replace_PositionComp(pos);
    }
}
