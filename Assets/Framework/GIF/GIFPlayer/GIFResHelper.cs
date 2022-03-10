using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CM_GIF;
using System.IO;
using SpriteDicing;

public class GIFResHelper : MonoBehaviour
{
    static bool init = false;
    public static void Init()
    {
        if (init)
            return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += onPlayModeChanged;
#endif

    }

    static GIFResHelper _ins;

    public static void UnloadAsset()
    {
        if (inQuiting)
            return;

        if (_ins == null)
        {
            _ins = new GameObject("GIFResHelper").AddComponent<GIFResHelper>();
            _ins.gameObject.hideFlags = HideFlags.HideInHierarchy;
        }

        _ins.unloadDirty = true;
    }
    
    static bool inQuiting;

#if UNITY_EDITOR


    static void onPlayModeChanged(UnityEditor.PlayModeStateChange change)
    {
        if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode ||
            change == UnityEditor.PlayModeStateChange.EnteredEditMode)
        {
            inQuiting = true;
        }
    }
#endif


    private void OnDestroy()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= onPlayModeChanged;
#endif
    }

    bool unloadDirty;
    private void LateUpdate()
    {
        if (unloadDirty)
        {
            unloadDirty = false;
#if USE_EASYASSETS
            AssetMaintainer.UnloadUnusedAssets();
#else
            Resources.UnloadUnusedAssets();
#endif
        }
    }

    public static GifConfigData LoadGifConfig(TextAsset config)
    {
        using (StringReader sr = new StringReader(config.text))
        {
            GifConfigData gcd = new GifConfigData();
            gcd.head = sr.ReadLine();
            while (sr.Peek() > -1)
            {
                string line = sr.ReadLine();
                if (line.StartsWith("#Set"))
                {
                    var sps = line.Split(',');
                    if (sps.Length >= 3)
                        gcd.rawSize = new Vector2(float.Parse(sps[1]), float.Parse(sps[2]));
                    continue;
                }

                if (line.StartsWith("#Mode="))
                {
                    line = line.Replace("#Mode=", "");
                    gcd.assetType = (AssetType)System.Enum.Parse(typeof(AssetType), line);
                    continue;
                }

                if (line.StartsWith("Frame_"))
                {
                    line = line.Replace("Frame_", "");
                    var frame_str = line;
                    var offset_str = "";
                    //Debug.Log(line);
                    if (line.Contains("&"))
                    {
                        frame_str = line.Split('&')[0];
                        offset_str = line.Split('&')[1];
                        //Debug.Log(offset_str);
                    }

                    var frame_index = int.Parse(frame_str.Split('x')[0]) - 1;
                    var frame_count = int.Parse(frame_str.Split('x')[1]);
                    gcd.TotalPictureCount++;
                    var offset = Vector3.zero;
                    if (offset_str != "")
                    {
                        var ofs = offset_str.Split(',');
                        offset = new Vector3(float.Parse(ofs[0]), float.Parse(ofs[1]), float.Parse(ofs[2]));
                        //Debug.Log("offset = " + offset);
                    }

                    gcd.frameRepeat.Add(frame_count);
                    for (int i = 0; i < frame_count; i++)
                    {
                        gifFrameData gfd = new gifFrameData();
                        gfd.realIndex = frame_index;
                        gfd.offest = offset;
                        gcd.frameDatas.Add(gfd);
                        //Debug.Log("read index add " + frame_index);
                    }
                }
                else if (line.StartsWith("Delay_"))
                {
                    line = line.Replace("Delay_", "");
                    var frame_str = line;
                    var offset_str = "";
                    //Debug.Log(line);
                    if (line.Contains("&"))
                    {
                        frame_str = line.Split('&')[0];
                        offset_str = line.Split('&')[1];
                        //Debug.Log(offset_str);
                    }

                    var frame_index = int.Parse(frame_str.Split('=')[0]) - 1;
                    var frame_delay = float.Parse(frame_str.Split('=')[1]);

                    var offset = Vector3.zero;
                    if (offset_str != "")
                    {
                        var ofs = offset_str.Split(',');
                        offset = new Vector3(float.Parse(ofs[0]), float.Parse(ofs[1]), float.Parse(ofs[2]));
                        //Debug.Log("offset = " + offset);
                    }

                    gifFrameData gfd = new gifFrameData();
                    gfd.realIndex = frame_index;
                    gfd.offest = offset;
                    gfd.dalay = frame_delay;
                    gcd.delayDatas.Add(gfd);
                }
                //int frame_index = 
            }

            return gcd;
        }
        //Debug.Log("=======>  " + m_GifFileName + " end load gif");
    }

    public static AssetType CertainAssetType(string path)
    {
#if USE_EASYASSETS
        var atlas = AssetMaintainer.LoadAsset<DicedSpriteAtlas>(path, null);
        if (atlas != null)
            return AssetType.Atlas;
        else
            return AssetType.SingleSprite;
#else
        var atlas = Resources.Load<DicedSpriteAtlas>(path);
        if (atlas != null)
            return AssetType.Atlas;
        else
            return AssetType.SingleSprite;

#endif
    }
}
