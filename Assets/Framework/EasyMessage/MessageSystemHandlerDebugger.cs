#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using MessageSystem;
using UnityEngine;

public class MessageSystemHandlerDebugger : MonoBehaviour
{
    public Dictionary<string, List<IMessageSystemHandler>> Msg_IHandlers_Map = new Dictionary<string, List<IMessageSystemHandler>>();
    public void AddHandler(IMessageSystemHandler IHandler)
    {
        if (!Msg_IHandlers_Map.ContainsKey(IHandler.getMessageUid))
            Msg_IHandlers_Map.Add(IHandler.getMessageUid, new List<IMessageSystemHandler>());

        if (!Msg_IHandlers_Map[IHandler.getMessageUid].Contains(IHandler))
            Msg_IHandlers_Map[IHandler.getMessageUid].Add(IHandler);
    }

    public MessageHandler GetMessageHandler(IMessageSystemHandler IHandler)
    {
        var hash = IHandler.GetHashCode();

        if (MessageCore.Instance == null)
            return null;

        var map = MessageCore.Instance.getMessageHandlersMap();
        if (map.ContainsKey(IHandler.getMessageUid))
        {
            var hds = map[IHandler.getMessageUid];
            if (hds.ContainsKey(hash))
                return hds[hash];
        }

        return null;
    }
}

#endif