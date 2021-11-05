using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyAssets
{
    /// <summary>
    /// 资源包
    /// </summary>
    public class EasyBundle
    {
        public string bundlePath { get; private set; }

        public string bundleName { get; private set; }

        public EasyBundle(string bundleName, bool isManaged)
        {
            this.bundleName = bundleName;
            bundlePath = PathHelper.EXTERNAL_ASSET_PATH + bundleName;
            this.isManaged = isManaged;
        }

        //Asset Bundle
        private AssetBundle bundle;
        public AssetBundle getBundle() { return bundle; }

        //是否已使用过
        public bool used { get; private set; } = false;
        public void SetUsed() { used = true; }

        /// <summary>
        /// 是否托管,非托管的EasyBundle对象不会自动卸载和释放资源，但仍可手动进行卸载与释放操作
        /// </summary>
        public bool isManaged { get; private set; } = false;

        #region 加载

        public bool isLoaded { get { return bundle != null; } }     //是否已加载
        private float whenLoaded;                                   //何时加载
        public float BundleLoadedTime { get { if (!isLoaded) return 0; return Time.realtimeSinceStartup - whenLoaded; } }   //已加载时间

        //加载Assetbundle
        public bool LoadBundle()
        {
            if (isLoaded)
                return true;

            Debug.Log("Load " + bundlePath);

            try
            {
                bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle != null)
                {
                    whenLoaded = Time.realtimeSinceStartup;
                    used = false;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            return false;
        }

        //异步加载AssetBundle
        public AssetBundleCreateRequest LoadBundleAsync()
        {
            if (isLoaded)
                return null;

            return ExternalReqeuestPool.Instance.LoadAssetBundleAsync(bundlePath);
        }

        public void onLoadBundle(AssetBundleCreateRequest request)
        {
            if (request.isDone)
            {
                bundle = request.assetBundle;
                whenLoaded = Time.realtimeSinceStartup;
                used = false;
                ExternalReqeuestPool.Instance.DisposeRequest(request);
                Debug.Log("Loaded " + bundleName + " : " + bundle);
            }
        }

        #endregion

        #region 卸载

        //废弃于
        public float disposedAt { get; private set; } = -1f;
        //废弃时间
        public float disposedTime { get { return disposed ? Time.realtimeSinceStartup - disposedAt : 0f; } }
        //是否已经废弃
        public bool disposed { get; private set; } = false;
        //是否可重用
        public bool canResotre { get; private set; } = true;

        //卸载
        public void UnloadBundle()
        {
            if (bundle != null)
            {
                bundle.Unload(false);
                bundle = null;
            }
        }

        //释放
        public void ReleaseBundle()
        {
            if (disposed)
                return;
            disposed = true;
            disposedAt = Time.realtimeSinceStartup;

            Debug.Log("Dispose Bundle : " + bundleName);
        }

        //重用
        public void Restore()
        {
            if (!canResotre)
                return;
            if (!disposed)
                return;
            disposed = false;
            Debug.Log("Resotre Bundle : " + bundleName);
        }

        //强制释放
        public void ForceDispose()
        {
            if (disposed)
                return;

            canResotre = true;
            disposed = true;
            disposedAt = -1e15f;
        }

        public void onDispose()
        {
            loadedAssets.Clear();
            hash2assetpathMap.Clear();
            references.Clear();

            if (bundle != null)
            {
                bundle.Unload(true);
                bundle = null;
            }
        }

        #endregion

        #region 资源维护

        //已加载的外部资源
        Dictionary<string, UnityEngine.Object> loadedAssets = new Dictionary<string, UnityEngine.Object>();
        Dictionary<int, string> hash2assetpathMap = new Dictionary<int, string>();
        public T GetAsset<T>(string assetPath)
            where T : UnityEngine.Object
        {
            used = true;
            if (loadedAssets.ContainsKey(assetPath))
                return loadedAssets[assetPath] as T;

            if (isLoaded)
            {
                var asset = bundle.LoadAsset<T>(assetPath);
                loadedAssets.Add(assetPath, asset);
                hash2assetpathMap.Add(asset.GetHashCode(), assetPath);
                return loadedAssets[assetPath] as T;
            }

            return null;
        }

        public IEnumerator GetLoadedAssets()
        {
            return loadedAssets.Values.GetEnumerator();
        }

        public string GetAssetPath(int hash)
        {
            if (hash2assetpathMap.ContainsKey(hash))
                return hash2assetpathMap[hash];
            return "";
        }

        public string GetAssetPath(UnityEngine.Object asset)
        {
            return GetAssetPath(asset.GetHashCode());
        }

        #endregion

        #region 资源引用

        private Dictionary<int, object> references = new Dictionary<int, object>();
        public IEnumerator GetReferences()
        {
            return references.Values.GetEnumerator();
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

            var refHash = refrence.GetHashCode();
            if (!references.ContainsKey(refHash))
            {
                references.Add(refHash, refrence);
                //Debug.Log(bundleName+ " real add refrence " + refrence);
            }
            else
                Debug.LogWarning(bundleName + " already contains refrence " + refrence);
        }

        public bool HasRefrence { get { return references.Count > 0; } }

        public int RefrenceCount { get { return references.Count; } }

        #endregion

        List<int> rm = new List<int>();
        void CheckRefrence()
        {
            rm.Clear();
            foreach (var r in references)
            {
                if (r.Value == null
                    || r.Value.ToString() == "null")
                    rm.Add(r.Key);
            }

            foreach (var k in rm)
                references.Remove(k);
        }

        void CheckBundleLive()
        {
            if (!isManaged)
                return;

            if (!used)
                return;

            if (BundleLoadedTime >= Setting.AssetBundleLiveTime)
                UnloadBundle();
        }

        public void Tick()
        {
            CheckRefrence();
            CheckBundleLive();
        }
    }
}