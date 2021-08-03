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
        public string buildVersion { get; private set; } = "0.0.0.0";

        //bundle name <=> md5 映射
        public Dictionary<string, string> bundles = new Dictionary<string, string>();

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
                    var sps = line.Split(':');
                    var bundleName = sps[0];
                    var md5 = sps[1];

                    if (!bundles.ContainsKey(bundleName))
                        bundles.Add(bundleName, md5);
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

        public UpdateBundle(string bundleName, string md5)
        {
            this.bundleName = bundleName;
            this.md5 = md5;
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
        TimeOut,
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
                return null;

            var bundleInfo = new BundleInfo();

            using (var memory_stream = new MemoryStream(data))
            {
                StreamReader reader = new StreamReader(memory_stream, Encoding.UTF8);
                bundleInfo.LoadBundleInfo(reader);
            }

            return bundleInfo;
        }

        #region 本地

        public BundleInfo localBundleInfo;
        void LoadLocalBundleInfo()
        {
            localBundleInfo = new BundleInfo();

            string localBundleInfoPath = PathHelper.EXTERNAL_ASSET_PATH + EASY_DEFINE.BUNDLE_INFO_FILE;
            if (File.Exists(localBundleInfoPath))
            {
                localBundleInfo.LoadBundleInfo(File.OpenText(localBundleInfoPath));
                return;
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
                {
                    list.Add(new UpdateBundle(bundle.Key, bundle.Value));
                    Debug.LogFormat("Need Update Bundle (name = {0},ms5 = {1})", bundle.Key, bundle.Value);
                }
            }
            return list;
        }



        #endregion

        #region 远程

        public BundleInfo remoteBundleInfo;
        Action onUpdateFinishCB;
        Action<BundleCheckResult> onCheckFinishCB;
        Action<float> onDownloadProgressCB;

        void LoadRemoteBundleInfo(byte[] data)
        {
            remoteBundleInfo = LoadBundleInfo(data);
        }

        //远程 bundle info 下载完成
        void onDownloadBundleInfo()
        {
            //var req = downloads[EASY_DEFINE.BUNDLE_INFO_FILE];
            var data = File.ReadAllBytes(PathHelper.EXTERNAL_ASSET_PATH + EASY_DEFINE.BUNDLE_INFO_FILE);
            LoadRemoteBundleInfo(data);
            if (remoteBundleInfo == null)
            {
                Debug.LogError("无法获取远程服务器上的BundleInfo,该文件内容为空!");
                onCheckFinishCB?.Invoke(BundleCheckResult.DownloadError);
                onUpdateFinishCB?.Invoke();
                return;
            }

            var updateList = GetUpdateListByRemote();
            if (updateList.Count <= 0)
            {
                try
                {
                    onCheckFinishCB?.Invoke(BundleCheckResult.Succeed);
                    Debug.Log("当前无需更新");
                    onUpdateFinishCB?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                return;
            }

            try
            {
                onCheckFinishCB?.Invoke(BundleCheckResult.Succeed);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            BundleDownloadManager.DownloadBundles(updateList, OnDownloadBundleFinish, onDownloadProgressCB);
        }

        static UpdateBundle CreateDownloadBundle(string bundleName, string md5)
        {
            var ub = new UpdateBundle(bundleName, md5);
            ub.CombineUrl(RemoteUrlBaseVersion(Instance.remoteBundleInfo.buildVersion));
            return ub;
        }

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
                    updateBundles.Add(CreateDownloadBundle(rm_bd.Key, rm_bd.Value));
                    continue;
                }

                if (local_info.bundles.ContainsKey(rm_bd.Key))
                {//若已有文件记录，则比对文件md5值，不一样的加入更新列表
                    var local_md5 = local_info.bundles[rm_bd.Key];
                    if (local_md5 != rm_bd.Value)
                        updateBundles.Add(CreateDownloadBundle(rm_bd.Key, rm_bd.Value));
                }
                else
                {//若无文件记录,检测本地有无该文件
                    if (File.Exists(local_file_path))
                    {//若本地存在文件，比对一下md5值
                        var local_md5 = Utils.GetMD5(local_file_path);
                        if (local_md5 != rm_bd.Value)
                            updateBundles.Add(CreateDownloadBundle(rm_bd.Key, rm_bd.Value));
                    }
                }
            }

            return updateBundles;
        }

        public static void CheckUpdateFromRemote(
            string version,
            Action<BundleCheckResult> onCheckFinish,
            Action onUpdateFinish,
            Action<float> onDownloadProgress = null)
        {
            UpdateBundle remote_bundleInfo = new UpdateBundle(EASY_DEFINE.BUNDLE_INFO_FILE, "");
            remote_bundleInfo.EnableCheck(false);
            remote_bundleInfo.CombineUrl(RemoteUrlBaseVersion(version));
            Instance.onCheckFinishCB = onCheckFinish;
            Instance.onUpdateFinishCB = onUpdateFinish;
            Instance.onDownloadProgressCB = onDownloadProgress;
            BundleDownloadManager.DownloadBundle(remote_bundleInfo, Instance.onDownloadBundleInfo);
        }

        public static void CheckUpdateFromRemote(
            Action<BundleCheckResult> onCheckFinish,
            Action onUpdateFinish,
            Action<float> onDownloadProgress = null)
        {
            var buildVerion = Instance.localBundleInfo.buildVersion;
            CheckUpdateFromRemote(buildVerion, onCheckFinish, onUpdateFinish, onDownloadProgress);
        }

        static string RemoteUrlBaseVersion(string version)
        {
            return Setting.RemoteRootDomain + "/" + version;
        }

        void OnDownloadBundleFinish()
        {
            //Debug.Log("OnDownloadBundleFinish , Total Count = " + downloads.Count);
            //StartWriteFiles(downloads.Values.GetEnumerator());
            try
            {
                onUpdateFinishCB?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        #endregion

        //#region 文件写入

        //void StartWriteFiles(IEnumerator<BundleDownloadRequest> requests)
        //{
        //    if (writeStart)
        //        return;

        //    //将下载文件写入本地
        //    WriteFiles.Clear();
        //    while (requests.MoveNext())
        //    {
        //        WriteFiles.Push(requests.Current);
        //        Debug.Log("添加到文件写入列表: " + requests.Current.bundleName);
        //    }

        //    writeStart = true;
        //}

        //void TickWrite()
        //{
        //    if (!writeStart)
        //        return;

        //    if (inFileWriting)
        //        return;

        //    if (WriteFiles.Count <= 0)
        //    {
        //        OnWriteAllFile();
        //        return;
        //    }

        //    BeginWriteFile();
        //}

        ////开始写入文件
        //void BeginWriteFile()
        //{
        //    if (WriteFiles.Count > 0)
        //    {
        //        try
        //        {
        //            inFileWriting = true;
        //            curWriteReq = WriteFiles.Pop();
        //            var path = PathHelper.EXTERNAL_ASSET_PATH + curWriteReq.bundleName;
        //            if (File.Exists(path))
        //                File.Delete(path);
        //            curFile = File.Create(path);
        //            curFile.BeginWrite(curWriteReq.data, 0, curWriteReq.data.Length, onWriteFile, null);
        //            Debug.Log("开始写入 " + path);
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.LogException(ex);
        //        }
        //    }
        //}

        ////完成文件写入
        //void onWriteFile(object obj)
        //{
        //    Debug.Log("完成写入 " + curWriteReq.bundleName);
        //    curFile.Flush();
        //    curFile.Close();
        //    curFile = null;
        //    curWriteReq.Dispose();
        //    curWriteReq = null;

        //    inFileWriting = false;

        //}

        ////完成全部文件写入
        //void OnWriteAllFile()
        //{
        //    writeStart = false;

        //    try
        //    {
        //        onUpdateFinishCB?.Invoke();
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.LogException(ex);
        //    }
        //}

        //#endregion

        //private void Update()
        //{
        //    TickWrite();
        //}
    }
}