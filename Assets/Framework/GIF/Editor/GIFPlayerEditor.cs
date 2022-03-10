using System.Collections;
using System.Collections.Generic;
using CM_GIF;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(GIFPlayer))]
public class GIFPlayerEditor : Editor
{
    int jumbIdx = 0;
    public override void OnInspectorGUI()
    {
        GIFPlayer ngp = target as GIFPlayer;
        EditorGUI.BeginChangeCheck();

        GUILayout.Space(5);

        ngp.GIFPath = EditorGUILayout.TextField("GIF路径:", ngp.GIFPath);

        //GUILayout.Space(10);
        //ngp.assetType = (AssetType)EditorGUILayout.EnumPopup("资源形式:", ngp.assetType);

        if (EditorApplication.isPlaying)
        {
            if (ngp.assetType == AssetType.NoAssets)
                EditorGUILayout.HelpBox("找不到动画资源", MessageType.Error);
            else
                EditorGUILayout.HelpBox("动画资源类型: " + ngp.assetType, MessageType.Info);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical("OL box");
        GUILayout.Space(10);
        ngp.renderType = (GIFPlayer.RenderType)EditorGUILayout.EnumPopup("渲染方式:", ngp.renderType);

        if (ngp.renderType != GIFPlayer.RenderType.SpriteRenderer
            && ngp.renderType != GIFPlayer.RenderType.NoRenderer)
        {
            GUILayout.Space(5);
            ngp.openForceSize = EditorGUILayout.Toggle("是否锁定尺寸:", ngp.openForceSize);

            GUILayout.Space(5);
            if (!ngp.openForceSize)
                ngp.keepNativeRatio = EditorGUILayout.Toggle("是否保持原图比例:", ngp.keepNativeRatio);
            else
                ngp.forcesize = EditorGUILayout.Vector2Field("锁定尺寸:", ngp.forcesize);

            GUILayout.Space(5);
            ngp.applyOffset = EditorGUILayout.Toggle("是否使用偏移值:", ngp.applyOffset);
        }
        EditorGUILayout.Space(10);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("OL box");
        GUILayout.Space(10);
        ngp.methodType = (MethodType)EditorGUILayout.EnumPopup("播放方式:", ngp.methodType);
        

        if (ngp.methodType == MethodType.ByFrame)
        {
            GUILayout.Space(5);
            ngp.TargetFPS = EditorGUILayout.IntField("动画帧率:", ngp.TargetFPS);
        }
        else if (ngp.methodType == MethodType.ByDelay)
        {
            GUILayout.Space(5);
            ngp.playSpeed = EditorGUILayout.FloatField("动画速率:", ngp.playSpeed);
        }

        GUILayout.Space(5);
        ngp.loop = EditorGUILayout.Toggle("是否循环:", ngp.loop);

        GUILayout.Space(5);
        ngp.autoPlay = EditorGUILayout.Toggle("自动播放:", ngp.autoPlay);


        if (EditorApplication.isPlaying)
        {
            GUILayout.Space(10);
            if (!ngp.isPause)
            {
                if (GUILayout.Button("暂停播放"))
                {
                    ngp.Pause();
                }
            }
            else
            {
                if (GUILayout.Button("恢复播放"))
                {
                    ngp.Play(false);
                }
                if (GUILayout.Button("重新播放"))
                {
                    ngp.Play();
                }

                if (ngp.methodType == MethodType.ByFrame)
                {
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal("box");
                    GUILayout.Space(5);
                    jumbIdx = EditorGUILayout.IntField(string.Format("跳转到第 {0} 帧", jumbIdx), jumbIdx, GUILayout.MaxWidth(250));
                    jumbIdx = Mathf.Clamp(jumbIdx, 0, ngp.totalFrame - 1);
                    EditorGUILayout.LabelField(string.Format("/总计 {0} 帧", ngp.totalFrame), GUILayout.MaxWidth(100));
                    EditorGUILayout.LabelField(string.Format("Sprite: {0}", ngp.getSpriteName(ngp.currentIndex)));
                    GUILayout.Space(5);
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("跳转"))
                    {
                        ngp.JumpTo(jumbIdx);
                    }
                }
            }
        }
        EditorGUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
    }
}
