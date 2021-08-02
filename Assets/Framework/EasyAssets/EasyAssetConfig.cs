using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyAsset
{
    [CreateAssetMenu]
    public class EasyAssetConfig : ScriptableObject
    {
        [HideInInspector]
        public string LoadPath = "/Asset";
        [HideInInspector]
        public float RefrenceCheckTime = 1f;
        [HideInInspector]
        public float DisposeCacheTime = 5f;
        [HideInInspector]
        public float AssetBundleLiveTime = 5f;
        //[HideInInspector]
        //public string RemoteBundleInfoUrl;
        [HideInInspector]
        public string RemoteBundleRootUrl;
    }
}