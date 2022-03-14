using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TexFormatText : MonoBehaviour
{
    public Text texMemory;


    public void Update()
    {
        texMemory.text = string.Format("Tex Memory = " + (Texture.currentTextureMemory / 1024).ToString("0.0") + "KB");
    }
}
