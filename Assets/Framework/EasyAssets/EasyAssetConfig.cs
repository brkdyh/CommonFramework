using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyAssets
{
    [System.Serializable]
    public class AssetExtentions
    {
        public string Type;
        public List<string> Extentions;
    }

    [CreateAssetMenu]
    public class EasyAssetConfig : ScriptableObject
    {
        /*资源管理*/
        [HideInInspector]
        public string LoadPath = "/Asset";
        [HideInInspector]
        public float RefrenceCheckTime = 1f;
        [HideInInspector]
        public float DisposeCacheTime = 5f;
        [HideInInspector]
        public float AssetBundleLiveTime = 5f;


        /*资源填充扩展名管理*/
        [SerializeField]
        public string AutoFillPathRoot = "";
        public string GetAutoFillPathRoot { get { return "Assets/" + AutoFillPathRoot; } }
        [SerializeField]
        public List<AssetExtentions> AssetExtentionsMap = new List<AssetExtentions>()
        {
            new AssetExtentions
            {
                Type = "Texture",
                Extentions = new List<string>
                {
                    ".png",".jpg",".psd",".jpeg"
                }
            },
            new AssetExtentions
            {
                Type = "Texture2D",
                Extentions = new List<string>
                {
                    ".png",".jpg",".psd",".jpeg"
                }
            },
            new AssetExtentions
            {
                Type = "TextAsset",
                Extentions = new List<string>
                {
                    ".bytes",".csv",".xml",".json",".html",".txt"
                }
            },
            new AssetExtentions
            {
                Type = "AudioClip",
                Extentions = new List<string>
                {
                    ".mp3",".wav"
                }
            },
            new AssetExtentions
            {
                Type = "GameObject",
                Extentions = new List<string>
                {
                    ".prefab"
                }
            },
        };

        [SerializeField]
        public List<string> UnmanagedBundles = new List<string>();

        /*资源下载*/
        [HideInInspector]
        public string RemoteBundleRootDomain;      //remote root domain
        [HideInInspector]
        public float RequestTimeOut = 60;       //download request time out
        [HideInInspector]
        public Setting.BundleCheckMode bundleCheckMode = Setting.BundleCheckMode.MD5;
        [HideInInspector]
        public bool OpenCompress = false;
        [HideInInspector]
        public string CompressPassword = "";
    }
}