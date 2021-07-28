using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using System;
using System.Security.Cryptography;
using System.Text;

namespace EasyAsset
{
    /// <summary>
    /// 打包管理器
    /// </summary>
    public class BundleBuilder : EditorWindow
    {
        static BundleBuilder instance;
        [MenuItem("Common Framework/Easy Assets/Open Bundle Builder", priority = 104)]
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
                instance = CreateWindow<BundleBuilder>();
                instance.Init();
            }

            return instance;
        }

        public void Init()
        { 
            var config = AssetDatabase.LoadAssetAtPath<EasyAssetConfig>(Definer.EditorPath + "/Resources/EasyAssetConfig.asset");
            minSize = new Vector2(480, 400);
            title = "打包管理器";
        }

        public BuildTarget curTarget = BuildTarget.iOS;
        public string buildPath = "";
        bool copyToPersistentPath = false;
        string buildVersion = "0.0.1";
        bool buildBundleList = true;
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
            GUILayout.TextField(buildPath, GUILayout.MinWidth(260));
            GUILayout.Space(15);
            if (GUILayout.Button("选择输出路径", GUILayout.Width(90)))
            {
                buildPath = EditorUtility.OpenFolderPanel("AssetBundle输出路径", Application.dataPath, "");
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            buildVersion = EditorGUILayout.TextField("构建版本号:", buildVersion);
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
                GUILayout.FlexibleSpace();
                buildBundleList = GUILayout.Toggle(buildBundleList, "生成Bundle信息");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("构建AssetBundle", GUILayout.MaxWidth(position.width - 150)))
                {
                    BuildBundle();
                    GenAssetList();
                    if (buildBundleList)
                        GenBundleInfo();

                    if (copyToPersistentPath)
                        CopyToPersistentPath();

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
            //清空上次结果
            string[] files = Directory.GetFiles(buildPath, "*.*", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                File.Delete(f);
            }

            //打包AssetBundle
            BuildPipeline.BuildAssetBundles(buildPath, BuildAssetBundleOptions.UncompressedAssetBundle, curTarget);
        }

        void GenAssetList()
        {
            //生成资源清单
            var formatPath = buildPath.Replace("\\", "/");
            var split_path = formatPath.Split('/');
            var manifestName = split_path[split_path.Length - 1];
            var manifestBundle = AssetBundle.LoadFromFile(buildPath + "/" + manifestName);

            try
            {
                using (var sw = File.CreateText(buildPath + "/assetlist"))
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

        void GenBundleInfo()
        {
            try
            {
                //生成资源包信息
                using (var sw = File.CreateText(buildPath + "/bundleinfo"))
                {
                    sw.WriteLine("/* Auto Generated By EasyAsset */");
                    sw.WriteLine("build_version:" + buildVersion);

                    string[] files = Directory.GetFiles(buildPath, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        if (file.Contains(".manifest")
                            || file.Contains("assetlist")
                            || file.Contains("bundleinfo"))
                            continue;

                        string md5 = GetMD5(file);
                        string fileName = Path.GetFileName(file);
                        sw.WriteLine(fileName + ":" + md5);
                    }

                    sw.Flush();
                    sw.Close();
                }

            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message + "\n" + ex.StackTrace);
            }
        }

        string GetMD5(string filePath)
        {
            byte[] bs = File.ReadAllBytes(filePath);
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(bs);
            string str = "";
            for(int i = 0; i < hash.Length; i++)
            {
                str += hash[i].ToString("x");
            }
            return str;
        }

        void CopyToPersistentPath()
        {
            var config = AssetDatabase.LoadAssetAtPath<EasyAssetConfig>(Definer.EditorPath + "/Resources/EasyAssetConfig.asset");
            PathHelper.Init(config.LoadPath);

            if (!Directory.Exists(PathHelper.EXTERNAL_ASSET_PATH))
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

        [MenuItem("Common Framework/Easy Assets/Open Local Asset Path", priority = 105)]
        public static void OpenAssetLocalPath()
        {
            var config = AssetDatabase.LoadAssetAtPath<EasyAssetConfig>(Definer.EditorPath + "/Resources/EasyAssetConfig.asset");
            PathHelper.Init(config.LoadPath);
            if (!Directory.Exists(PathHelper.EXTERNAL_ASSET_PATH))
                Directory.CreateDirectory(PathHelper.EXTERNAL_ASSET_PATH);

            System.Diagnostics.Process.Start(PathHelper.EXTERNAL_ASSET_PATH);
        }
    }
}