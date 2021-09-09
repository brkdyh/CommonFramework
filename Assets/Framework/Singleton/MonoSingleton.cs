using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour
    where T : MonoBehaviour, new()
{
    private static T _singleton = null;

    public virtual void Awake()
    {
        if (_singleton == null)
            _singleton = this as T;
    }

    public static T Instance
    {
        get
        {
            if (_singleton != null) return _singleton;
            var go = new GameObject(typeof(T).ToString());
            GameObject.DontDestroyOnLoad(go);
            _singleton = go.AddComponent<T>();
            (_singleton as MonoSingleton<T>).onGetInstance();
            return _singleton;
        }
    }

    public static void Close() {
        var cName = typeof(T).ToString();
        var cObject = GameObject.Find(cName);
        if (cObject != null) {
            GameObject.Destroy(cObject);
        }

        _singleton = null;
    }

    protected virtual void onGetInstance()
    {

    }
}