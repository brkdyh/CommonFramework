using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SampleECS;

[Component]
public struct test_struct
{
    public string myname;
    public int id;
    public Vector3 pos;
}

public class TestECSLogic : MonoBehaviour
{
    //public ECS_Entity entity;

    //public List<ECS_Entity> entities;

    //test_struct[] ds = new test_struct[100000];
    //test_class[] dc = new test_class[100000];


    ECS_Context Context;

    ECS_Entity[] ets = new ECS_Entity[100000];

    private void Start()
    {
        //for (int i = 0; i < 100000; i++)
        //{
        //    dc[i] = new test_class();
        //}
        Context = ECS_Context.CreateContext("game");
        for (int i = 0; i < 100000; i++)
        {
            var entity = Context.CreateEntity();
            var com = entity.AddComponent<test_struct>();
            entity.Replace(com);
            ets[i] = entity;
        }
    }

    private void Update()
    {
        for (int i = 0; i < 100000; i++)
        {
            var com = ets[i].GetComponent<test_struct>();
            com.id = i;
            ets[i].Replace(com);
        }

        Context.Tick();
    }

    private void LateUpdate()
    {
        Context.LateTick();
    }
}
