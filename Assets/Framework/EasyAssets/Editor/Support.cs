using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyAssets
{
    public class Support
    {
#if UNITY_EDITOR

        [UnityEditor.MenuItem("公共框架/Easy Assets/修改框架加载方式", priority = 50)]
        public static void ChangeLoadMode()
        {
            string defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Standalone);
            if (defines.Contains("USE_EASYASSETS"))
            {
                if (UnityEditor.EditorUtility.DisplayDialog("修改加载方式", "CommonFramework 正在使用 EasyAsset 加载资源，是否使用 Resources 加载?", "修改", "取消"))
                {
                    defines = defines.Replace("USE_EASYASSETS;", "");
                    defines = defines.Replace("USE_EASYASSETS", "");
                    UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Standalone, defines);
                    UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Android, defines);
                    UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.iOS, defines);
                    UnityEditor.AssetDatabase.SaveAssets();
                }
            }
            else
            {
                if (UnityEditor.EditorUtility.DisplayDialog("修改加载方式", "CommonFramework 正在使用 Resources 加载资源，是否使用 EasyAsset 加载?", "修改", "取消"))
                {
                    if (defines == "")
                        defines = defines + "USE_EASYASSETS;";
                    else if (defines.EndsWith(";"))
                        defines = defines + "USE_EASYASSETS;";
                    else
                        defines = ";USE_EASYASSETS;";
                    UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Standalone, defines);
                    UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Android, defines);
                    UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.iOS, defines);
                    UnityEditor.AssetDatabase.SaveAssets();
                }
            }
        }

#endif
    }
}
