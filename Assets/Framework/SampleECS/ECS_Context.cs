using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SampleECS
{
    #region Obsulate
    ///// <summary>
    ///// 匹配条件
    ///// </summary>
    //public class ECS_Match
    //{
    //    List<List<Type>> types;
    //    Dictionary<string, byte> allTypes;

    //    int or_index;

    //    public static ECS_Match operator |(ECS_Match m1, ECS_Match m2)
    //    {
    //        ECS_Match em = new ECS_Match(m1.types);
    //        for (int i = 0; i < m2.types.Count; i++)
    //        {
    //            em.types.Add(new List<Type>());
    //            em.types[em.or_index].AddRange(m2.types[i]);
    //            em.or_index++;
    //        }
    //        em.SyncAllTypes();
    //        return em;
    //    }

    //    public static ECS_Match operator &(ECS_Match m1, ECS_Match m2)
    //    {

    //        List<List<Type>> nts = new List<List<Type>>();

    //        int count = Math.Max(m1.types.Count, m2.types.Count);
    //        for (int i = 0; i < count; i++)
    //        {
    //            nts.Add(new List<Type>());
    //            if (i < m1.types.Count)
    //                nts[i].AddRange(m1.types[i]);
    //            if (i < m2.types.Count)
    //                nts[i].AddRange(m2.types[i]);
    //        }
    //        ECS_Match em = new ECS_Match(nts);
    //        return em;
    //    }

    //    public ECS_Match(params Type[] types)
    //    {
    //        this.types = new List<List<Type>>();
    //        this.types.Add(new List<Type>());
    //        if (types != null && types.Length > 0)
    //            this.types[0].AddRange(types);

    //        or_index = this.types.Count;
    //        allTypes = new Dictionary<string, byte>();
    //        SyncAllTypes();
    //    }

    //    private ECS_Match(List<List<Type>> types)
    //    {
    //        this.types = types;
    //        or_index = this.types.Count;

    //        allTypes = new Dictionary<string, byte>();
    //        SyncAllTypes();
    //    }

    //    void SyncAllTypes()
    //    {
    //        allTypes.Clear();
    //        foreach (var tps in types)
    //        {
    //            foreach (var tp in tps)
    //            {
    //                allTypes = new Dictionary<string, byte>();
    //                if (!allTypes.ContainsKey(tp.ToString()))
    //                    allTypes.Add(tp.ToString(), 0);
    //            }
    //        }
    //    }

    //    public bool MatchEntity(ECS_Entity entity)
    //    {
    //        bool result = false;

    //        for (int i = 0; i < types.Count; i++)
    //        {
    //            bool ex_result = true; 
    //            for (int j = 0; j < types[i].Count; j++)
    //            {
    //                var tp = types[i][j];
    //                if (!entity.HasComponent(tp))
    //                {
    //                    ex_result = false;
    //                    break;
    //                }
    //            }

    //            result = result | ex_result;
    //            if (result)
    //                break;
    //        }

    //        return result;
    //    }

    //    public bool MatchComponentType(string type)
    //    {
    //        return allTypes.ContainsKey(type);
    //    }
    //}
    #endregion

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
            euid_idx_map.Add(entity.uid, entity_container_ptr);

            entity_container_ptr++;
            ECS_Utils.SetArrayElement(ref entity_container, entity_container_ptr, entity);

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

        #region Component Pool

        public ECS_Component_Pool<T> GetPool<T>()
            where T : struct
        {
            var com_type = typeof(T);
            return ECS_Component_Pool<T>.GetPool(_context_idx, com_type);
        }

        public IECS_Component_Pool GetIPool<T>()
            where T : struct
        {
            return GetPool<T>();
        }

        //public IECS_Component_Pool FindIPool(Type type)
        //{
        //    IECS_Component_Pool ipool = null;
        //    component_pool_container.TryGetValue(type, out ipool);
        //    return ipool;
        //}

        #endregion

        #region Collection

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
                Debug.LogError("重复添加System: " + tp);
            systems_container.Add(tp, system);

        }

        ECS_Entity_Collections getSystemCollection(ECS_System system)
        {
            var type = system.GetType();
            if (!system_collections.ContainsKey(type))
                system_collections.Add(type, new ECS_Entity_Collections());
            return system_collections[type];
        }


        //int arr_excute_ptr = -1;
        //ECS_Entity[] excuteEntities = new ECS_Entity[1024];
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
                    var len = collection.RealLength;
                    for (int i = 0; i < len; i++)
                    {
                        var entity = entities[i];
                        if (system.GetTrigger(entity))
                        {
                            system.Excute(entity);
                        }
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

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick()
        {
            DoExcute();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LateTick()
        {
            for (int i = 0, l = component_pool_container.Length; i < l; i++)
                component_pool_container[i].CleanDirtyMark();
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