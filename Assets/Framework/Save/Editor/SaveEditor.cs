using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SaveDataConfig))]
public class SaveEditor : Editor
{
    bool fold = false;

    public override void OnInspectorGUI()
    {
        SaveDataConfig config = target as SaveDataConfig;
        EditorGUILayout.Space();

        config.isEnc = EditorGUILayout.Toggle("是否加密(仅在编辑器下): ", config.isEnc);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextField("AES加密密钥: ", config.AES_KSY);
        if (GUILayout.Button("随机生成密钥"))
        {
            if (EditorUtility.DisplayDialog("确认生成密钥", "重新生成密钥会导致原有的存档不可用，是否确认生成?", "确认", "取消"))
            {
                char[] cs = new char[16];
                for (int i = 0; i < cs.Length; i++)
                {
                    cs[i] = (char)Random.Range(97, 123);
                }

                config.GENERATED_KEY.Add(config.AES_KSY);
                config.AES_KSY = new string(cs);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        if (config.GENERATED_KEY.Count > 0)
        {
            GUILayout.BeginHorizontal();
            fold = EditorGUILayout.Foldout(fold, "历史AES密钥: ", true);
            if (fold)
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("清空历史密钥"))
                {
                    config.GENERATED_KEY.Clear();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            if (fold)
            {
                for (int i = config.GENERATED_KEY.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.TextField("版本 " + (i + 1), config.GENERATED_KEY[i]);
                }


            }
        }

        EditorGUILayout.Space();
        config.USE_UNITY_VERSION = EditorGUILayout.Toggle("使用Unity版本号: ", config.USE_UNITY_VERSION);
        if (!config.USE_UNITY_VERSION)
            config.SAVE_DATA_VERSION = EditorGUILayout.TextField("存档版本号: ", config.SAVE_DATA_VERSION);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
    }
}
