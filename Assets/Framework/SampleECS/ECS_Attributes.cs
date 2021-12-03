using System;

namespace SampleECS
{
    /// <summary>
    /// ECS Component
    /// </summary>
    public class ComponentAttribute : Attribute
    {
        public bool isStatic = false;
        public ComponentAttribute(bool isStatic = false)
        {
            this.isStatic = isStatic;
        }
    }

    public enum SystemMode
    {
        /// <summary>
        /// 当实体组件触发信号时，才会调用Exsute。
        /// </summary>
        Action,
        /// <summary>
        /// 每帧调用Excute
        /// </summary>
        Loop,
    }

    /// <summary>
    /// ECS System
    /// </summary>
    public class SystemAttribute : Attribute
    {
        public SystemMode systemMode = SystemMode.Action;
        public string context;
        public SystemAttribute() { systemMode = SystemMode.Action; context = "All"; }
        public SystemAttribute(SystemMode systemMode, string context) { this.systemMode = systemMode; this.context = context; }
    }
}
