using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using EasyAsset;
using System.Text;

namespace EasyAsset
{
    public class PathHelper
    {
        static bool inited = false;
        public static string EXTERNAL_ASSET_PATH { get; private set; }
        public static void Init()
        {
            if (inited)
                return;
            inited = true;
            EXTERNAL_ASSET_PATH = Application.persistentDataPath + "/Asset/";
        }
    }

    /// <summary>
    /// 资源清单
    /// </summary>
    public class AssetList
    {
        //原始数据
        public string rawData { get; private set; }

        //Manifest文件名称
        public string ManifestFilename { get; private set; }

        //资源 <----> Assebundle 映射
        Dictionary<string, string> asset2bundleMapping = new Dictionary<string, string>();

        public static AssetList CreateAssetList(string path)
        {
            try
            {
                using (var sr = File.OpenText(path))
                {
                    StringBuilder raw = new StringBuilder();
                    AssetList assetList = new AssetList();

                    raw.Append(sr.ReadLine());
                    var manifest_line = sr.ReadLine();
                    raw.AppendLine();
                    raw.Append(manifest_line);
                    assetList.ManifestFilename = manifest_line.Split(':')[1];
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line))
                            continue;

                        raw.AppendLine();
                        raw.Append(line);

                        var pair = line.Split(':');
                        var asset_path = pair[0];
                        var bundle_name = pair[1];
                        assetList.asset2bundleMapping.Add(asset_path, bundle_name);
                    }

                    return assetList;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message + "\n" + ex.StackTrace);
            }
            return null;
        }

        public string GetBundleName(string asset_path)
        {
            if (asset2bundleMapping.ContainsKey(asset_path))
                return asset2bundleMapping[asset_path];

            return "null";
        }
    }

    public class EasyBundle
    {
        public string bundleName
        {
            get
            {
                return bundle == null ? "null" : bundle.name;
            }
        }

        private AssetBundle bundle;
        public AssetBundle getBundle() { return bundle; }

        private Dictionary<int, object> references = new Dictionary<int, object>();

        public EasyBundle(AssetBundle bundle)
        {
            this.bundle = bundle;
        }

        public void AddRefrence(object refrence)
        {
            if (refrence.GetType().IsValueType)
            {
                Debug.LogWarning("不能添加值类型的引用");
                return;
            }

            references.Add(refrence.GetHashCode(), refrence);
        }
    }
}
/// <summary>
/// 资源管理器
/// </summary>
public class AssetMaintainer : MonoSingleton<AssetMaintainer>
{
    //外部资源清单
    public AssetList externalAssetList { get; private set; }

    AssetBundleManifest manifest = null;

    Dictionary<string, EasyBundle> loadedBundles = new Dictionary<string, EasyBundle>();

    public override void Awake()
    {
        base.Awake();
        PathHelper.Init();
    }

    public static bool Init()
    {
        Instance.externalAssetList = AssetList.CreateAssetList(PathHelper.EXTERNAL_ASSET_PATH + "assetlist");

        if (Instance.externalAssetList == null)
        {
            Debug.LogError("加载外部资源清单失败!");
            return false;
        }

        Instance.manifest = AssetBundle.LoadFromFile(PathHelper.EXTERNAL_ASSET_PATH + Instance.externalAssetList.ManifestFilename).LoadAsset<AssetBundleManifest>("assetbundlemanifest");
        return true;
    }

    public static T LoadAsset<T>(string assetPath, object referenceObject)
        where T : UnityEngine.Object
    {
        return Instance.LoadAsset_Internal<T>(assetPath, referenceObject);
    }

    //同步加载资源
    T LoadAsset_Internal<T>(string assetPath, object referenceObject)
        where T : UnityEngine.Object
    {
        var bundleName = externalAssetList.GetBundleName(assetPath);
        if (!loadedBundles.ContainsKey(bundleName))
            LoadBundle(bundleName);

        var direct_bundle = loadedBundles[bundleName];
        //加载依赖
        string[] dps = manifest.GetAllDependencies(direct_bundle.bundleName);
        foreach (var dp in dps)
        {
            var bd = LoadBundle(dp);
            if (bd == null)
            {
                Debug.LogError("加载 " + assetPath + " 失败!" + "\n无法加载资源依赖包 => " + dp);
                return null;
            }
            bd.AddRefrence(referenceObject);
        }

        direct_bundle.AddRefrence(referenceObject);
        T asset = direct_bundle.getBundle().LoadAsset<T>(assetPath);
        return asset;
    }


    EasyBundle LoadBundle(string bundleName)
    {
        if (!loadedBundles.ContainsKey(bundleName))
        {
            var bundle = AssetBundle.LoadFromFile(PathHelper.EXTERNAL_ASSET_PATH + bundleName);
            if (bundle != null)
            {
                EasyBundle eBundle = new EasyBundle(bundle);
                loadedBundles.Add(eBundle.bundleName, eBundle);
                return eBundle;
            }

            return null;
        }

        return loadedBundles[bundleName];
    }

    void LoadBundleAsync()
    {
        return;
    }
}
