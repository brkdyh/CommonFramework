using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

namespace SampleECS
{
    public partial class ECS_Entity
    {
        public uint uid { get; private set; }
        /// <summary>
        /// Don't Modify it Manually!
        /// </summary>
        public int idx = -1;
        //Context
        public int contextIdx { get; private set; }
        ECS_Context context;

        int[] poolIndecies;

        public int dirtyFrame = 0;
        public int dirtyMarkPtr = -1;
        public int[] dirtyMark;

        public ECS_Entity(uint uid, int contextIdx)
        {
            this.uid = uid;
            this.contextIdx = contextIdx;
            context = ECS_Context.GetContext(contextIdx);

            poolIndecies = new int[ECS_Component_Type.COMPONENT_TYPE_COUNT];
            dirtyMark = new int[ECS_Component_Type.COMPONENT_TYPE_COUNT];
            //Debug.Log("entity = " + dirtyMark.Length);
        }
    }
}