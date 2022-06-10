using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

namespace MessageSystem
{
    public static class ColorDefine
    {
        public static Color white = Color.white;
        public static Color red = Color.red;
        public static Color cyan = Color.cyan;
        public static Color green = Color.green;
        public static Color yellow = Color.yellow;
        public static Color orange = new Color(0.9f, 0.5f, 0f);
        public static Color blue = Color.blue;
        public static Color lightblue = new Color(0f, 0.3f, 0.75f);
    }

    //public class MessageSystemEditor
    //{

    //    [SettingsProvider]
    //    public static SettingsProvider getMessageSetting()
    //    {
    //        return new SettingsProvider("Project/Message Setting", SettingsScope.Project)
    //        {
    //            label = "Message System",
    //            activateHandler = (searchContext, rootElement) =>
    //            {
    //                MessageSettingObject.Instance();
    //            },
    //            guiHandler = (searchContext)=>
    //            {
    //                var ins = MessageSettingObject.Instance();
    //                EditorGUILayout.BeginVertical();
    //                EditorGUILayout.Space();

    //                var raw_1 = ins.SysWorkMode;
    //                var raw_2 = ins.DebugMode;
    //                var raw_3 = ins.OpenLog;

    //                ins.SysWorkMode = (MessageSetting.WorkMode)EditorGUILayout.EnumPopup("WorkMode :",ins.SysWorkMode);
    //                ins.DebugMode = EditorGUILayout.Toggle("Enable Debug Mode: ", ins.DebugMode);
    //                ins.OpenLog = EditorGUILayout.Toggle("Enable Log: ", ins.OpenLog);
    //                EditorGUILayout.EndVertical();

    //                if (ins.SysWorkMode != raw_1
    //                || ins.DebugMode != raw_2
    //                || ins.OpenLog != raw_3)
    //                {
    //                    EditorUtility.SetDirty(ins);
    //                    AssetDatabase.SaveAssets();
    //                    //Debug.Log("Save");
    //                }
    //            },
    //        };
    //    }
    //}

    [CustomEditor(typeof(MessageCore))]
    public class MessageCoreEditor : Editor
    {
        static Dictionary<string, bool> msg_toogle = new Dictionary<string, bool>();
        static Dictionary<string, bool> hash_toogle = new Dictionary<string, bool>();

        static Vector2 scrollPos;

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            var raw_color = GUI.color;


            MessageCore core = target as MessageCore;
            var handlerMap = core.getMessageHandlersMap();

