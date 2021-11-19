using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SampleECS
{
    public class ECS_Utils
    {
#if UNITY_EDITOR
        //[MenuItem("公共框架/Sample ECS/复制Entity %D")]
        //public static void DuplicatingEntity()
        //{
        //    var obj = Selection.activeObject;

        //    if (obj == null
        //        || !(obj is GameObject))
        //        return;

        //    var go = obj as GameObject;
        //    if (go.GetComponent<ECS_Entity>() != null)
        //    {
        //        var new_entity_go = GameObject.Instantiate(go);
        //        new_entity_go.name = go.name;
        //        ECS_Entity new_entity = new_entity_go.GetComponent<ECS_Entity>();
        //        new_entity.entity_uid = ApplyUID();
        //        EditorUtility.SetDirty(new_entity_go);
        //    }
        //}

        //public static ECS_Component CreateComponent(string type)
        //{
        //    try
        //    {
        //        Type add_type = Type.GetType(type, true);
        //        if (type != null)
        //        {
        //            object com = Activator.CreateInstance(add_type);
        //            string guid = ApplyGUID();
        //            add_type.GetField("guid").SetValue(com, guid);
        //            add_type.GetField("name").SetValue(com, add_type.ToString());
        //            //add_type.GetMethod("setHostEntity").Invoke(com, new object[] { host });
        //            add_type.GetField("type").SetValue(com, type);
        //            return com as ECS_Component;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //    }

        //    return null;
        //}
#endif
        const uint UID_OFFSET = 0xF0000000;
        static uint UID_COUNTER = UID_OFFSET;
        public static uint ApplyUID() { return UID_COUNTER++; }


        /// <summary>
        /// 获取自定义属性类型数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static CustomAttributeData GetAttributeData<T>(object obj)
            where T : Attribute
        {
            var type = obj.GetType();
            return GetAttributeData<T>(type);
        }

        /// <summary>
        /// 获取自定义属性类型数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static CustomAttributeData GetAttributeData<T>(Type type)
            where T : Attribute
        {
            var get_type = typeof(T);
            var ats = type.GetCustomAttributesData();
            foreach (var a in ats)
            {
                if (a.AttributeType == get_type)
                {
                    return a;
                }
            }

            return null;
        }

        /// <summary>
        /// 类型是否有自定义属性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool HasAttribute<T>(Type type)
            where T : Attribute
        {
            return GetAttributeData<T>(type) != null;
        }

        /// <summary>
        /// 获取自定义属性值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="memeberName"></param>
        /// <param name="attributeData"></param>
        /// <returns></returns>
        public static T GetAttributeMemberValue<T>(string memeberName, CustomAttributeData attributeData)
        {
            foreach (var na in attributeData.NamedArguments)
            {
                if (na.MemberName == memeberName)
                    return (T)na.TypedValue.Value;
            }

            return default;
        }

        public static void SetArrayElement<T>(ref T[] array, int index, T element, byte extendRate = 2)
        {
            if (index >= array.Length)
            {
                var new_size = (array.Length == 0 ? 1 : array.Length) * extendRate;
                Array.Resize<T>(ref array, new_size);
                SetArrayElement<T>(ref array, index, element, extendRate);
                return;
            }

            array[index] = element;
        }

        public static bool DelArrayElement<T>(ref T[] array, int index)
        {
            int len = array.Length;
            if (index < 0 || index >= len)
                return false;

            if (len <= 1)
            {
                array = null;
                return true;
            }

            T[] new_array = new T[array.Length - 1];
            if (index == 0)
                Array.Copy(array, 1, new_array, 0, new_array.Length);
            else if (index == array.Length - 1)
                Array.Copy(array, 0, new_array, 0, new_array.Length);
            else
            {//重组数组
                Array.Copy(array, 0, new_array, 0, index);
                Array.Copy(array, index + 1, new_array, index, new_array.Length - index);
            }
            return true;
        }
    }
}