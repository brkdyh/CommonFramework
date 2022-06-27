using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SampleECS
{
    public interface IDisposable
    {
        bool disposed { get; set; }
    }

    public class ECS_RecyclePool<T>
        where T : IDisposable, new()
    {
        protected int data_ptr = -1;
        public T[] data_pool = new T[0];

        Stack<int> disposedCache = new Stack<int>();

        //float releaseRate = 0.3f;
        //public void SetReleaseRate(float releaseRate)
        //{
        //    this.releaseRate = Mathf.Clamp(releaseRate, 0f, 1f);
        //}

        public bool ContainIndex(int idx) { return idx >= 0 && idx <= data_ptr; }

        public void Recycle(int idx)
        {
            if (!ContainIndex(idx))
                return;
            if (data_pool[idx].disposed)
                return;

            data_pool[idx].disposed = true;
            disposedCache.Push(idx);
            //Debug.Log("Recyle " + typeof(T).ToString() + " idx = " + idx);
        }

        public ref T Apply(out int idx)
        {
            if (disposedCache.Count > 0)
            {
                var av_idx = disposedCache.Pop();
                idx = av_idx;
                data_pool[idx].disposed = false;
                //Debug.Log("Reapply " + typeof(T).ToString());
                return ref data_pool[idx];
            }

            T data = new T();
            data_ptr++;
            ECS_Utils.SetArrayElement(ref data_pool, data_ptr, data);
            idx = data_ptr;
            return ref data_pool[idx];
        }

        public ref T Apply<W>(out int idx)
            where W : T, new()
        {
            if (disposedCache.Count > 0)
            {
                var av_idx = disposedCache.Pop();
                idx = av_idx;
                data_pool[idx].disposed = false;
                //Debug.Log("Reapply " + typeof(T).ToString());
                return ref data_pool[idx];
            }

            W data = new W();
            data_ptr++;
            ECS_Utils.SetArrayElement(ref data_pool, data_ptr, data);
            idx = data_ptr;
            return ref data_pool[idx];
        }

        public bool TryGetData(int idx, out T data)
        {
            if (ContainIndex(idx))
            {
                data = data_pool[idx];
                return true;
            }
            data = default;
            return false;
        }

        public void Clean()
        {
            data_ptr = -1;
            data_pool = new T[0];
            disposedCache.Clear();
        }

        public void RecyleAll()
        {
            for (int i = 0; i < data_ptr + 1; i++)
                Recycle(i);
            //Debug.Log(typeof(T) + " RecyleAll");
        }
    }
}