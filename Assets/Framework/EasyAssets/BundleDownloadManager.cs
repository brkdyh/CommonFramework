using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyAsset;
using System;
using System.IO;

public class BundleDownloadManager : MonoSingleton<BundleDownloadManager>
{
    public enum Status
    {
        Idle,
        Downloading,
        Writing,
        CheckNext,
        Pause,

        UnknowError,
    }

    [HideInInspector]
    public Status currentStatus = Status.Idle;

    BundleDownloadRequest currentRequest;

    Queue<BundleDownloadRequest> reqQueue = new Queue<BundleDownloadRequest>();

    public float downloadSpeed { get { return isDownloading ? downloadbytes / _downloadTime : 0f; } }

    public float downloadProgress { get; private set; } = 0;

    public bool isDownloading
    {
        get
        {
            return currentStatus == Status.Downloading
                || currentStatus == Status.Writing
                || currentStatus == Status.CheckNext
                || currentStatus == Status.Pause;
        }
    }

    public long totalbytes { get; private set; } = 0;

    long _lastFinishDownBytes = 0;
    long _curDownBytes = 0;
    public long downloadbytes { get { return _lastFinishDownBytes + _curDownBytes; } }

    public int currentStep { get; private set; } = 0;
    public int totalStep { get; private set; } = 1;

    float _downloadTime = 0.001f;

    public FileStream curFile;

#if UNITY_EDITOR
    public Queue<BundleDownloadRequest> finishRequests = new Queue<BundleDownloadRequest>();
#endif

