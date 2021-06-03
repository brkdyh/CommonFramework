using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;
using System;
using System.Xml;

namespace REFU
{
    public class REFU_Data
    {
        XmlDocument data;
        public string dataPath { get { return Application.dataPath.Replace("Assets", "ProjectSettings/") + "REFU.xml"; } }

        public class GenCodeConfig
        {
            public bool use_namespace;
        }

        XmlNode genCodeConfigNode;
        Dictionary<string, GenCodeConfig> genCodeConfigs = new Dictionary<string, GenCodeConfig>(); 

        public REFU_Data()
        {
            if (!File.Exists(dataPath))
            {
                XmlDocument new_data = new XmlDocument();

                XmlElement root = new_data.CreateElement("root");
                new_data.AppendChild(root);


                var gcc = new_data.CreateElement("GenerateCodeConfig");
                root.AppendChild(gcc);

                SaveXml(new_data);
            }
        }

        void SaveXml(XmlDocument xml)
        {
            using (var sw = File.CreateText(dataPath))
            {
                //sw.Write(new_data.ToString());
                xml.Save(sw);
                sw.Flush();
            }
        }

        void SaveXml()
        {
            SaveXml(data);
        }

        public void LoadData()
        {
            string xml;
            using (var sr = File.OpenText(dataPath))
            {
                xml = sr.ReadToEnd();
            }
            data = new XmlDocument();
            data.LoadXml(xml);


            genCodeConfigs.Clear();

            var root = data.DocumentElement;

            genCodeConfigNode = root.ChildNodes[0];
            foreach (var genConfig in genCodeConfigNode.ChildNodes)
            {
                var ele = genConfig as XmlElement;
                GenCodeConfig gcc = new GenCodeConfig();
                string path = ele.GetAttribute("source_file_path");
                gcc.use_namespace = bool.Parse(ele.GetAttribute("use_namespace"));

                genCodeConfigs.Add(path, gcc);
            }
        }

        XmlElement FindGenCodeConfigNode(string sourceFilePath)
        {
            foreach (var genConfig in genCodeConfigNode.ChildNodes)
            {
                var ele = genConfig as XmlElement;
                string path = ele.GetAttribute("source_file_path");
                if (path == sourceFilePath)
                    return ele;
            }

            return null;
        }

        public void SetSourceUseNamespace(string sourceFilePath, bool useNamespace)
        {
            if (!genCodeConfigs.ContainsKey(sourceFilePath))
            {
                genCodeConfigs.Add(sourceFilePath, new GenCodeConfig());
                XmlElement ele = data.CreateElement("Content");
                ele.SetAttribute("source_file_path", sourceFilePath);
                genCodeConfigNode.AppendChild(ele);
            }

            genCodeConfigs[sourceFilePath].use_namespace = useNamespace;

            var node = FindGenCodeConfigNode(sourceFilePath);
            if (node != null)
                node.SetAttribute("use_namespace", useNamespace.ToString());

            SaveXml();
        }

        public bool GetSourceUseNamespace(string sourceFilePath)
        {
            if (genCodeConfigs.ContainsKey(sourceFilePath))
                return genCodeConfigs[sourceFilePath].use_namespace;

            return false;
        }
    }

    public class REFU : EditorWindow
    {

        static REFU instance;
        [MenuItem("REFU/Open Window")]
        public static void OpenWindow()
        {
            GetInstance();
            instance.Show();
            instance.Focus();
        }

        public static REFU GetInstance()
        {
            if (instance == null)
            {
                instance = CreateWindow<REFU>();
                instance.Init();
            }

            return instance;
        }

        static REFU_Data _refu_data;
        REFU_Data refu_data
        {
            get
            {
                if (_refu_data == null)
                {
                    _refu_data = new REFU_Data();
                    _refu_data.LoadData();
                }

                return _refu_data;
            }
        }

        string lastSelectPath;
        string excelPath;
        string exportPath;

        bool useNamespace = false;

        public static string GetFileName(string path)
        {
            return Path.GetFileName(path).Replace(Path.GetExtension(path), "");
        }

        public void Init()
        {
            minSize = new Vector2(480, 200);
            title = "REFU Window";
            exportPath = Application.dataPath + "/Resources/REFU";
            lastSelectPath = excelPath;
        }

