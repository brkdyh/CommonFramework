using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using SampleECS;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Reflection;

public class ECS_Example : MonoBehaviour
{
    public int count = 50000;
    ECS_Context context;

    ECS_Entity gameEntity;
    void Start()
    {
        Application.targetFrameRate = -1;
        context = ECS_Context.CreateContext("game");
        gameEntity = context.CreateEntity();
        CreateComp comp = new CreateComp();
        comp.create_count = count;
        gameEntity.AddComponent(comp);
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

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ECS_Context.EnableContext("game");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            ECS_Context.DisableContext("game");
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("FPS: " + disFPS, "box", GUILayout.Width(200), GUILayout.Height(50));
        GUILayout.Label("Entity Count: " + count, "box", GUILayout.Width(200), GUILayout.Height(50));
    }
}
