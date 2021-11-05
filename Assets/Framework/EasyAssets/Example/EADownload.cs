using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyAssets;

public class EADownload : MonoBehaviour
{
    float downloadProgress;

    string raw_domain;
    EasyAssetConfig config;
    private void Awake()
    {
        string local_domain = "file://" + Application.dataPath.Replace("Assets", "") + "AssetsBundles";

        config = Resources.Load<EasyAssetConfig>("EasyAssetConfig");
        raw_domain = config.RemoteBundleRootDomain;
        config.RemoteBundleRootDomain = local_domain;
        BundleCheck.CheckUpdateFromRemote(onCheckFinish, onUpdateFinish, onProgress);
    }

    void onProgress(float progress)
    {
        downloadProgress = progress;
    }

    void onCheckFinish(BundleCheckResult result)
    {
        Debug.Log("检查资源更新结果: " + result);
    }

    void onUpdateFinish()
    {
        Debug.Log("更新完成");
        AssetMaintainer.Init();
        AssetMaintainer.LoadScene("Assets/Framework/EasyAssets/Example/EAExample.unity", onLoadScene);
    }

    void onLoadScene()
    {
    }

    private void OnGUI()
    {
        GUILayout.Label("进度: " + (downloadProgress * 100).ToString("0") + "%");
        GUILayout.Label("下载速度: " + Utils.FormatBytesUnit(BundleDownloadManager.Instance.downloadSpeed) + "/s");
    }

    private void OnDestroy()
    {
        config.RemoteBundleRootDomain = raw_domain;
    }
}
