using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class SaveDataConfig : ScriptableObject
{
#if UNITY_EDITOR
    //是否加密(仅编辑器下有效)
    public bool isEnc = false;
#endif

    //AES加密密钥
    public string AES_KSY = "dhjkfhskajflwoxj";

    //存档版本号
    public string SAVE_DATA_VERSION = "0.0.1";

    //使用Unity版本
    public bool USE_UNITY_VERSION = false;

    //生成过的密钥
    public List<string> GENERATED_KEY;
}