            GUILayout.Label("WorkMode : " + MessageSetting.SysWorkMode + (MessageSetting.DebugMode ? "(Debug)" : ""), "box");
            GUILayout.Space(5);
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            foreach (var handlinfo in handlerMap)
            {
                GUILayout.BeginVertical("CurveEditorBackground");

                if (!msg_toogle.ContainsKey(handlinfo.Key))
                    msg_toogle.Add(handlinfo.Key, false);

                //GUILayout.BeginHorizontal("sv_label_4", GUILayout.MinHeight(30));
                GUI.contentColor = msg_toogle[handlinfo.Key] ? ColorDefine.green : ColorDefine.cyan;
                msg_toogle[handlinfo.Key] = GUILayout.Toggle(msg_toogle[handlinfo.Key], "Message UID : " + handlinfo.Key, msg_toogle[handlinfo.Key] ? "ToolbarPopup" : "ToolbarDropDown");
                GUI.contentColor = raw_color;

                //GUILayout.EndHorizontal();
                if (msg_toogle[handlinfo.Key])
                {
                    GUILayout.BeginVertical("OL box");

                    var hder = handlerMap[handlinfo.Key];
                    GUILayout.Space(5);

                    int counter = 0;
                    foreach (var hd in hder)
                    {
                        var hash_toogle_str = hd.Key + "+" + hd.Value.messageUid;
                        if (!hash_toogle.ContainsKey(hash_toogle_str))
                            hash_toogle.Add(hash_toogle_str, false);
                        //GUILayout.BeginHorizontal();
                        GUILayout.Space(5);

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("", "radio"/*"radio"*/))
                        {//查找并选中物体

                            var str = hd.Value.IHdnaler.ToString();
                            str = str.Replace("(Clone)", "%^&");
                            str = str.Replace(" (", "!");
                            var ss = str.Split('!');
                            var find_str = ss[0].Replace("%^&", "(Clone)");
                            var find_object = GameObject.Find(find_str);
                            if (find_object != null)
                            {
                                Selection.activeGameObject = find_object;
                                EditorWindow.FocusWindowIfItsOpen<SceneView>();
                            }
                            else
                                Debug.LogError("Can't Find GameObject In SceneView (" + find_str + "),Is this GameObject inactive in SceneView?");
                        }

                        //GUILayout.FlexibleSpace();

                        GUI.contentColor = ColorDefine.white;
                        hash_toogle[hash_toogle_str] = GUILayout.Toggle(hash_toogle[hash_toogle_str], "Handler " + counter + " => " + hd.Value.IHdnaler.ToString() + "    Hash : " + hd.Key.ToString(),
                            hash_toogle[hash_toogle_str] ? "ToolbarPopup" : "ToolbarDropDown");
                        GUI.contentColor = raw_color;

                        GUILayout.FlexibleSpace();

                        GUILayout.EndHorizontal();

                        if (hash_toogle[hash_toogle_str])
                        {
                            GUILayout.Space(5);
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            GUILayout.BeginVertical("U2D.createRect");
                            foreach (var method_info in hd.Value.HandleMethodMap)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                DrawLabel("Method ID : " + method_info.Key, ColorDefine.yellow);
                                GUILayout.FlexibleSpace();
                                DrawLabel("<+>");
                                GUILayout.FlexibleSpace();
                                DrawLabel(method_info.Value.Method.ToString(), ColorDefine.orange);
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();
                            GUILayout.Space(20);
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.Space(5);
                        //GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.EndVertical();
                GUILayout.Space(5);
            }

            GUILayout.EndScrollView();
            Repaint();
        }

        public void DrawLabel(string label)
        {
            DrawLabel(label, GUI.color);
        }

        public void DrawLabel(string label, Color color, string style = "label")
        {
            var raw = GUI.color;
            GUI.color = color;
            GUILayout.Label(label, style);
            GUI.color = raw;
        }
    }

    [CustomEditor(typeof(MessageSystemHandlerDebugger), true, isFallback = true)]
    public class IMessageSystemHandlerEditor : Editor
    {
        protected override bool ShouldHideOpenButton()
        {
            return true;
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("Message Debugger");
        }

        public class HandlerControler
        {
            public bool handlerToogle;
            public bool methodToogle;
            public bool logToogle;
            public Vector2 logScrollPos;
        }

        Dictionary<string, bool> msg_toogle = new Dictionary<string, bool>();
        Dictionary<string, HandlerControler> handlerControler = new Dictionary<string, HandlerControler>();

        string log_filter = "";

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            //GUILayout.Label("xxxx");
            MessageSystemHandlerDebugger debugger = target as MessageSystemHandlerDebugger;
            GUI.color = ColorDefine.white;

            GUILayout.Space(10);
            foreach (var map in debugger.Msg_IHandlers_Map)
            {
                if (!msg_toogle.ContainsKey(map.Key))
                    msg_toogle.Add(map.Key, false);

                GUI.contentColor = msg_toogle[map.Key] ? ColorDefine.green : ColorDefine.cyan;
                GUILayout.BeginVertical("CurveEditorBackground");
                msg_toogle[map.Key] = GUILayout.Toggle(msg_toogle[map.Key], "Message UID : " + map.Key, msg_toogle[map.Key] ? "ToolbarPopup" : "ToolbarDropDown");
                GUILayout.EndVertical();
                GUI.contentColor = ColorDefine.white;

                //GUILayout.EndHorizontal();
                if (msg_toogle[map.Key])
                {
                    int counter = 0;
                    foreach (var handler in map.Value)
                    {
                        DrawHandlerArea(debugger, handler, counter);
                        counter++;
                    }
                }

                GUILayout.Space(15);
            }

            Repaint();
        }

