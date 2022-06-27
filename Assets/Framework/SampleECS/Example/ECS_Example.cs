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
    ECS_Game_Context context;

    ECS_Game_Entity gameEntity;
    void Start()
    {
        Application.targetFrameRate = -1;
        context = ECS_Context.GetContext<ECS_Game_Context>("Game");
        gameEntity = context.CreateEntity();
        CreateComp comp = new CreateComp();
        comp.create_count = count;
        gameEntity.Add_CreateComp(comp);
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
            ECS_Context.EnableContext("Game");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            ECS_Context.DisableContext("Game");
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            context.Replace_MsgComp(new MsgComp() { msg = "Message From Static Component" });
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("FPS: " + disFPS, "box", GUILayout.Width(200), GUILayout.Height(50));
        GUILayout.Label("Entity Count: " + count, "box", GUILayout.Width(200), GUILayout.Height(50));

        if (GUILayout.Button("EnableContext", GUILayout.Width(200), GUILayout.Height(50)))
            ECS_Context.EnableContext("Game");
        if (GUILayout.Button("DisableContext", GUILayout.Width(200), GUILayout.Height(50)))
            ECS_Context.DisableContext("Game");
        if (GUILayout.Button("Show Message", GUILayout.Width(200), GUILayout.Height(50)))
            context.Replace_MsgComp(new MsgComp() { msg = "Message From Static Component" });
    }
}
