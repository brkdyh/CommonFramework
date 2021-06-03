using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LitJson;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

//版本号结构
public class SaveVersion : SaveJsonData
{
    public override string GetFileName => SaveDataMgr.VERSION_FILE_NAME;

    public string saveDataVersion;

    public override T GetDefault<T>()
    {
        SaveVersion sv = new SaveVersion();
        sv.saveDataVersion = "0.0.0";
        return sv as T;
    }
}

public class SaveJsonData
{
    public virtual string GetFileName
    {
        get { return ""; }
    }

    public bool isFirstLoad { set; get; }

    public virtual T GetDefault<T>()
        where T : SaveJsonData
    {
        return default;
    }

    //是否改变
    private bool isDirty = false;
    public bool IsDitty() { return isDirty; }
    public void SetDirty() { isDirty = true; }
    public void ResetDirty() { isDirty = false; }
}

public class SaveDataMgr : MonoSingleton<SaveDataMgr>
{
    #region 路径
    public const string VERSION_FILE_NAME = "SaveVersion.json";

    const string SAVEPATH = "/Save/";
    const string BACKUPPATH = "/BackUp/";
    const string BACKUP_EXTENSION = ".backup";
    const string TEMP_EXTENSION = ".temp";

    private string ROOTPATH()
    {
        return Application.persistentDataPath + SAVEPATH;
    }

    private string FILEPATH(string name)
    {
        return Application.persistentDataPath + SAVEPATH + name;
    }

    private string BACKUPFILEPATH(string name)
    {
        return Application.persistentDataPath + BACKUPPATH + name;
    }

