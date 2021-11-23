using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using SampleECS;
using System.Runtime.CompilerServices;
using System.Diagnostics;

public class RunTest : MonoBehaviour
{
    static int count = 10000;
    ECS_Context context;
    ECS_Entity[] es = new ECS_Entity[count];
    void Start()
    {
        //context = ECS_Context.CreateContext("game");
        //for (int i = 0; i < count; i++)
        //{
        //    es[i] = context.CreateEntity();
        //    TestComp com = new TestComp();
        //    com.go = Object.Instantiate(Resources.Load<GameObject>("go"));
        //    com.go.transform.position = new Vector3(i + Random.Range(-0.1f, 0.1f), (i / 500) + Random.Range(-0.1f, 0.1f), 0);
        //    es[i].Add_TestComp(com);
        //}
    }

    void Update()
    {
        //for (int i = 0; i < count; i++)
        //{
        //    var com = new TestComp();
        //    com.go = es[i].testcomp.go;
        //    com.position = new Vector3(i + Random.Range(-0.1f, 0.1f), (i / 500) + Random.Range(-0.1f, 0.1f), 0);
        //    com.test_field = es[i].testcomp.test_field;
        //    es[i].Replace_TestComp(com);

        //    //es[i].testcomp.go.transform.position = new Vector3(i + Random.Range(-0.1f, 0.1f), (i / 500) + Random.Range(-0.1f, 0.1f), 0);
        //}
        //context.Tick();
        //Stopwatch sw = new Stopwatch();
        //sw.Start();
        //for (int i = 0; i < 1000000; i++)
        //{
        //    Test();
        //}
        //sw.Stop();
        //UnityEngine.Debug.Log(sw.ElapsedMilliseconds);
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Test()
    {
        float s = 10;
        int i = 99;
        _ = i * s;
        _ = i / s;
    }

    private void LateUpdate()
    {
        //context.LateTick();
    }
}
