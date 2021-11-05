using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EasyAssets;

public class EAExampleEditor
{
    [MenuItem("公共框架/Easy Assets/构建示例Bundle")]
    public static void OpenExampleWindow()
    {
        BundleBuilder.OpenWindow();
        BundleBuilder.GetInstance().buildRootPath = Application.dataPath.Replace("Assets", "AssetsBundles");
    }
}
