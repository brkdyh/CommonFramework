using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using EasyAsset.EditorCoroutines;
using System;

namespace EasyAsset
{
    public static class Definer
    {
        static bool inited = false;

        static string editorPath;
        static string editorGUIPath;

        public static void Inided()
        {
            if (inited)
                return;

            inited = true;

            DirectoryInfo rootDir = new DirectoryInfo(Application.dataPath);
            FileInfo[] files = rootDir.GetFiles("AssetMaintainer.cs", SearchOption.AllDirectories);
            editorPath = Path.GetDirectoryName(files[0].FullName.Replace("\\", "/").Replace(Application.dataPath, "Assets"));
            editorGUIPath = editorPath + "/GUI";

        }

        public static string EditorPath
        {
            get
            {
                Inided();
                return editorPath;
            }
        }
    }

    /// <summary>
    /// 资源数据
    /// </summary>
    public class AssetData
    {
        public enum AssetType
        {
            Scene,
            Prefab,
            Material,
            Shader,
            Audio,
            Sprite,
            Texture,
            Other,
        }
        public string assetPath = "";
        public AssetType assetType { get; private set; }
        public Texture cachedIcon
        {
            get
            {
                return AssetDatabase.GetCachedIcon(assetPath);
            }
        }

        public bool dirty { get; private set; } = false;

        string _assetBundleName;
        public string assetBundleName
        {
            get
            {
                return _assetBundleName;
            }
            set
            {
                if (_assetBundleName == value)
                    return;
                _assetBundleName = value;
                CheckDirty();
            }
        }

        string _assetBundleVariant;
        public string assetBundleVariant
        {
            get
            {
                return _assetBundleVariant;
            }
            set
            {
                if (_assetBundleVariant == value)
                    return;

                _assetBundleVariant = value;
                CheckDirty();
            }
        }

        void CheckDirty()
        {
            var ai = AssetImporter.GetAtPath(assetPath);

            dirty = !Equals(ai.assetBundleName, assetBundleName) || !Equals(ai.assetBundleVariant, assetBundleVariant);
        }

        bool Equals(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1)
                && string.IsNullOrEmpty(s2))
                return true;

            return s1 == s2;
        }

        public static AssetType Path2AssetType(string path)
        {
            string ex = Path.GetExtension(path);
            switch (ex)
            {
                case ".png":
                    return AssetType.Texture;
                case ".mat":
                    return AssetType.Material;
            }

            return AssetType.Other;
        }

        public void ApplyAssetBundleName()
        {
            var ai = AssetImporter.GetAtPath(assetPath);
            ai.assetBundleName = assetBundleName == "" ? null : assetBundleName;
            if (!string.IsNullOrEmpty(ai.assetBundleName))
                ai.assetBundleVariant = assetBundleVariant == "" ? null : assetBundleVariant;

            dirty = false;
        }

        public AssetData(string path)
        {
            assetPath = path;
            assetType = Path2AssetType(path);
            var ai = AssetImporter.GetAtPath(assetPath);
            if (ai != null)
            {
                assetBundleName = ai.assetBundleName;
                assetBundleVariant = ai.assetBundleVariant;
            }
            else
                Debug.LogError("无法导入的资源类型，路径为: " + assetPath);
        }
    }

    /// <summary>
    /// 分析数据
    /// </summary>
    public class AnalysisData
    {
        public AssetData assetData;

        Dictionary<string, int> applies = new Dictionary<string, int>();            //反向依赖
        Dictionary<string, int> dependencies = new Dictionary<string, int>();       //正向依赖

        public Dictionary<string, int> GetApplies()
        {
            return applies;
        }

        public Dictionary<string, int> GetDependencies()
        {
            return dependencies;
        }

        public int AppliesCount { get { return applies.Count; } }

        public void AddApplyCount(string assetPath)
        {
            if (assetPath == assetData.assetPath)
                return;
            //Debug.Log("add apply " + assetPath);
            if (!applies.ContainsKey(assetPath))
                applies.Add(assetPath, 1);
            else
                applies[assetPath] += 1;
        }

        public int DependencyCount { get { return dependencies.Count; } }
        public void AddDependencyCount(string assetPath)
        {
            if (assetPath == assetData.assetPath)
                return;

            if (!dependencies.ContainsKey(assetPath))
                dependencies.Add(assetPath, 1);
            else
                dependencies[assetPath] += 1;
        }

        public AnalysisData(AssetData assetData)
        {
            this.assetData = assetData;
        }
    }
}