using System.Collections.Generic;
using UnityEngine;

namespace SampleECS
{
    [HideInInspector]
    public class ECS_Runtime : MonoBehaviour
    {
        static ECS_Runtime _singleton;
        static ECS_Runtime Runtime { get { return GetRuntime(); } }

        public static ECS_Runtime GetRuntime()
        {
            if (_singleton != null)
                return _singleton;

            GameObject go = new GameObject("ECS_Runtime");
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            _singleton = go.AddComponent<ECS_Runtime>();
            return _singleton;
        }

        List<ECS_Context> _Contexts = new List<ECS_Context>();

        Queue<ECS_Context> _ContextInjectQueue = new Queue<ECS_Context>();
        public static void InjectContext(ECS_Context context)
        {
            if (context == null)
                return;
            Runtime._ContextInjectQueue.Enqueue(context);
            Runtime.updateContext = true;
        }

        Stack<ECS_Context> _ContextDisposeStack = new Stack<ECS_Context>();
        public static void DisposeContext(ECS_Context context)
        {
            if (context == null)
                return;
            Runtime._ContextDisposeStack.Push(context);
            Runtime.updateContext = true;
        }

        bool updateContext = false;
        void ContextsChange()
        {
            if (!updateContext)
                return;

            while (_ContextInjectQueue.Count > 0)
            {
                var ctx = _ContextInjectQueue.Dequeue();
                if (!_Contexts.Contains(ctx))
                    _Contexts.Add(ctx);
            }

            while (_ContextDisposeStack.Count > 0)
            {
                var ctx = _ContextDisposeStack.Pop();
                _Contexts.Remove(ctx);
            }
        }

        void TickContexts()
        {
            for (int i = 0, l = _Contexts.Count; i < l; i++)
                _Contexts[i].Tick();
        }

        #region Unity Function

        private void Awake()
        {
            if (_singleton == null)
                _singleton = this;
        }

        private void Update()
        {
            ContextsChange();
            TickContexts();
        }

        #endregion
    }
}