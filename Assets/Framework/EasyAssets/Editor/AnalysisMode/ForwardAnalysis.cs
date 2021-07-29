using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyAsset
{
    /// <summary>
    /// 公共依赖分析
    /// </summary>
    public class CommonDependencyAnalysis
    {
        //依赖资源-<原资源,次数>
        Dictionary<string, Dictionary<string, int>> dp_asset_count_Map = new Dictionary<string, Dictionary<string, int>>();

        //记录引用
        public void RecordDependency(string dependencyPath, string assetPath)
        {
            if (!dp_asset_count_Map.ContainsKey(dependencyPath))
                dp_asset_count_Map.Add(dependencyPath, new Dictionary<string, int>());

            var _dp = dp_asset_count_Map[dependencyPath];
            if (!_dp.ContainsKey(assetPath))
                _dp.Add(assetPath, 0);
            _dp[assetPath] += 1;
        }

        //是否是公共依赖
        public bool isCommonDependency(string dependencyPath)
        {
            if (!dp_asset_count_Map.ContainsKey(dependencyPath))
                return false;

            var _dp = dp_asset_count_Map[dependencyPath];

            return _dp.Count > 1;
        }

        public void Clear()
        {
            dp_asset_count_Map.Clear();
        }
    }

    public class ForwardAnalysis : BaseAnalysis
    {
        int assetCount = 0;                 //资源计数

        int depCount = 0;                   //依赖计数

        //Scroll Position
        Vector2 desViewScrollPos;           
        Vector2 detailViewScrollPos;

        string batchAssetBundleName_a;      //资源项AB包名称
        string batchAssetVariant_a;         //资源项AB包后缀

        string batchAssetBundleName_b;      //依赖项AB包名称
        string batchAssetVariant_b;         //依赖项AB包后缀

        int dpBatchType = 0;        //依赖批处理类型
        bool getSkip(int type, string assetPath, string assetBundleName)
        {
            bool skip = false;
            if (type == 0)
                skip = false;
            else if (type == 1)
                skip = !common.isCommonDependency(assetPath);
            else if (type == 2)
                skip = common.isCommonDependency(assetPath);
            else if (type == 3)
                skip = string.IsNullOrEmpty(assetBundleName);
            else
                skip = !string.IsNullOrEmpty(assetBundleName);
            return skip;
        }

        //公共依赖分析
        CommonDependencyAnalysis common;

        public ForwardAnalysis()
        {
            SetPageIndex("asset", 0);
            SetPageIndex("dep", 0);
            common = new CommonDependencyAnalysis();
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
                                DrawAsset(dp.Value.assetData, curAssetPath == dp.Key ? Color.yellow : GUI.contentColor,
                                    true, "依赖详情", true, IntersectionAnalysis.enabled);

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
                        GUILayout.FlexibleSpace();
                        dpBatchType = GUILayout.Toolbar(dpBatchType, new string[] { "全部", "公共", "非公共" ,"有AB标记","无AB标记"}, GUILayout.Width(300));
                        GUILayout.Space(10);
                        GUILayout.EndHorizontal();
                        GUILayout.Space(5);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(30);
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
                                    if (!getSkip(dpBatchType, dp.Key, adp.assetData.assetBundleName))
                                    {
                                        adp.assetData.assetBundleName = batchAssetBundleName_b;
                                        adp.assetData.assetBundleVariant = batchAssetVariant_b;
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
                        detailViewScrollPos = EditorGUILayout.BeginScrollView(detailViewScrollPos, "OL box", GUILayout.MaxHeight(350));

                        EditorGUILayout.BeginVertical();

                        depCount = 0;
                        foreach (var dp in dpd.GetDependencies())
                        {
                            var asset = FindAnalysisData(dp.Key).assetData;

                            if (!getSkip(dpBatchType, dp.Key, asset.assetBundleName))
                            {
                                if (depCount >= pageIndexMin("dep")
                                    && depCount <= pageMaxIndex("dep"))
                                    DrawAsset(asset, common.isCommonDependency(dp.Key) ? Color.green : GUI.contentColor,
                                        false, "", true, false);

                                depCount++;
                                SetItemCount("dep", depCount);
                            }
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

        public override void Analysis(string[] paths, string[] ignores)
        {
            common.Clear();
            base.Analysis(paths, ignores);
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
                    dp.AddDependencyCount(adp);
                    common.RecordDependency(adp, path);
                }
            }
        }
    }
}