﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyAsset
{

    /// <summary>
    /// 资源分析器
    /// </summary>
    public class AssetAnalysis : EditorWindow
    {
        public static AssetAnalysis instance { get; private set; }
        public static void OpenWindow()
        {
            //Definer.Inided();
            GetInstance();
            instance.Show();
            instance.Focus();
        }

        public static AssetAnalysis GetInstance()
        {
            if (instance == null)
            {
                instance = CreateWindow<AssetAnalysis>();
                instance.Init();
            }

            return instance;
        }

        public void Init()
        {
            minSize = new Vector2(850, 800);
            title = "资源分析器";

            forwardAnalysis = new ForwardAnalysis();
            backAnalysis = new BackAnalysis();
        }

        string analysisPath;

        //正向分析器
        ForwardAnalysis forwardAnalysis;
        //反向分析器
        BackAnalysis backAnalysis;

        public enum AnalysisType
        {
            正向分析,
            反向分析,
        }

        AnalysisType analysisType = AnalysisType.正向分析;

        bool showIgnore = false;
        string analysisIgnore = ".meta;.DS_Store";

        private void OnGUI()
        {
            GUILayout.Space(5);

            //GUILayout.BeginVertical();

            EditorGUILayout.TextField("资源分析路径: ", analysisPath);

            GUILayout.BeginHorizontal();

            analysisType = (AnalysisType)EditorGUILayout.EnumPopup("分析类型: ", analysisType);

            BaseAnalysis curAnalysis = backAnalysis;
            if (analysisType == AnalysisType.正向分析)
                curAnalysis = forwardAnalysis;

            GUILayout.Space(20);

            if (GUILayout.Button("选择资源分析路径"))
            {
                analysisPath = EditorUtility.OpenFolderPanel("分析路径", Application.dataPath, "");
                curAnalysis.Clear();
                return;
            }
            GUILayout.EndHorizontal();

            showIgnore = EditorGUILayout.Foldout(showIgnore, "忽略文件", true);
            if (showIgnore)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                analysisIgnore = GUILayout.TextField(analysisIgnore);
                GUILayout.Space(20);
                GUILayout.EndHorizontal();
            }

            if (!string.IsNullOrEmpty(analysisPath))
            {
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                curAnalysis.filter = EditorGUILayout.TextField("过滤器:", curAnalysis.filter);
                GUILayout.Space(30);
                if (GUILayout.Button("分析"))
                {
                    curAnalysis.Clear();
                    curAnalysis.analysisPath = analysisPath;
                    curAnalysis.curAssetPath = "";
                    string[] ignores = analysisIgnore.Split(';');
                    curAnalysis.Analysis(Directory.GetFiles(analysisPath, "*.*", SearchOption.AllDirectories), ignores);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.EndHorizontal();

                if (curAnalysis.inAnalysing)
                {
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(15);
                    Rect rect = GUILayoutUtility.GetRect(position.width - 100, 15);
                    EditorGUI.ProgressBar(rect, curAnalysis.analysingProgress, "正在分析中: ");
                    GUILayout.Space(15);
                    GUILayout.EndHorizontal();
                    Repaint();
                }

                curAnalysis.onGUI();
            }
        }
    }
}