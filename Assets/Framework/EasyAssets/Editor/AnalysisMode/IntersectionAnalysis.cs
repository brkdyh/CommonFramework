using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyAsset
{
    /// <summary>
    /// 交集分析器
    /// </summary>
    public class IntersectionAnalysis : EditorWindow
    {
        public static IntersectionAnalysis instance { get; private set; }

        Dictionary<string, AnalysisData> analysisDatas = new Dictionary<string, AnalysisData>();

        public static bool enabled { get; private set; }

        [MenuItem("公共框架/Easy Assets/交集分析器", priority = 104)]
        public static void OpenWindow()
        {
            AssetAnalysis.OpenWindow();
            GetInstance();
            instance.Show();
            instance.Focus();
        }

        private void OnEnable()
        {
            enabled = true;
        }

        private void OnDisable()
        {
            enabled = false;
        }

        public static IntersectionAnalysis GetInstance()
        {
            if (instance == null)
            {
                instance = GetWindow<IntersectionAnalysis>();
                instance.Init();
            }

            return instance;
        }

        public void Init()
        {
            minSize = new Vector2(850, 800);
            title = "交集分析器";

        }

        Vector2 assets_scroll;

        Vector2 cross_scroll;

        int asset_tool_index = 0;
        int cross_tool_index = 0;

        string asset_asset_bundle_name;
        string asset_asset_variant;

        string cross_asset_bundle_name;
        string cross_asset_variant;

        int asset_count;
        int cross_count;

        bool getSkip(int index,string assetBundleName)
        {
            bool skip = false;
            if (index == 0)
                skip = false;
            else if (index == 1)
                skip = string.IsNullOrEmpty(assetBundleName);
            else
                skip = !string.IsNullOrEmpty(assetBundleName);
            return skip;
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("已添加的资源 共({0})项:", asset_count));
            GUILayout.FlexibleSpace();
            asset_tool_index = GUILayout.Toolbar(asset_tool_index, new string[] { "全部", "有AB标记", "无AB标记" });
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            //批量应用
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            GUILayout.Label("AssetBundle: ");
            asset_asset_bundle_name = GUILayout.TextField(asset_asset_bundle_name, GUILayout.Width(150));
            GUILayout.Label("Variant: ");
            asset_asset_variant = GUILayout.TextField(asset_asset_variant, GUILayout.Width(150));
            GUILayout.Space(20);
            if (GUILayout.Button("批量应用"))
            {
                if (EditorUtility.DisplayDialog("确认修改", "是否确认批量修改？", "确认"))
                {
                    foreach (var ana in analysisDatas)
                    {
                        var asset = ana.Value.assetData;
                        if (!getSkip(asset_tool_index, asset.assetBundleName))
                        {
                            asset.assetBundleName = asset_asset_bundle_name;
                            asset.assetBundleVariant = asset_asset_variant;
                            asset.ApplyAssetBundleName();
                        }
                    }
                }
            }
            GUILayout.Space(10);
            GUILayout.EndHorizontal();

            assets_scroll = GUILayout.BeginScrollView(assets_scroll, "box", GUILayout.MinWidth(position.width - 20), GUILayout.MaxHeight(250));
            asset_count = 0;
            foreach (var ana in analysisDatas)
            {
                var asset = ana.Value.assetData;
                if (!getSkip(asset_tool_index, asset.assetBundleName))
                {
                    DrawAsset(asset, GUI.contentColor, true);
                    asset_count++;
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("依赖交集 共({0})项:", cross_count));
            GUILayout.FlexibleSpace();
            cross_tool_index = GUILayout.Toolbar(cross_tool_index, new string[] { "全部", "有AB标记", "无AB标记" });
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            //批量应用
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            GUILayout.Label("AssetBundle: ");
            cross_asset_bundle_name = GUILayout.TextField(cross_asset_bundle_name, GUILayout.Width(150));
            GUILayout.Label("Variant: ");
            cross_asset_variant = GUILayout.TextField(cross_asset_variant, GUILayout.Width(150));
            GUILayout.Space(20);
            if (GUILayout.Button("批量应用"))
            {
                if (EditorUtility.DisplayDialog("确认修改", "是否确认批量修改？", "确认"))
                {
                    foreach (var asset in intersectionList)
                    {
                        var ad = AssetAnalysis.instance.curAnalysis.FindAnalysisData(asset);

                        if (!getSkip(cross_tool_index, ad.assetData.assetBundleName))
                        {
                            ad.assetData.assetBundleName = cross_asset_bundle_name;
                            ad.assetData.assetBundleVariant = cross_asset_variant;
                            ad.assetData.ApplyAssetBundleName();
                        }
                    }
                }
            }
            GUILayout.Space(10);
            GUILayout.EndHorizontal();

            cross_scroll = GUILayout.BeginScrollView(cross_scroll, "box", GUILayout.MinWidth(position.width - 20));
            cross_count = 0;
            foreach (var asset in intersectionList)
            {
                var ad = AssetAnalysis.instance.curAnalysis.FindAnalysisData(asset);

                if (!getSkip(cross_tool_index, ad.assetData.assetBundleName))
                {
                    DrawAsset(ad.assetData, GUI.contentColor, true);
                    cross_count++;
                }
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }

        public void AddAnalysisData(AssetData assetData)
        {
            var assetPath = assetData.assetPath;
            if (!analysisDatas.ContainsKey(assetPath))
            {
                var analysis = AssetAnalysis.instance.curAnalysis.FindAnalysisData(assetPath);
                analysisDatas.Add(assetPath, analysis);
                Cross();
                Repaint();
            }
        }

        public void RemoveAnalysisData(AssetData assetData)
        {
            var assetPath = assetData.assetPath;
            if (analysisDatas.ContainsKey(assetPath))
            {
                analysisDatas.Remove(assetPath);
                Cross();
                Repaint();
            }
        }

        List<string> intersectionList = new List<string>();
        List<AnalysisData> cross_AnalysisDatas = new List<AnalysisData>();
        int cur_cross_index = 0;
        AnalysisData GetCurData()
        {
            if (cur_cross_index < cross_AnalysisDatas.Count)
            {
                var cur = cross_AnalysisDatas[cur_cross_index];
                cur_cross_index++;
                return cur;
            }

            return null;
        }

        //计算交集
        void Cross()
        {
            cur_cross_index = 0;
            intersectionList.Clear();
            cross_AnalysisDatas.Clear();
            cross_AnalysisDatas.AddRange(analysisDatas.Values);
            var start_data = GetCurData();
            if (start_data == null)
                return;
            intersectionList.AddRange(start_data.GetDependencies().Keys);
            get_intersection(ref intersectionList);
        }

        void get_intersection(ref List<string> list)
        {
            var data = GetCurData();
            if (data == null)
                return;

            var result = new List<string>();

            foreach (var dp in data.GetDependencies())
            {
                if (list.Contains(dp.Key))
                {
                    result.Add(dp.Key);
                }
            }
            list = result;
            get_intersection(ref list);
        }

        public void DrawAsset(AssetData assetData, Color assetColor,bool select)
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
    }
}