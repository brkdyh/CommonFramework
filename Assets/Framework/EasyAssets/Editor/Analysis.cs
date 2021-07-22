using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using EasyAsset.EditorCoroutines;

namespace EasyAsset
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

        public void DrawAsset(AssetData assetData, bool detail, string detailName, bool select)
        {
            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);

            GUILayout.Box(assetData.cachedIcon, GUILayout.Width(18), GUILayout.Height(18));
            GUILayout.Label(assetData.assetPath);

            GUILayout.FlexibleSpace();

            if (detail && GUILayout.Button(detailName))
            {
                curAssetPath = assetData.assetPath;
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

            assetData.assetBundleName = EditorGUILayout.TextField("AssetBundle: ", assetData.assetBundleName);
            GUILayout.FlexibleSpace();
            assetData.assetBundleVariant = EditorGUILayout.TextField("Variant: ", assetData.assetBundleVariant);
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
    }

    public class ForwardAnalysis : BaseAnalysis
    {
        int assetCount = 0;

        int depCount = 0;

        Vector2 desViewScrollPos;

        Vector2 detailViewScrollPos;

        string batchAssetBundleName_a;
        string batchAssetVariant_a;

        string batchAssetBundleName_b;
        string batchAssetVariant_b;

        public ForwardAnalysis()
        {
            SetPageIndex("asset", 0);
            SetPageIndex("dep", 0);
        }

        public override bool onDrawPage(string id, int index)
        {
            base.onDrawPage(id, index);

            if (!string.IsNullOrEmpty(analysisPath))
            {
                #region 资源分析

                if (id == "asset")
                {
                    GUILayout.Space(10);

                    #region Area Title
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField(string.Format("资源项(合计 {0} 项)", assetCount));
                    GUILayout.Space(AssetAnalysis.instance.position.width - 750);
                    GUILayout.Label("AssetBundle: ");
                    batchAssetBundleName_a = GUILayout.TextField(batchAssetBundleName_a, GUILayout.Width(150));
                    GUILayout.Label("Variant: ");
                    batchAssetVariant_a = GUILayout.TextField(batchAssetVariant_a, GUILayout.Width(150));
                    GUILayout.Space(20);
                    if (GUILayout.Button("批量应用"))
                    {
                        if (EditorUtility.DisplayDialog("确认修改", "是否确认批量修改？", "确认"))
                        {
                            foreach (var dp in maps)
                            {
                                if (dp.Value.DependencyCount > 0)
                                {
                                    if (!string.IsNullOrEmpty(filter)
                                        && !dp.Key.Contains(filter))
                                        continue;

                                    var adp = dp.Value;
                                    adp.assetData.assetBundleName = batchAssetBundleName_a;
                                    adp.assetData.assetBundleVariant = batchAssetVariant_a;
                                    adp.assetData.ApplyAssetBundleName();
                                }
                            }
                        }
                    }
                    GUILayout.Space(10);
                    GUILayout.EndHorizontal();
                    #endregion

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    desViewScrollPos = EditorGUILayout.BeginScrollView(desViewScrollPos, "OL box", GUILayout.MaxHeight(350));
                    EditorGUILayout.BeginVertical();

                    assetCount = 0;
                    foreach (var dp in maps)
                    {
                        if (dp.Value.DependencyCount > 0)
                        {
                            if (!string.IsNullOrEmpty(filter)
                                && !dp.Key.Contains(filter))
                                continue;

                            //Debug.Log(pageIndexMin("asset") + "<>" + pageMaxIndex("asset"));

                            if (assetCount >= pageIndexMin("asset")
                                && assetCount <= pageMaxIndex("asset"))
                                DrawAsset(dp.Value.assetData, true, "依赖详情", true);

                            assetCount++;

                            SetItemCount("asset", assetCount);
                        }
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndScrollView();
                    GUILayout.Space(10);
                    GUILayout.EndHorizontal();

                    return true;
                }
                #endregion

                #region 资源依赖详情
                else if (id == "dep")
                {
                    if (!string.IsNullOrEmpty(curAssetPath))
                    {
                        GUILayout.Space(15);
                        var dpd = FindAnalysisData(curAssetPath);

                        #region Area Title
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField(string.Format("资源依赖项(合计 {0} 项): ", dpd.AppliesCount));
                        GUILayout.Space(AssetAnalysis.instance.position.width - 750);
                        GUILayout.Label("AssetBundle: ");
                        batchAssetBundleName_b = GUILayout.TextField(batchAssetBundleName_b, GUILayout.Width(150));
                        GUILayout.Label("Variant: ");
                        batchAssetVariant_b = GUILayout.TextField(batchAssetVariant_b, GUILayout.Width(150));
                        GUILayout.Space(20);
                        if (GUILayout.Button("批量应用"))
                        {
                            if (EditorUtility.DisplayDialog("确认修改", "是否确认批量修改？", "确认"))
                            {
                                foreach (var dp in dpd.GetDependencies())
                                {
                                    var adp = FindAnalysisData(dp.Key);
                                    adp.assetData.assetBundleName = batchAssetBundleName_b;
                                    adp.assetData.assetBundleVariant = batchAssetVariant_b;
                                    adp.assetData.ApplyAssetBundleName();
                                }
                            }
                        }
                        GUILayout.Space(10);
                        GUILayout.EndHorizontal();
                        #endregion

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        detailViewScrollPos = EditorGUILayout.BeginScrollView(detailViewScrollPos, "OL box", GUILayout.MaxHeight(350));

                        EditorGUILayout.BeginVertical();

                        depCount = 0;
                        foreach (var dp in dpd.GetDependencies())
                        {
                            var asset = FindAnalysisData(dp.Key).assetData;

                            if (depCount >= pageIndexMin("dep")
                                && depCount <= pageMaxIndex("dep"))
                                DrawAsset(asset, false, "", true);

                            depCount++;
                            SetItemCount("dep", depCount);
                        }

                        EditorGUILayout.EndVertical();

                        EditorGUILayout.EndScrollView();
                        GUILayout.Space(10);
                        GUILayout.EndHorizontal();

                        GUILayout.Space(20);
                        return true;
                    }
                }
                #endregion

            }
            return false;
        }

        protected override void AnalysisPath(string p)
        {
            if(IgnorePath(p))
                return;

            string path = p.Replace(Application.dataPath + "/", "Assets/");
            //Debug.Log(path);
            var dp = FindAnalysisData(path);

            string[] dps = AssetDatabase.GetDependencies(dp.assetData.assetPath);
            foreach (var adp in dps)
            {
                if (!IgnorePath(adp))
                    dp.AddDependencyCount(adp);
            }
        }
    }

    /// <summary>
    /// 反向分析器
    /// </summary>
    public class BackAnalysis : BaseAnalysis
    {
        int assetCount = 0;

        int applyCount = 0;

        Vector2 desViewScrollPos;

        Vector2 detailViewScrollPos;

        string batchAssetBundleName_a;
        string batchAssetVariant_a;
        string batchAssetBundleName_b;
        string batchAssetVariant_b;
        public BackAnalysis()
        {
            SetPageIndex("asset", 0);
            SetPageIndex("apply", 0);
        }

        public override bool onDrawPage(string id, int index)
        {
            base.onDrawPage(id, index);

            if (!string.IsNullOrEmpty(analysisPath))
            {
                #region 底层资源分析

                if (id == "asset")
                {
                    GUILayout.Space(10);

                    //EditorGUILayout.LabelField(string.Format("资源项(合计 {0} 项)", assetCount));
                    #region Area Title
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField(string.Format("资源项(合计 {0} 项)", assetCount));
                    GUILayout.Space(AssetAnalysis.instance.position.width - 750);
                    GUILayout.Label("AssetBundle: ");
                    batchAssetBundleName_a = GUILayout.TextField(batchAssetBundleName_a, GUILayout.Width(150));
                    GUILayout.Label("Variant: ");
                    batchAssetVariant_a = GUILayout.TextField(batchAssetVariant_a, GUILayout.Width(150));
                    GUILayout.Space(20);
                    if (GUILayout.Button("批量应用"))
                    {
                        if (EditorUtility.DisplayDialog("确认修改", "是否确认批量修改？", "确认"))
                        {
                            foreach (var dp in maps)
                            {
                                if (dp.Value.DependencyCount > 0)
                                {
                                    if (!string.IsNullOrEmpty(filter)
                                        && !dp.Key.Contains(filter))
                                        continue;

                                    var adp = dp.Value;
                                    adp.assetData.assetBundleName = batchAssetBundleName_a;
                                    adp.assetData.assetBundleVariant = batchAssetVariant_a;
                                    adp.assetData.ApplyAssetBundleName();
                                }
                            }
                        }
                    }
                    GUILayout.Space(10);
                    GUILayout.EndHorizontal();
                    #endregion

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    desViewScrollPos = EditorGUILayout.BeginScrollView(desViewScrollPos, "OL box", GUILayout.MaxHeight(350));
                    EditorGUILayout.BeginVertical();

                    assetCount = 0;
                    foreach (var dp in maps)
                    {
                        if (dp.Value.AppliesCount > 0)
                        {
                            if (!string.IsNullOrEmpty(filter)
                                && !dp.Key.Contains(filter))
                                continue;

                            //Debug.Log(pageIndexMin("asset") + "<>" + pageMaxIndex("asset"));

                            if (assetCount >= pageIndexMin("asset")
                                && assetCount <= pageMaxIndex("asset"))
                                DrawAsset(dp.Value.assetData, true, "引用详情", true);

                            assetCount++;

                            SetItemCount("asset", assetCount);
                        }
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndScrollView();
                    GUILayout.Space(10);
                    GUILayout.EndHorizontal();

                    return true; ;
                    #endregion
                }
                else if (id == "apply")
                {
                    #region 资源引用详情
                    if (!string.IsNullOrEmpty(curAssetPath))
                    {
                        GUILayout.Space(15);
                        var dpd = FindAnalysisData(curAssetPath);

                        #region Area Title
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField(string.Format("资源引用项(合计 {0} 项): ", dpd.AppliesCount));
                        GUILayout.Space(AssetAnalysis.instance.position.width - 750);
                        GUILayout.Label("AssetBundle: ");
                        batchAssetBundleName_b = GUILayout.TextField(batchAssetBundleName_b, GUILayout.Width(150));
                        GUILayout.Label("Variant: ");
                        batchAssetVariant_b = GUILayout.TextField(batchAssetVariant_b, GUILayout.Width(150));
                        GUILayout.Space(20);
                        if (GUILayout.Button("批量应用"))
                        {
                            if (EditorUtility.DisplayDialog("确认修改", "是否确认批量修改？", "确认"))
                            {
                                foreach (var dp in dpd.GetApplies())
                                {
                                    var adp = FindAnalysisData(dp.Key);
                                    adp.assetData.assetBundleName = batchAssetBundleName_b;
                                    adp.assetData.assetBundleVariant = batchAssetVariant_b;
                                    adp.assetData.ApplyAssetBundleName();
                                }
                            }
                        }
                        GUILayout.Space(10);
                        GUILayout.EndHorizontal();
                        #endregion

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        detailViewScrollPos = EditorGUILayout.BeginScrollView(detailViewScrollPos, "OL box", GUILayout.MaxHeight(350));

                        EditorGUILayout.BeginVertical();

                        applyCount = 0;
                        foreach (var dp in dpd.GetApplies())
                        {
                            var asset = FindAnalysisData(dp.Key).assetData;


                            if (applyCount >= pageIndexMin("apply")
                                && applyCount <= pageMaxIndex("apply"))
                                DrawAsset(asset, false, "", true);

                            applyCount++;
                            SetItemCount("apply", applyCount);
                        }

                        EditorGUILayout.EndVertical();

                        EditorGUILayout.EndScrollView();
                        GUILayout.Space(10);
                        GUILayout.EndHorizontal();

                        GUILayout.Space(20);
                        return true;
                    }
                }
                #endregion

            }
            return false;
        }

        protected override void AnalysisPath(string p)
        {
            if (IgnorePath(p))
                return;

            string path = p.Replace(Application.dataPath + "/", "Assets/");
            //Debug.Log(path);
            var dp = FindAnalysisData(path);

            string[] dps = AssetDatabase.GetDependencies(dp.assetData.assetPath);
            foreach (var adp in dps)
            {
                if (!IgnorePath(adp))
                {
                    var apply_asset = FindAnalysisData(adp);
                    apply_asset.AddApplyCount(dp.assetData.assetPath);
                }
            }
        }
    }
}