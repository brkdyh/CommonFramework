using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyAsset
{
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
        //[HideInInspector]
        //public string RemoteBundleInfoUrl;

        /*资源下载*/
        [HideInInspector]
        public string RemoteBundleRootDomain;      //remote root domain
        [HideInInspector]
        public float RequestTimeOut = 60;       //download request time out
    }
}