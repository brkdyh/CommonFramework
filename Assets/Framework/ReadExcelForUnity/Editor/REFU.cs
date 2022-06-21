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
        [MenuItem("公共框架/REFU/打开REFU Window",priority = 102)]
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
                var extention = Path.GetExtension(excelPath);
                var ex_file = excelPath.Replace(extention, "");
                if (ex_file.EndsWith("__"))
                {//选择了表格分片
                    var part_idx = ex_file.IndexOf("__");
                    var main_ex_path = ex_file.Substring(0, part_idx);
                    main_ex_path = main_ex_path + extention;
                    excelPath = main_ex_path;
                }
                if (lastSelectPath != excelPath)
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
                    Dictionary<string, CodeGenerateData> gencode_map = new Dictionary<string, CodeGenerateData>();
                    foreach (var sheet in _excelWorksheet)
                    {
                        var sheet_name = sheet.Key.Trim();
                        var data_type = sheet_name;
                        if (sheet_name.Contains("#"))
                        {//解析继承类型
                            var sn_sps = sheet_name.Split('#');
                            sheet_name = sn_sps[0];
                            data_type = sn_sps[1];
                        }

                        var tfi = getFieldInfo(sheet.Value[0]);

                        string code_namespace = "";
                        bool use_namespace = refu_data.GetSourceUseNamespace(excelPath);
                        if (use_namespace)
                            code_namespace = GetFileName(excelPath);
                        //Debug.Log(data_type);
                        if (!gencode_map.ContainsKey(data_type))
                        {//添加不重复的数据类型
                            CodeGenerateData cgd = new CodeGenerateData(GetFileName(excelPath), data_type, tfi, code_namespace);
                            gencode_map.Add(data_type, cgd);
                            //Debug.Log("Add data type: " + data_type);
                        }
                        //Debug.Log(code_namespace);
                    }

                    foreach (var cgd in gencode_map.Values)
                        CodeGenerator.CreateType(cgd);
                }
            }

            GUILayout.Space(10);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("读取表格数据"))
            {
                if (!Directory.Exists(exportPath))
                    Directory.CreateDirectory(exportPath);

                ClearLoad();

                List<string> excelLoadPaths = new List<string>();
                excelLoadPaths.Add(excelPath);
                var extention = Path.GetExtension(excelPath);
                var main_ex_file = excelPath.Replace(extention, "");

                int part_idx = 1;
                var part_path = string.Format(main_ex_file + "__{0}__" + extention, part_idx);
                while (File.Exists(part_path))
                {
                    excelLoadPaths.Add(part_path);
                    part_idx++;
                    part_path = string.Format(main_ex_file + "__{0}__" + extention, part_idx);
                }

                foreach (var load_path in excelLoadPaths)
                    LoadExcel(load_path);

                foreach (var sheetsKP in _excelWorksheet)
                {
                    var sheets_list = sheetsKP.Value;
                    if (sheets_list.Count > 0)
                    {
                        var sheet = sheets_list[0];
                        if (sheet == null)
                            throw new Exception("RFFU: Find Null Sheet! Excel-File = {0}");

                        string code_namespace = "";
                        bool use_namespace = refu_data.GetSourceUseNamespace(excelPath);
                        if (use_namespace)
                            code_namespace = GetFileName(excelPath);
                        //Debug.Log(sheet + ",d = " + sheet.Dimension);
                        var tfi = getFieldInfo(sheet);

                        LoadSheet(sheets_list, tfi, code_namespace, exportPath);
                        //sheet_co.Add(sheet.Name, new SheetCollection(sheet.Name, code_namespace, tfi, this));
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                foreach (var ex_pack in _excelPackages)
                    ex_pack.Value.Dispose();
                EditorUtility.DisplayDialog("读取完成", "表格数据与读取完成，文件生成路径为 " + exportPath, "确定");
            }



            GUILayout.Space(20);

            Repaint();
        }

        Dictionary<string, ExcelPackage> _excelPackages = new Dictionary<string, ExcelPackage>();
        Dictionary<string, List<ExcelWorksheet>> _excelWorksheet = new Dictionary<string, List<ExcelWorksheet>>();

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
                    if (!_excelWorksheet.ContainsKey(sheet.Name))
                        _excelWorksheet.Add(sheet.Name, new List<ExcelWorksheet>());
                    _excelWorksheet[sheet.Name].Add(sheet);
                }
            }

            return _excelPackages[excel];
        }

        private List<ExcelWorksheet> GetWorksheet(string sheet)
        {
            if (_excelWorksheet.ContainsKey(sheet))
                return _excelWorksheet[sheet];

            return null;
        }

        TypeFieldInfo[] getFieldInfo(ExcelWorksheet sheet)
        {
            if (sheet.Dimension == null)
                return null;
            try
            {
                var col_count = sheet.Dimension.Columns;
                TypeFieldInfo[] fis = new TypeFieldInfo[col_count];

                for (int col = 1; col <= col_count; col++)
                {
                    var name = sheet.GetValue<string>(1, col);
                    //Debug.Log(name);
                    var type = sheet.GetValue<string>(2, col);
                    //Debug.Log(type);

                    if (string.IsNullOrEmpty(name)
                        || string.IsNullOrEmpty(type))
                        throw new Exception(string.Format("Sheet:{0} Field Name or Type is Null or Empty at Colume:{1},Please Check it",
                            sheet.Name, col));

                    fis[col - 1] = new TypeFieldInfo();
                    if (name.StartsWith("#"))
                    {//#号开头，识别为主键
                        name = name.Substring(1, name.Length - 1);
                        fis[col - 1].mainKey = true;
                    }
                    fis[col - 1].FieldName = name;
                    fis[col - 1].FieldType = TypeMapper.TYPE_MAPPER.ContainsKey(type) ?
                         TypeMapper.TYPE_MAPPER[type] : System.Type.GetType(type);
                    //Debug.Log("type = " + fis[col - 1].FieldType);

                }
                return fis;

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        //加载表格，反射赋值
        void LoadSheet(List<ExcelWorksheet> sheets, TypeFieldInfo[] fieldInfos,string use_namespace,string exportPath)
        {
            if (sheets == null)
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

            var main_sheet = sheets[0];
            var sheet_name = main_sheet.Name.Trim();
            var data_type = sheet_name;
            if (sheet_name.Contains("#"))
            {
                var sn_sps = sheet_name.Split('#');
                sheet_name = sn_sps[0];
                data_type = sn_sps[1];
            }

            var data = ScriptableObject.CreateInstance((string.IsNullOrEmpty(use_namespace) ? "" : use_namespace + ".") + data_type);

            if (data == null)
            {
                Debug.LogError("Can't Find Mapping Type by Sheet : " + data_type);
                return;
            }

            var type = data.GetType();


            int[] sheet_rows = new int[sheets.Count];
            for (int i = 0; i < sheets.Count; i++)
            {
                var p_sheet = sheets[i];
                if (p_sheet.Dimension == null)
                {
                    Debug.LogWarningFormat("Sheet:{0} Dimension is Null : {1}", p_sheet.Name, data_type);
                    return;
                }
                sheet_rows[i] = p_sheet.Dimension.Rows;
            }

            SheetPointer pointer = new SheetPointer(sheet_rows);
            HashSet<int> filter_lines = new HashSet<int>();                   //过滤被注释的行
            for (int pre_row = 1; pre_row <= pointer.total_count; pre_row++)
            {
                try
                {
                    var o_idx = pointer.offset_idx;
                    var ptr = pointer.offset_ptr + 1;
                    if (ptr < 3)
                    {//跳过每张表格的前两行
                        filter_lines.Add(pre_row);
                        pointer.Next();
                        continue;
                    }
                    var cur_sheet = sheets[o_idx];
                    object value = cur_sheet.GetValue(ptr, 1);
                    //Debug.Log($"{sheet_name} 读取第{pre_row}行,第{1}列,[{o_idx}]({ptr},{1}),value = {value}");
                    if (value == null)
                        filter_lines.Add(pre_row);
                    else if (value.ToString().StartsWith("!"))
                        filter_lines.Add(pre_row);

                    pointer.Next();

                }
                catch (Exception ex)
                {
                    Debug.LogError(sheet_name + " Error at row = " + pre_row);
                    Debug.LogException(ex);
                    return;
                }
            }

            for (int col = 1; col <= main_sheet.Dimension.Columns; col++)
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

                pointer.Reset();
                int index = 0;
                var row_count = pointer.total_count;
                Array array = Array.CreateInstance(field_type, row_count - filter_lines.Count);
                //Debug.Log(row_count + "," + filter_lines.Count);
                for (int row = 1; row <= row_count; row++)
                {
                    if (filter_lines.Contains(row))
                    {
                        //Debug.Log($"{sheet_name} 过滤第{row}行,第{col}列");
                        continue;
                    }
                    pointer.SetPtr(row - 1);
                    int o_idx = pointer.offset_idx;
                    var cur_sheet = sheets[o_idx];

                    int ptr = pointer.offset_ptr + 1;
                    object value = cur_sheet.GetValue(ptr, col);
                    //Debug.Log($"{sheet_name} 读取第{row}行,第{col}列,[{o_idx}]({ptr},{col}),value = {value}");
                    try
                    {
                        value = Convert.ChangeType(value, field_type);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(string.Format(cur_sheet.Name + " -> Convert Change Type Faild: at r={0},c={1},val={2}: ", ptr, col, value)
                            + ex.Message + "\n" + ex.StackTrace);
                        break;
                    }
                    array.SetValue(value, index);
                    index++;
                }

                get_field.SetValue(data, array);
            }


            var path = exportPath.Replace(Application.dataPath, "Assets/");
            if (File.Exists(path))
                File.Delete(path);

            AssetDatabase.CreateAsset(data, path + "/" + sheet_name + ".asset");
        }

        //表格分段指针
        public class SheetPointer
        {
            public int total_count { get; private set; }

            int c_ptr = 0;
            int current_ptr { get { return c_ptr; } }

            int o_ptr = 0;
            public int offset_ptr { get { return o_ptr; } }

            int o_idx = 0;
            public int offset_idx { get { return o_idx; } }

            int[] number_offset = null;
            public SheetPointer(params int[] offset)
            {
                number_offset = new int[offset.Length + 1];
                number_offset[0] = 0;
                Array.Copy(offset, 0, number_offset, 1, offset.Length);

                total_count = 0;
                foreach (var o in number_offset)
                    total_count += o;
                Reset();
            }

            public void SetPtr(int ptr)
            {
                if (c_ptr >= total_count)
                    return;
                c_ptr = ptr;
                var sum = 0;
                for (int i = 0; i < number_offset.Length; i++)
                {
                    var judge = sum + number_offset[i];
                    if (c_ptr < judge)
                    {
                        o_ptr = c_ptr - sum;
                        o_idx = i - 1;
                        break;
                    }
                    sum = judge;
                }
            }

            public void Next() { SetPtr(c_ptr + 1); }

            public void Reset()
            {
                c_ptr = 0;
                o_ptr = 0;
                o_idx = 0;
            }
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
            //using (var excel = refu.LoadExcel(excelPath))
            //{
            //    foreach (var sheet in refu._excelWorksheet)
            //    {

            //        string code_namespace = "";
            //        bool use_namespace = refu.refu_data.GetSourceUseNamespace(excelPath);
            //        if (use_namespace)
            //            code_namespace = GetFileName(excelPath);

            //        var tfi = refu.getFieldInfo(sheet.Value);
            //        refu.LoadSheet(sheet.Value, tfi, code_namespace, exportPath);
            //    }
            //}

            refu.Close();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(string.Format("读取表格 {0} 数据完成", GetFileName(excelPath)));
        }
    }

}