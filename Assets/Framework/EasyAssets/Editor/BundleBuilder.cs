using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using System;
using System.Text;

namespace EasyAsset
{
    /// <summary>
    /// 打包管理器
    /// </summary>
    public class BundleBuilder : EditorWindow
    {
        static BundleBuilder instance;
        [MenuItem("公共框架/Easy Assets/打包管理器", priority = 200)]
        public static void OpenWindow()
        {
            GetInstance();
            instance.Show();
            instance.Focus();
        }

        public static BundleBuilder GetInstance()
        {
            if (instance == null)
            {
                instance = GetWindow<BundleBuilder>();
                instance.Init();
            }

            return instance;
        }

        public void Init()
        { 
            var config = AssetDatabase.LoadAssetAtPath<EasyAssetConfig>(EditorDefiner.EditorPath + "/Resources/EasyAssetConfig.asset");
            minSize = new Vector2(480, 400);
            title = "打包管理器";

#if UNITY_EDITOR && UNITY_ANDROID
            curTarget = BuildTarget.Android;      
#elif UNITY_EDITOR && UNITY_IOS
            curTarget = BuildTarget.iOS;
#endif
            string format_str = buildVersion.Replace("(", "_").Replace(")", "");
            version = format_str.Split('_')[0];
            buildNumber = int.Parse(format_str.Split('_')[1]);
        }

        public BuildTarget curTarget = BuildTarget.Android;
        public string buildRootPath = "";
        public string buildPath { get { return buildRootPath + "/" + buildVersion; } }
        bool copyToPersistentPath = false;
        string buildVersion = "0.0.1(0)";

