using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessageSystem;

public class SampleSender : MonoBehaviour
{
    private void OnGUI()
    {
        if (GUILayout.Button("全体扫地"))
        {
            MessageCore.SendMessage("Person_Do_Something", "Do_Something", "扫地");
        }

        if (GUILayout.Button("班级1 擦黑板"))
        {
            MessageCore.SendMessageInclude("Person_Do_Something", "Do_Something", new object[] { "班级1" }, "擦黑板");
        }

        if (GUILayout.Button("李四 拖地"))
        {
            MessageCore.SendMessageInclude("Person_Do_Something", "Do_Something", new object[] { "李四" }, "拖地");
        }

        if (GUILayout.Button("除了李四和班级2 做作业"))
        {
            MessageCore.SendMessageExcept("Person_Do_Something", "Do_Something", new object[] { "李四", "班级2" }, "做作业");
        }

        if (GUILayout.Button("所有班级 放学"))
        {
            MessageCore.SendMessage("Class_Do_Something", "Do_Something", "放学");
        }
    }
}
