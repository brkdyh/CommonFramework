using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SampleECS
{
    /// <summary>
    /// 简易ECS框架-Component
    /// </summary>
    public struct ECS_Component_Data<T> : IDisposable
        where T : struct
    {
        public string name;
        public uint uid;
        public string type;
        public T user_struct;
        public bool disposed { get; set; }
    }

    public interface IECS_Component_Pool
    {
        int NewComponent();
        int NewComponent(object com);
        object FindComponent(int idx);
        void Clean();
        void RecyleAll();
        //void SetCompoent(int idx, object com);
    }

    /// <summary>
    /// 组件对象池
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ECS_Component_Pool<T> : ECS_RecyclePool<ECS_Component_Data<T>>, IECS_Component_Pool
        where T : struct
    {
        static ECS_Component_Pool<T>[] context_pools = new ECS_Component_Pool<T>[0];

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
            int ptr;
            ref var new_com = ref Apply(out ptr);
            new_com.uid = ECS_Utils.ApplyUID();
            var type_str = typeof(T).ToString();
            new_com.name = type_str;
            new_com.type = type_str;
            new_com.user_struct = user_struct;
            return ptr;
        }

        public bool TryFindComponent(int idx, out T com)
        {
            ECS_Component_Data<T> data;
            if (TryGetData(idx, out data))
            {
                com = data.user_struct;
                return true;
            }
            com = default;
            return false;
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

        #endregion
    }
}