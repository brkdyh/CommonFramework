using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyAsset
{

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
                                if (showCountZero ? dp.Value.AppliesCount >= 0 : dp.Value.AppliesCount > 0)
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
                        if (showCountZero ? dp.Value.AppliesCount >= 0 : dp.Value.AppliesCount > 0)
                        {
                            if (!string.IsNullOrEmpty(filter)
                                && !dp.Key.Contains(filter))
                                continue;

                            //Debug.Log(pageIndexMin("asset") + "<>" + pageMaxIndex("asset"));

                            if (assetCount >= pageIndexMin("asset")
                                && assetCount <= pageMaxIndex("asset"))
                                DrawAsset(dp.Value.assetData, GUI.contentColor, dp.Value.AppliesCount > 0, "引用详情", true, false);

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
                                DrawAsset(asset, GUI.contentColor, false, "", true, false);

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