//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;
//using System;
//using System.Reflection;

//namespace SampleECS
//{

//    [CustomEditor(typeof(ECS_Entity))]
//    public class ECS_EntityEditor : Editor
//    {
//        ECS_Entity entity;

//        string add_component_type = "";

//        bool componentsFold = false;

//        Vector2 componentsScroll = Vector2.zero;

//        ECS_ComponentEditor _ComponentEditor = new ECS_ComponentEditor();

//        private void OnEnable()
//        {
//            //components = serializedObject.FindProperty("_Components");
//            entity = target as ECS_Entity;
//            if (string.IsNullOrEmpty(entity.entity_guid))
//                entity.entity_guid = ECS_Utils.ApplyGUID();

//            if (!EditorApplication.isPlaying)
//                entity.syncComponentsMap();
//        }

//        public override void OnInspectorGUI()
//        {
//            //base.OnInspectorGUI();

//            //serializedObject.Update();

//            var dirty = false;

//            EditorGUILayout.Space(5);
//            EditorGUILayout.LabelField("Guid", entity.entity_guid);
//            EditorGUILayout.Space(2);
//            EditorGUI.BeginChangeCheck();
//            entity.auto_register = EditorGUILayout.Toggle("自动注册", entity.auto_register);
//            if (EditorGUI.EndChangeCheck()) dirty = true;
//            EditorGUILayout.Space(10);
//            componentsFold = EditorGUILayout.Foldout(componentsFold, "组件列表: ");
//            if (componentsFold)
//            {
//                EditorGUI.BeginChangeCheck();

//                List<string> del_coms = new List<string>();

//                //for (int i = 0; i < entity._Components.Count; i++)
//                int count = 0;
//                foreach (var kp in entity.GetAllComponents())
//                {
//                    componentsScroll = EditorGUILayout.BeginScrollView(componentsScroll, "box");
//                    EditorGUILayout.BeginVertical();

//                    var com = kp.Value;
//                    EditorGUILayout.BeginHorizontal();
//                    GUILayout.Space(5);
//                    var __cp_fold = EditorGUILayout.Foldout(_ComponentEditor.GetFoldoutValue(com.guid, "__cp_fold"), "组件" + count + ":" + com.name);
//                    _ComponentEditor.SetFoldoutValue(com.guid, "__cp_fold", __cp_fold);
//                    count++;
//                    EditorGUILayout.Space();
//                    if (GUILayout.Button("删除组件"))
//                    {
//                        del_coms.Add(com.guid);
//                    }
//                    GUILayout.Space(5);
//                    EditorGUILayout.EndHorizontal();

//                    if (__cp_fold)
//                    {
//                        //显示组件属性
//                        Type com_type = entity.GetComponentType(com.guid);
//                        //Debug.Log(com_type);
//                        FieldInfo[] fields = com_type.GetFields();
//                        foreach (var fi in fields)
//                        {
//                            EditorGUILayout.BeginHorizontal();
//                            GUILayout.Space(8);
//                            //Debug.Log(fi);
//                            if (fi.FieldType.IsArray)
//                            {
//                                DrawArray(com.guid, fi, com);
//                            }
//                            else
//                            {
//                                DrawFieldInfo(fi, com);
//                            }
//                            EditorGUILayout.EndHorizontal();
//                        }
//                    }
//                    EditorGUILayout.EndVertical();
//                    EditorGUILayout.EndScrollView();
//                }

//                if (del_coms.Count > 0)
//                {
//                    foreach (var del in del_coms)
//                        entity.RemoveComponent(del);
//                }
//                dirty = EditorGUI.EndChangeCheck();
//            }

//            EditorGUILayout.Space(10);
//            add_component_type = EditorGUILayout.TextField("组件类型", add_component_type);
//            if (GUILayout.Button("添加组件"))
//            {
//                if (!string.IsNullOrEmpty(add_component_type))
//                {
//                    var new_com = ECS_Utils.CreateComponent(add_component_type);
//                    if (new_com != null)
//                    {
//                        entity.AddComponent(new_com);
//                        dirty = true;
//                    }
//                    else
//                        Debug.LogError("找不到组件类型:" + add_component_type);
//                }
//            }

//            if (dirty)
//            {
//                entity.SaveComponents();
//                EditorUtility.SetDirty(target);
//                AssetDatabase.SaveAssets();
//            }

