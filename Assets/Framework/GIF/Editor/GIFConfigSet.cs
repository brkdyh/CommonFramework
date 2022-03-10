using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CM_GIF;
using UnityEditor;
using UnityEngine;

public class GIFConfigSet : EditorWindow
{
    static GIFConfigSet instance;
    [MenuItem("公共框架/GIF/GIF 属性设置", priority = 301)]
    public static void OpenWindow()
    {
        GetInstance();
        instance.Show();
        instance.Focus();
    }

    public static GIFConfigSet GetInstance()
    {
        if (instance == null)
        {
            instance = GetWindow<GIFConfigSet>();
            instance.Init();
        }

        return instance;
    }

    public void Init()
    {
        minSize = new Vector2(300, 250);
        title = "GIF 属性设置";
    }

    AssetType assetType;
    List<Texture2D> picList;
    public void onOpenWindow(string path)
    {
        var pp = path.Replace("_config.bytes", "");
        assetType = GIFResHelper.CertainAssetType(GIFEditorHelper.GetResourcesPath(pp));

        if (assetType == AssetType.SingleSprite)
        {
            picList = new List<Texture2D>();
            int idx = 1;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(pp + "/f-" + idx + ".png");
            //Debug.Log(pp + "f-" + idx + ".png");
            while (tex != null)
            {
                picList.Add(tex);
                idx++;
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(pp + "/f-" + idx + ".png");
            }

            if (picList.Count == 0)
                assetType = AssetType.NoAssets;
            //Debug.Log(picList.Count);
        }
    }

    static GifConfigData gif_config;
    static string curf_rel_path;

    public static void OpenSetWindow(TextAsset config,string r_path)
    {
        try
        {
            curf_rel_path = r_path;
            gif_config = GIFResHelper.LoadGifConfig(config);
            OpenWindow();
            instance.onOpenWindow(curf_rel_path);
        }
        catch (Exception ex)
        {
            Debug.LogError("Gif配置文件类型错误:\n" + ex.Message + "\n" + ex.StackTrace);
        }
    }

    Vector2 frame_scroll = Vector2.zero;
    MethodType methodType = MethodType.ByFrame;

    private void OnGUI()
    {
        if (gif_config == null)
            return;

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("编辑GIF配置文件");

        EditorGUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Space(5);
        EditorGUILayout.BeginVertical("OL box");
        EditorGUILayout.Space(2);
        var f_str = "文件路径:  " + curf_rel_path;
        EditorGUILayout.Space(2);

        //gif_config.assetType = (AssetType)EditorGUILayout.EnumPopup("资源类型", assetType);
        if (assetType == AssetType.NoAssets)
            EditorGUILayout.HelpBox(f_str + "\n找不到GIF资源", MessageType.Error);
        else if (assetType == AssetType.SingleSprite)
            EditorGUILayout.HelpBox(f_str + "\n资源类型:  图片", MessageType.Info);
        else
            EditorGUILayout.HelpBox(f_str + "\n资源类型:  图集", MessageType.Info);

        EditorGUILayout.Space(5);
        gif_config.rawSize = EditorGUILayout.Vector2Field("原始尺寸", gif_config.rawSize);

        EditorGUILayout.Space(10);

        methodType = (MethodType)EditorGUILayout.EnumPopup("帧信息类型", methodType);
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("帧信息:  ");
        frame_scroll = EditorGUILayout.BeginScrollView(frame_scroll);
        if (methodType == MethodType.ByFrame)
        {
            for (int i = 0; i < gif_config.frameRepeat.Count; i++)
            {
                var fd = gif_config.frameDatas[i];
                GUILayout.BeginHorizontal();
                GUILayout.Space(5);
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(string.Format("第{0}张图", i + 1));
                gif_config.frameRepeat[i] = EditorGUILayout.IntField(string.Format("占用帧数"), gif_config.frameRepeat[i]);
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                if (assetType == AssetType.SingleSprite
                    && i < picList.Count)
                    GUILayout.Box(picList[i], GUILayout.Width(40), GUILayout.Height(40));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
                GUILayout.EndHorizontal();

                if (i < gif_config.frameDatas.Count - 1)
                    GUILayout.Space(5);
            }
        }
        else if (methodType == MethodType.ByDelay)
        {
            for (int i = 0; i < gif_config.delayDatas.Count; i++)
            {
                var fd = gif_config.delayDatas[i];
                GUILayout.BeginHorizontal();
                GUILayout.Space(5);
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(string.Format("第{0}张图", i + 1));
                fd.dalay = EditorGUILayout.FloatField(string.Format("延迟时间"), fd.dalay);
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                if (assetType == AssetType.SingleSprite
                    && i < picList.Count)
                    GUILayout.Box(picList[i], GUILayout.Width(40), GUILayout.Height(40));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
                GUILayout.EndHorizontal();

                if (i < gif_config.frameDatas.Count - 1)
                    GUILayout.Space(5);
            }
        }
        EditorGUILayout.EndScrollView();


        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
        GUILayout.EndHorizontal();

        EditorGUILayout.Space(15);

        if (GUILayout.Button("确认修改"))
        {
            var str = gif_config.ToString();

            //Debug.Log(str);
            using (StreamWriter sw = File.CreateText(GIFEditorHelper.getAbsPath(curf_rel_path)))
            {
                sw.Write(str);
                sw.Flush();
            }

            AssetDatabase.Refresh();
            Close();
        }
        EditorGUILayout.Space(15);
    }

    [MenuItem("公共框架/GIF/GIF 属性设置 %E", priority = 301)]
    public static void OpenSetWindow()
    {
        var gids = Selection.assetGUIDs;
        foreach (var id in gids)
        {
            var path = AssetDatabase.GUIDToAssetPath(id);

            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (ta != null)
            {
                //Debug.Log(ta.text);
                OpenSetWindow(ta, path);
                break;
            }
        }
    }
}
