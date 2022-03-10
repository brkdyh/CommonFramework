using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SpriteDicing;
using CM_GIF;
using System.Text;

namespace CM_GIF
{
    //资源类型
    public enum AssetType
    {
        NoAssets,
        SingleSprite,
        Atlas,
    }

    //播放方式
    public enum MethodType
    {
        ByFrame,
        ByDelay,
    }

    //GIF配置文件
    public class GifConfigData
    {
        //文件头
        public string head;
        //原始尺寸
        public Vector2 rawSize;
        //资源类型
        public AssetType assetType;
        //播放方式
        public MethodType methodType;
        //帧数据
        public List<gifFrameData> frameDatas = new List<gifFrameData>();
        public List<gifFrameData> delayDatas = new List<gifFrameData>();
        //帧数量
        public int TotalFrameCount { get { return frameDatas.Count; } }
        //图元数量
        public int TotalPictureCount = 0;
        //帧重复数量
        public List<int> frameRepeat = new List<int>();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(head);
            sb.AppendLine(string.Format("#Set,{0},{1}", rawSize.x, rawSize.y));
            sb.AppendLine("#Mode=" + assetType);
            for (int i = 0; i < frameRepeat.Count; i++)
                sb.AppendLine("Frame_" + (i + 1) + "x" + frameRepeat[i]);
            for (int i = 0; i < delayDatas.Count; i++)
                sb.AppendLine("Delay_" + (i + 1) + "=" + delayDatas[i].dalay);
            return sb.ToString();
        }
    }

    public class gifFrameData
    {
        public int realIndex = 0;
        public Vector3 offest = Vector3.zero;
        public float dalay = 0.02f;
    }
}

public class GIFPlayer : MonoBehaviour
{
    //渲染类型
    public enum RenderType
    {
        NoRenderer,
        Image,
        RawImage,
        SpriteRenderer,
    }

    [SerializeField]
    string _gif_path;
    public string GIFPath
    {
        get { return _gif_path; }
        set
        {
            if (value != _gif_path)
            {
                _gif_path = value;
                onPathChange();
            }
        }
    }

    public GifConfigData gifConfig = null;

    public int totalFrame { get { return gifConfig.TotalFrameCount; } }                         //动画总帧数

    public int totalPictureCount { get { return gifConfig.TotalPictureCount; } }                //动画总图元数量

    public AssetType assetType = AssetType.NoAssets;                              //资源类型

    public MethodType methodType = MethodType.ByFrame;                            //播放方式

    List<Sprite> frameSprite = new List<Sprite>();                      //动画帧资源-精灵
    List<Texture> frameTextures = new List<Texture>();                  //动画帧资源-纹理
    DicedSpriteAtlas dsAtlas = null;                                    //动画帧资源-图集

    /* 设置参数 */
    public int TargetFPS = 24;                              //目标帧率
    public float playSpeed = 1f;                            //播放速率

    public bool autoPlay = false;                           //是否自动播放
    public bool loop = false;                               //是否循环播放
    public bool keepNativeRatio = true;                     //是否保持原图比例
    public bool applyOffset = true;                         //是否使用偏移值
    public bool openForceSize = false;                      //是否使用强制尺寸
    public Vector2 forcesize = Vector2.zero;                //强制尺寸

    /* 播放状态参数 */
    public bool isPlaying { get; private set; } = false;
    public bool isPause { get; private set; } = false;
    public int currentIndex { get; private set; } = 0;
    float timer = 0f;

    public Action playFinish = null;

    Image image = null;
    RawImage rawImage = null;
    SpriteRenderer spriteRenderer = null;

    public RenderType renderType = RenderType.NoRenderer;
    RenderType _lastRender = RenderType.NoRenderer;

    private void Awake()
    {
        GIFResHelper.Init();
        var m_image = GetComponent<Image>();
        if (m_image != null)
            DestroyImmediate(m_image);
        var m_rawImage = GetComponent<RawImage>();
        if (m_rawImage != null)
            DestroyImmediate(m_rawImage);
        var m_spriteRenderer = GetComponent<SpriteRenderer>();
        if (m_spriteRenderer != null)
            DestroyImmediate(m_spriteRenderer);

        _lastRender = RenderType.NoRenderer;
    }

