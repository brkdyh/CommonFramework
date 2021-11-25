using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SampleECS
{
    /// <summary>
    /// 简易ECS框架-Component
    /// </summary>
    public struct ECS_Component_Data<T>
        where T : struct
    {
        public string name;
        public uint uid;
        public string type;
        public T user_struct;

        public bool _dirty;
    }

    public interface IECS_Component_Pool
    {
        int NewComponent();
        int NewComponent(object com);
        object FindComponent(int idx);
        void CleanDirtyMark();
        //void SetCompoent(int idx, object com);
    }

    /// <summary>
    /// 组件对象池
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ECS_Component_Pool<T> : IECS_Component_Pool
        where T : struct
    {
        static ECS_Component_Pool<T>[] context_pools = new ECS_Component_Pool<T>[0];

        public ECS_Component_Data<T>[] use_coms = new ECS_Component_Data<T>[32];
        int use_com_ptr = -1;

        public ECS_Component_Data<T>[] available_coms = new ECS_Component_Data<T>[32];
        int available_com_ptr = -1;

        Stack<int> dirtyCache = new Stack<int>();


        public static ECS_Component_Pool<T> GetPool(int context_idx, Type data_type)
        {
            if (context_idx >= context_pools.Length
                || context_pools[context_idx] == null)
            {
                var type = typeof(ECS_Component_Pool<>);
                var g_type = type.MakeGenericType(data_type);
                ECS_Component_Pool<T> pool = Activator.CreateInstance(g_type) as ECS_Component_Pool<T>;
                ECS_Utils.SetArrayElement(ref context_pools, context_idx, pool);
            }

            return context_pools[context_idx];
        }

        public int NewComponent(T user_struct)
        {
            ECS_Component_Data<T> new_com;
            if (available_com_ptr > 0)
            {
                new_com = available_coms[available_com_ptr];
                available_com_ptr--;
            }
            else
            {
                new_com = new ECS_Component_Data<T>();
                new_com.uid = ECS_Utils.ApplyUID();
                var type_str = typeof(T).ToString();
                new_com.name = type_str;
                new_com.type = type_str;
                new_com.user_struct = user_struct;
            }

            use_com_ptr++;
            ECS_Utils.SetArrayElement(ref use_coms, use_com_ptr, new_com);

            return use_com_ptr;
        }

        public bool TryFindComponent(int idx, out T com)
        {
            if (idx >= 0 && idx < use_coms.Length)
            {
                com = use_coms[idx].user_struct;
                return true;
            }
            com = default;
            return false;
        }

        public bool ContainIndex(int idx) { return idx >= 0 && idx <= use_com_ptr; }
        public ref T FindComponentPtr(int idx)
        {
            return ref use_coms[idx].user_struct;
        }

        public void SetDirty(int idx, T com)
        {
            if (idx >= 0 && idx < use_coms.Length)
            {
                if (!use_coms[idx]._dirty)
                {
                    dirtyCache.Push(idx);
                    use_coms[idx]._dirty = true;
                }

                use_coms[idx].user_struct = com;
                //Debug.Log(idx + " = " + ((TestComp)((object)use_coms[idx].user_struct)).go);
            }
        }

        #region Impl Interface

        public int NewComponent(object com)
        {
            if (com.GetType() != typeof(T))
                return -1;

            return NewComponent((T)com);
        }

        public int NewComponent()
        {
            return NewComponent(new T());
        }

        public object FindComponent(int idx)
        {
            T com;
            if (TryFindComponent(idx, out com))
                return com;
            return null;
        }

        public void CleanDirtyMark()
        {
            while (dirtyCache.Count > 0)
            {
                var idx = dirtyCache.Pop();
                use_coms[idx]._dirty = false;
            }
        }

        #endregion
    }

    public static partial class ECS_Component_Type
    {
        public static int COMPONENT_TYPE_COUNT { get; private set; } = 0;
        public static void SetTypeCount(int count) { COMPONENT_TYPE_COUNT = count; }
    }

    public static partial class ECS_Component_Wrap
    {
        static Dictionary<string, int> COM_TYPE_ID_MAP = new Dictionary<string, int>();
        public static int Type2ID(Type type)
        {
            var str = type.ToString();
            return Type2ID(str);
        }
        public static int Type2ID(string type)
        {
            if (COM_TYPE_ID_MAP.ContainsKey(type))
                return COM_TYPE_ID_MAP[type];

            return -1;
        }
    }
}