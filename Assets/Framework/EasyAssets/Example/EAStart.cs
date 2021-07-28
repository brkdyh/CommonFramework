using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EAStart : MonoBehaviour
{
    private void Awake()
    {
        AssetMaintainer.Init();
        AssetMaintainer.LoadScene("Assets/Framework/EasyAssets/Example/Resources/EAExample.unity", onLoadScene);
    }

    void onLoadScene()
    {
        
    }
}
