using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyAsset
{

    /// <summary>
    /// Asset Bundle加载请求池
    /// </summary>
    public class ExternalReqeuestPool : Singleton<ExternalReqeuestPool>
    {
        Dictionary<string, AssetBundleCreateRequest> externalReqPool = new Dictionary<string, AssetBundleCreateRequest>();
        Dictionary<int, string> hash2reqPathMap = new Dictionary<int, string>();
        public AssetBundleCreateRequest LoadAssetBundleAsync(string bundlePath)
        {
            if (!externalReqPool.ContainsKey(bundlePath))
            {
                var req = AssetBundle.LoadFromFileAsync(bundlePath);
                externalReqPool.Add(bundlePath, req);
                hash2reqPathMap.Add(req.GetHashCode(), bundlePath);
            }

            return externalReqPool[bundlePath];
        }

        Stack<string> disposedRequest = new Stack<string>();

        public void DisposeRequest(AssetBundleCreateRequest req)
        {
            lock (disposedRequest)
            {
                var hash = req.GetHashCode();
                if (hash2reqPathMap.ContainsKey(hash))
                {
                    var path = hash2reqPathMap[hash];
                    disposedRequest.Push(path);
                }
            }
        }

        public void Tick()
        {
            foreach (var disposed in disposedRequest)
            {
                if (externalReqPool.ContainsKey(disposed))
                {
                    var hash = externalReqPool[disposed].GetHashCode();
                    externalReqPool.Remove(disposed);
                    hash2reqPathMap.Remove(hash);
                }
            }
        }
    }

    /// <summary>
    /// 异步加载任务
    /// </summary>
    public class LoadAsyncTask : IDisposable
    {
        static ulong TASK_UID_COUNTER = 10000;
        static ulong ApplyTaskUID()
        {
            return TASK_UID_COUNTER++;
        }

        //task uid
        public ulong taskUid { get; private set; } = 0;

        /* Resource 模式 */
        ResourceRequest internalReq;

        /* Asset Bundle 模式*/
        AssetBundleCreateRequest externalReq;//异步加载请求
        int loadStep = 0;                   //加载进度
        string assetPath;                   //asset路径
        string assetBundleName;             //包含asset的bundle名称
        List<EasyBundle> easyBundles;       //需要加载的bundle列表
        object refrence;                    //资源引用
        bool asyncDone = false;             //Bundle加载是否完成

        //加载模式（内部资源 or 外部资源）
        bool loadInternal = false;

        //是否只加载Asset Bundle
        bool onlyLoadBundle = false;

        Delegate onAssetLoadFinish;         //资源加载完成
        
        Delegate onBundleLoadFinish;        //AB包加载完成

        public bool loadGameObject { get; private set; }
        Transform parent = null;
        public void MarkLoadGameobject(bool loadGameObject, Transform parent)
        {
            this.loadGameObject = loadGameObject;
            this.parent = parent;
        }

        //创建异步任务--加载资源
        public LoadAsyncTask(string assetPath, string assetBundleName, List<EasyBundle> easyBundles,
            object refrence, Delegate callback)
        {
            taskUid = ApplyTaskUID();

            this.assetPath = assetPath;
            this.assetBundleName = assetBundleName;
            this.easyBundles = easyBundles;
            this.refrence = refrence;
            onAssetLoadFinish = callback;
            loadInternal = false;
            loadStep = 0;
            asyncDone = false;
            onlyLoadBundle = false;
        }

        //创建异步任务--只加载AB包,用于加载场景
        public LoadAsyncTask(string assetPath, List<EasyBundle> easyBundles, Action<string, List<EasyBundle>> callback)
        {
            this.assetPath = assetPath;
            this.easyBundles = easyBundles;
            onBundleLoadFinish = callback;
            loadInternal = false;
            loadStep = 0;
            asyncDone = false;
            onlyLoadBundle = true;
        }

        public LoadAsyncTask(ResourceRequest request,
            Delegate callback)
        {
            taskUid = ApplyTaskUID();
            internalReq = request;
            onAssetLoadFinish = callback;
            loadInternal = true;
        }

        public bool isFinish
        {
            get
            {
                if (loadInternal)
                    return internalReq.isDone;
                else
                    return asyncDone;
            }
        }

        public void Tick()
        {
            if (isFinish)
                return;

            if (!loadInternal)
            {
                if (externalReq == null ||
                    externalReq.isDone)
                {
                    if (loadStep >= easyBundles.Count)
                    {
                        //Debug.Log(externalReq.isDone);
                        onLoadBundle();
                        asyncDone = true;
                        externalReq = null;
                        return;
                    }

                    onLoadBundle();
                    LoadNextBundle();
                }
            }
        }

        void onLoadBundle()
        {
            if (externalReq != null)
            {
                var cur_eb = easyBundles[loadStep - 1];
                cur_eb.onLoadBundle(externalReq);
            }
        }

        void LoadNextBundle()
        {
            var eb = easyBundles[loadStep];
            if (!eb.isLoaded)
            {
                externalReq = eb.LoadBundleAsync();
                //Debug.Log("Load " + easyBundles[loadStep].bundleName + " => " + externalReq);
            }
            loadStep++;
        }

        EasyBundle FindBundle(string bundleName)
        {
            foreach (var bd in easyBundles)
            {
                if (bd.bundleName == bundleName)
                    return bd;
            }
            return null;
        }

        void TrackingAsset(UnityEngine.Object asset, object refrence)
        {
            //追踪引用
            BundleLoadTrack track = new BundleLoadTrack(easyBundles.Count);
            foreach (var eb in easyBundles)
                track.AddBundle(eb.bundleName);
            track.SetHash(asset.GetHashCode());
            AssetMaintainer.Instance.AddBundleLoadTrack(track);
            AssetMaintainer.TrackingAsset(asset, refrence);
        }

        public void InvokeFinish()
        {
            if (loadInternal)
            {
                try
                {
                    if (!loadGameObject)
                        onAssetLoadFinish.DynamicInvoke(internalReq.asset);
                    else
                    {
                        var go = GameObject.Instantiate(internalReq.asset, parent);
                        onAssetLoadFinish.DynamicInvoke(go);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
            else
            {
                if (!onlyLoadBundle)
                {//加载资源
                    var abd = FindBundle(assetBundleName);
                    if (abd != null)
                    {
                        var asset = abd.GetAsset<UnityEngine.Object>(assetPath);
                        UnityEngine.Object go = null;
                        if (loadGameObject)
                            go = GameObject.Instantiate(asset, parent);

                        TrackingAsset(asset, loadGameObject ? go : refrence);

                        try
                        {
                            onAssetLoadFinish?.DynamicInvoke(loadGameObject ? go : asset);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                }
                else
                {//加载场景时，只加载包
                    try
                    {
                        onBundleLoadFinish?.DynamicInvoke(assetPath, easyBundles);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        public void Dispose()
        {
            onAssetLoadFinish = null;
            internalReq = null;
            externalReq = null;
            easyBundles = null;
        }
    }
}
