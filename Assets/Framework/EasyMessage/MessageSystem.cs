using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
            var setting = Resources.Load<MessageSettingObject>("MessageSetting");
            if (setting != null)
            {
                //Debug.Log("Load Setting");
                DebugMode = setting.DebugMode;
                OpenLog = setting.OpenLog;
                SysWorkMode = setting.SysWorkMode;
            }
        }
    }

    /// <summary>
    /// 消息处理委托
    /// </summary>
    /// <param name="message_params"></param>
    public delegate void MessageHandleMethod(params object[] message_params);

    /// <summary>
    /// 消息过滤委托
    /// </summary>
    /// <param name="mark">过滤参数</param>
    /// <returns>是否触发</returns>
    public delegate bool MessageFilterMethod(string msg_uid, object mark);

    public interface IBaseMessageHandler { }

    /// <summary>
    /// 消息处理接口
    /// </summary>
    public interface IMessageHandler : IBaseMessageHandler
    {
        string getMessageUid { get; }
        void initHandleMethodMap(Dictionary<string, MessageHandleMethod> HandleMethodMap);
    }

    /// <summary>
    /// 多重消息处理接口
    /// </summary>
    public interface IMultiMessageHandler : IBaseMessageHandler
    {
        void initMessageUids(List<string> MessageUids);
        void initHandleMethodMap(Dictionary<string, Dictionary<string, MessageHandleMethod>> HandleMethodMap);
    }

    /// <summary>
    /// 消息调用信息
    /// </summary>
    public struct MessageCallInfo
    {
        public string Message_UID;
        public string Method_ID;

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
                if (Sender_Params == null
                    || Sender_Params.Length == 0)
                    _senderString += "null";
                else
                {
                    foreach (var p in Sender_Params)
                        _senderString += (p + ",");
                }
                var rm_index = _senderString.LastIndexOf(',');
                _senderString = (rm_index > -1 ? _senderString.Remove(rm_index) : _senderString) + "]";
            }
            return _senderString;
        }
    }

    public abstract class BaseMessageHandler
    {
        public Stack<MessageCallInfo> callStack = new Stack<MessageCallInfo>();
        public void RecordCallInfo(string msg_id, string method_id, long send_timeStamp, string sender_info, object[] params_info, long handle_timeStamp, string handle_method)
        {
            if (!MessageSetting.DebugMode)
                return;

            MessageCallInfo messageCall = new MessageCallInfo();

            messageCall.Message_UID = msg_id;
            messageCall.Method_ID = method_id;

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
            if (fitFilter(messageCall))
                filterCallStack.Push(messageCall);
            //Debug.Log(sender_info + "\n" + handler_info);
        }

        string stackfilter = "";
        public Stack<MessageCallInfo> filterCallStack = new Stack<MessageCallInfo>();
        public void SetFilter(string filter)
        {
            if (this.stackfilter == filter)
                return;

            this.stackfilter = filter;

            if (string.IsNullOrEmpty(this.stackfilter))
            {
                filterCallStack = new Stack<MessageCallInfo>(callStack);
                filterCallStack = RevertStack(filterCallStack);
                return;
            }

            filterCallStack.Clear();

            //var temp = new Stack<MessageCallInfo>();
            var curStack = callStack.GetEnumerator();
            while (curStack.MoveNext())
            {
                var info = curStack.Current;
                if (fitFilter(info))
                    filterCallStack.Push(info);
            }

            filterCallStack = RevertStack(filterCallStack);
        }

        Stack<MessageCallInfo> RevertStack(Stack<MessageCallInfo> raw)
        {
            var temp = new Stack<MessageCallInfo>(raw);
            return temp;
        }

        bool fitFilter(MessageCallInfo info)
        {
            if (string.IsNullOrEmpty(stackfilter))
                return true;

            bool res = false;
            var fs = stackfilter.Split(';');
            foreach (var f in fs)
            {
                var nf = f.Trim();
                if (string.IsNullOrEmpty(nf))
                    continue;
                if (nf[0] == '!')
                    res = ("!" + info.Method_ID) != nf;
                else
                    res = info.Method_ID == nf;

                if (res)
                    return true;
            }

            return res;
        }

        public void ClearCallStack()
        {
            callStack.Clear();
            filterCallStack.Clear();
        }
    }

    /// <summary>
    /// 消息处理器
    /// </summary>
    public class MessageHandler: BaseMessageHandler
    {
        public IBaseMessageHandler IHdnaler;
        public Dictionary<string, MessageHandleMethod> HandleMethodMap;

        public int registerObjectHash { get { return IHdnaler.GetHashCode(); } }
        public string messageUid { get; private set; }

        public bool hasFilter { get { return filterMethod != null; } }
        public MessageFilterMethod filterMethod;

        public MessageHandler(IBaseMessageHandler IHdnaler, string messageUid,
            Dictionary<string, MessageHandleMethod> HandleMethodMap, MessageFilterMethod filterMethod = null)
        {
            this.IHdnaler = IHdnaler;
            this.messageUid = messageUid;
            this.HandleMethodMap = HandleMethodMap;
            this.filterMethod = filterMethod;
        }
    }

    /// <summary>
    /// 消息发送器
    /// </summary>
    public struct MessageSender
    {
        public string message_uid;
        public string method_id;
        public object[] filter_mark;
        public object[] message_params;

        public long send_timeStamp;
        public string sender_info;

        //过滤模式
        public enum FilterMode
        {
            DontFilter,
            Include,
            Except,
        }

        public FilterMode filterMode;

        public MessageSender(string message_uid, string method_id, FilterMode filterMode, object[] filter_mark, object[] message_params)
        {
            this.message_uid = message_uid;
            this.method_id = method_id;
            this.filterMode = filterMode;
            this.filter_mark = filter_mark;
            this.message_params = message_params;
            send_timeStamp = DateTime.Now.ToBinary();
            sender_info = "Unknow Sender";
        }

        public void RecordSender(string sender_info)
        {
            this.sender_info = sender_info;
        }
    }

    /// <summary>
    /// 消息内核
    /// </summary>
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

        void addMessage(string msg_uid)
        {
            if (!messageHandlersMap.ContainsKey(msg_uid))
            {
                messageHandlersMap.Add(msg_uid, new Dictionary<int, MessageHandler>());
            }
        }

        void addDebug(IBaseMessageHandler interface_handler, MessageHandler handler)
        {
#if UNITY_EDITOR
            if (MessageSetting.DebugMode)
            {//开启调试
                var gameObject = MessageSystemEditorHelper.FindHandlerGameObject(interface_handler);
                if (gameObject != null)
                {
                    var debugger = gameObject.GetComponent<MessageSystemHandlerDebugger>();
                    if (debugger == null)
                        debugger = gameObject.AddComponent<MessageSystemHandlerDebugger>();
                    debugger.AddHandler(handler);
                    debugger.hideFlags = HideFlags.DontSave;
                    //debugger.hideFlags = HideFlags.HideInInspector;
                }
            }
#endif
        }

        void addHandler(IMessageHandler interface_handler, MessageFilterMethod messageFilter = null)
        {
            string msg_uid = interface_handler.getMessageUid;
            addMessage(msg_uid);

            var handlerDic = messageHandlersMap[msg_uid];
            var handler_hash = interface_handler.GetHashCode();
            if (!handlerDic.ContainsKey(handler_hash))
            {
                Dictionary<string, MessageHandleMethod> methodsMap = new Dictionary<string, MessageHandleMethod>();
                interface_handler.initHandleMethodMap(methodsMap);
                MessageHandler handler = new MessageHandler(interface_handler, interface_handler.getMessageUid, methodsMap, messageFilter);
                handlerDic.Add(handler_hash, handler);
            }

            addDebug(interface_handler, handlerDic[handler_hash]);
        }

        void addHandler(IMultiMessageHandler interface_handler, MessageFilterMethod messageFilter = null)
        {
            List<string> msg_uids = new List<string>();
            interface_handler.initMessageUids(msg_uids);

            Dictionary<string, Dictionary<string, MessageHandleMethod>> msg_methods_map = new Dictionary<string, Dictionary<string, MessageHandleMethod>>();
            foreach (var msg_id in msg_uids)
                msg_methods_map.Add(msg_id, new Dictionary<string, MessageHandleMethod>());

            interface_handler.initHandleMethodMap(msg_methods_map);

            foreach (var msg_uid in msg_uids)
            {
                addMessage(msg_uid);

                var handlerDic = messageHandlersMap[msg_uid];
                var handler_hash = interface_handler.GetHashCode();
                if (!handlerDic.ContainsKey(handler_hash))
                {
                    MessageHandler handler = new MessageHandler(interface_handler, msg_uid, msg_methods_map[msg_uid], messageFilter);
                    handlerDic.Add(handler_hash, handler);
                }

                addDebug(interface_handler, handlerDic[handler_hash]);
            }
        }

        void MarkHandlerDispose(IMessageHandler interface_handler)
        {
            lock (removeHandlers)
            {
                removeHandlers.Push(Handler2Identiry(interface_handler));
            }
        }

        void MarkHandlerDispose(IMultiMessageHandler interface_handler)
        {
            lock (removeHandlers)
            {
                List<string> rm_msg_uids = new List<string>();
                interface_handler.initMessageUids(rm_msg_uids);
                foreach(var msg_uid in rm_msg_uids)
                {
                    removeHandlers.Push(Handler2Identiry(msg_uid, interface_handler.GetHashCode()));
                }
            }
        }

        void MarkHandlerDispose(MessageHandler handler)
        {
            lock (removeHandlers)
            {
                removeHandlers.Push(Handler2Identiry(handler.messageUid, handler.registerObjectHash));
            }
        }

        void removeHandler(string msg_uid, int handler_hash)
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
                        if (handler.Value.IHdnaler.ToString() == "null")
                        {
                            MarkHandlerDispose(handler.Value);
                            continue;
                        }

                        bool breakMark = false;
                        if (msg.filterMode != MessageSender.FilterMode.DontFilter
                            && msg.filter_mark != null)
                        {//handler filter
                            if (!handler.Value.hasFilter)
                                continue;

                            breakMark = MessageSender.FilterMode.Include == msg.filterMode;

                            for (int i = 0; i < msg.filter_mark.Length; i++)
                            {
                                var mark = msg.filter_mark[i];
                                if (handler.Value.filterMethod(msg.message_uid, mark))
                                {
                                    breakMark = !(msg.filterMode == MessageSender.FilterMode.Include);
                                    break;
                                }
                            }

                            if (breakMark)
                                continue;
                        }

                        current_handler = string.Format("{0}+[{1}]", handler.Value.IHdnaler.ToString(), handler.Key);
                        var methods = handler.Value.HandleMethodMap;
                        if (methods.ContainsKey(msg.method_id))
                        {
                            var handle_mehtod = methods[msg.method_id];
                            //Debug.Log(handle_mehtod);
                            if (handle_mehtod != null)
                            {
                                long handle_timeStamp = DateTime.Now.ToBinary();
                                handle_mehtod(msg.message_params);

                                if (MessageSetting.DebugMode)
                                {
                                    if (MessageSetting.SysWorkMode == MessageSetting.WorkMode.Synchronized)
                                    {//同步模式记录
                                        handler.Value.RecordCallInfo(msg.message_uid,msg.method_id,
                                            msg.send_timeStamp, msg.sender_info, msg.message_params,
                                            msg.send_timeStamp, handle_mehtod.Method.ToString());
                                    }
                                    else
                                    {//异步模式记录
                                        handler.Value.RecordCallInfo(msg.message_uid, msg.method_id,
                                            msg.send_timeStamp, msg.sender_info, msg.message_params,
                                            handle_timeStamp, handle_mehtod.Method.ToString());
                                    }
                                }

                                Log(string.Format("<color=#00efef>MessageSystem =></color> Handle Message :current handler =  <color=#ef0000>{0}</color> ," +
                                    "msg uid = <color=#efef00>{1}</color>, method id = <color=#efef00>{2}</color>",
                                    current_handler, msg.message_uid, msg.method_id));
                            }
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

        static string Handler2Identiry(IMessageHandler handler)
        {
            return Handler2Identiry(handler.getMessageUid, handler.GetHashCode());
        }

        static string Handler2Identiry(string msg_uid,int hashCode)
        {
            return msg_uid + "+" + hashCode;
        }

        #region Static Methods

        public static void RegisterHandler(IMessageHandler handler, MessageFilterMethod messageFilter = null)
        {
            Instance.addHandler(handler, messageFilter);
        }

        public static void RegisterHandler(IMultiMessageHandler handler, MessageFilterMethod messageFilter = null)
        {
            Instance.addHandler(handler, messageFilter);
        }

        public static void SendMessage(string msg_uid, string method_id,params object[] msg_params)
        {

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

#endif
            MessageSender msgData = new MessageSender(msg_uid, method_id, MessageSender.FilterMode.DontFilter, null, msg_params);

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

        /// <summary>
        /// 发送消息，满足过滤条件的消息监听者将被触发。
        /// </summary>
        /// <param name="msg_uid">消息UID</param>
        /// <param name="method_id">子ID</param>
        /// <param name="filter_mark">过滤参数</param>
        /// <param name="msg_params">消息参数</param>
        public static void SendMessageInclude(string msg_uid, string method_id, object[] filter_mark, params object[] msg_params)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

#endif
            MessageSender msgData = new MessageSender(msg_uid, method_id, MessageSender.FilterMode.Include, filter_mark, msg_params);
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

        /// <summary>
        /// 发送消息，满足过滤条件的消息监听者会被过滤。
        /// </summary>
        /// <param name="msg_uid">消息UID</param>
        /// <param name="method_id">子ID</param>
        /// <param name="filter_mark">过滤参数</param>
        /// <param name="msg_params">消息参数</param>
        public static void SendMessageExcept(string msg_uid, string method_id, object[] filter_mark, params object[] msg_params)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

#endif
            MessageSender msgData = new MessageSender(msg_uid, method_id, MessageSender.FilterMode.Except, filter_mark, msg_params);
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

        public static void UnregisterHandler(IMessageHandler handler)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

#endif
            Instance.MarkHandlerDispose(handler);
        }

        public static void UnregisterHandler(IMultiMessageHandler handler)
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
    public static GameObject FindHandlerGameObject(MessageSystem.IBaseMessageHandler handler)
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
