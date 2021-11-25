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

        bool inited = false;
        public void Init(ECS_Context core)
        {
            getTrigger = GetTrigger();
            inited = true;
            OnSystemInited(core);
        }

        /// <summary>
        /// 当系统初始化的时候会调用
        /// </summary>
        /// <param name="core"></param>
        public virtual void OnSystemInited(ECS_Context core)
        {

        }

        /// <summary>
        /// 系统执行方法
        /// </summary>
        /// <param name="entity"></param>
        public virtual void Excute(ECS_Entity entity)
        {

        }

        /// <summary>
        /// 获取 System 关联的 Entity 的匹配条件
        /// </summary>
        /// <returns></returns>
        public virtual bool GetSystemMatch(ECS_Entity entity)
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
        public SystemMode getSystemMode
        {
            get
            {
                if ((int)_systemMode == -1)
                {
                    var att = ECS_Utils.GetAttributeData<SystemAttribute>(this);
                    if (att != null)
                        _systemMode = ECS_Utils.GetAttributeMemberValue<SystemMode>("systemMode", att);
                    else
                        _systemMode = SystemMode.Action;
                }
                return _systemMode;
            }
        }
    }
}