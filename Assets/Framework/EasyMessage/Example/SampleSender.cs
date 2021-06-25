using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessageSystem;

public class SampleSender : MonoBehaviour
{
    private void OnGUI()
    {
        if (GUILayout.Button("复位"))
        {
            MessageCore.SendMessage("Person_Do_Something", "Reset");
        }

        if (GUILayout.Button("所有人去A点"))
        {
            MessageCore.SendMessage("Person_Do_Something", "Move", "A");
        }

        if (GUILayout.Button("班级1去B点"))
        {
            MessageCore.SendMessageInclude("Person_Do_Something", "Move", new object[] { "班级1" }, "B");
        }

        if (GUILayout.Button("张三去A点"))
        {
            MessageCore.SendMessageInclude("Person_Do_Something", "Move", new object[] { "张三" }, "A");
        }

        if (GUILayout.Button("除了张三去B点"))
        {
            MessageCore.SendMessageExcept("Person_Do_Something", "Move", new object[] { "张三" }, "B");
        }

        if (GUILayout.Button("班级1和班级3去C点"))
        {
            MessageCore.SendMessageInclude("Person_Do_Something", "Move", new object[] { "班级1", "班级3" }, "C");
        }

        if (GUILayout.Button("除了班级1和班级3去C点"))
        {
            MessageCore.SendMessageExcept("Person_Do_Something", "Move", new object[] { "班级1", "班级3" }, "C");
        }
    }
}
