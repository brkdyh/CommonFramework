using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SampleECS
{
    public class ECS_System
    {
        /// <summary>
        /// 系统执行顺序
        /// </summary>
        public virtual int ExcuteIndex { get; } = 0;

        protected ECS_Context __context;

        bool inited = false;
        public void Init(ECS_Context context)
        {
            __context = context;
            getTrigger = GetTrigger();
            inited = true;
            __OnSystemInited(context);
        }

        /// <summary>
        /// 当系统初始化的时候会调用
        /// </summary>
        /// <param name="core"></param>
        protected virtual void __OnSystemInited(ECS_Context context)
        {

        }

        /// <summary>
        /// 在执行所有 Excute() 之前被调用
        /// </summary>
        public virtual void BeforeExcute()
        {

        }

        /// <summary>
        /// 系统执行方法
        /// </summary>
        /// <param name="entity"></param>
        public virtual void __Excute(ECS_Entity entity)
        {

        }

        /// <summary>
        /// 在执行所有 Excute() 之后被调用
        /// </summary>
        public virtual void AfterExcute()
        {

        }

        /// <summary>
        /// 获取 System 关联的 Entity 的匹配条件
        /// </summary>
        /// <returns></returns>
        public virtual bool __GetSystemMatch(ECS_Entity entity)
        {
            return true;
        }

        public ECS_Trigger getTrigger;
        /// <summary>
        /// 获取 System 关联的 Entity 的触发条件
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual ECS_Trigger GetTrigger()
        {
            return default;
        }

        SystemMode _systemMode = (SystemMode)(-1);
        public void SetSystemMode(SystemMode systemMode) { _systemMode = systemMode; }
        public SystemMode getSystemMode
        {
            get
            {
                //if ((int)_systemMode == -1)
                //{
                //    var att = ECS_Utils.GetAttributeData<SystemAttribute>(this);
                //    if (att != null)
                //        _systemMode = ECS_Utils.GetAttributeMemberValue<SystemMode>("systemMode", att);
                //    else
                //        _systemMode = SystemMode.Action;
                //}
                return _systemMode;
            }
        }
    }

    public class ECS_Static_System : ECS_System
    {
        /// <summary>
        /// 是否在第一帧执行
        /// </summary>
        public virtual bool runAtFirstFrame { get { return false; } }

        /// <summary>
        /// 系统执行方法
        /// </summary>
        /// <param name="context"></param>
        public virtual void __ExcuteStatic(ECS_Context context)
        {

        }
    }
}