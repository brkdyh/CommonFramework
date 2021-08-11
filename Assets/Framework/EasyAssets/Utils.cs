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

        public static long GetFileSize(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            if (fi.Exists)
                return fi.Length;

            return 0;
        }

        const float KB = 1024;
        const float MB = KB * KB;
        const float GB = MB * MB;
        public static string FormatBytesUnit(float byteLength, string format = "0.00")
        {
            if (byteLength < KB)
                return byteLength.ToString("0") + "B";
            else if (byteLength < MB)
                return (byteLength / KB).ToString(format) + "KB";
            else if (byteLength < GB)
                return (byteLength / MB).ToString(format) + "MB";
            else
                return (byteLength / GB).ToString(format) + "GB";
        }
    }
}