    void TickDownload()
    {
        if (!isDownloading)
            return;

        if (currentStatus == Status.Pause)
            return;

        if (currentStatus == Status.Writing)
            return;

        if (currentStatus == Status.CheckNext)
        {//检测下一项
            if (reqQueue.Count <= 0)
            {//下载已经全部完成
                if (currentRequest != null)
                    currentRequest.Dispose();
                currentRequest = null;
                currentStatus = Status.Idle;

                try
                {
                    onFinishCB?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                return;
            }

            //继续下载
            currentStep++;
            currentStatus = Status.Downloading;
            currentRequest = reqQueue.Dequeue();
            currentRequest.BeginDownload();
        }

        if (currentRequest != null)
        {
            if (currentRequest.isError)
            {
                Debug.LogErrorFormat("{0} DownLoad Error ,code = {1},url = {2}",
                    currentRequest.bundleName, currentRequest.error, currentRequest.url);
                currentStatus = Status.Pause;
                try
                {//调用下载错误回调
                    onDownloadErrorCB?.Invoke(currentRequest);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                return;
            }


            if (currentRequest.isDone)
            {//当前下载已经完成
                if (!currentRequest.Check())
                {//完整性验证未通过,重新下载
                    Debug.LogErrorFormat(currentRequest.bundleName + " 下载出错，文件内容不一致。 local_md5:{0},remote_md5:{1}",
                        currentRequest.currentMD5, currentRequest.bundleMD5);
                    currentRequest.Reset();
                    return;
                }

                _lastFinishDownBytes += (long)currentRequest.downloadSize;
                _curDownBytes = 0;
                Debug.Log("完成下载: " + currentRequest.bundleName);

#if UNITY_EDITOR
                finishRequests.Enqueue(currentRequest);
#endif

                currentStatus = Status.Writing;
                var path = PathHelper.EXTERNAL_ASSET_PATH + currentRequest.bundleName;
                if (File.Exists(path))
                    File.Delete(path);

                if (!currentRequest.needDecompress)
                {//开始写入文件
                    curFile = File.Create(path);
                    curFile.BeginWrite(currentRequest.data, 0, currentRequest.data.Length, onWriteFile, null);
                    Debug.Log("开始写入文件 : " + currentRequest.bundleName + "\n at " + path);
                }
                else
                {//开始解压文件
                    string extention = Path.GetExtension(path);
                    var bundle_path = path.Replace(extention, "");
                    BundleCompress.BeginDecompress(currentRequest.data, path, bundle_path, onDecompress);
                    Debug.Log("开始解压文件 : " + currentRequest.bundleName + "\n at " + bundle_path);
                }
            }
            else
            {//更新下载进度
                _curDownBytes = (long)currentRequest.downloadSize;
                downloadProgress = (float)downloadbytes / totalbytes;
                //Debug.Log(currentRequest.bundleName + " => progress = " + downloadProgress + ", curStep = " + currentStep
                //    + ", curPro = " + currentRequest.progress + ", total step = " + totalStep + ", downBytes = " + _curDownBytes);
                try
                {
                    onProgressCB?.Invoke(downloadProgress);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            _downloadTime += Time.deltaTime;
        }
    }

    void onWriteFile(object obj)
    {
        //写入完成，释放资源
        curFile.Flush();
        curFile.Close();
        curFile = null;

        Debug.Log("完成写入文件 : " + currentRequest.bundleName);
        currentRequest.Dispose();
        currentRequest = null;

        currentStatus = Status.CheckNext;//检测下一项
    }

    void onDecompress(BundleDecompressResult result, string outPath)
    {
        if (result != BundleDecompressResult.Succeed)
        {
            Debug.LogErrorFormat("{0} Decompress Failed,error = {1} , url = {2}",
            currentRequest.bundleName, result.ToString(), currentRequest.url);
            currentRequest.SetDecompressError(result.ToString());
            currentStatus = Status.Pause;
            try
            {//调用下载错误回调
                onDownloadErrorCB?.Invoke(currentRequest);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            return;
        }

        Debug.Log("完成解压文件 : " + currentRequest.bundleName);
        currentRequest.Dispose();
        currentRequest = null;

        currentStatus = Status.CheckNext;//检测下一项
    }

    private void Update()
    {
        TickDownload();
        BundleCompress.Tick();
    }

    Action<float> onProgressCB;
    Action onFinishCB;

    Action<BundleDownloadRequest> onDownloadErrorCB;
    internal static void AddDownloadErrorHandler(Action<BundleDownloadRequest> downloadErrorCB)
    { Instance.onDownloadErrorCB += downloadErrorCB; }
    internal static void ClearDownloadErrorHandler() { Instance.onDownloadErrorCB = null; }

    private bool DownloadBundles_Internal(List<UpdateBundle> updateBundles,
        Action onFinish, Action<float> onProgress = null)
    {
        if (isDownloading)
            return false;

        totalbytes = 0;
        _lastFinishDownBytes = 0;
        _curDownBytes = 0;
        _downloadTime = 0.001f;

        reqQueue.Clear();
        foreach (var ub in updateBundles)
        {
            totalbytes += ub.bundleSize;
            var req = BundleDownloadRequest.CreateRequest(ub);
            reqQueue.Enqueue(req);
        }
        totalStep = reqQueue.Count;
        if (totalStep <= 0)
            return false;

        onFinishCB = onFinish;
        onProgressCB = onProgress;

        //开始下载
        currentStatus = Status.Downloading;
        currentRequest = reqQueue.Dequeue();
        currentRequest.BeginDownload();
        return true;
    }

    public static bool DownloadBundle(UpdateBundle bundle, Action onFinish, Action<float> onProgress = null)
    {
        return Instance.DownloadBundles_Internal(new List<UpdateBundle>() { bundle }, onFinish, onProgress);
    }

    public static bool DownloadBundles(List<UpdateBundle> updateBundles,
        Action onFinish, Action<float> onProgress = null)
    {
        return Instance.DownloadBundles_Internal(updateBundles, onFinish, onProgress);

    }

    /// <summary>
    /// 重置当前下载请求状态，可以重新开始下载。
    /// </summary>
    public static bool ResetCurrentRequest()
    {
        if (Instance.currentRequest != null)
        {
            Instance.currentRequest.Reset();
            Instance.currentStatus = Status.Downloading;
            return true;
        }

        return false;
    }

    //取消下载
    public static void CancleDownload()
    {
        Instance.currentStatus = Status.Idle;
        Instance.reqQueue.Clear();
        Instance.totalbytes = 0;
        Instance.totalStep = 1;
        Instance.currentStep = 0;
        Instance._lastFinishDownBytes = 0;
        Instance._curDownBytes = 0;
        Instance.downloadProgress = 0;

        if (Instance.curFile != null)
            Instance.curFile.Close();
        Instance.curFile = null;

        if (Instance.currentRequest != null)
            Instance.currentRequest.Dispose();
        Instance.currentRequest = null;

    }
}
