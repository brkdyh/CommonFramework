using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SampleECS
{
    //#region Obsulate
    //#endregion
    public struct ECS_Trigger
    {
        public int[] type_ids;

        public ECS_Trigger(params int[] types)
        {
            if (types == null)
            {
                type_ids = new int[0];
                return;
            }
            type_ids = types;
        }

        public static ECS_Trigger operator &(ECS_Trigger m1, ECS_Trigger m2)
        {
            int[] nm = new int[m1.type_ids.Length + m2.type_ids.Length];
            Array.Copy(m1.type_ids, 0, nm, 0, m1.type_ids.Length);
            Array.Copy(m2.type_ids, 0, nm, m1.type_ids.Length, m2.type_ids.Length);
            return new ECS_Trigger(nm);
        }
    }

    /// <summary>
    /// Entity集合
    /// </summary>
    public class ECS_Entity_Collections
    {

        Dictionary<uint, int> uid_idx_map = new Dictionary<uint, int>();
        int entity_Ptr = -1;
        public int RealLength { get { return entity_Ptr + 1; } }
        ECS_Entity[] _entities = new ECS_Entity[0];
        public ECS_Entity[] entities { get { return _entities; } }
        //public ECS_Entity[] limitEntities { get { return } }
        public void AddEntity(ECS_Entity entity)
        {
            if (!uid_idx_map.ContainsKey(entity.uid))
            {
                entity_Ptr++;
                uid_idx_map.Add(entity.uid, entity_Ptr);
                ECS_Utils.SetArrayElement(ref _entities, entity_Ptr, entity);
            }
        }

        public void RemoveEntity(ECS_Entity entity)
        {
            var idx = -1;
            if (!uid_idx_map.TryGetValue(entity.uid, out idx))
                return;

        }

        //public void FormatEntitySize()
        //{
        //    if (_entities.Length != RealLength)
        //    {
        //        Array.Resize(ref _entities, RealLength);
        //    }
        //}

        public bool ContainEntity(ECS_Entity entity) { return uid_idx_map.ContainsKey(entity.uid); }
    }

    /// <summary>
    /// ECS运行环境
    /// </summary>
    public partial class ECS_Context
    {
        public int RUN_FRAME = 0;

        #region  Container

        static int context_ptr = -1;
        //Context容器
        static ECS_Context[] context_container = new ECS_Context[4];

        #endregion

        public string context_name { get; private set; } = "";
        //context idx
        int _context_idx = -1;
        public int context_idx { get { return _context_idx; } }

        ////组件池
        ////Dictionary<Type, IECS_Component_Pool> component_pool_container = new Dictionary<Type, IECS_Component_Pool>();
        public int component_pool_container_ptr = -1;
        IECS_Component_Pool[] component_pool_container = new IECS_Component_Pool[0];

        //全体Entity容器
        ECS_Entity[] entity_container = new ECS_Entity[1024];
        int entity_container_ptr = -1;
        Dictionary<uint, int> euid_idx_map = new Dictionary<uint, int>();

        //全体System容器
        Dictionary<Type, ECS_System> systems_container = new Dictionary<Type, ECS_System>();
        Dictionary<Type, ECS_Entity_Collections> system_collections = new Dictionary<Type, ECS_Entity_Collections>();

        #region Context

        public static ECS_Context CreateContext(string context_name)
        {
            typeof(ECS_Component_Wrap).GetMethod("Init").Invoke(null, null);
            ECS_Context context = new ECS_Context();
            context_ptr++;
            context._context_idx = context_ptr;
            context.context_name = context_name;
            context.InitECSContext();
            ECS_Utils.SetArrayElement(ref context_container, context_ptr, context);
            return context;
        }

        public static ECS_Context GetContext(int idx)
        {
            if (idx <= context_ptr)
                return context_container[idx];
            return null;
        }

        void InitECSContext()
        {
            //Init Component Pools
            typeof(ECS_Context).GetMethod("InitComPool").Invoke(this, new object[] { context_idx });

            //收集 System
            var asm_all_types = Assembly.GetAssembly(typeof(ECS_Context)).GetTypes();
            foreach (var type in asm_all_types)
            {
                SystemAttribute sa = type.GetCustomAttribute<SystemAttribute>();
                if (sa != null)
                    if (sa.context == "All" || sa.context.Contains(context_name))
                        CreateSystem(type);
            }
        }

        #endregion

        #region Entity

        public ECS_Entity CreateEntity()
        {
            var uid = ECS_Utils.ApplyUID();
            ECS_Entity entity = new ECS_Entity(uid, _context_idx);
            if (AddEntity(entity))
                return entity;
            return null;
        }

        bool AddEntity(ECS_Entity entity)
        {
            if (euid_idx_map.ContainsKey(entity.uid))
                return false;
            entity_container_ptr++;
            euid_idx_map.Add(entity.uid, entity_container_ptr);
            entity.idx = entity_container_ptr;
            ECS_Utils.SetArrayElement(ref entity_container, entity_container_ptr, entity);
            if (excuteEntities.Length < entity_container.Length)
            {//跟随entity_container扩容
                excuteEntities = new int[entity_container.Length];
                //Debug.Log(entity_container.Length);
            }

            return CollectEntity(entity);
        }

        //收集Entity 到 EntityCollection 中 
        bool CollectEntity(ECS_Entity entity)
        {
            foreach (var sys_kp in systems_container)
            {
                var sys = sys_kp.Value;
                var collection = getSystemCollection(sys);
                if (sys.GetSystemMatch(entity))
                    collection.AddEntity(entity);
            }

            return true;
        }

        //移除 EntityCollection 中的 Entity
        void DisposeEntity(ECS_Entity entity)
        {
            foreach (var sys_kp in systems_container)
            {
                var sys = sys_kp.Value;
                var collection = getSystemCollection(sys);
                if (collection.ContainEntity(entity))
                    collection.RemoveEntity(entity);
            }
        }

        public bool RemoveEntity(ECS_Entity entity)
        {
            if (!euid_idx_map.ContainsKey(entity.uid))
                return false;
            euid_idx_map.Remove(entity.uid);
            DisposeEntity(entity);
            return true;
        }

        public void OnEntityChange(ECS_Entity entity)
        {
            DisposeEntity(entity);
            CollectEntity(entity);
        }

        #endregion

        #region System

        void CreateSystem(Type systemType)
        {
            if (!systems_container.ContainsKey(systemType))
            {
                var system = Activator.CreateInstance(systemType) as ECS_System;
                CollectSystem(system);
            }
        }

        public void CollectSystem(ECS_System system)
        {
            var tp = system.GetSysType();
            if (systems_container.ContainsKey(tp))
            {
                Debug.LogError("重复添加System: " + tp);
                return;
            }
            system.Init(this);
            systems_container.Add(tp, system);

        }

        ECS_Entity_Collections getSystemCollection(ECS_System system)
        {
            var type = system.GetType();
            if (!system_collections.ContainsKey(type))
                system_collections.Add(type, new ECS_Entity_Collections());
            return system_collections[type];
        }


        public int excute_ptr = -1;
        public int[] excuteEntities = new int[0];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExcuteSystem(ECS_System system)
        {
            try
            {
                var type = system.GetType();
                ECS_Entity_Collections collection = null;
                if (!system_collections.TryGetValue(type, out collection))
                    return;
                var entities = collection.entities;
                if (system.getSystemMode == SystemMode.Loop)
                {
                    var len = collection.RealLength;
                    for (int i = 0; i < len; i++)
                    {
                        system.Excute(entities[i]);
                    }
                }
                else if (system.getSystemMode == SystemMode.Action)
                {
                    var trigger_types = system.getTrigger.type_ids;
                    var len = collection.RealLength;
                    for (int i = 0; i < len; i++)
                    {
                        var entity = entities[i];

                        //whether Entity Fit Condition of System-Trigger
                        //为了更高的执行效率，不封装这段代码以减少方法调用次数
                        var e_dirty_ptr = entity.dirtyMarkPtr;
                        bool dirty = false;
                        if (e_dirty_ptr >= 0)
                        {
                            //whether entity fit every condition of System-Trigger
                            dirty = true;
                            var dm = entity.dirtyMark;
                            for (int j = 0, l1 = trigger_types.Length; j < l1; j++)
                            {
                                int cur_type = trigger_types[j];
                                bool contain_type = false;
                                for (int k = 0, l2 = e_dirty_ptr + 1; k < l2; k++)
                                {
                                    if (dm[k] == cur_type)
                                    {
                                        contain_type = true;
                                        break;
                                    }
                                }

                                if (!contain_type)
                                {
                                    dirty = false;
                                    break;
                                }
                            }
                        }

                        if (dirty)
                            system.Excute(entity);
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DoExcute()
        {
            foreach (var sys in systems_container.Values)
                ExcuteSystem(sys);
        }

        void CleanEntityMark()
        {
            if (excute_ptr < 0)
                return;

            for (int i = 0, l = excute_ptr + 1; i < l; i++)
                entity_container[excuteEntities[i]].dirtyMarkPtr = -1; //重置dirty mark指针

            excute_ptr = -1;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick()
        {
            DoExcute();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LateTick()
        {
            CleanEntityMark();
            RUN_FRAME++;
        }
    }

    /// <summary>
    /// ECS扩展方法
    /// </summary>
    public static class ECS_Extension
    {
        public static Type GetSysType(this ECS_System system)
        {
            return system.GetType();
        }

        public static bool isVaildUid(in this uint uid)
        {
            return uid != 0;
        }
    }
}