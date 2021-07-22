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

    /// <summary>
    /// 资源包
    /// </summary>
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
        public IEnumerator GetReferences()
        {
            return references.Values.GetEnumerator();
        }

        public EasyBundle(AssetBundle bundle)
        {
            this.bundle = bundle;
        }

        public void AddRefrence(object refrence)
        {
            if (refrence == null)
                return;

            if (refrence.GetType().IsValueType)
            {
                Debug.LogWarning("不能添加值类型的引用");
                return;
            }

            var hash = refrence.GetHashCode();
            if (!references.ContainsKey(hash))
                references.Add(hash, refrence);
        }
    }

    /// <summary>
    /// Bundle加载轨迹
    /// </summary>
    public class BundleLoadTrack
    {
        public int assetHash;
        public string[] bundles;

        int _curindex = 0;

        public BundleLoadTrack(int bundleCount)
        {
            bundles = new string[bundleCount];
            _curindex = 0;
        }

        public void AddBundle(string bundleName)
        {
            if (_curindex < bundles.Length)
            {
                bundles[_curindex] = bundleName;
                _curindex++;
            }
        }

        public void SetHash(int assetHash)
        {
            this.assetHash = assetHash;
        }
    }

    /// <summary>
    /// Bundle包含的资源Hash值，用于卸载Bundle时，快速查询并移除加载轨迹
    /// </summary>
    public class BundleAssetsHash
    {
        public List<int> assetHashList = new List<int>();
        public void AddAsset(int hash)
        {
            assetHashList.Add(hash);
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

    //已加载的bundle
    Dictionary<string, EasyBundle> loadedBundles = new Dictionary<string, EasyBundle>();

    //已加载的外部资源
    Dictionary<string, UnityEngine.Object> externalAssets = new Dictionary<string, UnityEngine.Object>();

    //bundle加载轨迹
    Dictionary<int, BundleLoadTrack> bundleLoadTracks = new Dictionary<int, BundleLoadTrack>();
    public BundleLoadTrack FindTrack(int hash)
    {
        if (bundleLoadTracks.ContainsKey(hash))
            return bundleLoadTracks[hash];
        return null;
    }

    //bundle包含资源hash
    Dictionary<string, BundleAssetsHash> bundleAssetHash = new Dictionary<string, BundleAssetsHash>();
    public void RecordBundleAssetHash(string bundleName, int hash)
    {
        if (!bundleAssetHash.ContainsKey(bundleName))
            bundleAssetHash.Add(bundleName, new BundleAssetsHash());
        bundleAssetHash[bundleName].AddAsset(hash);
    }

    public IEnumerator GetLoadedBundles()
    {
        return loadedBundles.Values.GetEnumerator();
    }

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

    /// <summary>
    /// 记录资源的引用
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="refrence"></param>
    public static void TrackingAsset(UnityEngine.Object asset, object refrence)
    {
        var hash = asset.GetHashCode();

        var tracks = Instance.FindTrack(hash);
        if (tracks != null)
        {
            //记录引用
            foreach (var bundleName in tracks.bundles)
            {
                var track_bundle = Instance.FindBundle(bundleName);
                if (track_bundle != null)
                {
                    track_bundle.AddRefrence(refrence);
                    Instance.RecordBundleAssetHash(bundleName, hash);
                }
            }
        }
    }

    /// <summary>
    /// 同步加载GameObject
    /// </summary>
    /// <param name="assetPath">资源路径</param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static GameObject LoadGameobject(string assetPath, Transform parent = null)
    {
        UnityEngine.Object temp = Instance.LoadAsset_Internal<GameObject>(assetPath);

        if (temp == null)
            return null;
        GameObject go = Instantiate(temp, parent) as GameObject;
        TrackingAsset(temp, go);
        return go;
    }

    /// <summary>
    /// 同步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="assetPath">资源路径</param>
    /// <param name="referenceObject">引用对象</param>
    /// <returns></returns>
    public static T LoadAsset<T>(string assetPath, object referenceObject)
        where T : UnityEngine.Object
    {
        var asset = Instance.LoadAsset_Internal<T>(assetPath);
        TrackingAsset(asset, referenceObject);
        return asset;
    }

    //同步加载资源，记录加载轨迹
    T LoadAsset_Internal<T>(string assetPath)
        where T : UnityEngine.Object
    {
        var bundleName = externalAssetList.GetBundleName(assetPath);
        if (bundleName == "null")
        {//如果是内部资源,直接使用Resources加载
            return Resources.Load<T>(assetPath);
        }

        if (externalAssets.ContainsKey(assetPath))
            return externalAssets[assetPath] as T;

        if (!loadedBundles.ContainsKey(bundleName))
            LoadBundle(bundleName);

        var direct_bundle = loadedBundles[bundleName];
        //加载依赖
        string[] dps = manifest.GetAllDependencies(direct_bundle.bundleName);

        BundleLoadTrack track = new BundleLoadTrack(dps.Length + 1);
        //tracks = new EasyBundle[dps.Length + 1];
        for (int i = 0; i < dps.Length; i++)
        {
            var dp = dps[i];
            var bd = LoadBundle(dp);
            if (bd == null)
            {
                Debug.LogError("加载 " + assetPath + " 失败!" + "\n无法加载资源依赖包 => " + dp);
                return null;
            }
            track.AddBundle(bd.bundleName);
        }

        track.AddBundle(direct_bundle.bundleName);
        T asset = direct_bundle.getBundle().LoadAsset<T>(assetPath);
        track.SetHash(asset.GetHashCode());
        bundleLoadTracks.Add(track.assetHash, track);   //添加记录

        externalAssets.Add(assetPath, asset);           //缓存外部资源 
        return asset;
    }

    //查找bundle
    public EasyBundle FindBundle(string bundleName)
    {
        if (loadedBundles.ContainsKey(bundleName))
            return loadedBundles[bundleName];
        return null;
    }

    //加载Bundle
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
