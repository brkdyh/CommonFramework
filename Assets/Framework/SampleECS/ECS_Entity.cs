using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

namespace SampleECS
{
    internal struct Entity_Serialize_Data
    {
        public string[] _type;
        public string[] _components;
    }

    internal struct Entity_Com_Data
    {
        public uint _uid;
        public Type _type;
        public int _poolIdx;
        public int _dataIdx;
    }

    public partial class ECS_Entity
    {
        public uint uid { get; private set; }
        public int contextIdx { get; private set; }

        public ECS_Entity(uint uid, int contextIdx)
        {
            this.uid = uid;
            this.contextIdx = contextIdx;
            context = ECS_Context.GetContext(contextIdx);
        }


        int ecd_ptr = -1;
        //All Components Data
        Entity_Com_Data[] ecd_container = new Entity_Com_Data[4];

        //Context
        ECS_Context context;
        public ECS_Context Context { get { return context; } }

        //public Dictionary<Type, object> GetAllComponents()
        //{
        //    Dictionary<Type, object> acs = new Dictionary<Type, object>();
        //    var context = ECS_Context.GetContext(contextIdx);
        //    for (int i = 0; i < ecd_container.Length; i++)
        //    {
        //        var ecd = ecd_container[i];
        //        var ipool = context.FindIPool(ecd._type);
        //        if (ipool == null)
        //            continue;
        //        var com_data = ipool.FindComponent(ecd._poolIdx);
        //        acs.Add(ecd._type, com_data);
        //    }

        //    return acs;
        //}

        Entity_Com_Data FindECD(Type type)
        {
            for (int i = 0, len = ecd_container.Length; i < len; i++)
            {
                Entity_Com_Data ecd = ecd_container[i];
                if (ecd._type == type)
                    return ecd;
            }

            return default;
        }

        public object GetComponent(Type type)
        {
            return null;
        }

        public T GetComponent<T>()
            where T : struct
        {
            Type c_type = typeof(T);
            var ecd = FindECD(c_type);
            if (!ecd._uid.isVaildUid())
                return default;

            ECS_Component_Pool<T> pool = context.GetPool<T>();
            T com;
            pool.TryFindComponent(ecd._poolIdx, out com);
            return com;
        }

        public bool HasComponent(Type type)
        {
            //Debug.Log("HasComponent? " + type);
            return FindECD(type)._uid.isVaildUid();
        }

        public bool HasComponent<T>()
            where T : struct
        {
            var type = typeof(T);
            return HasComponent(type);
        }

        public T AddComponent<T>()
            where T : struct
        {
            return AddComponent(new T());
        }

        public T AddComponent<T>(T component)
            where T : struct
        {
            var context = ECS_Context.GetContext(contextIdx);
            var pool = context.GetPool<T>();
            var pool_idx = pool.NewComponent(component);

            ecd_ptr++;
            Entity_Com_Data ecd = new Entity_Com_Data();
            ecd._uid = ECS_Utils.ApplyUID();
            ecd._type = typeof(T);
            ecd._poolIdx = pool_idx;
            ecd._dataIdx = ecd_ptr;

            ECS_Utils.SetArrayElement(ref ecd_container, ecd_ptr, ecd);

            context.OnEntityChange(this);

            return component;
        }

        public void RemoveComponent<T>()
            where T : struct
        {

        }

        public void RemoveComponet(Type type)
        {

        }

        Queue<int> replacedComIdx = new Queue<int>();

        public void Replace<T>(T com)
            where T : struct
        {
            //replacedComIdx.Clear();
            //Type c_type = typeof(T);
            //var ecd = FindECD(c_type);
            //if (!ecd._uid.isVaildUid())
            //    return;

            //replacedComIdx.Enqueue(ecd._poolIdx);
        }

        public void ComponentAction<T>()
            where T : struct
        {

        }
    }
}