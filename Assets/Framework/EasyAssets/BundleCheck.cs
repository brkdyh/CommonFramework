using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using EasyAsset;

namespace EasyAsset
{
    public class BundleInfo
    {
        public string buildVersion { get; private set; } = "0.0.0";

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
}

//需要更新的bundle信息
public class UpdateBundle
{
    public string bundleName { get; private set; } = "";
    public string md5 { get; private set; } = "";
    public UpdateBundle(string bundleName, string md5)
    {
        this.bundleName = bundleName;
        this.md5 = md5;
    }
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

    public BundleInfo localBundleInfo;
    public static List<UpdateBundle> GetUpdateListByLocal()
    {
        Instance.Init();
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
        using (var memory_stream = new MemoryStream(ta.bytes))
        {
            StreamReader reader = new StreamReader(memory_stream, Encoding.UTF8);
            localBundleInfo.LoadBundleInfo(reader);
        }
    }
}
