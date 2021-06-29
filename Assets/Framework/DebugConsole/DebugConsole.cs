using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugConsole : MonoSingleton<DebugConsole>
{
    public Color grey_color = new Color(0.8f, 0.8f, 0.8f);

    public Color blue_color = new Color(0f, 0.9f, 0.9f);

    static ulong logUidCounter;

    bool onlyError;

    public static void StartDebugConsole(bool onlyError = false)
    {
        Instance.onlyError = onlyError;
    }

    public class LogInfo
    {
        public ulong uid;
        public System.DateTime timeStamp;
        public Color color;
        public string condition;
        public string stackTrace;

        public string conditionStr { get { return string.Format("[{0}]  {1}", timeStamp.ToString("HH:mm:ss"), condition); } }
    }

    public List<LogInfo> logInfos = new List<LogInfo>();

    public override void Awake()
    {
        base.Awake();
        Application.logMessageReceived += onLogRecieve;
        InitGUIStyle();
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= onLogRecieve;
    }

    void onLogRecieve(string condition, string stackTrace, UnityEngine.LogType type)
    {
        if (onlyError)
        {
            if (type == LogType.Log || type == LogType.Warning)
                return;
        }

        logUidCounter++;
        LogInfo info = new LogInfo();
        info.uid = logUidCounter;
        info.timeStamp = System.DateTime.Now;
        info.condition = condition;
        info.stackTrace = stackTrace;
        info.color = grey_color;
        if (type == LogType.Warning)
            info.color = Color.yellow;
        if (type == LogType.Error
            || type == LogType.Exception
            || type == LogType.Assert)
            info.color = Color.red;
        logInfos.Add(info);
    }

    private void OnGUI()
    {
        DrawGUI();
    }

    bool showGUI = false;

    GUIStyle font;
    //GUIStyle button_font;
    void InitGUIStyle()
    {
        font = new GUIStyle();
        font.fontSize = 24;
        font.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        font.normal.background = Texture2D.blackTexture;

        font.focused.textColor = new Color(0.8f, 0.8f, 0.8f);
        font.focused.background = Texture2D.blackTexture;

        //button_font = GUI.skin.button; //new GUIStyle("button");
        //button_font.fontSize = 24;
    }

    Vector2 logScrollPos;
    Vector2 detailScrollPos;

    LogInfo curInfo = null;

    Color rawColor;
    void SetGUIColor(Color color)
    {
        rawColor = GUI.color;
        GUI.color = color;
    }

    void ResetGUIColor()
    {
        GUI.color = rawColor;
    }

    float OpBtnWidth = 100;
    float OpBtnHeight = 100;

    float OpBtnScrollHorSpeed = 0.02f;
    float OpBtnScrollVerSpeed = 0.02f;

    void DrawGUI()
    {
        if (showGUI)
        {
            GUILayout.BeginVertical("Box");
            GUILayout.Label("Console: (Debug Console v2.0)", font);
            GUILayout.Label(string.Format("<color=#ff0000>FPS: {0}</color> | <color=#00ff00>TexMem: {1}MB</color> | <color=#0000ff>MaxTexMem: {2}MB</color>", FPS, TexMemory.ToString("0.0"), MaxTexMemory.ToString("0.0")), font);
            GUILayout.Space(10);

            #region Log 区域

            GUILayout.BeginHorizontal();

            #region Log面板 
            logScrollPos = GUILayout.BeginScrollView(logScrollPos, "Box", GUILayout.MaxWidth(0.85f * Screen.width), GUILayout.MaxHeight(Screen.height / 3));
            for (int i = 0; i < logInfos.Count; i++)
            {
                var info = logInfos[i];

                bool setColor = false;
                if (curInfo != null && curInfo.uid == info.uid)
                {
                    setColor = true;
                    SetGUIColor(blue_color);
                }

                if (setColor)
                {
                    if (GUILayout.Button(info.conditionStr, font))
                    {
                        curInfo = info;
                    }
                }
                else
                {
                    SetGUIColor(info.color);
                    if (GUILayout.Button(info.conditionStr, font))
                    {
                        curInfo = info;
                    }
                    ResetGUIColor();
                }

                if (setColor)
                    ResetGUIColor();

                GUILayout.Space(10);
            }
            GUILayout.EndScrollView();
            #endregion

            #region 画控制按钮

            GUILayout.FlexibleSpace();
            DrawOpBtn(ref logScrollPos);

            #endregion

            GUILayout.EndHorizontal();

            #endregion

            GUILayout.Space(20);

            #region Detail 区域

            GUILayout.BeginHorizontal();

            #region 绘制细节面板

            detailScrollPos = GUILayout.BeginScrollView(detailScrollPos, "box", GUILayout.MaxWidth(0.85f * Screen.width));

            GUILayout.Space(20);
            GUILayout.BeginVertical();
            GUILayout.Label("Detail:", font);

            GUILayout.Space(10);
            if (curInfo != null)
            {
                GUILayout.Label(curInfo.conditionStr, font);

                GUILayout.Space(20);
                //SetGUIColor(Color.blue);
                GUILayout.Label(curInfo.stackTrace, font);
                //ResetGUIColor();
            }
            GUILayout.EndVertical();

            GUILayout.EndScrollView();

            #endregion

            #region 画控制按钮

            DrawOpBtn(ref detailScrollPos);

            #endregion

            GUILayout.EndHorizontal();

            #endregion

            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Console", GUILayout.MinWidth(Screen.width / 10), GUILayout.MinHeight(Screen.height / 20)))
            {
                curInfo = null;
                logInfos.Clear();
            }

            if (GUILayout.Button("Hide Console",  GUILayout.MinWidth(Screen.width / 10), GUILayout.MinHeight(Screen.height / 20)))
                showGUI = false;

            if (GUILayout.Button("Close Console", GUILayout.MinWidth(Screen.width / 10), GUILayout.MinHeight(Screen.height / 20)))
                Close();
            GUILayout.EndHorizontal();


            #region Draw Hierarchy

            DrawHierarchyButton();

            DrawHierarchy();

            #endregion
        }
        else
        {
            //GUILayout.Space(100);

#if UNITY_EDITOR
            GUILayout.BeginHorizontal();
            GUILayout.Space(100);
#endif
            if (GUILayout.Button("Show Console",  GUILayout.MinWidth(Screen.width / 10), GUILayout.MinHeight(Screen.height / 20)))
                showGUI = true;
#if UNITY_EDITOR
            GUILayout.EndHorizontal();
#endif
        }
    }

    void DrawOpBtn(ref Vector2 scrollValue)
    {
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();

        if (GUILayout.RepeatButton("Up", GUILayout.MinWidth(OpBtnWidth), GUILayout.MinHeight(OpBtnHeight / 1.5f)))
        {
            //Debug.Log(detailScrollPos);
            scrollValue += Screen.width * OpBtnScrollVerSpeed * Vector2.down;
        }

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        if (GUILayout.RepeatButton("Left  ", GUILayout.MinWidth(OpBtnWidth / 2), GUILayout.MinHeight(OpBtnHeight)))
        {
            scrollValue += Screen.width * OpBtnScrollHorSpeed * Vector2.left;
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.RepeatButton("Right", GUILayout.MinWidth(OpBtnWidth / 2), GUILayout.MinHeight(OpBtnHeight)))
        {
            scrollValue += Screen.width * OpBtnScrollHorSpeed * Vector2.right;
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (GUILayout.RepeatButton("Down", GUILayout.MinWidth(OpBtnWidth), GUILayout.MinHeight(OpBtnHeight / 1.5f)))
        {
            scrollValue += Screen.width * OpBtnScrollVerSpeed * Vector2.up;
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
    }

    public int FPS { get; private set; }
    public float TexMemory { get; private set; }
    public float MaxTexMemory { get; private set; }
    int fpsCounter;

    float lastFpsCountTime = 0;
    private void Update()
    {
        fpsCounter++;

        if (Time.realtimeSinceStartup - lastFpsCountTime >= 1f)
        {
            lastFpsCountTime = Time.realtimeSinceStartup;
            FPS = fpsCounter;
            fpsCounter = 0;

        }

        TexMemory = ((float)Texture.currentTextureMemory) / (1024 * 1024);
        MaxTexMemory = TexMemory > MaxTexMemory ? TexMemory : MaxTexMemory;

    }

    Dictionary<string, bool> drawHierachies = new Dictionary<string, bool>();
    public static void SetDrawHierachy(string root)
    {
        if (!Instance.drawHierachies.ContainsKey(root))
            Instance.drawHierachies.Add(root, false);
    }

    string curDrawHierarchy = "";
    void DrawHierarchyButton()
    {
        GUILayout.BeginHorizontal();
        var keys = new List<string>();
        keys.AddRange(drawHierachies.Keys);
        foreach (var key in keys)
        {
            //var bs = GUILayout.Button(key, button_font);
            //if (bs != drawHierachies[key])
            //    drawHierachies[key] = bs;
            if(GUILayout.Button(key))
            {
                curDrawHierarchy = key;
            }
        }
        GUILayout.EndHorizontal();
    }

    void DrawHierarchy()
    {
        //foreach (var draw in drawHierachies)
        //{
        //    if(draw.Value)
        //    {
        //        DrawHierarchy(draw.Key);
        //    }
        //}
        DrawHierarchy(curDrawHierarchy);
    }

    Vector3 hierarchyPos;
    void DrawHierarchy(string root)
    {
        var rootObj = GameObject.Find(root);
        if (rootObj == null) 
            return;
        hierarchyPos = GUILayout.BeginScrollView(hierarchyPos,"box");
        GUILayout.BeginVertical();
        SetGUIColor(Color.white);
        GUILayout.Label(rootObj.name, font);
        for (int i = 0; i < rootObj.transform.childCount; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            var trans = rootObj.transform.GetChild(i);
            if (trans.gameObject.activeSelf)
                SetGUIColor(Color.white);
            else
                SetGUIColor(grey_color);
            GUILayout.Label(trans.name, font);

            GUILayout.EndHorizontal();
        }

        ResetGUIColor();
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }
}
