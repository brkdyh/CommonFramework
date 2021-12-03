using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

namespace SampleECS
{
    public partial class ECS_Entity : IDisposable
    {
        public uint uid { get; private set; }
        /// <summary>
        /// Don't Modify it Manually!
        /// </summary>
        public int _in_context_idx = -1;
        //Context
        public int contextIdx { get; private set; }
        ECS_Context context;

        public bool disposed { get; set; } = false;

        public void Reset(uint uid, int contextIdx)
        {
            this.uid = uid;
            this.contextIdx = contextIdx;
            context = ECS_Context.GetContext(contextIdx);

            if (poolIndecies == null)
                poolIndecies = new int[ECS_Component_Type.COMPONENT_TYPE_COUNT];
            if (com_dirtyMarkFront == null)
                com_dirtyMarkFront = new int[ECS_Component_Type.COMPONENT_TYPE_COUNT];
            if (com_dirtyMarkBack == null)
                com_dirtyMarkBack = new int[ECS_Component_Type.COMPONENT_TYPE_COUNT];

            for (int i = 0, l = ECS_Component_Type.COMPONENT_TYPE_COUNT; i < l; i++)
                poolIndecies[i] = 0;

            entityDirty = false;

            //重置指针
            com_dirtyMarkFront_Ptr = -1;
            com_dirtyMarkBack_Ptr = -1;
        }

        #region Component Data

        int[] poolIndecies;

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

        public void RemoveComponent<T>()
        {
            Type type = typeof(T);
            var method = GetType().GetMethod("Remove_" + type.Name);
            if (method != null)
                method.Invoke(this, null);
            else
                throw new Exception("ECS => Invaild Component Type: " + type);
        }
    }
}