        void DrawHandlerArea(MessageSystemHandlerDebugger debugger, MessageHandler handler, int index = 0)
        {
            GUILayout.Space(5);
            //var handler = debugger.GetMessageHandler(Ihandler);
            //if (handler == null)
            //    return;

            var hash = handler.registerObjectHash + "+" + handler.messageUid;

            if (!handlerControler.ContainsKey(hash))
            {
                //handlerLogToogle.Add(hash, false);
                var contorler = new HandlerControler();
                handlerControler.Add(hash, contorler);
            }

            var curControler = handlerControler[hash];

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            //GUI.color = ColorDefine.
            curControler.handlerToogle = EditorGUILayout.Foldout(curControler.handlerToogle, "Handler " + index + " => " + handler.IHdnaler.ToString() + "  Hash : " + handler.IHdnaler.GetHashCode());
            GUILayout.Space(10);
            GUILayout.EndHorizontal();

            if (!curControler.handlerToogle)
                return;

            GUILayout.BeginVertical("CurveEditorBackground");
            //GUILayout.BeginVertical("OL Box");

            GUILayout.Space(10);
            //Draw Method Info
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            curControler.methodToogle = EditorGUILayout.Foldout(curControler.methodToogle, "Method Info");
            GUILayout.EndHorizontal();

            if (curControler.methodToogle)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.BeginVertical("U2D.createRect");

                foreach (var method_info in handler.HandleMethodMap)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    DrawLabel("Method ID : " + method_info.Key, ColorDefine.yellow);
                    GUILayout.FlexibleSpace();
                    DrawLabel("<+>");
                    GUILayout.FlexibleSpace();
                    DrawLabel(method_info.Value.Method.ToString(), ColorDefine.orange);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                GUILayout.Space(20);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            //Draw Method Info
            GUILayout.BeginHorizontal();
            GUILayout.Space(20); curControler.logToogle = EditorGUILayout.Foldout(curControler.logToogle, string.Format("Message Call Log ({0})", handler.callStack.Count));
            //GUILayout.FlexibleSpace();
            //GUILayout.Space(5); GUILayout.Label(string.Format("({0})", handler.callStack.Count));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear Log")) { handler.ClearCallStack(); }
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            log_filter = EditorGUILayout.TextField("Method ID Filter:", log_filter);
            handler.SetFilter(log_filter);
            GUILayout.EndHorizontal();

            if (curControler.logToogle)
            {
                //绘制调用记录
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);

                curControler.logScrollPos = GUILayout.BeginScrollView(curControler.logScrollPos, "dockarea", GUILayout.MinHeight(0), GUILayout.MaxHeight(400));
                GUILayout.Space(10);

                GUILayout.BeginVertical();

                int ct = 0;
                var callStack = handler.filterCallStack.GetEnumerator();
                while (callStack.MoveNext())
                {
                    GUILayout.BeginVertical(ct % 2 == 0 ? "box" : "dockarea");
                    ct++;
                    GUILayout.Space(2);
                    var call = callStack.Current;
                    GUILayout.BeginHorizontal();
                    DrawLabel(string.Format("Sent at [{0}]", call.GetSendTime.ToString("HH:mm:ss :fff")));
                    DrawLabel(" Sender => ");
                    DrawLabel(call.getSenderString(), ColorDefine.yellow);
                    DrawLabel(string.Format("{0}", call.Sender_StackTrace), ColorDefine.lightblue);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    DrawLabel(string.Format("Handled at [{0}]", call.GetHandleTime.ToString("HH:mm:ss :fff")));
                    DrawLabel("Handle Method =>");
                    DrawLabel(call.getHandlerString(), ColorDefine.orange);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2);
                    GUILayout.EndVertical();
                }

                GUILayout.EndVertical();

                GUILayout.Space(10);
                GUILayout.EndScrollView();

                GUILayout.Space(20);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.EndVertical();
        }

        public void DrawLabel(string label)
        {
            DrawLabel(label, GUI.color);
        }

        public void DrawLabel(string label, Color color, string style = "label")
        {
            var raw = GUI.color;
            GUI.color = color;
            GUILayout.Label(label, style);
            GUI.color = raw;
        }
    }
}




