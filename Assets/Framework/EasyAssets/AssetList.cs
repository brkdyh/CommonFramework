using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace EasyAsset
{
    /// <summary>
    /// 资源清单
    /// </summary>
    public class AssetList
    {
        //原始数据
        public string rawData { get; private set; }

        //Manifest文件名称
        public string ManifestFilename { get; private set; }

        public string BuildVersion { get; private set; }

        //资源 <----> Assebundle 映射
        Dictionary<string, string> asset2bundleMapping = new Dictionary<string, string>();

        public static AssetList CreateAssetList(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return new AssetList();

                using (var sr = File.OpenText(path))
                {
                    StringBuilder raw = new StringBuilder();
                    AssetList assetList = new AssetList();

                    raw.Append(sr.ReadLine());
                    var version_line = sr.ReadLine();
                    raw.AppendLine();
                    raw.Append(version_line);
                    assetList.BuildVersion = version_line.Split(':')[1];
                    //Debug.Log(assetList.BuildVersion);

                    var manifest_line = sr.ReadLine();
                    raw.AppendLine();
                    raw.Append(manifest_line);
                    assetList.ManifestFilename = manifest_line.Split(':')[1];
                    //Debug.Log(assetList.ManifestFilename);

                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line))
                            continue;

                        raw.AppendLine();
                        raw.Append(line);

                        var pair = line.Split(':');
                        var asset_path = pair[0];
                        var bundle_name = pair[1];
                        assetList.asset2bundleMapping.Add(asset_path, bundle_name);
                        //Debug.Log(asset_path + ":" + bundle_name);
                    }

                    return assetList;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message + "\n" + ex.StackTrace);
            }
            return new AssetList();
        }

        public string GetBundleName(string asset_path)
        {
            if (asset2bundleMapping.ContainsKey(asset_path))
                return asset2bundleMapping[asset_path];

            return "null";
        }
    }
}