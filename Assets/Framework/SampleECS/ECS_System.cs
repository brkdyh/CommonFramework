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
        /// <param name="entities"></param>
        public virtual void Excute(ECS_Entity[] entities)
        {

        }

        ECS_Match _match = null;
        /// <summary>
        /// 获取 System 的 Entity 匹配条件
        /// </summary>
        public ECS_Match getMatch
        {
            get
            {
                if (_match == null)
                    _match = GetSystemMatch();
                return _match;
            }
        }

        /// <summary>
        /// 获取 System 关联的 Entity 的匹配条件
        /// </summary>
        /// <returns></returns>
        public virtual ECS_Match GetSystemMatch()
        {
            return null;
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

        public virtual bool GetTrigger(ECS_Entity entity)
        {
            return true;
        }
    }
}