using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Gif2Textures;
using CM_GIF;

public class GIFImporter : EditorWindow
{
    static GIFImporter instance;
    [MenuItem("公共框架/GIF/GIF 导入管理器", priority = 201)]
    public static void OpenWindow()
    {
        GetInstance();
        instance.Show();
        instance.Focus();
    }

    public static GIFImporter GetInstance()
    {
        if (instance == null)
        {
            instance = GetWindow<GIFImporter>();
            instance.Init();
        }

        return instance;
    }

    public void Init()
    {
        minSize = new Vector2(300, 250);
        title = "GIF 导入管理器";
    }

    public class FolderData
    {
        public string assetPath;
        public string absPath;
        public string config_address = "";
        public string asset_address=  "";
        public string folderName = "";
        Texture icon;

        public FolderData(string assetPath)
        {
            this.assetPath = assetPath;
            absPath = GIFEditorHelper.getAbsPath(assetPath);
            folderName = GIFEditorHelper.getLatePathName(assetPath);
            icon = AssetDatabase.GetCachedIcon(assetPath);
        }

        public void onDrawFolderData()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("路径_" + folderName);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Box(icon, GUILayout.Width(18), GUILayout.Height(18));
            GUILayout.Space(5);
            GUILayout.Label(assetPath);
            EditorGUILayout.EndHorizontal();

