using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SampleECS
{
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
    internal class ECS_Entity_Collections
    {

        Dictionary<uint, int> uid_idx_map = new Dictionary<uint, int>();
        int entity_Ptr = -1;
        internal int RealLength { get { return entity_Ptr + 1; } }
        ECS_Entity[] _entities = new ECS_Entity[0];
        internal ECS_Entity[] entities { get { return _entities; } }

        Queue<ECS_Entity> addCache = new Queue<ECS_Entity>();

        internal void AddEntity(ECS_Entity entity)
        {
            if (uid_idx_map.ContainsKey(entity.uid))
                return;
            addCache.Enqueue(entity);
        }

        internal void RealAddEntity()
        {
            while (addCache.Count > 0)
            {
                var entity = addCache.Dequeue();
                if (!uid_idx_map.ContainsKey(entity.uid))
                {
                    entity_Ptr++;
                    uid_idx_map.Add(entity.uid, entity_Ptr);
                    ECS_Utils.SetArrayElement(ref _entities, entity_Ptr, entity);
                }
            }
        }

        HashSet<int> removeCache = new HashSet<int>();

        internal void RemoveEntity(ECS_Entity entity)
        {
            int idx = -1;
            if (uid_idx_map.TryGetValue(entity.uid, out idx))
                return;
            removeCache.Add(idx);
        }

        internal void RealRemoveEntity()
        {
            if (removeCache.Count <= 0)
                return;

            entity_Ptr++;
            var new_entities = new ECS_Entity[0];
            entity_Ptr = -1;
            uid_idx_map.Clear();
            for (int i = 0; i < RealLength; i++)
            {
                if (removeCache.Contains(i))
                    continue;

                var entity = _entities[i];
                entity_Ptr++;
                ECS_Utils.SetArrayElement(ref new_entities, entity_Ptr, entity);
                uid_idx_map.Add(entity.uid, entity_Ptr);
            }
            _entities = new_entities;

            removeCache.Clear();
        }

        //public void FormatEntitySize()
        //{
        //    if (_entities.Length != RealLength)
        //    {
        //        Array.Resize(ref _entities, RealLength);
        //    }
        //}

        public bool ContainEntity(ECS_Entity entity) { return uid_idx_map.ContainsKey(entity.uid); }

        public void Clean()
        {
            uid_idx_map.Clear();
            entity_Ptr = -1;
            addCache.Clear();
            removeCache.Clear();
        }

        public void Diepose()
        {
            Clean();
            _entities = new ECS_Entity[0];
        }
    }

    /// <summary>
    /// ECS运行环境
    /// </summary>
    public partial class ECS_Context : ECS_RecyclePool<ECS_Entity>
    {
        /// <summary>
        /// Don‘t Modify This Value!!!
        /// </summary>
        public bool ExcutingSystem = false;

        #region  Container

        static int context_ptr = -1;
        //Context容器
        static ECS_Context[] context_container = new ECS_Context[4];
        public static ECS_Context[] AllContexts { get { return context_container; } }

        #endregion

        public string context_name { get; private set; } = "";
        //context idx
        int _context_idx = -1;
        public int context_idx { get { return _context_idx; } }

        //全部组件池
        public int component_pool_container_ptr = -1;
        IECS_Component_Pool[] component_pool_container = new IECS_Component_Pool[0];

        Dictionary<uint, int> euid_idx_map = new Dictionary<uint, int>();

        //全体System容器
        Dictionary<Type, ECS_System> systems_container = new Dictionary<Type, ECS_System>();
        //System执行容器 --  按照执行顺序排序后的System列表
        List<ECS_System> ex_systems_container = new List<ECS_System>();
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
            EnableContext(context_name);
            return context;
        }

        public static ECS_Context GetContext(int idx)
        {
            if (idx <= context_ptr)
                return context_container[idx];
            return null;
        }

        public static ECS_Context GetContext(string context_name)
        {
            for (int i = 0, l = context_container.Length; i < l; i++)
            {
                if (context_name == context_container[i].context_name)
                    return context_container[i];
            }

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
                        CreateSystem(type, sa.systemMode);
            }
        }

        public static void EnableContext(ECS_Context context) { ECS_Runtime.InjectContext(context); }

        public static void EnableContext(string context_name) { EnableContext(GetContext(context_name)); }

        public static void DisableContext(ECS_Context context) { ECS_Runtime.DisposeContext(context); }

        public static void DisableContext(string context_name) { DisableContext(GetContext(context_name)); }

        void CleanContext()
        {
            if (ExcutingSystem)
            {
                throw new Exception("Can not Clean Context during Excuting System!");
            }

            euid_idx_map.Clear();
            removeCache.Clear();
            excute_ptr = -1;

            //Clean collections
            foreach (var collection in system_collections)
                collection.Value.Clean();

            //Recyle Component
            for (int i = 0; i < component_pool_container_ptr + 1; i++)
            {
                component_pool_container[i].RecyleAll();
            }

            //Recyle Entity
            RecyleAll();
        }

        public static void CleanContext(string context_name)
        {
            var context = GetContext(context_name);
            if (context != null)
                context.CleanContext();
        }

        void DisposeContext()
        {
            if (ExcutingSystem)
            {
                throw new Exception("Can not Dispose Context during Excuting System!");
            }

            euid_idx_map.Clear();
            euid_idx_map = null;
            removeCache.Clear();
            removeCache = null;
            excute_ptr = -1;
            excuteEntities = null;

            systems_container.Clear();
            systems_container = null;
            ex_systems_container.Clear();
            ex_systems_container = null;

            //Dispose collections
            foreach (var collection in system_collections)
                collection.Value.Diepose();
            system_collections.Clear();
            system_collections = null;

            //Dispose Component
            for (int i = 0; i < component_pool_container_ptr + 1; i++)
                component_pool_container[i].Clean();
            component_pool_container = null;

            //Dispose Entity
            Clean();
        }

        public static void DisposeContext(string context_name)
        {
            var context = GetContext(context_name);
            if (context != null)
            {
                context.DisposeContext();

                ECS_Context[] new_contexts = null;
                if (context_ptr > 1)
                    new_contexts = new ECS_Context[context_ptr];
                else
                    new_contexts = new ECS_Context[4];

                int new_idx_counter = 0;
                for (int i = 0; i < context_ptr + 1; i++)
                {
                    if (context_container[i].context_name == context_name)
                        continue;
                    new_contexts[new_idx_counter] = context_container[i];
                    new_idx_counter++;
                }

                context_container = new_contexts;
            }
        }

        #endregion

        #region Entity

        public ECS_Entity CreateEntity()
        {
            var uid = ECS_Utils.ApplyUID();
            int ptr;
            ECS_Entity entity = Apply(out ptr);
            entity.Reset(uid, _context_idx);
            if (AddEntity(entity, ptr))
                return entity;
            return null;
        }

        bool AddEntity(ECS_Entity entity, int ptr)
        {
            if (euid_idx_map.ContainsKey(entity.uid))
            {
                Debug.LogError("Error Return!");
                return false;
            }
            entity._in_context_idx = ptr;
            //Debug.Log("add entity! idx = " + ptr);
            euid_idx_map.Add(entity.uid, ptr);

            if (excuteEntities.Length < data_pool.Length)
            {//跟随entity_container扩容
                Array.Resize(ref excuteEntities, data_pool.Length);
                //Debug.Log("Resize = " + excuteEntities.Length);
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

        Stack<int> removeCache = new Stack<int>();
        public bool RemoveEntity(ECS_Entity entity)
        {
            if (!euid_idx_map.ContainsKey(entity.uid))
                return false;

            removeCache.Push(entity._in_context_idx);
            return true;
        }

        void RealRemoveEntity(ECS_Entity entity)
        {
            if (euid_idx_map.Remove(entity.uid))
            {
                DisposeEntity(entity);
                Recycle(entity._in_context_idx);    //回收Entity
            }
        }

        public void OnEntityChange(ECS_Entity entity)
        {
            DisposeEntity(entity);
            CollectEntity(entity);
        }

        void UpdateEntities()
        {
            while (removeCache.Count > 0)
            {
                var idx = removeCache.Pop();
                var entity = data_pool[idx];
                RealRemoveEntity(entity);
            }
        }

        public ECS_Entity FindEntity(uint euid)
        {
            try
            {
                int idx = -1;
                if (euid_idx_map.TryGetValue(euid, out idx))
                {
                    return data_pool[idx];
                }

                return null;
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        #endregion

        #region Collection

        public void UpdateCollection()
        {
            var e = system_collections.GetEnumerator();
            while (e.MoveNext())
            {
                var collection = e.Current.Value;
                collection.RealRemoveEntity();
                collection.RealAddEntity();
            }
        }

        #endregion

        #region System

        void CreateSystem(Type systemType, SystemMode systemMode)
        {
            if (!systems_container.ContainsKey(systemType))
            {
                var system = Activator.CreateInstance(systemType) as ECS_System;
                system.SetSystemMode(systemMode);
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
            ex_systems_container.Add(system);
            ex_systems_container.Sort((s1, s2) => { return s1.ExcuteIndex < s2.ExcuteIndex ? -1 : 1; });
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
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    system.BeforeExcute();              //调用 BeforeExcute

                    var len = collection.RealLength;
                    for (int i = 0; i < len; i++)
                    {
                        system.Excute(entities[i]);
                    }

                    system.AfterExcute();              //调用 AfterExcute
                }
                else if (system.getSystemMode == SystemMode.Action)
                {
                    bool call_before = false;

                    var trigger_types = system.getTrigger.type_ids;
                    var len = collection.RealLength;
                    for (int i = 0; i < len; i++)
                    {
                        var entity = entities[i];

                        //whether Entity Fit Condition of System-Trigger
                        //为了更高的执行效率，不封装这段代码以减少方法调用次数
                        var e_dirty_ptr = entity.com_dirtyMarkFront_Ptr;
                        bool dirty = false;
                        if (e_dirty_ptr >= 0)
                        {
                            //whether entity fit every condition of System-Trigger
                            dirty = true;
                            var dm = entity.com_dirtyMarkFront;
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
                        {
                            if (!call_before)
                            {
                                system.BeforeExcute();
                                call_before = true;
                            }
                            system.Excute(entity);
                        }
                    }

                    if (call_before)
                        system.AfterExcute();
                }

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DoExcute()
        {
            ExcutingSystem = true;

            foreach (var sys in ex_systems_container)
                ExcuteSystem(sys);

            ExcutingSystem = false;
        }

        void CleanEntityMark()
        {
            if (excute_ptr < 0)
                return;

            int new_excute_ptr = -1;
            try
            {
                for (int i = 0, l = excute_ptr + 1; i < l; i++)
                {
                    var entity = data_pool[excuteEntities[i]];
                    if (entity.com_dirtyMarkBack_Ptr < 0)
                    {
                        entity.entityDirty = false;
                        entity.com_dirtyMarkFront_Ptr = -1; //重置dirty mark指针
                    }
                    else
                    {
                        //Switch Cache
                        var temp_ptr = entity.com_dirtyMarkBack_Ptr;
                        var temp_mark = entity.com_dirtyMarkBack;

                        entity.com_dirtyMarkBack = entity.com_dirtyMarkFront;
                        entity.com_dirtyMarkBack_Ptr = -1;

                        entity.com_dirtyMarkFront = temp_mark;
                        entity.com_dirtyMarkFront_Ptr = temp_ptr;

                        entity.entityDirty = true;
                        new_excute_ptr++;
                        excuteEntities[new_excute_ptr] = entity._in_context_idx;
                        //Debug.Log("In System Change = " + entity.uid);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            excute_ptr = new_excute_ptr;
        }

        #endregion

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick()
        {
            /*Sweet Little Puppy!*/

            //Update Entities
            UpdateEntities();

            //Update Collection
            UpdateCollection();

            //Excute System;
            DoExcute();

            //Clean Dirty Mark
            CleanEntityMark();
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
    }
}