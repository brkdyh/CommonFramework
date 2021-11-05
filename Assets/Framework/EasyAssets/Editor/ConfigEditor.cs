using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyAssets
{
    [CustomEditor(typeof(EasyAssetConfig))]
    public class ConfigEditor : Editor
    {
        bool f1;
        bool f2;
        bool f3;
        bool f4;

        SerializedProperty unmanagedProperty;

        private void OnEnable()
        {
            unmanagedProperty = serializedObject.FindProperty("UnmanagedBundles");
        }

        public override void OnInspectorGUI()
        {
            EasyAssetConfig config = target as EasyAssetConfig;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            GUILayout.Space(5);
            GUILayout.BeginVertical("box");
            GUILayout.Label("资源打包设置:", EA_GUIStyle.mid_label);
            config.OpenCompress = EditorGUILayout.Toggle("是否开启Bundle压缩:", config.OpenCompress);
            if (config.OpenCompress)
            {
                GUILayout.Space(5);
                config.CompressPassword = EditorGUILayout.TextField("Bundle压缩密码:", config.CompressPassword);
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            GUILayout.Label("资源管理器设置:", EA_GUIStyle.mid_label);
            GUILayout.Space(5);
            config.LoadPath = EditorGUILayout.TextField("外部资源加载路径:", config.LoadPath);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            f1 = EditorGUILayout.Foldout(f1, "AssetBundle卸载时间:");
            GUILayout.Space(20);
            config.AssetBundleLiveTime = EditorGUILayout.FloatField("", config.AssetBundleLiveTime);
            GUILayout.EndHorizontal();
            if (f1)
                EditorGUILayout.HelpBox(string.Format("这是已加载的 \"AssetBundle\" 对象的存活时间，" +
                    "已经加载并且使用过的 AssetBundle 对象会在 {0}s 后自动卸载。", config.AssetBundleLiveTime), MessageType.Info);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            f2 = EditorGUILayout.Foldout(f2, "Bundle引用检测频率:");
            GUILayout.Space(20);
            config.RefrenceCheckTime = EditorGUILayout.FloatField("", config.RefrenceCheckTime);
            GUILayout.EndHorizontal();
            if (f2)
                EditorGUILayout.HelpBox(string.Format("这里是对Bundle引用数量的检测间隔时间，当前每间隔 {0}s 检测一次。", config.RefrenceCheckTime), MessageType.Info);

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            f3 = EditorGUILayout.Foldout(f3, "Bundle废弃缓存时间:");
            GUILayout.Space(20);
            config.DisposeCacheTime = EditorGUILayout.FloatField("", config.DisposeCacheTime);
            GUILayout.EndHorizontal();
            if (f3)
                EditorGUILayout.HelpBox(string.Format("当释放Bundle对象时，该对象会进入废弃缓冲池，并在 {0}s 后真正释放并销毁。", config.DisposeCacheTime), MessageType.Info);

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.PropertyField(unmanagedProperty, new GUIContent("非托管Bundle清单:"), true);
            GUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("非托管Bundle不会自动被卸载与释放，有需要时，手动卸载和释放它们。常用于高频率引用的资源包或者公共资源包。", MessageType.Info);
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("box");

            GUILayout.Label("资源下载设置:", EA_GUIStyle.mid_label);
            GUILayout.Space(5);
            config.RemoteBundleRootDomain = EditorGUILayout.TextField("服务器Bundle文件根路径:", config.RemoteBundleRootDomain);
            GUILayout.Space(5);
            config.RequestTimeOut = EditorGUILayout.FloatField("下载请求超时时间(单位 s):", config.RequestTimeOut);
            GUILayout.Space(5);
            config.bundleCheckMode = (Setting.BundleCheckMode)EditorGUILayout.EnumPopup("Bundle验证方式:", config.bundleCheckMode);
            GUILayout.Space(15);
            GUILayout.EndVertical();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
