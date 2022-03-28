using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using EasyAssets;
using System.Text;
using UnityEngine.SceneManagement;

namespace EasyAssets
{
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

        //添加包引用
        public void AddBundle(string bundleName)
        {
            if (_curindex < bundles.Length)
            {
                bundles[_curindex] = bundleName;
                _curindex++;
            }
        }

        //设置资源Hash值
        public void SetHash(int assetHash)
        {
            this.assetHash = assetHash;
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
    public static string AssetVersion { get { return Instance.externalAssetList != null ? Instance.externalAssetList.BuildVersion : "no version"; } }

    AssetBundleManifest manifest = null;

    //已构建的easy bundle
    Dictionary<string, EasyBundle> easyBundles = new Dictionary<string, EasyBundle>();
    public IEnumerator GetLoadedBundles()
    {
        return easyBundles.Values.GetEnumerator();
    }

    #region 资源引用记录

    //bundle加载轨迹
    Dictionary<int, BundleLoadTrack> bundleLoadTracks = new Dictionary<int, BundleLoadTrack>();
    public BundleLoadTrack FindTrack(int assetHash)
    {
        if (bundleLoadTracks.ContainsKey(assetHash))
            return bundleLoadTracks[assetHash];
        return null;
    }
    public void AddBundleLoadTrack(BundleLoadTrack track)
    {
        if (!bundleLoadTracks.ContainsKey(track.assetHash))
        {
            bundleLoadTracks.Add(track.assetHash, track);
            //Debug.Log("real 增加加载轨迹 " + track.assetHash);
        }
    }

    void RemoveBundleLoadTrack(int assetHash)
    {
        if (bundleLoadTracks.ContainsKey(assetHash))
            bundleLoadTracks.Remove(assetHash);
    }

    /// <summary>
    /// 记录资源的引用
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="refrence"></param>
    public static void TrackingAsset(object asset, object refrence)
    {
        if (asset == null
            || refrence == null)
            return;

        var hash = asset.GetHashCode();

        var track = Instance.FindTrack(hash);
        if (track != null)
        {
            //记录引用
            foreach (var bundleName in track.bundles)
            {
                var track_bundle = Instance.FindBundle(bundleName);
                if (track_bundle != null)
                {
                    //Debug.Log("want add refrence => " + hash + "," + refrence);
                    track_bundle.AddRefrence(refrence);
                }
            }
        }
    }

    #endregion

    #region 同步加载

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
        if (externalAssetList == null)
        {//如果是内部资源,直接使用Resources加载
            return Resources.Load<T>(assetPath);
        }

        var real_path = "";
        var bundleName = externalAssetList.GetAssetBundleName<T>(assetPath, ref real_path);
        if (bundleName == "null")
        {//如果是内部资源,直接使用Resources加载
            return Resources.Load<T>(assetPath);
        }

        assetPath = real_path;
        var direct_bundle = GetEasyBundle(bundleName);

        //加载依赖
        string[] dps = manifest.GetAllDependencies(direct_bundle.bundleName);

        BundleLoadTrack track = new BundleLoadTrack(dps.Length + 1);
        for (int i = 0; i < dps.Length; i++)
        {
            var dp = dps[i];
            var bd = GetEasyBundle(dp);
            if (!bd.LoadBundle())
            {
                Debug.LogError("加载 " + assetPath + " 失败!" + "\n无法加载资源依赖包 => " + dp);
                return null;
            }
            track.AddBundle(bd.bundleName);
        }

        if (!direct_bundle.LoadBundle())
        {
            Debug.LogError("加载 " + assetPath + " 失败!" + "\n无法加载资源依赖包 => " + direct_bundle.bundleName);
            return null;
        }

        track.AddBundle(direct_bundle.bundleName);
        T asset = direct_bundle.GetAsset<T>(assetPath);
        track.SetHash(asset.GetHashCode());
        AddBundleLoadTrack(track);

        return asset;
    }

    //查找bundle
    public EasyBundle FindBundle(string bundleName)
    {
        if (easyBundles.ContainsKey(bundleName))
            return easyBundles[bundleName];
        return null;
    }

