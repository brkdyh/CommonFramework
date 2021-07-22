using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EasyAsset
{
    [CustomEditor(typeof(AssetMaintainer))]
    public class AssetMaintainerEditor : Editor
    {
        Dictionary<string, bool> bundleFold = new Dictionary<string, bool>();

        EasyBundle curBundle = null;
        Vector2 contentScroll;
        bool GetBundleFold(string id)
        {
            if (!bundleFold.ContainsKey(id))
                bundleFold.Add(id, false);
            return bundleFold[id];
        }

        void SetBundleFold(string id, bool fold)
        {
            if (!bundleFold.ContainsKey(id))
                bundleFold.Add(id, false);
            bundleFold[id] = fold;
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            AssetMaintainer maintainer = target as AssetMaintainer;
            var bds = maintainer.GetLoadedBundles();
            while (bds.MoveNext())
            {
                GUILayout.BeginHorizontal();
                var eb = bds.Current as EasyBundle;
                SetBundleFold(eb.bundleName, EditorGUILayout.Foldout(GetBundleFold(eb.bundleName), eb.bundleName));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("显示包内容"))
                {
                    //eb.getBundle().GetAllAssetNames();
                    curBundle = eb;
                }
                GUILayout.EndHorizontal();
                if (GetBundleFold(eb.bundleName))
                {
                    int rf_count = 0;
                    var rfs = eb.GetReferences();
                    while (rfs.MoveNext())
                    {
                        var rf_obj = rfs.Current as Object;
                        EditorGUILayout.ObjectField("引用"+ rf_count, rf_obj, rf_obj.GetType());
                        rf_count++;
                    }
                }
            }

            if (curBundle != null)
            {
                GUILayout.Space(10);
                GUILayout.Label(curBundle.bundleName + " 包内容:");
                contentScroll = GUILayout.BeginScrollView(contentScroll, "helpbox");
                string[] all = curBundle.getBundle().GetAllAssetNames();

                foreach (var a in all)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Label(a);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
        }
    }
}
