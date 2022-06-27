using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SampleECS;

[System(context = "Game", systemMode = SystemMode.Action)]
public class MsgStaticSystem : ECS_Game_Static_System
{
    //如果想在Context启动的第一帧执行系统方法，将此参数改为true
    //public override bool runAtFirstFrame => true;

    public override ECS_Trigger GetTrigger()
    {
        return new ECS_Trigger(Game_Component_Type.Static_MsgComp);
    }

    public override void ExcuteStatic(ECS_Game_Context context)
    {
        base.ExcuteStatic(context);
        Debug.Log(context.static_msgcomp.msg);
    }
}

[System(context = "Game", systemMode = SystemMode.Action)]
public class CreateSystem : ECS_Game_System
{
    public override bool GetSystemMatch(ECS_Game_Entity entity)
    {
        return entity.has_CreateComp;
    }

    public override ECS_Trigger GetTrigger()
    {
        return new ECS_Trigger(Game_Component_Type.CreateComp);
    }

    public override void Excute(ECS_Game_Entity entity)
    {
        var count = entity.createcomp.create_count;
        for (int i = 0; i < count; i++)
        {
            var new_entity = context.CreateEntity();        //新建实体
            IDComp id = new IDComp();
            id.id = i;
            new_entity.Add_IDComp(id);

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

[System(context = "Game", systemMode = SystemMode.Action)]
public class MoveSystem : ECS_Game_System
{
    public override bool GetSystemMatch(ECS_Game_Entity entity)
    {
        return entity.has_TransformComp && entity.has_PositionComp;
    }

    public override ECS_Trigger GetTrigger()
    {
        return new ECS_Trigger(Game_Component_Type.PositionComp);
    }

    public override void Excute(ECS_Game_Entity entity)
    {
        entity.transformcomp.transform.position = entity.positioncomp.position;
    }
}

[System(context = "Game", systemMode = SystemMode.Loop)]
public class ChangePostionSystem : ECS_Game_System
{
    public override bool GetSystemMatch(ECS_Game_Entity entity)
    {
        return entity.has_IDComp && entity.has_PositionComp;
    }

    public override void Excute(ECS_Game_Entity entity)
    {
        //修改位置
        PositionComp pos = new PositionComp();
        pos.position = new Vector3((entity.idcomp.id % 50) + Random.Range(-0.1f, 0.1f),
            (entity.idcomp.id / 50) + Random.Range(-0.1f, 0.1f), 0);
        entity.Replace_PositionComp(pos);
    }
}
