using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace EasyAsset
{
    public static class Utils
    {
        public static string GetMD5(string filePath)
        {
            byte[] bs = File.ReadAllBytes(filePath);
            if (bs == null || bs.Length == 0)
                return "";
            return GetMD5(bs);
        }

        public static string GetMD5(byte[] bs)
        {
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(bs);
            string str = "";
            for (int i = 0; i < hash.Length; i++)
            {
                str += hash[i].ToString("x");
            }
            return str;
        }
    }
}
