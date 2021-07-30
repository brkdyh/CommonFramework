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

        public bool isDone { get { return webRequest == null ? false : webRequest.isDone; } }

        public bool isError { get { return webRequest == null ? false : webRequest.isNetworkError; } }

        public string error { get { return webRequest == null ? "Null Web Request" : webRequest.error; } }

        public float progress { get { return webRequest == null ? 0f : webRequest.downloadProgress; } }

        public void BeginDownload()
        {
            webRequest = new UnityWebRequest(url);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SendWebRequest();
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

        public void Dispose()
        {
            if (webRequest != null)
                webRequest.Dispose();
            webRequest = null;
        }

        public static BundleDownloadRequest CreateRequest(string bundleName, string bundleMD5, string url)
        {
            BundleDownloadRequest req = new BundleDownloadRequest();
            req.bundleName = bundleName;
            req.bundleMD5 = bundleMD5;
            req.url = url;
            return req;
        }

        public static BundleDownloadRequest CreateRequest(UpdateBundle updateBundle)
        {
            return CreateRequest(updateBundle.bundleName, updateBundle.md5, updateBundle.url);
        }
    }
}
