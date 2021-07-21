using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EAExample : MonoBehaviour
{
    private void Awake()
    {
        AssetMaintainer.Init();

        var go = AssetMaintainer.LoadAsset<GameObject>("assets/framework/easyassets/resources/cube.prefab", this);
        Instantiate(go);
    }
}