            asset_address = EditorGUILayout.TextField("资源输出路径: Asset/", asset_address);
            EditorGUILayout.Space(3);
            config_address = EditorGUILayout.TextField("配置输出路径: Asset/", config_address);


            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button("导入GIF"))
            {
                var abs_in = GIFEditorHelper.getAbsPath(assetPath);
                var config_abs_out = GIFEditorHelper.getAbsPath("Assets/" + config_address + "/");
                var asset_abs_out = GIFEditorHelper.getAbsPath("Assets/" + asset_address + "/");

                ImportGif(abs_in, config_abs_out, asset_abs_out);
            }

            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            EditorGUILayout.EndVertical();
        }
    }

    public List<FolderData> folderDatas = new List<FolderData>();
    public bool ContainFolder(string assetPath)
    {
        foreach (var f in folderDatas)
        {
            if (f.assetPath == assetPath)
                return true;
        }

        return false;
    }

    Vector2 folder_scroll;

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("资源路径:");
        if (folderDatas.Count > 0)
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("清空"))
            {
                folderDatas.Clear();
            }
        }
        EditorGUILayout.EndHorizontal();
        folder_scroll = EditorGUILayout.BeginScrollView(folder_scroll, "OL Box", GUILayout.MinHeight(100), GUILayout.MaxHeight(400));
        EditorGUILayout.BeginVertical();
        foreach (var fold in folderDatas)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            fold.onDrawFolderData();
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    static void FormatFiles(string in_dir)
    {
        string[] gifFiles = Directory.GetFiles(in_dir, "*.gif", SearchOption.AllDirectories);
        foreach (var gif in gifFiles)
        {
            FileInfo fi = new FileInfo(gif);
            var new_fp = gif.Replace(".gif", ".bytes");
            if (File.Exists(new_fp))
                File.Delete(new_fp);
            fi.CopyTo(new_fp);
        }

        AssetDatabase.Refresh();
    }

    static void GenConfigByDir(string in_dir, string out_dir)
    {
        float totalSize = 0;
        int fileCount = 0;

        string[] gifFiles = Directory.GetFiles(in_dir, "*.bytes", SearchOption.AllDirectories);
        foreach (var gif in gifFiles)
        {
            string exten = Path.GetExtension(gif);
            if (exten != ".meta" && !gif.Contains("config"))
            {
                var config_file = out_dir + Path.GetFileName(gif.Replace(exten, "")) + "_config.bytes";
                if (File.Exists(config_file))
                    File.Delete(config_file);

                Debug.Log("配置文件路径 => " + config_file);

                var rs_path = (gif.Replace(Application.dataPath, "Assets/"));
                //Debug.Log(rs_path);

                TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(rs_path);

                //获取
                using (MemoryStream ms = new MemoryStream(ta.bytes))
                {
                    GifFrames gf = new GifFrames();
                    gf.Load(ms, false);
                    var width = gf.GetGIFSize().x;
                    var height = gf.GetGIFSize().y;

                    var tex = gf.CacheTextures();
                    var frameCount = gf.GetFrameCount();
                    var delayL = gf.Frame2Delay();
                    using (var sw = File.CreateText(config_file))
                    {

                        totalSize += (width * height * frameCount * 4 / (1024 * 1024));
                        sw.WriteLine("Original_Frame=" + frameCount + ",Width=" + width + ",Height=" + height + ",ARGB32 Size=" + (width * height * frameCount * 4 / (1024 * 1024)) + "MB");
                        sw.WriteLine("#Set," + width + "," + height);
                        sw.WriteLine("#Mode=" + AssetType.SingleSprite);
                        for (int i = 0; i < frameCount; i++)
                        {
                            sw.WriteLine("Frame_" + (i + 1) + "x1");
                        }

                        for (int i = 0; i < frameCount; i++)
                        {
                            sw.WriteLine("Delay_" + (i + 1) + "=" + delayL[i]);
                        }

                        sw.Flush();

                        fileCount++;
                    }
                }
            }
        }
        AssetDatabase.Refresh();
        Debug.Log("生成GIF配置文件成功," + "总计生成" + fileCount + "个文件");
        //+ "内存纹理占用估计为 " + totalSize + "MB"
    }

    static void GenFrameByDir(string in_dir,string out_dir)
    {
        string[] gifFiles = Directory.GetFiles(in_dir, "*.bytes", SearchOption.AllDirectories);
        foreach (var gif in gifFiles)
        {
            string exten = Path.GetExtension(gif);
            if (exten != ".meta" && !gif.Contains("config"))
            {
                var rs_path = (gif.Replace(Application.dataPath, "Assets/"));
                //Debug.Log(rs_path);

                TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(rs_path);

                //获取
                using (MemoryStream ms = new MemoryStream(ta.bytes))
                {
                    GifFrames gf = new GifFrames();
                    gf.Load(ms, false);
                    var textures = gf.CacheTextures();
                    //Debug.Log("texture count = " + textures.Count);
                    var frameCount = gf.GetFrameCount();

                    int idx = 1;
                    foreach (var tex in textures)
                    {
                        var f_name = Path.GetFileName(gif.Replace(exten, ""));
                        var dir = out_dir + "/" + f_name + "/";
                        var png_file = dir + "/f-" + idx + ".png";
                        idx++;

                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        if (File.Exists(png_file))
                            File.Delete(png_file);

                        var png = (tex as Texture2D).EncodeToPNG();
                        using (var sw = File.Create(png_file))
                        {
                            sw.Write(png, 0, png.Length);
                            sw.Flush();
                        }
                    }
                }
            }
        }


        AssetDatabase.Refresh();
    }

    static void ImportGif(string in_dir, string config_out_dir, string asset_out_dir)
    {
        if (!Directory.Exists(config_out_dir))
            Directory.CreateDirectory(config_out_dir);

        if (!Directory.Exists(asset_out_dir))
            Directory.CreateDirectory(asset_out_dir);

        FormatFiles(in_dir);

        GenConfigByDir(in_dir, config_out_dir);
        GenFrameByDir(in_dir, asset_out_dir);
    }

    [MenuItem("公共框架/GIF/GIF 导入管理器 %I", priority = 201)]
    public static void AddFolderPath()
    {
        var instance = GetInstance();
        var gids = Selection.assetGUIDs;
        foreach (var id in gids)
        {
            var path = AssetDatabase.GUIDToAssetPath(id);

            if (!Directory.Exists(GIFEditorHelper.getAbsPath(path)))
                continue;

            if (!instance.ContainFolder(path))
                instance.folderDatas.Add(new FolderData(path));
        }

        OpenWindow();
    }
}
