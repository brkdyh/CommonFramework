using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using EasyAssets.EditorCoroutines;

namespace EasyAssets
{

    public class BaseAnalysis
    {
        public string analysisPath;

        public string curAssetPath;

        public string filter;

        #region Page Control

        public int perPageCount = 50;
        public Dictionary<string, int> pageIndex = new Dictionary<string, int>();
        public Dictionary<string, int> itemCount = new Dictionary<string, int>();
        public void SetItemCount(string id, int page)
        {
            if (!itemCount.ContainsKey(id))
                itemCount.Add(id, 0);
            itemCount[id] = page;
        }
        public int MaxPage(string id)
        {
            var maxCount = GetItemCount(id);
            var max = maxCount / perPageCount;
            return max;
        }

        public int GetPageIndex(string id)
        {
            if (pageIndex.ContainsKey(id))
                return pageIndex[id];

            return -1;
        }
        public void SetPageIndex(string id, int page)
        {
            if (page < 0)
                return;
            var maxCount = itemCount.ContainsKey(id) ? itemCount[id] : 0;
            var max = maxCount / perPageCount;
            if (page > max)
                return;

            if (!pageIndex.ContainsKey(id))
                pageIndex.Add(id, 0);
            pageIndex[id] = page;
        }

        public int pageIndexMin(string id)
        {
            var idx = GetPageIndex(id);
            return idx * perPageCount;
        }

        public int pageMaxIndex(string id)
        {
            var idx = GetPageIndex(id);
            return idx * perPageCount + perPageCount - 1;
        }

        public int GetItemCount(string id)
        {
            if (itemCount.ContainsKey(id))
                return itemCount[id];

            return -1;
        }

        #endregion

        public virtual void onGUI()
        {
            if (maps.Count <= 0)
                return;

            List<string> keys = new List<string>();
            keys.AddRange(pageIndex.Keys);
            foreach (var pageKey in keys)
            //for (int i = 0; i < pageIndex.Keys.Count; i++)
            {
                var page = pageIndex[pageKey];
                GUILayout.BeginVertical();
                bool controlBtn = onDrawPage(pageKey, page);
                GUILayout.EndVertical();

                if (controlBtn)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("前一页"))
                    {
                        SetPageIndex(pageKey, page - 1);
                    }
                    var pageCount = MaxPage(pageKey);
                    GUILayout.Label((page + 1).ToString() + "/" + (pageCount > -1 ? (pageCount + 1).ToString() : "N"));
                    if (GUILayout.Button("后一页"))
                    {
                        SetPageIndex(pageKey, page + 1);
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }
        }

        public virtual bool onDrawPage(string id, int index)
        {
            return true;
        }

        protected Dictionary<string, AnalysisData> maps = new Dictionary<string, AnalysisData>();
        public AnalysisData FindAnalysisData(string path)
        {
            if (!maps.ContainsKey(path))
            {
                AssetData asset = new AssetData(path);
                maps.Add(path, new AnalysisData(asset));
            }

            return maps[path];
        }

        public void DrawAsset(AssetData assetData, Color assetColor, bool detail, string detailName, bool select, bool cross)
        {
            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);

            GUILayout.Box(assetData.cachedIcon, GUILayout.Width(18), GUILayout.Height(18));
            var rawLabelColor = GUI.contentColor;
            GUI.contentColor = assetColor;
            GUILayout.Label(assetData.assetPath);
            GUI.contentColor = rawLabelColor;

            GUILayout.FlexibleSpace();

            if (detail && GUILayout.Button(detailName))
            {
                curAssetPath = assetData.assetPath;
            }

            if (cross && GUILayout.Button("计算交集"))
            {
                if (!IntersectionAnalysis.enabled)
                    return;
                IntersectionAnalysis.instance.AddAnalysisData(assetData);
                return;
            }

            if (select && GUILayout.Button("选中"))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(assetData.assetPath);
                EditorUtility.FocusProjectWindow();
            }
            GUILayout.Space(15);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);

            assetData.assetBundleName = EditorGUILayout.TextField("AssetBundle: ", assetData.assetBundleName, GUILayout.MinWidth(350));
            GUILayout.FlexibleSpace();
            assetData.assetBundleVariant = EditorGUILayout.TextField("Variant: ", assetData.assetBundleVariant, GUILayout.MinWidth(350));
            GUILayout.FlexibleSpace();

            var rawcolor = GUI.backgroundColor;
            GUI.backgroundColor = assetData.dirty ? Color.yellow : rawcolor;
            if (GUILayout.Button("应用"))
            {
                assetData.ApplyAssetBundleName();
            }
            GUI.backgroundColor = rawcolor;
            GUILayout.Space(15);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        public bool inAnalysing { get; protected set; } = false;
        public float analysingProgress { get; protected set; } = 0f;

        protected string[] analysis_ignores;
        public virtual void Analysis(string[] paths, string[] ignores)
        {
            if (inAnalysing)
                return;

            inAnalysing = true;

            analysis_ignores = ignores;
            AssetAnalysis.instance.StartCoroutine(StartAnalysis(paths));
        }

        protected bool IgnorePath(string path)
        {
            foreach (var ignore in analysis_ignores)
            {
                if (path.Contains(ignore))
                    return true;
            }

            return false;
        }

        int frameBatchCount = 20;

        protected virtual IEnumerator StartAnalysis(string[] paths)
        {
            int totalCount = paths.Length;

            int batchCounter = 1;
            int ilterCount = 0;
            foreach (var p in paths)
            {
                ilterCount++;
                //Debug.Log("cur ilt = " + ilterCount);
                if (ilterCount >= batchCounter * frameBatchCount)
                {
                    batchCounter++;
                    analysingProgress = (float)ilterCount / totalCount;
                    //EditorUtility.DisplayProgressBar("分析中", "正在分析资源依赖关系", analysingProgress);
                    yield return new WaitForEndOfFrame();
                }

                AnalysisPath(p);
            }

            inAnalysing = false;
            //EditorUtility.ClearProgressBar();
        }

        protected virtual void AnalysisPath(string p)
        {

        }


        public virtual void Clear()
        {
            maps.Clear();
        }

        public bool showCountZero = false;
    }

}