    //获得EasyBundle
    EasyBundle GetEasyBundle(string bundleName)
    {
        if (!easyBundles.ContainsKey(bundleName))
        {
            EasyBundle eBundle = TryGetDisposeBundle(bundleName);

            if (eBundle == null)
            {
                bool isManaged = !Setting.UnmanagedBundles.Contains(bundleName);
                eBundle = new EasyBundle(bundleName, isManaged);
            }
            easyBundles.Add(eBundle.bundleName, eBundle);
            return eBundle;
        }

        return easyBundles[bundleName];
    }

    EasyBundle TryGetDisposeBundle(string bundleName)
    {
        if (disposePool.ContainsKey(bundleName))
        {
            var bundle = disposePool[bundleName];
            bundle.Restore();
            return bundle;
        }

        return null;
    }

    #endregion

    #region 异步加载

    Dictionary<ulong, LoadAsyncTask> loadAsyncTasks = new Dictionary<ulong, LoadAsyncTask>();
    Queue<LoadAsyncTask> finishTasks = new Queue<LoadAsyncTask>();

    public static void LoadAssetAsync<T>(string assetPath, object refrenceObject, Action<T> onFinish)
        where T : UnityEngine.Object
    {
        Instance.LoadAssetAsync_Internal(assetPath, refrenceObject, onFinish);
    }

    void LoadAssetAsync_Internal<T>(string assetPath, object refrenceObject, Action<T> onFinish,
        bool load_gameobject = false, Transform parent = null)
        where T : UnityEngine.Object
    {
        if (externalAssetList == null)
        {//如果是内部资源,直接使用Resources加载
            var request = Resources.LoadAsync<T>(assetPath);
            LoadAsyncTask r_task = new LoadAsyncTask(request, onFinish);
            r_task.MarkLoadGameobject(load_gameobject, parent);
            loadAsyncTasks.Add(r_task.taskUid, r_task);
            return;
        }

        var real_path = "";
        var bundleName = externalAssetList.GetAssetBundleName<T>(assetPath, ref real_path);
        if (bundleName == "null")
        {//如果是内部资源,直接使用Resources加载
            var request = Resources.LoadAsync<T>(assetPath);
            LoadAsyncTask r_task = new LoadAsyncTask(request, onFinish);
            r_task.MarkLoadGameobject(load_gameobject, parent);
            loadAsyncTasks.Add(r_task.taskUid, r_task);
            return;
        }

        assetPath = real_path;
        //异步加载外部资源
        List<EasyBundle> easyBundles = new List<EasyBundle>();
        string[] dps = manifest.GetAllDependencies(bundleName);
        foreach (var dp in dps)
        {
            easyBundles.Add(GetEasyBundle(dp));
        }
        easyBundles.Add(GetEasyBundle(bundleName));
        LoadAsyncTask a_task = new LoadAsyncTask(assetPath, bundleName, easyBundles,
            refrenceObject, onFinish);
        a_task.MarkLoadGameobject(load_gameobject, parent);
        loadAsyncTasks.Add(a_task.taskUid, a_task);
    }

    void TickRequest()
    {
        foreach (var task in loadAsyncTasks.Values)
        {
            task.Tick();
            if (task.isFinish)
                finishTasks.Enqueue(task);
        }

        while (finishTasks.Count > 0)
        {
            using (var f_task = finishTasks.Dequeue())
            {
                loadAsyncTasks.Remove(f_task.taskUid);
                f_task.InvokeFinish();
            }
        }
    }

    public static void LoadGameobjectAsync(string assetPath, Action<GameObject> onFinish, Transform parent = null)
    {
        Instance.LoadAssetAsync_Internal(assetPath, null, onFinish, true, parent);
    }

    #endregion

    #region 加载场景

    bool isLoadingScene = false;
    AsyncOperation sceneLoadOpeartion;
    Action onLoadSceneCB;
    Action<float> onProgressCB;
    bool trackingSceneRefrence = false;     //追踪场景引用
    List<EasyBundle> trackBundles;          //加载路径上的包

    //是否是内建的场景
    bool inBuildScene(string scenePath)
    {
        return SceneHelper.isBuildInSceneByPath(scenePath);
    }