        void onSelectExcelChange()
        {
            useNamespace = refu_data.GetSourceUseNamespace(excelPath);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("当前选择文件:");
            GUILayout.Label(Path.GetFileName(excelPath));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("选择表格文件"))
            {
                excelPath = EditorUtility.OpenFilePanel("选择Excel表格", Application.dataPath.Replace("/Assets", ""), "xlsx,xls");
                if(lastSelectPath!= excelPath)
                {
                    lastSelectPath = excelPath;
                    onSelectExcelChange();
                }
            }

            GUILayout.EndHorizontal();


            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox("请等待编译完成", MessageType.Warning);
                return;
            }

            GUILayout.Space(10);


            if (excelPath == null)
                return;

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label("当前输出路径:");
            GUILayout.Label(exportPath);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (GUILayout.Button("选择输出路径"))
            {
                exportPath = EditorUtility.OpenFolderPanel("选择路径", Application.dataPath.Replace("/Assets", ""), "");
            }
            GUILayout.Space(10);

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal("box");
            GUILayout.Space(10);

            useNamespace = GUILayout.Toggle(useNamespace, "使用命名空间");
            if (useNamespace != refu_data.GetSourceUseNamespace(excelPath))
            {
                refu_data.SetSourceUseNamespace(excelPath, useNamespace);
            }

            if (GUILayout.Button("生成数据类型"))
            {
                ClearLoad();
                using (var excel = LoadExcel(excelPath))
                {
                    foreach (var sheet in _excelWorksheet)
                    {
                        var tfi = getFieldInfo(sheet.Value);

                        string code_namespace = "";
                        bool use_namespace = refu_data.GetSourceUseNamespace(excelPath);
                        if (use_namespace)
                            code_namespace = GetFileName(excelPath);
                        //Debug.Log(code_namespace);
                        CodeGenerator.CreateType(GetFileName(excelPath), sheet.Key, tfi, code_namespace);
                    }
                }
            }

