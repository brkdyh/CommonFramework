using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyAsset;
using System;

public class BundleDownloadManager : MonoSingleton<BundleDownloadManager>
{
    BundleDownloadRequest currentRequest;

    Queue<BundleDownloadRequest> reqQueue = new Queue<BundleDownloadRequest>();

    Dictionary<string, BundleDownloadRequest> finishRequest = new Dictionary<string, BundleDownloadRequest>();

    public float downloadProgress { get; private set; } = 0;

    public bool isDownloading { get; private set; } = false;

    public bool isPause { get; private set; } = false;

    public int currentStep { get; private set; } = 0;
    public int totalStep { get; private set; } = 1;

    void TickDownload()
    {
        if (!isDownloading)
            return;

        if (isPause)
            return;

        if (currentRequest != null)
        {
            if(currentRequest.isError)
            {
                Debug.LogErrorFormat("{0} DownLoad Error ,code = {1},url = {2}",
                    currentRequest.bundleName, currentRequest.error, currentRequest.url);

                isPause = true;
                return;
            }

            if (currentRequest.isDone)
            {//当前下载已经完成
                if (!finishRequest.ContainsKey(currentRequest.bundleName))
                    finishRequest.Add(currentRequest.bundleName, currentRequest);

                if (reqQueue.Count <= 0)
                {//下载已经全部完成
                    currentRequest = null;
                    isDownloading = false;

                    try
                    {
                        onFinishCB?.Invoke(finishRequest);
                    }
                    catch(Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                    //废弃所有网络请求,释放内存
                    foreach(var req in finishRequest)
                    {
                        req.Value.Dispose();
                    }
                    finishRequest.Clear();
                    return;
                }

                currentStep++;
                currentRequest = reqQueue.Dequeue();
                currentRequest.BeginDownload();
            }
            else
            {//更新下载进度
                downloadProgress = (currentStep + currentRequest.progress) / totalStep;
                try
                {
                    onProgressCB?.Invoke(downloadProgress);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }

    private void Update()
    {
        TickDownload();
    }

    Action<float> onProgressCB;
    Action<Dictionary<string, BundleDownloadRequest>> onFinishCB;

    public bool DownloadBundles(List<UpdateBundle> updateBundles,
        Action<Dictionary<string, BundleDownloadRequest>> onFinish, Action<float> onProgress = null)
    {
        if (isDownloading)
            return false;

        reqQueue.Clear();
        foreach (var ub in updateBundles)
        {
            var req = BundleDownloadRequest.CreateRequest(ub);
            reqQueue.Enqueue(req);
        }
        totalStep = reqQueue.Count;
        if (totalStep <= 0)
            return false;

        onFinishCB = onFinish;
        onProgressCB = onProgress;
        isDownloading = true;
        isPause = false;

        //开始下载
        currentRequest = reqQueue.Dequeue();
        currentRequest.BeginDownload();
        return true;
    }
}
