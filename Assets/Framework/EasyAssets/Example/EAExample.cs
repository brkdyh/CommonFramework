using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EAExample : MonoBehaviour
{
    private void Awake()
    {
        AssetMaintainer.Init();

        //加载外部GameObject---方式1
        AssetMaintainer.LoadGameobject("assets/framework/easyassets/resources/cube.prefab").name = "External Cube 1.1";
        AssetMaintainer.LoadGameobject("assets/framework/easyassets/resources/cube.prefab").name = "External Cube 1.2";

        //加载外部GameObject---方式2
        var temp = AssetMaintainer.LoadAsset<GameObject>("assets/framework/easyassets/resources/cube.prefab", null);
        var new_go = Instantiate(temp);
        new_go.name = "External Cube 2";
        AssetMaintainer.TrackingAsset(temp, new_go);


        //加载内部GameObject
        AssetMaintainer.LoadGameobject("Cube").name = "Internal Cube";
    }
}