    void SetRenderer(RenderType renderType)
    {
        bool changeRender = renderType != _lastRender;

        if (changeRender)
        {
            if (image != null)
                DestroyImmediate(image);
            if (rawImage != null)
                DestroyImmediate(rawImage);
            if (spriteRenderer != null)
                DestroyImmediate(spriteRenderer);

            if (renderType == RenderType.Image)
            {
                image = gameObject.AddComponent<Image>();
                image.enabled = false;
                image.raycastTarget = false;
                if (assetType == AssetType.Atlas)
                {//使用图集资源
                    image.type = Image.Type.Simple;
                    image.useSpriteMesh = true;
                }
            }
            else if (renderType == RenderType.RawImage)
            {
                rawImage = gameObject.AddComponent<RawImage>();
                rawImage.enabled = false;
                rawImage.raycastTarget = false;
            }
            else if(renderType == RenderType.SpriteRenderer)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.enabled = false;
            }
        }
        _lastRender = renderType;
    }

    private void Start()
    {
        onPathChange();
    }

    void onPathChange()
    {

#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying)
            return;
#endif
        if (string.IsNullOrEmpty(GIFPath))
            return;

        LoadGifConfig();

        if (renderType == RenderType.NoRenderer)
        {
            assetType = AssetType.NoAssets;
            return;
        }

        if (totalPictureCount <= 0)
        {
            assetType = AssetType.NoAssets;
            return;
        }

        frameSprite.Clear();
        frameTextures.Clear();

        assetType = GIFResHelper.CertainAssetType(GIFPath);

        if (assetType == AssetType.SingleSprite)
        {//使用原始方式加载资源
            for (int i = 1; i <= totalPictureCount; i++)
            {
                if (renderType == RenderType.Image
                    || renderType == RenderType.SpriteRenderer)
                {
#if USE_EASYASSETS
                    var sprite = AssetMaintainer.LoadAsset<Sprite>(GIFPath + "/f-" + i, gameObject);
#else
                    var sprite = Resources.Load<Sprite>(GIFPath + "/f-" + i);
#endif
                    if (sprite != null)
                        frameSprite.Add(sprite);
                    else
                    {
                        assetType = AssetType.NoAssets;
                        break;
                    }
                }
                else if (renderType == RenderType.RawImage)
                {
#if USE_EASYASSETS
                    var texture = AssetMaintainer.LoadAsset<Texture>(GIFPath + "/f-" + i, gameObject);
#else
                    var texture = Resources.Load<Texture>(GIFPath + "/f-" + i);
#endif
                    if (texture != null)
                        frameTextures.Add(texture);
                    else
                    {
                        assetType = AssetType.NoAssets;
                        break;
                    }
                }
            }
        }
        else if (assetType == AssetType.Atlas)
        {//使用图集
#if USE_EASYASSETS
            dsAtlas = AssetMaintainer.LoadAsset<DicedSpriteAtlas>(GIFPath, gameObject);
#else
            dsAtlas = Resources.Load<DicedSpriteAtlas>(GIFPath);
#endif
        }
        //设置Renderer
        SetRenderer(renderType);

        if (autoPlay)
            Play(true);
        else
        {
            timer = 0f;
            currentIndex = 0;
        }
    }

    void LoadGifConfig()
    {
        //Debug.Log("=======>  " + m_GifFileName + " begin load gif");
        var config_ta = Resources.Load<TextAsset>(GIFPath + "_config");
        if (config_ta == null)
            return;

        gifConfig = GIFResHelper.LoadGifConfig(config_ta);

        //bool raw_image_renderer = false;
        //renderType = raw_image_renderer ? RenderType.RawImage : RenderType.Image;
    }

    public void Play(bool reset = true)
    {
        if (renderType == RenderType.NoRenderer)
            return;

        isPlaying = true;
        isPause = false;
        if (isPause && !reset)
            return;

        timer = 0f;
        currentIndex = 0;

        //Debug.Log("Play at " + GIFPath + "," + isPlaying);
    }

    public void Pause()
    {
        isPause = true;
    }

    void onStop()
    {
        Debug.Log(GIFPath + "  => Stop");
        isPlaying = false;
    }

    void onPlayOnce()
    {
        try
        {
            playFinish?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public void Tick(float delta)
    {
        //Debug.Log(GIFPath + " => " + isPlaying + "," + isPause + "," + renderType);
        if (renderType == RenderType.NoRenderer)
            return;

        if (!isPlaying)
            return;

        if (isPause)
            return;

        if (methodType == MethodType.ByFrame)
            TickByFrame(delta);
        else if (methodType == MethodType.ByDelay)
            TickByDelay(delta);
    }

    void TickByFrame(float delta)
    {
        float heap = 1f / TargetFPS;
        //Debug.Log(heap);
        timer += delta;
        if (timer >= heap)
        {
            timer -= heap;
            currentIndex++;
            if (currentIndex == totalFrame)
            {
                onPlayOnce();
                if (loop)
                {
                    currentIndex = 0;
                }
                else
                {
                    onStop();
                    return;
                }
            }

            SetFrame(currentIndex);
        }
    }

    void TickByDelay(float delta)
    {
        var fd = gifConfig.delayDatas[currentIndex];
        float heap = fd.dalay / playSpeed;
        //Debug.Log(heap);
        timer += delta;
        if (timer >= heap)
        {
            timer -= heap;
            currentIndex++;
            if (currentIndex == totalPictureCount)
            {
                onPlayOnce();
                if (loop)
                {
                    currentIndex = 0;
                }
                else
                {
                    onStop();
                    return;
                }
            }

            SetFrame(currentIndex);
        }
    }

    void SetFrame(int frame)
    {
        //Debug.Log("Set Frame + " + frame + "  at " + GIFPath);
        if (renderType == RenderType.Image)
        {
            var sprite = GetSprite(frame);
            image.enabled = true;
            image.sprite = sprite;
            if (openForceSize)
                image.rectTransform.sizeDelta = forcesize;
            else if (keepNativeRatio)
                image.rectTransform.sizeDelta = sprite.rect.size;
            if (applyOffset)
                image.rectTransform.anchoredPosition3D = GetOffset(frame);
        }
        else if (renderType == RenderType.SpriteRenderer)
        {
            var sprite = GetSprite(frame);
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = sprite;
        }
        else if (renderType == RenderType.RawImage)
        {
            var texture = GetTexture(frame);
            rawImage.enabled = true;
            rawImage.texture = texture;
            if (openForceSize)
                rawImage.rectTransform.sizeDelta = forcesize;
            else if (keepNativeRatio
                && gifConfig.rawSize != Vector2.zero)
                rawImage.rectTransform.sizeDelta = gifConfig.rawSize;
            if (applyOffset)
                rawImage.rectTransform.anchoredPosition3D = GetOffset(frame);
        }
    }

    public Sprite GetSprite(int frame)
    {
        if (assetType == AssetType.NoAssets)
            return null;


        int realIdx = methodType == MethodType.ByFrame ?
            gifConfig.frameDatas[frame].realIndex : gifConfig.delayDatas[frame].realIndex;

        if (assetType == AssetType.Atlas)
        {
            var sprite = dsAtlas.GetSprite("f-" + (realIdx + 1));
            return sprite;
        }
        else
            return frameSprite[realIdx];
    }

    public Texture GetTexture(int frame)
    {
        if (assetType == AssetType.NoAssets)
            return null;

        int realIdx = methodType == MethodType.ByFrame ?
            gifConfig.frameDatas[frame].realIndex : gifConfig.delayDatas[frame].realIndex;

        return frameTextures[realIdx];
    }

    public string getSpriteName(int frame)
    {
        if (frame >= 0 && frame < totalFrame)
        {
            int realIdx = gifConfig.frameDatas[frame].realIndex;
            //Debug.Log(realIdx);
            string s_Name = "f-" + (realIdx + 1);
            return s_Name;
        }
        else
            return "Null";
    }

    public Vector3 GetOffset(int frame)
    {
        if (frame < totalFrame)
            return gifConfig.frameDatas[frame].offest;

        return Vector3.zero;
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void JumpTo(int frame)
    {
        if (frame >= 0 && frame < totalFrame)
        {
            currentIndex = frame;
            SetFrame(currentIndex);
        }
    }

    public void UnloadAsset()
    {
        if (assetType == AssetType.NoAssets)
            return;

        frameSprite.Clear();
        frameTextures.Clear();

        GIFResHelper.UnloadAsset();
        Debug.Log("Unload GIF Asset => path at " + GIFPath);
    }

    private void OnDestroy()
    {
        UnloadAsset();
    }
}