#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using MessageSystem;
using UnityEngine;

public class MessageSystemHandlerDebugger : MonoBehaviour
{
    public Dictionary<string, List<MessageHandler>> Msg_IHandlers_Map = new Dictionary<string, List<MessageHandler>>();
    public void AddHandler(MessageHandler handler)
    {
        if (!Msg_IHandlers_Map.ContainsKey(handler.messageUid))
            Msg_IHandlers_Map.Add(handler.messageUid, new List<MessageHandler>());

        if (!Msg_IHandlers_Map[handler.messageUid].Contains(handler))
            Msg_IHandlers_Map[handler.messageUid].Add(handler);
    }
}

#endif