//            Repaint();
//        }

//        //绘制字段
//        public void DrawFieldInfo(FieldInfo fi, object obj)
//        {
//            if (!fi.IsPublic)
//                return;

//            if (fi.GetCustomAttribute<HideInInspector>() != null)
//                return;

//            //Debug.Log(fi.Name);
//            var value = fi.GetValue(obj);
//            if (fi.FieldType == typeof(int))
//                fi.SetValue(obj, EditorGUILayout.IntField(fi.Name, (int)value));
//            if (fi.FieldType == typeof(Vector3))
//                fi.SetValue(obj, EditorGUILayout.Vector3Field(fi.Name, (Vector3)value));
//            if (fi.FieldType == typeof(Vector2))
//                fi.SetValue(obj, EditorGUILayout.Vector2Field(fi.Name, (Vector2)value));
//            if (fi.FieldType == typeof(float))
//                fi.SetValue(obj, EditorGUILayout.FloatField(fi.Name, (float)value));
//            if (fi.FieldType == typeof(double))
//                fi.SetValue(obj, EditorGUILayout.DoubleField(fi.Name, (double)value));
//            if (fi.FieldType == typeof(string))
//                fi.SetValue(obj, EditorGUILayout.TextField(fi.Name, (string)value));
//            if (fi.FieldType == typeof(bool))
//                fi.SetValue(obj, EditorGUILayout.Toggle(fi.Name, (bool)value));
//            if (fi.FieldType == typeof(UnityEngine.Object))
//                fi.SetValue(obj, EditorGUILayout.ObjectField(fi.Name, (UnityEngine.Object)obj, fi.FieldType));

//        }

//        //绘制数组
//        public void DrawArray(string cp_guid, FieldInfo fi, object obj)
//        {
//            //Debug.Log(fi.FieldType + "," + fi.FieldType.GetElementType());
//            var elementType = fi.FieldType.GetElementType();
//            Array array = (Array)fi.GetValue(obj);
//            EditorGUILayout.BeginVertical();
//            var fold = EditorGUILayout.Foldout(_ComponentEditor.GetFoldoutValue(cp_guid, fi.Name), fi.Name);
//            _ComponentEditor.SetFoldoutValue(cp_guid, fi.Name, fold);
//            if (fold)
//            {
//                BeginSpace(8);
//                EditorGUILayout.IntField("Size", array.Length);
//                EndSpace(8);
//                for (int i = 0; i < array.Length; i++)
//                {
//                    BeginSpace(8);
//                    DrawArrayElement(ref array, elementType, i);
//                    EndSpace(8);
//                }
//            }
//            EditorGUILayout.EndVertical();
//        }

//        //绘制数组元素
//        public void DrawArrayElement(ref Array array, Type elementType, int idx)
//        {
//            var value = array.GetValue(idx);
//            var Name = "Element" + idx.ToString();
//            if (elementType == typeof(int))
//                array.SetValue(EditorGUILayout.IntField(Name, (int)value), idx);
//            if (elementType == typeof(Vector3))
//                array.SetValue(EditorGUILayout.Vector3Field(Name, (Vector3)value), idx);
//            if (elementType == typeof(Vector2))
//                array.SetValue(EditorGUILayout.Vector2Field(Name, (Vector2)value), idx);
//            if (elementType == typeof(float))
//                array.SetValue(EditorGUILayout.FloatField(Name, (float)value), idx);
//            if (elementType == typeof(double))
//                array.SetValue(EditorGUILayout.DoubleField(Name, (double)value), idx);
//            if (elementType == typeof(string))
//                array.SetValue(EditorGUILayout.TextField(Name, (string)value), idx);
//            if (elementType == typeof(bool))
//                array.SetValue(EditorGUILayout.Toggle(Name, (bool)value), idx);
//            if (elementType == typeof(UnityEngine.Object))
//                array.SetValue(EditorGUILayout.ObjectField(Name, (UnityEngine.Object)value, elementType), idx);
//        }

//        void BeginSpace(float space)
//        {
//            EditorGUILayout.BeginHorizontal();
//            GUILayout.Space(space);
//        }

//        void EndSpace(float space)
//        {
//            GUILayout.Space(space);
//            EditorGUILayout.EndHorizontal();
//        }
//    }
//}