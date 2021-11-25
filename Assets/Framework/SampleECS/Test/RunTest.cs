using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using SampleECS;
using System.Runtime.CompilerServices;
using System.Diagnostics;

public class RunTest : MonoBehaviour
{
    public int count = 50000;
    ECS_Context context;
    ECS_Entity[] es;
    void Start()
    {
        Application.targetFrameRate = -1;
        es = new ECS_Entity[count];
        context = ECS_Context.CreateContext("game");
        for (int i = 0; i < count; i++)
        {
            es[i] = context.CreateEntity();
            TestComp com = new TestComp();
            com.go = Object.Instantiate(Resources.Load<GameObject>("go")).transform;
            com.go.name = i.ToString();
            com.go.transform.position = new Vector3((i % 500) + Random.Range(-0.1f, 0.1f),
                (i / 500) + Random.Range(-0.1f, 0.1f), 0);
            es[i].Add_TestComp(com);
        }
    }

    int frameCount = 0;
    float timer = 0;
    float disFPS = 0;
    void Update()
    {
        frameCount++;
        if (Time.realtimeSinceStartup - timer >= 1f)
        {
            timer = Time.realtimeSinceStartup;
            disFPS = frameCount;
            frameCount = 0;
        }

        for (int i = 0; i < count; i++)
        {
            var com = new TestComp();
            com.go = es[i].testcomp.go;
            com.position = new Vector3((i % 200) + Random.Range(-0.1f, 0.1f), (i / 200) + Random.Range(-0.1f, 0.1f), 0);
            com.test_field = es[i].testcomp.test_field;
            es[i].Replace_TestComp(com);
            //es[i].testcomp = com;
        }
        context.Tick();
    }

    private void LateUpdate()
    {
        context.LateTick();
    }



    private void OnGUI()
    {
        GUILayout.Label("FPS: " + disFPS, "box", GUILayout.Width(200), GUILayout.Height(50));
        GUILayout.Label("Entity Count: " + count, "box", GUILayout.Width(200), GUILayout.Height(50));
    }
}
