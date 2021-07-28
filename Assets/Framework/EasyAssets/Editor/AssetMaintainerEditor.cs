using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EasyAsset
{
    [CustomEditor(typeof(AssetMaintainer))]
    public class AssetMaintainerEditor : Editor
    {
        Dictionary<string, List<bool>> bundleFold = new Dictionary<string, List<bool>>();

        EasyBundle curBundle = null;
        Vector2 contentScroll;
        bool GetBundleFold(string id, int index)
        {
            if (!bundleFold.ContainsKey(id))
            {
                List<bool> fl = new List<bool>();
                for(int i = 0; i < 10; i++)
                {
                    fl.Add(false);
                }
                bundleFold.Add(id, fl);
            }
            return bundleFold[id][index];
        }

        void SetBundleFold(string id, int index, bool fold)
        {
            if (!bundleFold.ContainsKey(id))
            {
                List<bool> fl = new List<bool>();
                for (int i = 0; i < 10; i++)
                {
                    fl.Add(false);
                }
                bundleFold.Add(id, fl);
            }
            bundleFold[id][index] = fold;
        }

        void BeginSpace(float space)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(space);
        }

        void EndSpace(float space)
        {
            GUILayout.Space(space);
            GUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            bool repaint = false;
            AssetMaintainer maintainer = target as AssetMaintainer;
            var bds = maintainer.GetLoadedBundles();
            while (bds.MoveNext())
            {
                var eb = bds.Current as EasyBundle;

                GUILayout.Space(5);

                GUILayout.BeginVertical("box");

                if (!eb.disposed)
                {
                    BeginSpace(10);
                    SetBundleFold(eb.bundleName, 0, EditorGUILayout.Foldout(GetBundleFold(eb.bundleName, 0), eb.bundleName));
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("显示详情"))
                    {
                        curBundle = eb;
                    }

                    EndSpace(10);
                    if (GetBundleFold(eb.bundleName, 0))
                    {
                        BeginSpace(15);
                        SetBundleFold(eb.bundleName, 1, EditorGUILayout.Foldout(GetBundleFold(eb.bundleName, 1), "引用"));
                        EndSpace(15);
                        if (GetBundleFold(eb.bundleName, 1))
                        {
                            int rf_count = 0;
                            var rfs = eb.GetReferences();
                            while (rfs.MoveNext())
                            {
                                var rf_obj = rfs.Current as Object;
                                if (rf_obj != null)
                                {
                                    BeginSpace(15);
                                    EditorGUILayout.ObjectField("引用" + rf_count, rf_obj, rf_obj.GetType());
                                    EndSpace(15);
                                    rf_count++;
                                }
                            }
                        }

                        BeginSpace(15);
                        SetBundleFold(eb.bundleName, 2, EditorGUILayout.Foldout(GetBundleFold(eb.bundleName, 2), "已加载的资源"));
                        EndSpace(15);
                        if (GetBundleFold(eb.bundleName, 2))
                        {
                            var unload = "";
                            int asset_count = 0;
                            var assets = eb.GetLoadedAssets();
                            while (assets.MoveNext())
                            {
                                var asset = assets.Current as Object;

                                BeginSpace(15);
                                EditorGUILayout.ObjectField("资源" + asset_count, asset, asset.GetType());
                                EndSpace(15);
                                asset_count++;
                            }
                        }
                    }

                }
                else
                {
                    BeginSpace(10);
                    GUILayout.Label(eb.bundleName);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("已废弃 " + eb.disposedTime.ToString("0") + "s");
                    EndSpace(10);
                    repaint = true;
                }

                GUILayout.EndVertical();
            }

            var dbs = maintainer.GetDisposedBundles();
            while (dbs.MoveNext())
            {
                GUILayout.Space(5);

                GUILayout.BeginVertical("box");
                var eb = dbs.Current as EasyBundle;

                BeginSpace(10);
                GUILayout.Label(eb.bundleName);
                GUILayout.FlexibleSpace();
                GUILayout.Label("已废弃 " + eb.disposedTime.ToString("0") + "s");
                EndSpace(10);
                repaint = true;

                GUILayout.EndVertical();
            }

            if (curBundle != null)
            {
                GUILayout.Space(10);
                GUILayout.BeginVertical("box");
                BeginSpace(5);
                GUILayout.Label(curBundle.bundleName + " 详情: ");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("隐藏"))
                {
                    curBundle = null;
                    EndSpace(5);
                    return;
                }
                EndSpace(5);

                BeginSpace(10);
                GUILayout.Label("Asset Bundle 内容: ");
                EndSpace(10);

                BeginSpace(10);
                contentScroll = GUILayout.BeginScrollView(contentScroll, "helpbox");
                var ab = curBundle.getBundle();
                if (ab != null)
                {
                    string[] all;
                    if (!ab.isStreamedSceneAssetBundle)
                        all = ab.GetAllAssetNames();
                    else
                        all = ab.GetAllScenePaths();

                    foreach (var a in all)
                    {
                        BeginSpace(15);
                        GUILayout.Label(a);
                        EndSpace(15);
                    }
                }
                else
                {
                    GUILayout.Label("Asset Bundle 已被卸载。");
                }
                GUILayout.EndScrollView();
                EndSpace(10);

                BeginSpace(10);
                GUILayout.Label("信息: ");
                EndSpace(10);

                BeginSpace(15);
                if (curBundle.isLoaded)
                    GUILayout.Label("已加载时间: " + curBundle.BundleLoadedTime.ToString("0") + " s");
                else
                    GUILayout.Label("已加载时间: " + "已卸载");

                repaint = true;
                EndSpace(15);

                BeginSpace(15);
                if (curBundle.isLoaded)
                    GUILayout.Label("是否使用过: " + (curBundle.used ? "已使用" : "未使用"));
                else
                    GUILayout.Label("是否使用过: "+"已卸载");

                EndSpace(15);

                BeginSpace(15);
                GUILayout.Label("当前引用数量: " + curBundle.RefrenceCount);
                EndSpace(15);

                GUILayout.Space(5);

                BeginSpace(10);
                if (curBundle != null)
                {
                    if (curBundle.isLoaded)
                    {
                        if (GUILayout.Button("卸载"))
                        {
                            curBundle.UnloadBundle();
                            return;
                        }
                        if (GUILayout.Button("释放"))
                        {
                            curBundle.SetUsed();
                            curBundle.ReleaseBundle();
                            curBundle = null;
                            return;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("加载"))
                        {
                            curBundle.LoadBundle();
                        }
                    }
                }
                EndSpace(10);

                GUILayout.EndVertical();

            }

            if (repaint)
                Repaint();
        }


    }
}
