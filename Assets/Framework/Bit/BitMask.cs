using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BitMask
{
    const int BIT_LENGTH = 32;
    const uint FULL_BIT = 0xFFFFFFFF;

    List<uint> maskList = new List<uint>();

    public int MaxIndex
    {
        get
        {
            return maskList.Count * BIT_LENGTH - 1;
        }
    }

    public bool GetMaskValue(int index)
    {
        var u_idx = index / BIT_LENGTH;
        var m_idx = index % BIT_LENGTH;
        if (u_idx < maskList.Count)
        {
            var cur_mask = maskList[u_idx];
            var bit_num = 1u << m_idx;
            return (cur_mask & bit_num) > 0;
        }

        return false;
    }

    public void SetMaskValue(int index, bool value)
    {
        var u_idx = index / BIT_LENGTH;
        var m_idx = index % BIT_LENGTH;

        Debug.Log("u_idx = " + u_idx + ",m_idx = " + m_idx);
        if (u_idx >= maskList.Count)
        {
            if (value == false)
                return;

            //增加位数
            int offset = (u_idx + 1 - maskList.Count);
            for (int i = 0; i < offset; i++)
                maskList.Add(0u);
        }

        var cur_mask = maskList[u_idx];
        var bit_num = 1u << m_idx;

        if (value)
            maskList[u_idx] = cur_mask | bit_num;
        else
            maskList[u_idx] = cur_mask & (FULL_BIT - bit_num);
    }

    public override string ToString()
    {
        if (maskList.Count == 0)
            return "null";
        string s = "";
        for (int i = 0; i < maskList.Count; i++)
        {
            if (i < maskList.Count - 1)
                s += maskList[i].ToString("X") + ":";
            else
                s += maskList[i].ToString("X");
        }
        return s;
    }

    public static BitMask From(uint[] data)
    {
        BitMask mask = new BitMask();
        mask.maskList.AddRange(data);
        return mask;
    }

    public static BitMask From(List<uint> data)
    {
        BitMask mask = new BitMask();
        mask.maskList.AddRange(data);
        return mask;
    }

    public static BitMask From(string data)
    {
        BitMask mask = new BitMask();

        if (string.IsNullOrEmpty(data))
            return mask;

        string[] mask_str = data.Split(':');
        for (int i = 0; i < mask_str.Length; i++)
        {
            var mask_value = uint.Parse(mask_str[i], System.Globalization.NumberStyles.HexNumber);
            mask.maskList.Add(mask_value);
        }

        return mask;
    }

    public List<int> GetTrueIndexList()
    {
        List<int> res = new List<int>();
        for (int i = 0; i <= MaxIndex; i += BIT_LENGTH)
        {
            var u_idx = i / BIT_LENGTH;

            bool full = maskList[u_idx] == FULL_BIT;

            for (int j = 0; j < BIT_LENGTH; j++)
            {
                var idx = i + j;
                if (full)
                    res.Add(idx);
                else if (GetMaskValue(idx))
                    res.Add(idx);
            }
        }

        return res;
    }
}
