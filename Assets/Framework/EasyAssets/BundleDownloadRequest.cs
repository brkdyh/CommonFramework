using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyAsset
{
    public class BundleDownloadRequest
    {
        private BundleDownloadRequest() { }

        public string bundleName { get; private set; }
        public string bundleMD5 { get; private set; }
        public string url { get; private set; }
        public long bundleSize { get; private set; }

        UnityWebRequest webRequest;

        public bool isDone
        {
            get
            {
                if (webRequest != null)
                {
                    //Debug.Log(bundleName + ", down_bytes = " + webRequest.downloadedBytes + ", progress = " + webRequest.downloadProgress);
                    //更新下载速度
                    if (Time.realtimeSinceStartup - _lastCalSpeedTime >= calculateFrequence)
                    {
                        if (_lastCacheDownloadBytes == webRequest.downloadedBytes)
                            _timeOutCounter += calculateFrequence;
                        else
                            _timeOutCounter = 0;

                        if (_timeOutCounter >= Setting.RequestTimeOut)
                            isTimeOut = true;           //下载超时了

                        _lastCalSpeedTime = Time.realtimeSinceStartup;
                        //var ratio = 1 / calculateFrequence;
                        //downloadSpeed = (webRequest.downloadedBytes - _lastCacheDownloadBytes) * ratio;
                        //_lastCacheDownloadBytes = webRequest.downloadedBytes;
                        //Debug.Log(bundleName + " downloading speed = " + downloadSpeed + ", ratio = " + ratio);
                    }

                    //if (webRequest.isDone)
                    //{
                    //    var downloadSpendTime = Time.realtimeSinceStartup - beginDownloadTime;
                    //    downloadSpeed = webRequest.downloadedBytes / downloadSpendTime;
                    //    Debug.Log(bundleName + " is done speed = " + downloadSpeed);
                    //}

                    return webRequest.isDone;
                }

                return false;
            }
        }

        public bool isError
        {
            get
            {
                if (isTimeOut)
                    return true;
                return webRequest == null ? false : webRequest.isNetworkError || webRequest.isHttpError;
            }
        }

        public string error
        {
            get
            {
                if (isTimeOut)
                    return "Request Time Out";
                return webRequest == null ? "Null Web Request" : webRequest.error;
            }
        }

        public float progress { get { return webRequest == null ? 0f : webRequest.downloadProgress; } }

        public ulong downloadSize { get { return webRequest == null ? 0 : webRequest.downloadedBytes; } }

        public ulong _lastCacheDownloadBytes { get; private set; }
        float _lastCalSpeedTime = 0;
        float calculateFrequence = 1f;
        //public float downloadSpeed { get; private set; } = 0;

        float _timeOutCounter = 0;
        bool isTimeOut = false; //是否超时

        public System.DateTime beginDownloadTime { get; private set; }        //开始下载时间

        public void BeginDownload()
        {
            _timeOutCounter = 0;
            isTimeOut = false;
            webRequest = new UnityWebRequest(url);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SendWebRequest();
            beginDownloadTime = System.DateTime.Now;
            Debug.Log("开始下载: " + bundleName + "\n" + url);
        }

        public byte[] data
        {
            get
            {
                if (webRequest == null)
                    return null;
                if (!webRequest.isDone)
                    return null;

                return webRequest.downloadHandler.data;
            }
        }

        bool enableCheck = true;
        public string currentMD5 { get; private set; } = "";
        //检查完整性
        public bool Check()
        {
            if (!enableCheck)
                return true;

            if (Setting.bundleCheckMode == Setting.BundleCheckMode.MD5)
            {
                currentMD5 = Utils.GetMD5(data);
                return currentMD5 == bundleMD5;
            }
            else
            {
                return data.Length == bundleSize;
            }
        }

        public void Dispose()
        {
            if (webRequest != null)
                webRequest.Dispose();
            webRequest = null;
        }

        public void Reset()
        {
            Dispose();
            BeginDownload();
        }

        public static BundleDownloadRequest CreateRequest(string bundleName, string bundleMD5, string url, long bundleSize, bool enableCheck)
        {
            BundleDownloadRequest req = new BundleDownloadRequest();
            req.bundleName = bundleName;
            req.bundleMD5 = bundleMD5;
            req.url = url;
            req.bundleSize = bundleSize;
            req.enableCheck = enableCheck;
            return req;
        }

        public static BundleDownloadRequest CreateRequest(UpdateBundle updateBundle)
        {
            return CreateRequest(updateBundle.bundleName, updateBundle.md5, updateBundle.url, updateBundle.bundleSize, updateBundle.enableCheck);
        }
    }
}
