using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessageSystem;

public class SampleSender : MonoBehaviour
{
    private void OnGUI()
    {
        if (GUILayout.Button("扫地"))
        {
            MessageCore.SendMessage("Person_Do_Something", "Do_Something", "扫地");
        }

        if (GUILayout.Button("擦黑板"))
        {
            MessageCore.SendFilterMessage("Person_Do_Something", "Do_Something", "班级1", "擦黑板");
        }

        if (GUILayout.Button("拖地"))
        {
            MessageCore.SendFilterMessage("Person_Do_Something", "Do_Something", "李四", "拖地");
        }

        if (GUILayout.Button("放学"))
        {
            MessageCore.SendMessage("Class_Do_Something", "Do_Something", "放学");
        }
    }
}
