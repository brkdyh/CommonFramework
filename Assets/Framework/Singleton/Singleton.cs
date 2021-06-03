public class Singleton<T>
    where T : new() {
    private static T _Instance;

    public static T Instance {
        get {
            if (_Instance != null) return _Instance;
            _Instance = new T();
            (_Instance as Singleton<T>)?.Init();

            return _Instance;
        }
    }

    protected virtual void Init() {
    }
}