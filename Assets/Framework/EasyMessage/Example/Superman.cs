using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessageSystem;

public class Superman : Person, IMultiMessageHandler
{
    public void initMessageUids(List<string> MessageUids)
    {
        MessageUids.Add("Superman_Do_Something");
    }

    public void initHandleMethodMap(Dictionary<string, Dictionary<string, MessageHandleMethod>> HandleMethodMap)
    {
        HandleMethodMap["Superman_Do_Something"].Add("Teleport", teleport);
    }

    protected override void Start()
    {
        base.Start();
        MessageCore.RegisterHandler(this as IMultiMessageHandler, getMessageFilter);
    }

    void teleport(params object[] ps)
    {
        Vector3 pos = (Vector3)ps[0];
        pos.y = 1f;
        transform.position = pos;
    }
}
