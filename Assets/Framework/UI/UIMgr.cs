using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIBase : MonoBehaviour
{
    public string Identity => GetType().FullName;

    public virtual void OnView()
    {
        gameObject.SetActive(true);
    }

    public virtual void OnDisView()
    {
        gameObject.SetActive(false);
    }

    public virtual void CloseSelf(bool destroy = false)
    {
        UIMgr.Instance.CloseUI(Identity, destroy);
    }
}

public class UIMgr : MonoSingleton<UIMgr>
{
    private const string UI_Root_Path = "Prefabs/UI/";

    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private Dictionary<string, UIBase> _UICaches = new Dictionary<string, UIBase>();
    public Dictionary<string, UIBase> UICaches
    {
        get
        {
            return _UICaches;
        }
    }
    public GameObject UIRoot;
    public RectTransform UIRoot_Rect;
    public Canvas UICanvas { get; private set; }
    public Camera UICamera { get { return UICanvas.worldCamera; } }

    public float canvasWidth;
    public float canvasHeight;

    public UIBase TopUI;



    #region Create Canvas

    /// <summary>
    /// 创建UI画布
    /// </summary>
    private Canvas CreateUICanvas(Camera camera)
    {
        GameObject uiCanvas = new GameObject("UICanvas");
        uiCanvas.layer = 5;
        Canvas canvas = uiCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        uiCanvas.AddComponent<CanvasScaler>();
        uiCanvas.AddComponent<GraphicRaycaster>();

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
        eventSystem.transform.SetParent(uiCanvas.transform);

        GameObject.DontDestroyOnLoad(uiCanvas);
        return uiCanvas.GetComponent<Canvas>();
    }

    /// <summary>
    /// 创建UI相机
    /// </summary>
    private Camera CreateUICamera()
    {
        GameObject uiCamera = new GameObject("UICamera");
        Camera camera = uiCamera.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Depth;
        camera.cullingMask = 1 << 5;
        camera.orthographic = true;
        camera.depth = 0;

        GameObject.DontDestroyOnLoad(uiCamera);
        return camera;
    }

    #endregion

    /// <summary>
    /// 设置Canvas缩放
    /// </summary>
    /// <param name="referResolution">参考的分辨率</param>
    /// <param name="isLandscape">是否为横屏</param>
    public void SetCanvasScaler(Vector2 referResolution, bool isLandscape)
    {
        CanvasScaler canvasScaler = UICanvas.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.matchWidthOrHeight = isLandscape ? 1 : 0;
        canvasScaler.referenceResolution = referResolution;
    }

    public override void Awake()
    {
        base.Awake();

        if (UIRoot == null)
            UICanvas = CreateUICanvas(CreateUICamera());

        UIRoot = UICanvas.gameObject;
        UIRoot_Rect = UIRoot.GetComponent<RectTransform>();
        //Debug.Log(UIRoot_Rect.sizeDelta);
    }

    private void Start()
    {
        canvasWidth = UIRoot.GetComponent<RectTransform>().sizeDelta.x;
        canvasHeight = UIRoot.GetComponent<RectTransform>().sizeDelta.y;
    }

    private void LateUpdate()
    {
        if (disposeDirty)
        {
            Resources.UnloadUnusedAssets();
            disposeDirty = false;
        }
    }

    public T OpenUI<T>(string uiName = "", string uiPath = "")
        where T : UIBase
    {
        T ui = default;
        if (uiName == "")
        {
            uiName = typeof(T).ToString();
        }

        var parent = UIRoot.transform;

        if (!ContainUI(uiName))
        {
#if USE_EASYASSETS
            var go = AssetMaintainer.LoadGameobject((uiPath == "" ? UI_Root_Path : uiPath) + uiName, parent);
#else
            var go = Instantiate(Resources.Load(UI_Root_Path + uiName) as GameObject, parent);
#endif
            if (go == null)
            {
                Debug.LogError("Can't Find UI by Name = " + uiName);
                return null;
            }

            ui = go.GetComponent<T>();
            _UICaches.Add(uiName, ui);
        }
        else
        {
            ui = _UICaches[uiName] as T;
        }

        if (ui == null) return ui;
        ui.transform.SetSiblingIndex(parent.childCount - 1);
        ui.OnView();
        FindTopUI();

        //UpdateUIShow(b);

        return ui;
    }

    public void CloseUI(string uiName, bool Destroy = false)
    {
        if (!ContainUI(uiName)) return;
        var ui = _UICaches[uiName];
        ui.OnDisView();

        if (Destroy)
            DisposeUI(uiName);

        FindTopUI();
    }

    bool disposeDirty = false;
    void DisposeUI(string uiName)
    {
        if (!ContainUI(uiName)) return;
        var ui = _UICaches[uiName];
        ui.OnDisView();

        _UICaches.Remove(uiName);
        GameObject.DestroyImmediate(ui.gameObject);

        disposeDirty = true;
        FindTopUI();
    }

    private void FindTopUI()
    {
        for (var i = UIRoot.transform.childCount - 1; i >= 0; i--)
        {
            if (UIRoot.transform.GetChild(i).GetComponent<UIBase>() == null ||
                !UIRoot.transform.GetChild(i).gameObject.activeSelf) continue;
            TopUI = UIRoot.transform.GetChild(i).GetComponent<UIBase>();
            break;
        }
    }

    private bool ContainUI(string uiName)
    {
        return _UICaches.ContainsKey(uiName);
    }

    public T FindUI<T>()
        where T : UIBase
    {
        return FindUI<T>("");
    }

    public T FindUI<T>(string _uiName)
       where T : UIBase
    {
        if (_uiName == "")
        {
            _uiName = typeof(T).ToString();
        }
        if (ContainUI(_uiName))
        {
            return _UICaches[_uiName] as T;
        }

        return default;
    }

    public int ComparerIndex<T1, T2>()
        where T1 : UIBase
        where T2 : UIBase
    {
        string uiName1 = typeof(T1).ToString();
        string uiName2 = typeof(T2).ToString();
        if (!ContainUI(uiName1))
        {
            return 1;
        }
        if (!ContainUI(uiName2))
        {
            return 0;
        }
        if (FindUI<T1>().transform.GetSiblingIndex() > FindUI<T2>().transform.GetSiblingIndex())
        {
            return 0;
        }
        if (FindUI<T1>().transform.GetSiblingIndex() > FindUI<T2>().transform.GetSiblingIndex())
        {
            return 1;
        }
        return -1;
    }

    public void ClearAllUI(bool destory = false)
    {
        var copy_keys = new List<string>();
        copy_keys.AddRange(UICaches.Keys);
        foreach (var ui in copy_keys)
        {
            CloseUI(ui, destory);
        }

        _UICaches.Clear();
        TopUI = null;
    }

#region Static 方法

    public static T Open<T>(string uiName = "", string uiPath = "") where T : UIBase
    {
        return Instance.OpenUI<T>(uiName, uiPath);
    }

    public static T Find<T>(string uiName = "") where T : UIBase
    {
        return Instance.FindUI<T>(uiName);
    }

    #endregion
}