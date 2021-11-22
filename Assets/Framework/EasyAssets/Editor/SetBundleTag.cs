using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyAssets
{
    public class SetBundleTag : EditorWindow
    {
        static SetBundleTag instance;
        [MenuItem("公共框架/Easy Assets/资源标签/资源标签设置", priority = 201)]
        public static void OpenWindow()
        {
            GetInstance();
            instance.Show();
            instance.Focus();
        }

        public static SetBundleTag GetInstance()
        {
            if (instance == null)
            {
                instance = GetWindow<SetBundleTag>();
                instance.Init();
            }

            return instance;
        }

        public void Init()
        {
            minSize = new Vector2(300, 250);
            title = "资源标签设置";
        }

        public class FolderData
        {
            public string assetPath;
            public string absPath;
            public string address = "";
            public string folderName = "";
            Texture icon;

            public FolderData(string assetPath)
            {
                this.assetPath = assetPath;
                absPath = getAbsPath(assetPath);
                folderName = getLatePathName(assetPath);
                icon = AssetDatabase.GetCachedIcon(assetPath);
            }

            public void onDrawFolderData()
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("路径_" + folderName, EA_GUIStyle.mid_label_min);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(icon, GUILayout.Width(18), GUILayout.Height(18));
                GUILayout.Space(5);
                GUILayout.Label(assetPath);
                EditorGUILayout.EndHorizontal();

                address = EditorGUILayout.TextField("前缀", address);

                EditorGUILayout.EndVertical();
            }
        }

        public List<FolderData> folderDatas = new List<FolderData>();
        public bool ContainFolder(string assetPath)
        {
            foreach (var f in folderDatas)
            {
                if (f.assetPath == assetPath)
                    return true;
            }

            return false;
        }

        Vector2 folder_scroll;

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("设置路径:");
            if (folderDatas.Count > 0)
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("清空"))
                {
                    folderDatas.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();
            folder_scroll = EditorGUILayout.BeginScrollView(folder_scroll, "OL Box", GUILayout.MinHeight(100), GUILayout.MaxHeight(400));
            EditorGUILayout.BeginVertical();
            foreach (var fold in folderDatas)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                fold.onDrawFolderData();
                GUILayout.Space(10);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("设置全部路径标签"))
                SetFolderTag();
            GUILayout.Space(10);
        }

        public static void SetFolderTag()
        {
            foreach (var f in instance.folderDatas)
            {
                //Debug.Log(absPath);
                if (!Directory.Exists(f.absPath))
                    continue;

                var asset_dirs = Directory.GetDirectories(f.absPath, "*", SearchOption.TopDirectoryOnly);
                foreach (var asset_dir in asset_dirs)
                {
                    AssetImporter ai = AssetImporter.GetAtPath(getAssetPath(asset_dir));
                    if (ai == null)
                    {
                        Debug.LogWarningFormat("Directory at {0} is not Asset!", asset_dir);
                        continue;
                    }
                    ai.assetBundleName = f.address + getLatePathName(asset_dir);
                }
            }
        }

        static string projectRootPath
        {
            get { return Application.dataPath.Replace("Assets", ""); }
        }

        static string getAbsPath(string path)
        {
            return projectRootPath + path;
        }

        static string getAssetPath(string absPath)
        {
            return absPath.Replace(projectRootPath, "");
        }

        static string getLatePathName(string path)
        {
            var sps = path.Split(Path.DirectorySeparatorChar);
            return sps[sps.Length - 1];
        }

        [MenuItem("公共框架/Easy Assets/资源标签/添加资源标签路径 %L", priority = 201)]
        public static void AddFolderPath()
        {
            var instance = GetInstance();
            var gids = Selection.assetGUIDs;
            foreach (var id in gids)
            {
                var path = AssetDatabase.GUIDToAssetPath(id);

                if (!Directory.Exists(getAbsPath(path)))
                    continue;

                if (!instance.ContainFolder(path))
                    instance.folderDatas.Add(new FolderData(path));
            }

            OpenWindow();
        }
    }
}