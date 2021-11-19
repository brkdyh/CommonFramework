using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class RunTest : MonoBehaviour
{
    int[] ds = new int[1000];

    Dictionary<int, byte> kp = new Dictionary<int, byte>();
    void Start()
    {
        var rd_idx = Random.Range(0, 1000);
        ds[rd_idx] = 100;
        Debug.Log("random index = " + rd_idx);

        for(int i = 0; i < 1000; i++)
        {
            kp.Add(i, 0);
        }
    }

    void Update()
    {
        //每帧查找10万次
        for (int i = 0; i < 100000; i++)
        {
            Profiler.BeginSample("S=>Array");
            //for (int j = 0, l = ds.Length; j < l; j++)
            //{
            //    if (ds[j] == 100)
            //        break;
            //}
            if ("dhsgfhsjghskfgjsjdhfskfjshf" == "dhsgfhsjghskfgjsjdhfskfjshf") ;
            Profiler.EndSample();

            //Profiler.BeginSample("S=>Dictionary");
            //kp.ContainsKey(100);
            //Profiler.EndSample();
        }
    }
}
