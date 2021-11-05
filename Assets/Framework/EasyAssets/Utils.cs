using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace EasyAssets
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

        public struct VersionStruct
        {
            public int v1;
            public int v2;
            public int v3;
            public int build;

            public static VersionStruct From(string str)
            {
                VersionStruct vs = new VersionStruct();
                var sps = str.Split('_');
                var versions_str = sps[0].Split('.');
                var build_str = sps[1];
                vs.v1 = int.Parse(versions_str[0]);
                vs.v2 = int.Parse(versions_str[1]);
                vs.v3 = int.Parse(versions_str[2]);
                vs.build = int.Parse(build_str);
                return vs;
            }

            public static bool LessThan(VersionStruct version, VersionStruct compare)
            {
                if (version.v1 < compare.v1)
                    return true;
                if (version.v2 < compare.v2)
                    return true;
                if (version.v3 < compare.v3)
                    return true;
                if (version.build < compare.build)
                    return true;

                return false;
            }
        }

        public static bool VersionLessThan(string version, string compare)
        {
            return VersionStruct.LessThan(VersionStruct.From(version), VersionStruct.From(compare));
        }
    }
}