    void InitSaveDir()
    {
        var dir = Path.GetDirectoryName(Application.persistentDataPath + SAVEPATH);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    #endregion

    SaveDataConfig Config;

    //AES加密Key
    private string AES_KEY = "dhjkfhskajflwoxj";

#if UNITY_EDITOR
    //是否加密
    private bool isEnc = false;
#else
    private const bool isEnc = true;
#endif

    public static void Initialize(Action<string, string> onSaveVersionChange)
    {
        onSaveVersionChangeCB = onSaveVersionChange;
        _ = Instance;
    }

    #region Unity Function

    public override void Awake()
    {
        base.Awake();
        InitSaveDir();

        Config = Resources.Load<SaveDataConfig>("SaveDataConfig");
        AES_KEY = Config.AES_KSY;
#if UNITY_EDITOR
        isEnc = Config.isEnc;
#endif

        InitSaveVersion();

        JudgeSaveVersionChange(onSaveVersionChangeCB);
    }

    private void Update()
    {
        lock (saveDataDic)
        {
            foreach (var savaData in saveDataDic)
            {
                if (savaData.Value.IsDitty())
                {
                    //var now = DateTime.Now;
                    try
                    {
                        RealSaveData<SaveJsonData>(savaData.Value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("存档出错: " + ex.Message + "\n" + ex.StackTrace);
                    }
                    savaData.Value.ResetDirty();
                    //Debug.Log("Span Time = " + (DateTime.Now - now).TotalMilliseconds + " ms");   //测试存档操作耗费的时间
                }
            }
        }
    }

    #endregion

    #region 加载/存储

    //已加载的存档缓存
    Dictionary<Type, SaveJsonData> saveDataDic = new Dictionary<Type, SaveJsonData>();

    public T LoadData<T>()
        where T : SaveJsonData, new()
    {
        var dataType = typeof(T);

        if (saveDataDic.ContainsKey(dataType)) return saveDataDic[dataType] as T;
        InitSaveDir();
        var data = new T();

        if (!File.Exists(FILEPATH(data.GetFileName)))
        {//如果找不到存档文件

            //尝试寻找有无Temp文件
            if (File.Exists(FILEPATH(data.GetFileName) + TEMP_EXTENSION))
            {//读取temp文件
                File.Move(FILEPATH(data.GetFileName) + TEMP_EXTENSION, FILEPATH(data.GetFileName));
            }
            else
            {//无存档，无Temp文件，创建新的存档
                data = data.GetDefault<T>();
                data.isFirstLoad = true;
                RealSaveData<T>(data);
                saveDataDic.Add(dataType, data);
                return saveDataDic[dataType] as T;
            }
        }

        using (var sr = File.OpenText(FILEPATH(data.GetFileName)))
        {
            //Debug.Log("Load Path = " + FILEPATH);
            var json = sr.ReadToEnd();

            var head = json.Substring(0, 1);
            json = json.Substring(1, json.Length - 1);
            if (head == "1")
                json = AesDecrypt(json, AES_KEY);

            data = JsonMapper.ToObject<T>(json);
            saveDataDic.Add(dataType, data);
        }

        return saveDataDic[dataType] as T;
    }

    public T LoadData<T>(string content)
        where T : SaveJsonData, new()
    {
        var dataType = typeof(T);
        InitSaveDir();

        if (string.IsNullOrEmpty(content)
            && !saveDataDic.ContainsKey(dataType))
        {
            Debug.LogError("Null SaveData Content & There is No Data Type " + dataType);
            return null;
        }

        var json = content;
        var head = json.Substring(0, 1);
        json = json.Substring(1, content.Length - 1);
        if (head == "1")
            json = AesDecrypt(content, AES_KEY);

        var data = JsonMapper.ToObject<T>(json);

        if (saveDataDic.ContainsKey(dataType))
            saveDataDic[dataType] = data;
        else
            saveDataDic.Add(dataType, data);

        RealSaveData<T>(data);
        return saveDataDic[dataType] as T;
    }

    public void SaveData<T>()
        where T : SaveJsonData
    {
        var type = typeof(T);
        if (saveDataDic.ContainsKey(type))
        {
            saveDataDic[type].SetDirty();
        }
    }

    void RealSaveData<T>(SaveJsonData data)
        where T : SaveJsonData
    {
        RealSaveData<T>(ROOTPATH(), data);
    }

    void RealSaveData<T>(string rootPath, SaveJsonData data, string extension = "")
        where T : SaveJsonData
    {
        //Debug.Log("path = " + rootPath + (data.GetFileName) + extension);
        //var now1 = DateTime.Now;

        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);

        if (string.IsNullOrEmpty(data.GetFileName))
            return;

        string real_file_path = rootPath + (data.GetFileName) + extension;      //真实存档文件
        string temp_file_path = real_file_path + TEMP_EXTENSION;                //临时存档文件

        using (var sw = File.CreateText(temp_file_path))
        {
            var td = data as T;
            var json = JsonMapper.ToJson(td);

            if (isEnc)
                json = "1" + AesEncrypt(json, AES_KEY); //文件头增加加密标识
            else
                json = "0" + json; //文件头未加密标识

            sw.Write(json);
            sw.Flush();
        }

        //var span1 = DateTime.Now - now1;
        //var now2 = DateTime.Now;

        if (File.Exists(real_file_path))
            File.Delete(real_file_path);
        FileInfo new_file = new FileInfo(temp_file_path);
        new_file.MoveTo(real_file_path);

        //var span2 = DateTime.Now - now2;
        //Debug.Log(string.Format("写文件耗时: {0} ms,移动文件耗时: {1} ms,总耗时: {2} ms", span1.TotalMilliseconds, span2.TotalMilliseconds, (span1 + span2).TotalMilliseconds));
    }

    public static void ClearAllDataInRuntime(bool containBackUp = true)
    {
        if (Directory.Exists(Application.persistentDataPath + SAVEPATH))
            Directory.Delete(Application.persistentDataPath + SAVEPATH, true);
        if (containBackUp)
        {
            if (Directory.Exists(Application.persistentDataPath + BACKUPPATH))
                Directory.Delete(Application.persistentDataPath + BACKUPPATH, true);
        }

        Instance.saveDataDic.Clear();
    }

    #endregion

    #region 加密/解密

    /// <summary>
    /// 加密数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public string EncryptData<T>(SaveJsonData data) where T : SaveJsonData
    {
        var td = data as T;
        var json = JsonMapper.ToJson(td);

        json = "1" + AesEncrypt(json, AES_KEY); //文件头增加加密标识

        return json;
    }

    /// <summary>
    /// AES 加密
    /// </summary>
    /// <param name="str">明文</param>
    /// <param name="key">密钥</param>
    /// <returns></returns>
    private string AesEncrypt(string str, string key)
    {
        if (string.IsNullOrEmpty(str)) return null;
        var toEncryptArray = Encoding.UTF8.GetBytes(str);

        var rm = new RijndaelManaged
        {
            Key = Encoding.UTF8.GetBytes(key),
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };

        var cTransform = rm.CreateEncryptor();
        var resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        return Convert.ToBase64String(resultArray);
    }

    /// <summary>
    /// AES 解密
    /// </summary>
    /// <param name="str">密文</param>
    /// <param name="key">密钥</param>
    /// <returns></returns>
    private string AesDecrypt(string str, string key)
    {
        if (string.IsNullOrEmpty(str)) return null;
        var toEncryptArray = Convert.FromBase64String(str);

        var rm = new RijndaelManaged
        {
            Key = Encoding.UTF8.GetBytes(key),
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };

        var cTransform = rm.CreateDecryptor();
        var resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

        return Encoding.UTF8.GetString(resultArray);
    }

    #endregion

    #region 版本号

    public string SAVE_DATA_VERSION
    {
        get
        {
            if (Config.USE_UNITY_VERSION)
                return Application.version;
            else
                return Config.SAVE_DATA_VERSION;
        }
    }

    SaveVersion curVersionFile = null;

    static Action<string, string> onSaveVersionChangeCB;    //版本号变化回调

    public void InitSaveVersion()
    {
        if (!File.Exists(FILEPATH(VERSION_FILE_NAME)))
        {
            RealSaveData<SaveVersion>(GetVersion());
        }
    }

    public SaveVersion GetVersion()
    {
        if (curVersionFile != null)
            return curVersionFile;

        //备份存档版本号文件
        if (File.Exists(FILEPATH(VERSION_FILE_NAME)))
        {
            using (var sr = File.OpenText(FILEPATH(VERSION_FILE_NAME)))
            {
                //Debug.Log("Load Path = " + FILEPATH);
                var json = sr.ReadToEnd();

                var head = json.Substring(0, 1);
                json = json.Substring(1, json.Length - 1);
                if (head == "1")
                    json = AesDecrypt(json, AES_KEY);

                curVersionFile = JsonMapper.ToObject<SaveVersion>(json);
            }
        }
        else
        {
            var versionFile = new SaveVersion();
            curVersionFile = versionFile.GetDefault<SaveVersion>();
            curVersionFile.saveDataVersion = SAVE_DATA_VERSION;
        }

        return curVersionFile;
    }

    public static bool VersionLessThan(string version, string reference)
    {
        try
        {
            if (version == reference)
                return false;

            var n_vs = version.Split('.');
            var o_vs = reference.Split('.');

            if (int.Parse(n_vs[0]) > int.Parse(o_vs[0]))
                return false;


            if (int.Parse(n_vs[1]) > int.Parse(o_vs[1]))
                return false;


            if (int.Parse(n_vs[2]) > int.Parse(o_vs[2]))
                return false;

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public void JudgeSaveVersionChange(Action<string, string> onSaveVersionChangeCB)
    {
        if (!File.Exists(FILEPATH("SaveVersion.json")))
        {
            SaveVersion data = new SaveVersion();
            data = data.GetDefault<SaveVersion>();
            onSaveVersionChangeCB?.Invoke(data.saveDataVersion, SAVE_DATA_VERSION);
            data.saveDataVersion = SAVE_DATA_VERSION;
            RealSaveData<SaveVersion>(data);
            return;
        }

        using (var sr = File.OpenText(FILEPATH(VERSION_FILE_NAME)))
        {
            //Debug.Log("Load Path = " + FILEPATH);
            var json = sr.ReadToEnd();

            var head = json.Substring(0, 1);
            json = json.Substring(1, json.Length - 1);
            if (head == "1")
                json = AesDecrypt(json, AES_KEY);

            SaveVersion data = JsonMapper.ToObject<SaveVersion>(json);
            if (data.saveDataVersion != SAVE_DATA_VERSION)
            {
                sr.Close();
                onSaveVersionChangeCB?.Invoke(data.saveDataVersion, SAVE_DATA_VERSION);
                data.saveDataVersion = SAVE_DATA_VERSION;
                RealSaveData<SaveVersion>(data);
            }
        }
    }

    #endregion

    #region 备份

    /// <summary>
    /// 备份存档
    /// </summary>
    public bool BackUp()
    {
        try
        {
            var save_root = Application.persistentDataPath + BACKUPPATH;
            //先存储备份存档
            foreach (var data in saveDataDic)
            {
                RealSaveData<SaveJsonData>(save_root, data.Value, BACKUP_EXTENSION);
            }

            SaveVersion versionFile = GetVersion();
            //备份存档版本号文件
            RealSaveData<SaveJsonData>(save_root, versionFile, BACKUP_EXTENSION);

            //再Rename备份存档
            var files = Directory.GetFiles(save_root, "*.backup", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var new_file = file.Replace(".backup", "");

                //Debug.Log(file + "  =>  " + new_file);

                if (File.Exists(new_file))
                {
                    File.Delete(new_file);
                    //Debug.LogError("Delete new file => " + new_file);
                }
                FileInfo fi = new FileInfo(file);
                fi.MoveTo(new_file);
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message + "\n" + ex.StackTrace);
            return false;
        }
    }

    public bool LoadBackUp()
    {
        try
        {
            ClearAllDataInRuntime(false);

            InitSaveDir();      //初始化存档路径

            var backup_root = Application.persistentDataPath + BACKUPPATH;

            //再Rename备份存档
            var files = Directory.GetFiles(backup_root, "*.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var new_file = file.Replace("/BackUp/", "/Save/");
                File.Copy(file, new_file);
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message + "/n" + ex.Message);
            Debug.LogError("加载备份存档失败！");
            return false;
        }
    }

    #endregion

#if UNITY_EDITOR
    [MenuItem("Tools/Save/Clear SaveData", priority = 101)]
    public static void ClearSaveJsonData()
    {
        if (Directory.Exists(Application.persistentDataPath + SAVEPATH))
            Directory.Delete(Application.persistentDataPath + SAVEPATH, true);

        if (Directory.Exists(Application.persistentDataPath + BACKUPPATH))
            Directory.Delete(Application.persistentDataPath + BACKUPPATH, true);
    }

    [MenuItem("Tools/Save/Open Save Path", priority = 100)]
    public static void OpenSaveDataPath()
    {
        if (!Directory.Exists(Application.persistentDataPath + "/Save"))
            Directory.CreateDirectory(Application.persistentDataPath + "/Save");

        var ps = new Process { StartInfo = { FileName = Application.persistentDataPath + "/Save" } };
        ps.Start();
    }

    //[UnityEditor.MenuItem("Tools/Test Rsa")]
    //public static void TestRsa()
    //{
    //    var enc = Instance.Encrypt("Test!");
    //    Debug.Log(enc);
    //    Debug.Log(Instance.Decrypt(enc));
    //}
#endif
}