    public static bool LoadScene(string scenePath, Action onLoadScene, Action<float> onProgress = null)
    {
        if (Instance.isLoadingScene)
            return false;

        if (Instance.inBuildScene(scenePath))
        {
            Instance.onLoadSceneCB = onLoadScene;
            Instance.onProgressCB = onProgress;
            var sceneName = SceneHelper.ScenePath2SceneName(scenePath);
            Instance.sceneLoadOpeartion = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Instance.isLoadingScene = true;
            Instance.trackingSceneRefrence = false;
            return true;
        }

        return Instance.LoadScene_Internal(scenePath, onLoadScene, onProgress);
    }

    bool LoadScene_Internal(string scenePath, Action onLoadScene, Action<float> onProgress)
    {
        var bundleName = externalAssetList.GetSceneBundleName(scenePath);
        if (bundleName == "null")
            return false;

        List<EasyBundle> easyBundles = new List<EasyBundle>();
        string[] dps = manifest.GetAllDependencies(bundleName);
        foreach (var dp in dps)
        {
            easyBundles.Add(GetEasyBundle(dp));
        }
        easyBundles.Add(GetEasyBundle(bundleName));

        onLoadSceneCB = onLoadScene;
        onProgressCB = onProgress;
        isLoadingScene = true;

        LoadAsyncTask a_task = new LoadAsyncTask(scenePath, easyBundles, onLoadSceneBundle);
        loadAsyncTasks.Add(a_task.taskUid, a_task);

        return true;
    }

    //记载完场景依赖的Bundle
    void onLoadSceneBundle(string scenePath, List<EasyBundle> trackBundles)
    {
        var sceneName = SceneHelper.ScenePath2SceneName(scenePath);
        this.trackBundles = trackBundles;
        trackingSceneRefrence = true;
        sceneLoadOpeartion = SceneManager.LoadSceneAsync(sceneName);
    }