            GUILayout.Space(10);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("读取表格数据"))
            {
                if (!Directory.Exists(exportPath))
                    Directory.CreateDirectory(exportPath);

                ClearLoad();
                using (var excel = LoadExcel(excelPath))
                {
                    foreach (var sheet in _excelWorksheet)
                    {

                        string code_namespace = "";
                        bool use_namespace = refu_data.GetSourceUseNamespace(excelPath);
                        if (use_namespace)
                            code_namespace = GetFileName(excelPath);

                        var tfi = getFieldInfo(sheet.Value);
                        LoadSheet(sheet.Value, tfi, code_namespace, exportPath);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("读取完成", "表格数据与读取完成，文件生成路径为 " + exportPath, "确定");
            }



            GUILayout.Space(20);

            Repaint();
        }

        Dictionary<string, ExcelPackage> _excelPackages = new Dictionary<string, ExcelPackage>();
        Dictionary<string, ExcelWorksheet> _excelWorksheet = new Dictionary<string, ExcelWorksheet>();

        private void ClearLoad()
        {
            _excelPackages.Clear();
            _excelWorksheet.Clear();
        }

        private ExcelPackage LoadExcel(string excel)
        {
            if (!_excelPackages.ContainsKey(excel))
            {
                var excelInfo = new FileInfo(excel);
                if (!excelInfo.Exists)
                {
                    Debug.Log("该路径下找不到Excel文件： " + excel);
                    return null;
                }


                var _excel = new ExcelPackage(excelInfo);
                _excelPackages.Add(excel, _excel);

                var il = _excel.Workbook.Worksheets.GetEnumerator();
                while (il.MoveNext())
                {
                    var sheet = il.Current as ExcelWorksheet;
                    _excelWorksheet.Add(sheet.Name, sheet);
                }
            }

            return _excelPackages[excel];
        }

        private ExcelWorksheet GetWorksheet(string sheet)
        {
            if (_excelWorksheet.ContainsKey(sheet))
                return _excelWorksheet[sheet];

            return null;
        }

        TypeFieldInfo[] getFieldInfo(ExcelWorksheet sheet)
        {
            if (sheet.Dimension == null)
                return null;


            var col_count = sheet.Dimension.Columns;

            TypeFieldInfo[] fis = new TypeFieldInfo[col_count];
            for (int col = 1; col <= col_count; col++)
            {
                var name = sheet.GetValue<string>(1, col);
                //Debug.Log(name);
                var type = sheet.GetValue<string>(2, col);
                //Debug.Log(type);

                fis[col - 1] = new TypeFieldInfo();
                fis[col - 1].FieldName = name;
                fis[col - 1].FieldType = TypeMapper.TYPE_MAPPER.ContainsKey(type) ?
                     TypeMapper.TYPE_MAPPER[type] : System.Type.GetType(type);
                //Debug.Log("type = " + fis[col - 1].FieldType);
            }

            return fis;
        }

        //加载表格，反射赋值
        void LoadSheet(ExcelWorksheet sheet, TypeFieldInfo[] fieldInfos,string use_namespace,string exportPath)
        {
            if (sheet == null)
            {
                Debug.LogError("Null Sheet!");
                return;
            }

            //Debug.Log(typeof(Person).Name);
            //Debug.Log(System.Type.GetType("Person"));
            //var type = System.Type.GetType(sheet.Name);
            //if (type == null)
            //{
            //    Debug.LogError("Can't Find Mapping Type by Sheet : " + sheet.Name);
            //    return;
            //}

            var data = ScriptableObject.CreateInstance((string.IsNullOrEmpty(use_namespace) ? "" : use_namespace + ".") + sheet.Name);

            if (data == null)
            {
                Debug.LogError("Can't Find Mapping Type by Sheet : " + sheet.Name);
                return;
            }

            var type = data.GetType();
            if (sheet.Dimension == null)
            {
                Debug.LogWarning("Sheet Dimension is Null : " + sheet.Name);
                return;
            }

            for (int col = 1; col <= sheet.Dimension.Columns; col++)
            {
                var field_name = fieldInfos[col - 1].FieldName;
                var field_type = fieldInfos[col - 1].FieldType;

                if (field_type == null)
                    continue;

                var get_field = type.GetField(field_name);

                if(get_field==null)
                {
                    Debug.LogError("Type Don't Contain Field,Try Re-Generate Code!");
                    return;
                }
                //if (get_field.FieldType != field_type)
                //{
                //    Debug.LogError("Field Type Can't Map! " + sheet.Name + " : " + field_name + " " + field_type + " <=> " + get_field.FieldType);
                //}
                var row_count = sheet.Dimension.Rows;
                Array array = Array.CreateInstance(field_type, row_count - 2);
                for (int row = 3; row <= row_count; row++)
                {
                    object value = sheet.GetValue(row, col);
                    try
                    {
                        value = Convert.ChangeType(value, field_type);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Convert Change Type Faild: " + ex.Message + "\n" + ex.StackTrace);
                        break;
                    }
                    array.SetValue(value, row - 3);
                }

                get_field.SetValue(data, array);
            }


            var path = exportPath.Replace(Application.dataPath, "Assets/");
            AssetDatabase.CreateAsset(data, path + "/" + sheet.Name + ".asset");
        }


        /// <summary>
        /// 对外接口，读取表格数据
        /// </summary>
        /// <param name="excelPath">表格路径</param>
        /// <param name="exportPath">输出路径</param>
        public static void ReadExcel(string excelPath, string exportPath)
        {
            var refu = GetInstance();
            if (!Directory.Exists(exportPath))
                Directory.CreateDirectory(exportPath);

            refu.ClearLoad();
            using (var excel = refu.LoadExcel(excelPath))
            {
                foreach (var sheet in refu._excelWorksheet)
                {

                    string code_namespace = "";
                    bool use_namespace = refu.refu_data.GetSourceUseNamespace(excelPath);
                    if (use_namespace)
                        code_namespace = GetFileName(excelPath);

                    var tfi = refu.getFieldInfo(sheet.Value);
                    refu.LoadSheet(sheet.Value, tfi, code_namespace, exportPath);
                }
            }

            refu.Close();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(string.Format("读取表格 {0} 数据完成", GetFileName(excelPath)));
        }
    }

}