        string version;
        int buildNumber;
        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("配置", EA_GUIStyle.mid_label);
            GUILayout.BeginVertical("box");
            GUILayout.Space(5);
            curTarget = (BuildTarget)EditorGUILayout.EnumPopup("目标平台:", curTarget);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("输出路径:", GUILayout.Width(70));
            GUILayout.TextField(buildRootPath, GUILayout.MinWidth(260));
            GUILayout.Space(15);
            if (GUILayout.Button("选择输出路径", GUILayout.Width(90)))
            {
                buildRootPath = EditorUtility.OpenFolderPanel("AssetBundle输出路径", Application.dataPath, "");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            version = EditorGUILayout.TextField("构建版本号:", version);
            GUILayout.FlexibleSpace();
            buildNumber = EditorGUILayout.IntField("构建Build号:", buildNumber);
            GUILayout.Space(10);
            if (GUI.changed)
                buildVersion = version + "(" + buildNumber + ")";
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();


            if (!string.IsNullOrEmpty(buildPath)
                && curTarget != BuildTarget.NoTarget)
            {
                GUILayout.Space(10);
                GUILayout.Label("构建", EA_GUIStyle.mid_label);
                GUILayout.BeginVertical("box");
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                copyToPersistentPath = GUILayout.Toggle(copyToPersistentPath, "输出到本地");
                //GUILayout.FlexibleSpace();
                //buildBundleList = GUILayout.Toggle(buildBundleList, "生成Bundle信息");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("构建AssetBundle", GUILayout.MaxWidth(position.width - 150)))
                {
                    BuildBundle();
                    GenAssetList();
                    GenBuildInScene();
                    //if (buildBundleList)
                    GenBundleInfo();

                    if (copyToPersistentPath)
                        CopyToPersistentPath();

                    CopyToInternalPath();

                    System.Diagnostics.Process.Start(buildPath);
                    return;
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("工具", EA_GUIStyle.mid_label);
            GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("资源分析器", GUILayout.MaxWidth(position.width - 150)))
            {
                AssetAnalysis.OpenWindow();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        void BuildBundle()
        {
            if (Directory.Exists(buildPath))
                Directory.Delete(buildPath, true);
            Directory.CreateDirectory(buildPath);

            //打包AssetBundle
            BuildPipeline.BuildAssetBundles(buildPath, BuildAssetBundleOptions.UncompressedAssetBundle, curTarget);
        }

        //生成资源清单
        void GenAssetList()
        {
            //生成资源清单
            var formatPath = buildPath.Replace("\\", "/");
            var split_path = formatPath.Split('/');
            var manifestName = split_path[split_path.Length - 1];
            var manifestBundle = AssetBundle.LoadFromFile(buildPath + "/" + manifestName);

            try
            {
                using (var sw = File.CreateText(buildPath + "/"+EASY_DEFINE.ASSET_LIST_FILE))
                {
                    sw.WriteLine("/* Auto Generated By EasyAsset */");
                    sw.WriteLine("build_version:" + buildVersion);
                    AssetBundleManifest manifest = manifestBundle.LoadAsset<AssetBundleManifest>("assetbundlemanifest");
                    //Debug.Log(asset[0] + "," + asset.Length);
                    Debug.Log(manifest.name);

                    sw.WriteLine("manifest:" + manifestName);
                    string[] allBundles = manifest.GetAllAssetBundles();
                    foreach (var bundle_name in allBundles)
                    {
                        Debug.Log("load bundle: " + bundle_name);
                        var bundle = AssetBundle.LoadFromFile(buildPath + "/" + bundle_name);
                        if (!bundle.isStreamedSceneAssetBundle)
                        {//非场景AB包
                            string[] bundleAssets = bundle.GetAllAssetNames();
                            foreach (var asset in bundleAssets)
                            {
                                Debug.Log(bundle.name + " => " + asset);
                                sw.WriteLine(asset + ":" + bundle.name);
                            }
                        }
                        else
                        {//场景AB包
                            string[] bundleScenes = bundle.GetAllScenePaths();
                            foreach (var scene in bundleScenes)
                            {
                                Debug.Log(bundle.name + " => " + scene);
                                sw.WriteLine(scene + ":" + bundle.name);
                            }
                        }

                        bundle.Unload(true);
                        bundle = null;
                    }

                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message + "\n" + ex.StackTrace);
            }

            manifestBundle.Unload(true);
            manifestBundle = null;
        }

        //生成内建场景清单
        void GenBuildInScene()
        {
            using (var sw = File.CreateText(buildPath + "/" + EASY_DEFINE.BUILDIN_SCENES_FILE))
            {
                var scenes = EditorBuildSettings.scenes;
                sw.WriteLine("/* Auto Generated By EasyAsset */");
                foreach (var scene in scenes)
                    sw.WriteLine(scene.path + ":" + scene.enabled);

                sw.Flush();
                sw.Close();
            }
        }

        //生成Bundle信息
        void GenBundleInfo()
        {
            try
            {
                //生成资源包信息
                using (var sw = File.CreateText(buildPath + "/" + EASY_DEFINE.BUNDLE_INFO_FILE))
                {
                    sw.WriteLine("/* Auto Generated By EasyAsset */");
                    sw.WriteLine("build_version:" + buildVersion);

                    long total_file_size = 0;
                    int total_file_count = 0;
                    string[] files = Directory.GetFiles(buildPath, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        if (file.Contains(".manifest") ||
                            file.Contains(EASY_DEFINE.BUNDLE_INFO_FILE))
                            continue;

                        string md5 = Utils.GetMD5(file);
                        string fileName = Path.GetFileName(file);
                        long fileSize = new FileInfo(file).Length;
                        sw.WriteLine(fileName + ":" + md5 + ":" + fileSize);
                        total_file_size += fileSize;
                        total_file_count++;
                    }

                    sw.WriteLine("#total_file_size:" + total_file_size);
                    sw.WriteLine("#total_file_count:" + total_file_count);

                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message + "\n" + ex.StackTrace);
            }
        }

        void CopyToInternalPath()
        {
            var dir = Application.dataPath + "/Resources/";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string[] files = Directory.GetFiles(buildPath, "*.bytes", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var fName = Path.GetFileName(f);
                string des_path = dir + fName;
                if (File.Exists(des_path))
                    File.Delete(des_path);
                File.Copy(f, des_path);
            }

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        void CopyToPersistentPath()
        {
            var config = AssetDatabase.LoadAssetAtPath<EasyAssetConfig>(EditorDefiner.EditorPath + "/Resources/EasyAssetConfig.asset");
            PathHelper.Init(config.LoadPath);

            if (Directory.Exists(PathHelper.EXTERNAL_ASSET_PATH))
                Directory.Delete(PathHelper.EXTERNAL_ASSET_PATH, true);

            Directory.CreateDirectory(PathHelper.EXTERNAL_ASSET_PATH);

            string[] files = Directory.GetFiles(buildPath, "*.*", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var fName = Path.GetFileName(f);
                string des_path = PathHelper.EXTERNAL_ASSET_PATH + fName;
                if (File.Exists(des_path))
                    File.Delete(des_path);
                File.Copy(f, des_path);
            }
        }

        private void OnDestroy()
        {
            EA_GUIStyle.Release();
        }

        [MenuItem("公共框架/Easy Assets/打开本地资源路径", priority = 300)]
        public static void OpenAssetLocalPath()
        {
            var config = AssetDatabase.LoadAssetAtPath<EasyAssetConfig>(EditorDefiner.EditorPath + "/Resources/EasyAssetConfig.asset");
            PathHelper.Init(config.LoadPath);
            if (!Directory.Exists(PathHelper.EXTERNAL_ASSET_PATH))
                Directory.CreateDirectory(PathHelper.EXTERNAL_ASSET_PATH);

            System.Diagnostics.Process.Start(PathHelper.EXTERNAL_ASSET_PATH);
        }

        [MenuItem("公共框架/Easy Assets/清空本地资源路径", priority = 400)]
        public static void ClearAssetLocalPath()
        {
            var config = AssetDatabase.LoadAssetAtPath<EasyAssetConfig>(EditorDefiner.EditorPath + "/Resources/EasyAssetConfig.asset");
            PathHelper.Init(config.LoadPath);
            if (Directory.Exists(PathHelper.EXTERNAL_ASSET_PATH))
            {
                Directory.Delete(PathHelper.EXTERNAL_ASSET_PATH, true);
                Directory.CreateDirectory(PathHelper.EXTERNAL_ASSET_PATH);
            }
        }
    }
}