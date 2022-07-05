using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SampleECS;

//必须为ECS System添加一个SystemAttribute特性
//这个特性必须表明系统所属的 Context 以及系统的运行模式 SystemMode
//系统的运行模式分为Action和Loop两种
//[System(context = "Game", systemMode = SystemMode.Action)]
public class IDSystem : ECS_Game_System //此处需继承对应Context命名的系统
{
    //重载此属性可以设置系统的执行顺序
    public override int ExcuteIndex => -1;

    //重写此方法返回系统关注的实体类型
    public override bool GetSystemMatch(ECS_Game_Entity entity)
    {
        return entity.has_IDComp;//若实体中有组件IDComp，则会成为该系统关注的对象。
    }

    //重写此方法返回系统 （关注的组件） 的触发类型
    public override ECS_Trigger GetTrigger()
    {
        /*
        * 此行代码返回一个ECS_Trigger,这个Trigger中包含一个组件类型IDComp。
        * 这意味着当使用Entity对象的Replace_IDComp()方法改变该实体的该组件时，
        * 会触发System的一次响应，并执行一次Excute()来处理该Entity对象的组件值变化。
        */
        return new ECS_Trigger(Game_Component_Type.IDComp);
    }

    //系统的执行方法，在Action模式下，只有Entity中与Trigger中的包含的
    //组件类型相同的组件的值发生改变的时候，执行一次该方法。
    //*注意* 当为一个Entity添加组件时，也会执行一次垓方法。
    public override void Excute(ECS_Game_Entity entity)
    {
        Debug.Log("My id is " + entity.idcomp.id);
    }
}

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


