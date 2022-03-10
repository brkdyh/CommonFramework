using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class GIFEditorHelper
{
    public static string projectRootPath
    {
        get { return Application.dataPath.Replace("Assets", ""); }
    }

    public static string getAbsPath(string path)
    {
        return projectRootPath + path;
    }

    public static string getAssetPath(string absPath)
    {
        return absPath.Replace(projectRootPath, "");
    }

    public static string getLatePathName(string path)
    {
        var sps = path.Split(Path.DirectorySeparatorChar);
        return sps[sps.Length - 1];
    }

    public static string getLastDirector(string path)
    {
        string str = "";
        var sps = path.Split(Path.DirectorySeparatorChar);
        for (int i = 0; i < sps.Length - 1; i++)
        {
            str += sps[i] + Path.DirectorySeparatorChar;
        }

        return str;
    }

    public static string GetResourcesPath(string path)
    {
        string str = "";
        bool sp = false;
        var sps = path.Split(Path.DirectorySeparatorChar);
        for (int i = 0; i < sps.Length; i++)
        {
            if (sps[i] == "Resources")
            {
                sp = true;
                continue;
            }
            if (sp)
                str += sps[i] + ((i < sps.Length - 1) ? "" + Path.DirectorySeparatorChar : "");
        }

        return str;
    }
}
