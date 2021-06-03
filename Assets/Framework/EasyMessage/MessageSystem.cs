using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MessageSystem
{

    public static class MessageSetting
    {
#if UNITY_EDITOR
        public static bool DebugMode = true;
#else
        public static bool DebugMode = false;
#endif
        public static bool OpenLog = false;

        public enum WorkMode
        {
            Synchronized,       //同步模式
            Asynchronized,      //异步模式
        }

        public static WorkMode SysWorkMode = WorkMode.Asynchronized;

        public static void InitSetting()
        {
            var setting = Resources.Load<MessageSettingObject>("Message System Setting");
            if (setting != null)
            {
                //Debug.Log("Load Setting");
                DebugMode = setting.DebugMode;
                OpenLog = setting.OpenLog;
                SysWorkMode = setting.SysWorkMode;
            }
        }
    }

    public delegate void MessageHandleMethod(params object[] message_params);

    public interface IMessageSystemHandler
    {
        string getMessageUid { get; }
        void initHandleMethodMap(Dictionary<string, MessageHandleMethod> HandleMethodMap);
    }

    /// <summary>
    /// 消息调用信息
    /// </summary>
    public struct MessageCallInfo
    {
        public long Send_timeStamp;
        public string Sender_Type;
        public string Sender_Mothod;
        public string[] Sender_Params;
        public string Sender_StackTrace;

        public long Handle_timeStamp;
        public string Handler_Method;

        public DateTime GetSendTime { get { return DateTime.FromBinary(Send_timeStamp); } }
        public DateTime GetHandleTime { get { return DateTime.FromBinary(Handle_timeStamp); } }
        string _senderString;
        public string getSenderString()
        {
            if (string.IsNullOrEmpty(_senderString))
            {
                _senderString = string.Format("{0}:{1}[", Sender_Type, Sender_Mothod);
                //Debug.Log(_senderString);
                //Debug.Log(Sender_Params.Length);
                if (Sender_Params == null
                    || Sender_Params.Length == 0)
                    _senderString += "null";
                else
                {
                    foreach (var p in Sender_Params)
                        _senderString += (p + ",");
                }
                //Debug.Log(_senderString);
                var rm_index = _senderString.LastIndexOf(',');
                _senderString = (rm_index > -1 ? _senderString.Remove(rm_index) : _senderString) + "]";
            }
            return _senderString;
        }

        //string _handleString;
        //public string getHandleString()
        //{
        //    if (string.IsNullOrEmpty(_senderString))
        //    {

        //    }

        //    return _handleString;
        //}
    }

    public class MessageHandler
    {
        public IMessageSystemHandler IHdnaler;
        public Dictionary<string, MessageHandleMethod> HandleMethodMap;

        public int registerObjectHash { get { return IHdnaler.GetHashCode(); } }
        public string messageUid { get { return IHdnaler.getMessageUid; } }

        public MessageHandler(IMessageSystemHandler IHdnaler)
        {
            this.IHdnaler = IHdnaler;
            HandleMethodMap = new Dictionary<string, MessageHandleMethod>();
            IHdnaler.initHandleMethodMap(HandleMethodMap);
        }


        public Stack<MessageCallInfo> callStack = new Stack<MessageCallInfo>();
        public void RecordCallInfo(long send_timeStamp, string sender_info, object[] params_info, long handle_timeStamp, string handle_method)
        {
            if (!MessageSetting.DebugMode)
                return;

            MessageCallInfo messageCall = new MessageCallInfo();

            var sps = sender_info.Split(' ');
            var mps = sps[0].Split(':');
            messageCall.Send_timeStamp = send_timeStamp;
            messageCall.Sender_Type = mps[0];
            messageCall.Sender_Mothod = mps[1].Substring(0, mps[1].IndexOf('('));
            messageCall.Sender_Params = new string[params_info.Length];
            messageCall.Sender_StackTrace = sender_info.Substring(sps[0].Length);

            messageCall.Handle_timeStamp = handle_timeStamp;
            messageCall.Handler_Method = handle_method;
            for (int i = 0; i < params_info.Length; i++)
            {
                messageCall.Sender_Params[i] = params_info[i].ToString();
            }

            callStack.Push(messageCall);
            //Debug.Log(sender_info + "\n" + handler_info);
        }
    }

    public struct MessageSender
    {
        public string message_uid;
        public string method_id;
        public object[] message_params;

        public long send_timeStamp;
        public string sender_info;

        public MessageSender(string message_uid, string method_id, object[] message_params)
        {
            this.message_uid = message_uid;
            this.method_id = method_id;
            this.message_params = message_params;
            send_timeStamp = DateTime.Now.ToBinary();
            sender_info = "Unknow Sender";
        }

        public void RecordSender(string sender_info)
        {
            this.sender_info = sender_info;
        }
    }

    public class MessageCore : MonoBehaviour
    {
        private static MessageCore _singleton = null;

        public virtual void Awake()
        {
            if (_singleton == null)
            {
                _singleton = this as MessageCore;
                GameObject.DontDestroyOnLoad(_singleton);
            }

            MessageSetting.InitSetting();
        }

        public static MessageCore Instance
        {
            get
            {
                if (_singleton != null) return _singleton;
                var go = new GameObject(typeof(MessageCore).ToString());
                GameObject.DontDestroyOnLoad(go);
                _singleton = go.AddComponent<MessageCore>();
                return _singleton;
            }
        }

        //Message Handler Map
        Dictionary<string, Dictionary<int, MessageHandler>> messageHandlersMap = new Dictionary<string, Dictionary<int, MessageHandler>>();
        public Dictionary<string, Dictionary<int, MessageHandler>> getMessageHandlersMap() { return messageHandlersMap; }

        Stack<string> removeHandlers = new Stack<string>();
        //消息队列，异步模式下使用
        Queue<MessageSender> messagesQueue = new Queue<MessageSender>();

        void addHandler(IMessageSystemHandler interface_handler)
        {
            string msg_uid = interface_handler.getMessageUid;
            if (!messageHandlersMap.ContainsKey(msg_uid))
            {
                messageHandlersMap.Add(msg_uid, new Dictionary<int, MessageHandler>());
            }

            var handlerDic = messageHandlersMap[msg_uid];
            var handler_hash = interface_handler.GetHashCode();
            if (!handlerDic.ContainsKey(handler_hash))
            {
                MessageHandler handler = new MessageHandler(interface_handler);
                handlerDic.Add(handler_hash, handler);
            }

#if UNITY_EDITOR
            if (MessageSetting.DebugMode)
            {//开启调试
                var gameObject = MessageSystemEditorHelper.FindHandlerGameObject(interface_handler);
                if (gameObject != null)
                {
                    var debugger = gameObject.GetComponent<MessageSystemHandlerDebugger>();
                    if (debugger == null)
                        debugger = gameObject.AddComponent<MessageSystemHandlerDebugger>();
                    debugger.AddHandler(interface_handler);
                    debugger.hideFlags = HideFlags.DontSave;
                    //debugger.hideFlags = HideFlags.HideInInspector;
                }
            }
#endif
        }

        void MarkHandlerDispose(IMessageSystemHandler interface_handler)
        {
            lock (removeHandlers)
            {
                removeHandlers.Push(Handler2Identiry(interface_handler));
            }
        }

        void removeHandler(string msg_uid,int handler_hash)
        {
            if (messageHandlersMap.ContainsKey(msg_uid))
            {
                var handlerDic = messageHandlersMap[msg_uid];
                if (handlerDic.ContainsKey(handler_hash))
                {
                    handlerDic.Remove(handler_hash);
                    Log("<color=#00efef>MessageSystem =></color> Remove Handler <color=#efef00>" + msg_uid + "</color>+<color=#ef0000>[" + handler_hash + "]</color>");

                }

                if (handlerDic.Count <= 0)
                {
                    messageHandlersMap.Remove(msg_uid);
                    Log("<color=#00efef>MessageSystem =></color> Remove All Handler, Message Uid is <color=#efef00>" + msg_uid + "</color>");
                }
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

#endif

            if (MessageSetting.SysWorkMode == MessageSetting.WorkMode.Asynchronized)
            {
                handMessageAsync();
            }
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

#endif

            while (removeHandlers.Count > 0)
            {
                var rm_handler = removeHandlers.Pop();
                var rm_params = rm_handler.Split('+');
                var rm_msg_uid = rm_params[0];
                var rm_hash = int.Parse(rm_params[1]);
                removeHandler(rm_msg_uid, rm_hash);
            }
        }

        void handleMessage(MessageSender msg)
        {
            string current_handler = "";
            try
            {
                if (messageHandlersMap.ContainsKey(msg.message_uid))
                {
                    var handlerDic = messageHandlersMap[msg.message_uid];
                    foreach (var handler in handlerDic)
                    {
                        current_handler = string.Format("{0}+[{1}]", handler.Value.IHdnaler.ToString(), handler.Key);
                        var methods = handler.Value.HandleMethodMap;
                        if (methods.ContainsKey(msg.method_id))
                        {
                            var handle_mehtod = methods[msg.method_id];

                            long handle_timeStamp = DateTime.Now.ToBinary();
                            handle_mehtod(msg.message_params);

                            if (MessageSetting.DebugMode)
                            {
                                if (MessageSetting.SysWorkMode == MessageSetting.WorkMode.Synchronized)
                                {//同步模式记录
                                    handler.Value.RecordCallInfo(msg.send_timeStamp, msg.sender_info, msg.message_params, msg.send_timeStamp, handle_mehtod.Method.ToString());
                                }
                                else
                                {//异步模式记录
                                    handler.Value.RecordCallInfo(msg.send_timeStamp, msg.sender_info, msg.message_params, handle_timeStamp, handle_mehtod.Method.ToString());
                                }
                            }

                            Log(string.Format("<color=#00efef>MessageSystem =></color> Handle Message :current handler =  <color=#ef0000>{0}</color> ," +
                                "msg uid = <color=#efef00>{1}</color>, method id = <color=#efef00>{2}</color>",
                                current_handler, msg.message_uid, msg.method_id));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(string.Format("<color=#00efef>MessageSystem =></color> Handle Message Exception : current handler = {0} ,msg uid = {1}, method id = {2},\n{3}\n{4}",
                    current_handler, msg.message_uid, msg.method_id, ex.Message, ex.StackTrace));
            }
        }

        public void EnqueueMessage(MessageSender msgData)
        {//将消息加入队列
            lock (messagesQueue)
            {
                messagesQueue.Enqueue(msgData);
            }
        }
        //异步处理消息
        void handMessageAsync()
        {
            while (messagesQueue.Count > 0)
            {
                var msg = messagesQueue.Dequeue();
                handleMessage(msg);
            }
        }

        static string Handler2Identiry(IMessageSystemHandler handler)
        {
            return handler.getMessageUid + "+" + handler.GetHashCode();
        }

#region Static Methods

        public static void RegisterHandler(IMessageSystemHandler handler)
        {
            Instance.addHandler(handler);
        }

        public static void SendMessage(string msg_uid, string method_id, params object[] msg_params)
        {

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

#endif
            MessageSender msgData = new MessageSender(msg_uid, method_id, msg_params);

            if (MessageSetting.DebugMode)
            {//Debug模式下记录发送者信息
                string sender_info = "";
                var frames = StackTraceUtility.ExtractStackTrace().Split('\n');
                if (frames.Length > 1)
                    sender_info = (frames[1]);
                else
                    sender_info = (frames[0]);

                msgData.RecordSender(sender_info);  //记录发送者
            }

            if (MessageSetting.SysWorkMode == MessageSetting.WorkMode.Synchronized)
                Instance.handleMessage(msgData);
            else
                Instance.EnqueueMessage(msgData);
        }

        public static void UnregisterHandler(IMessageSystemHandler handler)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

#endif
            Instance.MarkHandlerDispose(handler);
        }

        static void Log(object log)
        {
            if (MessageSetting.OpenLog)
                Debug.Log(log);
        }

        static void LogError(object log)
        {
            //if (MessageSetting.OpenLog)
                Debug.LogError(log);
        }

#endregion
    }
}


#if UNITY_EDITOR

public static class MessageSystemEditorHelper
{
    public static GameObject FindHandlerGameObject(MessageSystem.IMessageSystemHandler handler)
    {
        var objs = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
        foreach (var obj in objs)
        {
            if (obj.GetHashCode() == handler.GetHashCode())
            {
                return obj.gameObject;
            }
        }

        return null;
    }
}
#endif
