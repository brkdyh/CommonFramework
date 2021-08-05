using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using EasyAsset;
using System;

namespace EasyAsset
{
    public class BundleInfo
    {
        public class BundleInfoData
        {
            public string bundleName { get; private set; }
            public string bundleMD5 { get; private set; }
            public long bundleSize { get; private set; }

            public BundleInfoData(string name, string md5, long size)
            {
                bundleName = name;
                bundleMD5 = md5;
                bundleSize = size;
            }
        }
        public string buildVersion { get; private set; } = "0.0.1_0";

        //bundle name <=> md5 映射
        public Dictionary<string, BundleInfoData> bundles = new Dictionary<string, BundleInfoData>();

        public long totalFileSize { get; private set; } = 0;
        public int totalFileCount { get; private set; } = 0;

        public void LoadBundleInfo(StreamReader sr)
        {
            using (sr)
            {
                bundles.Clear();
                sr.ReadLine();
                var version_line = sr.ReadLine();
                buildVersion = version_line.Split(':')[1];

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    if (line.StartsWith("#"))
                    {
                        var sps = line.Split(':');
                        if (sps[0] == "total_file_size")
                            totalFileSize = long.Parse(sps[1]);
                        else if (sps[1] == "total_file_count")
                            totalFileCount = int.Parse(sps[1]);
                    }
                    else
                    {
                        var sps = line.Split(':');
                        var bundleName = sps[0];
                        var md5 = sps[1];
                        long size = 0;
                        if (sps.Length > 2)
                            size = long.Parse(sps[2]);
                        BundleInfoData data = new BundleInfoData(bundleName, md5, size);
                        if (!bundles.ContainsKey(bundleName))
                            bundles.Add(bundleName, data);
                    }
                }
            }
        }
    }

    //需要更新的bundle信息
    public class UpdateBundle
    {
        public string bundleName { get; private set; } = "";
        public string md5 { get; private set; } = "";
        public string url { get; private set; } = "";
        public bool couldDownload { get { return url != ""; } }
        public bool enableCheck { get; private set; } = true;
        public long bundleSize { get; private set; } = 0;

        public UpdateBundle(string bundleName, string md5, long bundleSize)
        {
            this.bundleName = bundleName;
            this.md5 = md5;
            this.bundleSize = bundleSize;
        }

        public void SetUrl(string url)
        {
            this.url = url;
        }

        public void CombineUrl(string root_url)
        {
            this.url = root_url + "/" + bundleName;
        }

        public void EnableCheck(bool enable)
        {
            enableCheck = enable;
        }
    }

    public enum BundleCheckResult
    {
        Succeed = 0,
        DownloadError,
        OtherError,
    }

    public class BundleCheck : Singleton<BundleCheck>
    {
        bool inited = false;
        protected override void Init()
        {
            if (inited)
                return;

            inited = true;
            Setting.InitSetting();
            PathHelper.Init(Setting.LoadPath);

            LoadLocalBundleInfo();
        }

        BundleInfo LoadBundleInfo(byte[] data)
        {
            if (data == null)
                throw (new Exception("Bundle Info Data is Null"));

            var bundleInfo = new BundleInfo();

            using (var memory_stream = new MemoryStream(data))
            {
                StreamReader reader = new StreamReader(memory_stream, Encoding.UTF8);
                bundleInfo.LoadBundleInfo(reader);
            }

            return bundleInfo;
        }

        public bool inChecking { get; private set; }

        #region 本地

        public BundleInfo localBundleInfo;
        void LoadLocalBundleInfo()
        {
            localBundleInfo = new BundleInfo();

            string localBundleInfoPath = PathHelper.EXTERNAL_ASSET_PATH + EASY_DEFINE.BUNDLE_INFO_FILE;
            if (File.Exists(localBundleInfoPath))
            {
                var sr = File.OpenText(localBundleInfoPath);
                try
                {
                    localBundleInfo.LoadBundleInfo(sr);
                    return;
                }
                catch (Exception ex)
                {
                    sr.Close();
                    if (File.Exists(localBundleInfoPath))
                        File.Delete(localBundleInfoPath);

                    Debug.LogException(ex);
                }
            }

            var ta = Resources.Load<TextAsset>(EASY_DEFINE.BUNDLE_INFO_NAME);
            if (ta == null)
                return;

            localBundleInfo = LoadBundleInfo(ta.bytes);
        }

        static List<UpdateBundle> GetUpdateListByLocal()
        {
            List<UpdateBundle> list = new List<UpdateBundle>();
            foreach (var bundle in Instance.localBundleInfo.bundles)
            {
                var bundlePath = PathHelper.EXTERNAL_ASSET_PATH + bundle.Key;
                if (!File.Exists(bundlePath))
                {//不存在文件，直接添加到更新列表
                    list.Add(new UpdateBundle(bundle.Key, bundle.Value.bundleMD5, bundle.Value.bundleSize));
                    //Debug.LogFormat("Need Update Bundle (name = {0},ms5 = {1})", bundle.Key, bundle.Value);
                }
                else
                {
                    if (Setting.bundleCheckMode == Setting.BundleCheckMode.MD5)
                    {//存在文件，验证文件md5
                        var md5 = Utils.GetMD5(bundlePath);
                        if (md5 != bundle.Value.bundleMD5)
                            list.Add(new UpdateBundle(bundle.Key, bundle.Value.bundleMD5, bundle.Value.bundleSize));
                    }
                    else
                    {//存在文件，验证文件尺寸
                        long size = Utils.GetFileSize(bundlePath);
                        if (size != bundle.Value.bundleSize)
                            list.Add(new UpdateBundle(bundle.Key, bundle.Value.bundleMD5, bundle.Value.bundleSize));
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 验证本地文件完整性
        /// </summary>
        /// <returns></returns>
        public static bool VerifyIntegrity()
        {
            return GetUpdateListByLocal().Count <= 0;
        }

        /// <summary>
        /// 从本地BundleInfo检测是否需要更新
        /// </summary>
        /// <param name="onUpdateFinish">更新完成回调</param>
        /// <param name="onDownloadProgress">下载进度回调</param>
        /// <returns>是否需要更新</returns>
        public static bool CheckUpdateFromLocal(
            Action onUpdateFinish,
            Action<float> onDownloadProgress = null)
        {
            if (Instance.inChecking)
                return false;

            Instance.onUpdateFinishCB = onUpdateFinish;
            var updateList = GetUpdateListByLocal();
            if (updateList.Count <= 0)
            {
                Debug.Log("当前无需更新");
                Instance.InvokeUpdateFinish();
                return false;
            }

            //开始下载
            BundleDownloadManager.ClearDownloadErrorHandler();
            if (Instance.onDownloadErrorCB != null)
                BundleDownloadManager.AddDownloadErrorHandler(Instance.onDownloadErrorCB);
            BundleDownloadManager.DownloadBundles(updateList, Instance.OnDownloadBundleFinish, onDownloadProgress);
            return true;
        }

        #endregion

        #region 远程

        public BundleInfo remoteBundleInfo;

        //远程 bundle info 下载完成
        void onDownloadBundleInfo()
        {
            List<UpdateBundle> updateList = new List<UpdateBundle>();
            try
            {
                var data = File.ReadAllBytes(PathHelper.EXTERNAL_ASSET_PATH + EASY_DEFINE.BUNDLE_INFO_FILE);
                remoteBundleInfo = LoadBundleInfo(data);

                updateList = GetUpdateListByRemote();
                //Debug.Log(updateList.Count);
                if (updateList.Count <= 0)
                {
                    Debug.Log("当前无需更新");
                    InvokeCheckFinish(BundleCheckResult.Succeed);
                    InvokeUpdateFinish();
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                InvokeCheckFinish(BundleCheckResult.OtherError);
                return;
            }

            InvokeCheckFinish(BundleCheckResult.Succeed);
            //设置下载
            BundleDownloadManager.ClearDownloadErrorHandler();
            if (onDownloadErrorCB != null)
                BundleDownloadManager.AddDownloadErrorHandler(onDownloadErrorCB);
            BundleDownloadManager.DownloadBundles(updateList, OnDownloadBundleFinish, onDownloadProgressCB);
        }

        //远程 bundle info 下载失败
        void onDownloadBundleInfoError(BundleDownloadRequest request)
        {
            BundleDownloadManager.CancleDownload();
            inChecking = false;
            try
            {
                onCheckFinishCB?.Invoke(BundleCheckResult.DownloadError);
            }
            catch(Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        //从远程bundle info中获得需要更新的文件
        static List<UpdateBundle> GetUpdateListByRemote()
        {
            List<UpdateBundle> updateBundles = new List<UpdateBundle>();

            var local_info = Instance.localBundleInfo;
            var remote_info = Instance.remoteBundleInfo;

            //if (remote_info.buildVersion == local_info.buildVersion)
            //return updateBundles;       //版本号一致，不进行更新检测

            foreach (var rm_bd in remote_info.bundles)
            {
                if (rm_bd.Key == EASY_DEFINE.BUNDLE_INFO_FILE)
                    continue;

                var local_file_path = PathHelper.EXTERNAL_ASSET_PATH + rm_bd.Key;
                if (!File.Exists(local_file_path))
                {//若本地无文件，直接加入下载列表
                    updateBundles.Add(CreateDownloadBundle(rm_bd.Key, rm_bd.Value.bundleMD5, rm_bd.Value.bundleSize));
                    continue;
                }

                if (Setting.bundleCheckMode == Setting.BundleCheckMode.MD5)
                {
                    //若本地存在文件，(比对一下md5值)
                    var local_md5 = Utils.GetMD5(local_file_path);
                    if (local_md5 != rm_bd.Value.bundleMD5)
                        updateBundles.Add(CreateDownloadBundle(rm_bd.Key, rm_bd.Value.bundleMD5, rm_bd.Value.bundleSize));
                }
                else
                {
                    //若本地存在文件，(比对一下文件大小)
                    var local_size = Utils.GetFileSize(local_file_path);
                    if (local_size != rm_bd.Value.bundleSize)
                        updateBundles.Add(CreateDownloadBundle(rm_bd.Key, rm_bd.Value.bundleMD5, rm_bd.Value.bundleSize));
                }
            }
            return updateBundles;
        }

        /// <summary>
        /// 从远程服务器检测是否需要更新
        /// </summary>
        /// <param name="version">远程版本号</param>
        /// <param name="onCheckFinish">检测结果回调</param>
        /// <param name="onUpdateFinish">更新完成回调</param>
        /// <param name="onDownloadProgress">下载进度回调</param
        public static void CheckUpdateFromRemote(
            string version,
            Action<BundleCheckResult> onCheckFinish,
            Action onUpdateFinish,
            Action<float> onDownloadProgress = null)
        {
            if (Instance.inChecking)
                return;
            UpdateBundle remote_bundleInfo = new UpdateBundle(EASY_DEFINE.BUNDLE_INFO_FILE, "", 0);
            remote_bundleInfo.EnableCheck(false);
            remote_bundleInfo.CombineUrl(RemoteUrlBaseVersion(version));
            Instance.onCheckFinishCB = onCheckFinish;
            Instance.onUpdateFinishCB = onUpdateFinish;
            Instance.onDownloadProgressCB = onDownloadProgress;
            BundleDownloadManager.ClearDownloadErrorHandler();
            BundleDownloadManager.AddDownloadErrorHandler(Instance.onDownloadBundleInfoError);
            BundleDownloadManager.DownloadBundle(remote_bundleInfo, Instance.onDownloadBundleInfo);
        }

        /// <summary>
        /// 从远程服务器检测是否需要更新
        /// </summary>
        /// <param name="onCheckFinish">检测结果回调</param>
        /// <param name="onUpdateFinish">更新完成回调</param>
        /// <param name="onDownloadProgress">下载进度回调</param>
        public static void CheckUpdateFromRemote(
            Action<BundleCheckResult> onCheckFinish,
            Action onUpdateFinish,
            Action<float> onDownloadProgress = null)
        {
            var buildVerion = Instance.localBundleInfo.buildVersion;
            CheckUpdateFromRemote(buildVerion, onCheckFinish, onUpdateFinish, onDownloadProgress);
        }

        #endregion

        Action onUpdateFinishCB;                        //更新完成回调
        Action<BundleCheckResult> onCheckFinishCB;      //检测完成回调
        Action<float> onDownloadProgressCB;             //下载进度回调
        void InvokeUpdateFinish()
        {
            try
            {
                onUpdateFinishCB?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        void InvokeCheckFinish(BundleCheckResult result)
        {
            try
            {
                onCheckFinishCB?.Invoke(result);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        Action<BundleDownloadRequest> onDownloadErrorCB; //下载文件失败回调
        /// <summary>
        /// 设置Bundle文件下载失败回调方法
        /// </summary>
        /// <param name="onDownloadError"></param>
        public static void SetDownloadBundleErrorHandler(Action<BundleDownloadRequest> onDownloadError)
        { Instance.onDownloadErrorCB = onDownloadError; }

        //创建下载包
        static UpdateBundle CreateDownloadBundle(string bundleName, string md5, long bundleSize)
        {
            var ub = new UpdateBundle(bundleName, md5, bundleSize);
            ub.CombineUrl(RemoteUrlBaseVersion(Instance.remoteBundleInfo.buildVersion));
            return ub;
        }

        //远程URL基地址
        static string RemoteUrlBaseVersion(string version)
        {
            string platform = "Unknow";
#if UNITY_IOS
            platform = "iOS";
#elif UNITY_ANDROID
            platform = "Android";
#endif
            return Setting.RemoteRootDomain + "/" + version + "/" + platform;
        }

        //下载bundle完成
        void OnDownloadBundleFinish()
        {
            InvokeUpdateFinish();
            onCheckFinishCB = null;
            onDownloadProgressCB = null;
            onUpdateFinishCB = null;
            inChecking = true;
        }
    }
}