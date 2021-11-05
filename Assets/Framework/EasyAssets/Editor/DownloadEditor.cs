using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.ComponentModel;

namespace EasyAssets
{
    [CustomEditor(typeof(BundleDownloadManager))]
    public class DownloadEditor : Editor
    {
        bool finishFold;

        public override void OnInspectorGUI()
        {
            var bdm = target as BundleDownloadManager;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");

            GUILayout.Space(5);
            EditorGUILayout.LabelField("当前状态: ", bdm.currentStatus.ToString());


            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            finishFold = EditorGUILayout.Foldout(finishFold, new GUIContent("已下载："), true);
            GUILayout.EndHorizontal();
            int idx = 0;
            if (finishFold)
            {
                foreach (var req in bdm.finishRequests)
                {
                    idx++;
                    DrawReq(idx,req);
                }
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();

        }

        void DrawReq(int idx, BundleDownloadRequest req)
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical("OL box");

            GUILayout.Space(2);
            EditorGUILayout.LabelField(idx < 0 ? "正在下载的 Bundle 信息" : "已下载" + idx, EA_GUIStyle.mid_label_min);
            GUILayout.Space(5);
            EditorGUILayout.TextField("Bundle名称:", req.bundleName);
            EditorGUILayout.TextField("Bundle MD5:", req.bundleMD5);
            EditorGUILayout.TextField("Bundle 大小:", Utils.FormatBytesUnit(req.bundleSize));
            EditorGUILayout.TextField("Bundle Url:", req.url);

            if (idx < 0)
            {//当前下载
                GUILayout.Space(10);
                EditorGUILayout.TextField("开始于: ", req.beginDownloadTime.ToString("yyyy-mm-dd HH:mm:ss"));
                EditorGUILayout.TextField("下载状态:", req.isError ? "发生错误" : (req.isDone ? "已完成" : "下载中"));
                if (req.isError)
                    EditorGUILayout.HelpBox(req.error, MessageType.Error);

                EditorGUILayout.TextField("已下载大小:", Utils.FormatBytesUnit(req.downloadSize));
            }

            GUILayout.Space(2);

            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }
    }
}