    //检测加载场景
    void TickLoadScene()
    {
        if (!isLoadingScene)
            return;
        if (sceneLoadOpeartion == null)
            return;
        if (!sceneLoadOpeartion.isDone)
        {//加载中
            try
            {
                onProgressCB?.Invoke(sceneLoadOpeartion.progress);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        else
        {
            isLoadingScene = false;
            sceneLoadOpeartion.allowSceneActivation = true;

            try
            {
                onLoadSceneCB?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            //引用记录
            if (trackingSceneRefrence
                && trackBundles != null)
            {
                BundleLoadTrack track = new BundleLoadTrack(trackBundles.Count);
                foreach (var eb in trackBundles)
                {
                    track.AddBundle(eb.bundleName);
                    eb.SetUsed();
                }
                var scene = SceneManager.GetActiveScene();
                var refrence = new GameObject(scene.name + "(Scene)");
                refrence.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                track.SetHash(scene.GetHashCode());
                AddBundleLoadTrack(track);
                TrackingAsset(scene, refrence);
            }

            trackingSceneRefrence = false;
            trackBundles = null;
            sceneLoadOpeartion = null;
            onLoadSceneCB = null;
            onProgressCB = null;
        }
    }

    #endregion

    #region 卸载

    //Bundle移除缓存
    Dictionary<string, EasyBundle> disposePool = new Dictionary<string, EasyBundle>();
    public IEnumerator GetDisposedBundles() { return disposePool.Values.GetEnumerator(); }
    Stack<EasyBundle> removeStack = new Stack<EasyBundle>();

    //处理移除的Bundle
    void HandleRemovedBundle()
    {
        while (removeStack.Count > 0)
        {
            var eBundle = removeStack.Pop();

            if (!eBundle.disposed)      //若包已经被捞出，则跳过
                continue;

            var loaded = eBundle.GetLoadedAssets();
            while (loaded.MoveNext())
                RemoveBundleLoadTrack(loaded.GetHashCode());

            eBundle.onDispose();

            disposePool.Remove(eBundle.bundleName);
            unloadUnusedDirty = true;
            Debug.Log("Release Bundle : " + eBundle.bundleName);
        }
    }

    Stack<string> disposeAddCache = new Stack<string>();
    void TickRefrence()
    {
        foreach (var bundle in easyBundles)
        {
            if (!disposePool.ContainsKey(bundle.Key))
            {
                var eb = bundle.Value;
                eb.Tick();
                if (eb.isManaged && eb.used && !eb.HasRefrence)
                    eb.ReleaseBundle();
                if (eb.disposed)
                    disposeAddCache.Push(bundle.Key);
            }
        }

        lock (disposeAddCache)
        {
            while (disposeAddCache.Count > 0)
            {
                var rm = disposeAddCache.Pop();
                disposePool.Add(rm, easyBundles[rm]);
                easyBundles.Remove(rm);
            }
        }
    }

    Stack<string> disposeRemoveCache = new Stack<string>();

    void TickDisposePool()
    {
        foreach (var rm_bundle in disposePool)
        {
            if (rm_bundle.Value.disposed)
            {
                if (rm_bundle.Value.disposedTime > Setting.DisposeCacheTime - Setting.RefrenceCheckTime)
                    removeStack.Push(rm_bundle.Value);
            }
            else
                disposeRemoveCache.Push(rm_bundle.Key);
        }

        while (disposeRemoveCache.Count > 0)
            disposePool.Remove(disposeRemoveCache.Pop());
    }

    void UnloadBundle_Internal(string bundleName)
    {
        if (!easyBundles.ContainsKey(bundleName))
            return;
        var eb = easyBundles[bundleName];
        eb.UnloadBundle();
    }

    void ReleaseBundle_Internal(string bundleName)
    {
        if (!easyBundles.ContainsKey(bundleName))
            return;
        var eb = easyBundles[bundleName];
        if (eb.isManaged)
            return;
        eb.ForceDispose();
        disposeAddCache.Push(bundleName);
    }

    /// <summary>
    /// 手动卸载Bundle对象，调用该方法会卸载Asset Bundle对象，不会释放已加载的资源。
    /// </summary>
    /// <param name="bundleName"></param>
    public static void UnloadBundle(string bundleName)
    {
        Instance.UnloadBundle_Internal(bundleName);
    }

    /// <summary>
    /// 手动释放Bundle对象,调用该方法会卸载Asset Bundle对象，并释放所有已加载的资源。
    /// (仅对非托管的Bundle对象有效)
    /// </summary>
    /// <param name="bundleName"></param>
    public static void ReleaseBundle(string bundleName)
    {
        Instance.ReleaseBundle_Internal(bundleName);
    }

    #endregion

    #region Unity Function

    public override void Awake()
    {
        base.Awake();
        Setting.InitSetting();
        PathHelper.Init(Setting.LoadPath);
        SceneHelper.Init();
    }

    public static bool Init()
    {
        Instance.externalAssetList = AssetList.CreateAssetList(PathHelper.EXTERNAL_ASSET_PATH + EASY_DEFINE.ASSET_LIST_FILE);

        if (Instance.externalAssetList == null)
        {
            Debug.LogError("加载外部资源清单失败!");
            return false;
        }

        var manifestPath = PathHelper.EXTERNAL_ASSET_PATH + Instance.externalAssetList.ManifestFilename;
        if (!File.Exists(manifestPath))
            return false;

        var manifestBundle = AssetBundle.LoadFromFile(manifestPath);
        if (manifestBundle == null)
            return false;

        Instance.manifest = manifestBundle.LoadAsset<AssetBundleManifest>("assetbundlemanifest");
        return true;
    }

    float lastUpdateTime = 0;
    private void Update()
    {
        if (Time.realtimeSinceStartup - lastUpdateTime >= Setting.RefrenceCheckTime)
        {
            //更新当前引用状态
            TickRefrence();

            TickDisposePool();
            lastUpdateTime = Time.realtimeSinceStartup;
        }

        //移除包
        HandleRemovedBundle();

        //更新请求
        TickRequest();

        //清理异步加载请求池
        ExternalReqeuestPool.Instance.Tick();

        //加载场景
        TickLoadScene();
    }

    bool unloadUnusedDirty = false;
    private void LateUpdate()
    {
        if (unloadUnusedDirty)
        {
            unloadUnusedDirty = false;
            Resources.UnloadUnusedAssets();
        }
    }

    /// <summary>
    /// 作用与Resources.UnloadUnusedAssets()相同。
    /// </summary>
    public static void UnloadUnusedAssets()
    {
        Instance.unloadUnusedDirty = true;
    }

    #endregion
}
