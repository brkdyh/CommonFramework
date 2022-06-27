using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

namespace SampleECS
{
    public class ECS_Entity : IDisposable
    {
        public uint uid { get; private set; }
        /// <summary>
        /// Don't Modify it Manually!
        /// </summary>
        public int _in_context_idx = -1;
        //Context
        public int contextIdx { get; private set; }

        public bool disposed { get; set; } = false;

        public virtual void Reset(uint uid, int contextIdx)
        {
            this.uid = uid;
            this.contextIdx = contextIdx;

            if (poolIndecies == null)
                poolIndecies = new int[Game_Component_Type.COMPONENT_TYPE_COUNT];
            if (com_dirtyMarkFront == null)
                com_dirtyMarkFront = new int[Game_Component_Type.COMPONENT_TYPE_COUNT];
            if (com_dirtyMarkBack == null)
                com_dirtyMarkBack = new int[Game_Component_Type.COMPONENT_TYPE_COUNT];

            for (int i = 0, l = Game_Component_Type.COMPONENT_TYPE_COUNT; i < l; i++)
                poolIndecies[i] = 0;

            entityDirty = false;

            //重置指针
            com_dirtyMarkFront_Ptr = -1;
            com_dirtyMarkBack_Ptr = -1;
        }

        #region Component Data

        protected int[] poolIndecies;

        /*********** 
         * Record Component Changes by Using dual caches to record that when some compoment changes。
         * Cache2(Back Cache) will be used during excuting ECS_System.Excute() function。
         * Cache1(Front Cache) will be used during other time。
         ***********/
        public bool entityDirty = false;

        /** Dual Cache **/
        public int com_dirtyMarkFront_Ptr = -1;
        public int[] com_dirtyMarkFront;            //Cache1

        public int com_dirtyMarkBack_Ptr = -1;
        public int[] com_dirtyMarkBack;             //Cache2


        #endregion

        public ECS_Entity() { }

        public ECS_Entity(uint uid, int contextIdx) { Reset(uid, contextIdx); }

        public void AddComponent<T>(T com)
            where T : struct
        {
            Type type = typeof(T);
            var method = GetType().GetMethod("Add_" + type.Name);
            if (method != null)
                method.Invoke(this, new object[] { com });
            else
                throw new Exception("ECS => Invaild Component Type: " + type);
        }

        public void AddComponent(Type type, object com)
        {
            var method = GetType().GetMethod("Add_" + type.Name);
            if (method != null)
                method.Invoke(this, new object[] { com });
            else
                throw new Exception("ECS => Invaild Component Type: " + type);
        }

        public void RemoveComponent<T>()
            where T : struct
        {
            Type type = typeof(T);
            var method = GetType().GetMethod("Remove_" + type.Name);
            if (method != null)
                method.Invoke(this, null);
            else
                throw new Exception("ECS => Invaild Component Type: " + type);
        }

        public bool HasComponent<T>()
        {
            Type type = typeof(T);
            var method = GetType().GetMethod("has_" + type.Name);
            if (method != null)
                return (bool)method.Invoke(this, null);
            else
                throw new Exception("ECS => Invaild Component Type: " + type);
        }

        public T GetComponent<T>()
           where T : struct
        {
            Type type = typeof(T);
            var prop = GetType().GetProperty(type.Name.ToLower());
            if (prop != null)
            {
                return (T)prop.GetValue(this);
            }

            return default;
        }
    }
}