using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MessageSystem
{
    [CreateAssetMenu]
    public class MessageSettingObject : ScriptableObject
    {
        //工作模式
        public MessageSetting.WorkMode SysWorkMode = MessageSetting.WorkMode.Asynchronized;

        //是否开启调试
        public bool DebugMode = true;

        //是否开启Log
        public bool OpenLog = false;

//#if UNITY_EDITOR
//        public const string SettingFilePath = "/Resources/Message System Setting.asset";

//        static string _FileRealPath;
//        public static string FileRealPath
//        {
//            get
//            {
//                if (string.IsNullOrEmpty(_FileRealPath))
//                {
//                    var scp_path = System.IO.Directory.GetFiles(Application.dataPath, "MessageSystem.cs", System.IO.SearchOption.AllDirectories);
//                    var dir_path = System.IO.Path.GetDirectoryName(scp_path[0]);
//                    var file_path = dir_path + SettingFilePath;
//                    _FileRealPath = "Assets" + file_path.Replace(Application.dataPath, "");
//                }
//                return _FileRealPath;
//            }
//        }

//        static MessageSettingObject _instance;
//        public static MessageSettingObject Instance()
//        {
//            if (_instance == null)
//            {
//                var scp_path = System.IO.Directory.GetFiles(Application.dataPath, "MessageSystem.cs", System.IO.SearchOption.AllDirectories);
//                var dir_path = System.IO.Path.GetDirectoryName(scp_path[0]);
//                var file_path = dir_path + SettingFilePath;
//                var real_path = "Assets" + file_path.Replace(Application.dataPath, "");

//                if (!System.IO.File.Exists(file_path))
//                {
//                    _instance = ScriptableObject.CreateInstance<MessageSettingObject>();

//                    dir_path = dir_path + "/Resources";
//                    if (!System.IO.Directory.Exists(dir_path)) System.IO.Directory.CreateDirectory(dir_path);
//                    UnityEditor.AssetDatabase.CreateAsset(_instance, real_path);
//                    return _instance;
//                }
//            }

//            _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<MessageSettingObject>(FileRealPath);
//            return _instance;
//        }
//#endif
    }
}