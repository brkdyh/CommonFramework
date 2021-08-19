using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EAExample : MonoBehaviour
{
    public Material mat;
    public Texture texture;

    private void Awake()
    {
        mat = AssetMaintainer.LoadAsset<Material>("assets/framework/easyassets/example/resources/mat.mat", this);
        texture = AssetMaintainer.LoadAsset<Texture>("assets/framework/easyassets/example/resources/tex.png", gameObject);

        //加载外部GameObject-- - 方式1
        AssetMaintainer.LoadGameobject("assets/framework/easyassets/example/resources/cube.prefab").name = "External Cube 1.1";
        AssetMaintainer.LoadGameobject("assets/framework/easyassets/example/resources/cube.prefab").name = "External Cube 1.2";

        //加载外部GameObject---方式2
        var temp = AssetMaintainer.LoadAsset<GameObject>("assets/framework/easyassets/example/resources/cube.prefab", null);
        var new_go = Instantiate(temp);
        new_go.name = "External Cube 2";
        AssetMaintainer.TrackingAsset(temp, new_go);

        ////加载内部GameObject
        //AssetMaintainer.LoadGameobject("Cube").name = "Internal Cube";

        ////异步加载内部GameObject
        //AssetMaintainer.LoadAssetAsync<GameObject>("Sphere", null, onLoadSphere0);

        //异步加载资源
        AssetMaintainer.LoadAssetAsync<GameObject>("assets/framework/easyassets/example/resources/sphere.prefab", null, onLoadSphere1);

        //异步加载Gameobject
        AssetMaintainer.LoadGameobjectAsync("assets/framework/easyassets/example/resources/sphere.prefab", onLoadSphere2);

    }

    void onLoadSphere0(GameObject temp)
    {

    }

    void onLoadSphere1(GameObject temp)
    {
        var sphere = Instantiate(temp);
        sphere.name = "Async External Sphere 1";
        AssetMaintainer.TrackingAsset(temp, sphere);
    }


    void onLoadSphere2(GameObject sphere)
    {
        sphere.name = "Async External Sphere 2";
    }

    private void OnGUI()
    {
        if (GUILayout.Button("切换场景"))
        {
            AssetMaintainer.LoadScene("Assets/Framework/EasyAssets/Example/EANull.unity", null);
        }
    }
}
