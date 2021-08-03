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

        UnityWebRequest webRequest;

        public bool isDone
        {
            get
            {
                if (webRequest != null)
                {
                    //更新下载速度
                    if (Time.realtimeSinceStartup - _lastCalSpeedTime >= 1f)
                    {
                        _lastCalSpeedTime = Time.realtimeSinceStartup;
                        downloadSpeed = webRequest.downloadedBytes - _lastCacheDownloadBytes;
                        _lastCacheDownloadBytes = webRequest.downloadedBytes;
                    }

                    return webRequest.isDone;
                }

                return false;
            }
        }

        public bool isError { get { return webRequest == null ? false : webRequest.isNetworkError; } }

        public string error { get { return webRequest == null ? "Null Web Request" : webRequest.error; } }

        public float progress { get { return webRequest == null ? 0f : webRequest.downloadProgress; } }

        ulong _lastCacheDownloadBytes;
        float _lastCalSpeedTime = 0;
        public ulong downloadSpeed { get; private set; } = 0;

        public void BeginDownload()
        {
            webRequest = new UnityWebRequest(url);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SendWebRequest();
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

            currentMD5 = Utils.GetMD5(data);
            return currentMD5 == bundleMD5;
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

        public static BundleDownloadRequest CreateRequest(string bundleName, string bundleMD5, string url,bool enableCheck)
        {
            BundleDownloadRequest req = new BundleDownloadRequest();
            req.bundleName = bundleName;
            req.bundleMD5 = bundleMD5;
            req.url = url;
            req.enableCheck = enableCheck;
            return req;
        }

        public static BundleDownloadRequest CreateRequest(UpdateBundle updateBundle)
        {
            return CreateRequest(updateBundle.bundleName, updateBundle.md5, updateBundle.url, updateBundle.enableCheck);
        }
    }
}
