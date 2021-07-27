using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class EasyAssetConfig : ScriptableObject
{
    [HideInInspector]
    public string LoadPath = "/Asset";
    [HideInInspector]
    public float RefrenceCheckTime = 1f;
    [HideInInspector]
    public float DisposeCacheTime = 5f;
}
