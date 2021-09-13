using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using UnityEditor;
using System;
using System.Threading;

namespace EasyAsset
{
    public enum BundleDecompressResult
    {
        Succeed,                //成功
        Decompressing,          //正在解压中
        NoEntry,                //空压缩包
        NotMatchRaw,            //解压后的文件与原始资源文件不匹配
        OtherError,             //其他错误
    }

    public class BundleCompress
    {
        public static void Compress(string filePath, string outPath, string password)
        {
            var fileName = Path.GetFileName(filePath);
            ZipEntry zipEntry = new ZipEntry(fileName);
            zipEntry.DateTime = DateTime.Now;
            using (ZipOutputStream zipOut = new ZipOutputStream(File.Create(outPath)))
            {
                zipOut.Password = password;
                zipOut.PutNextEntry(zipEntry);
                using (var fs = File.OpenRead(filePath))
                {
                    byte[] data = new byte[fs.Length];
                    fs.Read(data, 0, data.Length);
                    zipOut.Write(data, 0, data.Length);
                }
            }
        }

        const int READ_BLOCK_SIZE = 4096;

        static bool isDecompressing = false;
        static MemoryStream dataMemoryStream = null;
        static ZipInputStream currentInputStream = null;
        static bool handleZipFinish = false;
        static bool handleZipFailed = false;
        static Thread currentThread = null;

        static string zipPath = "";
        static string outPath = "";
        static Action<BundleDecompressResult, string> onDecopressCB = null;

        static BundleDecompressResult error;

        static void ResetDecompress()
        {
            isDecompressing = false;
            if (dataMemoryStream != null)
            {
                dataMemoryStream.Dispose();
                dataMemoryStream = null;
            }
            if (currentInputStream != null)
            {
                currentInputStream.Dispose();
                currentInputStream = null;
            }

            currentThread = null;
            handleZipFinish = false;
        }

        public static void BeginDecompress(byte[] data, string zipPath, string outPath,Action<BundleDecompressResult, string> onDecopress)
        {
            if (isDecompressing)
            {
                InvokeCB(onDecopress, BundleDecompressResult.Decompressing, "");
                return;
            }

            isDecompressing = true;
            onDecopressCB = onDecopress;
            BundleCompress.zipPath = zipPath;
            BundleCompress.outPath = outPath;
            dataMemoryStream = new MemoryStream(data);
            ZipEntry zipEntry = null;
            currentInputStream = new ZipInputStream(dataMemoryStream);
            currentInputStream.Password = Setting.config.CompressPassword;
            if (null != (zipEntry = currentInputStream.GetNextEntry()))
            {
                if (string.IsNullOrEmpty(zipEntry.Name))
                {
                    ResetDecompress();
                    InvokeCB(onDecopress, BundleDecompressResult.NoEntry, "");
                    return;
                }

                currentThread = new Thread(HandleZip);
                currentThread.Start();
            }
        }

        static void HandleZip()
        {
            FileStream zip_fs = null;
            FileStream bd_fs = null;
            try
            {
                //保存ZIP
                zip_fs = File.Create(zipPath);
                var data = dataMemoryStream.ToArray();
                zip_fs.Write(data, 0, data.Length);
                zip_fs.Flush();
                zip_fs.Close();

                if (File.Exists(outPath))
                    File.Delete(outPath);

                var bd_length = 0l;
                bd_fs = File.Create(outPath);
                //读取ZIP
                while (true)
                {
                    byte[] readBlock = new byte[READ_BLOCK_SIZE];
                    int real_read = currentInputStream.Read(readBlock, 0, readBlock.Length);
                    bd_fs.Write(readBlock, 0, real_read);

                    if (real_read <= 0)
                    {
                        bd_length = bd_fs.Length;
                        break;
                    }
                }

                bd_fs.Flush();
                bd_fs.Close();

                var bd_name = Path.GetFileName(outPath);
                var rm_info = BundleCheck.Instance.remoteBundleInfo;
                if (rm_info.raw_bundles.ContainsKey(bd_name))
                {
                    if (rm_info.raw_bundles[bd_name].bundleSize != bd_length)
                    {
                        error = BundleDecompressResult.NotMatchRaw;
                        handleZipFailed = true;
                        return;
                    }
                    //else
                    //{
                        //Debug.Log(bd_name + " : " + rm_info.raw_bundles[bd_name].bundleSize + " = " + bd_length);
                    //}
                }
            }
            catch (Exception ex)
            {
                if (zip_fs != null)
                    zip_fs.Close();
                if (bd_fs != null)
                    bd_fs.Close();

                handleZipFailed = true;
                Debug.LogException(ex);
                return;
            }

            handleZipFinish = true;
        }

        static void OnDecompress()
        {
            ResetDecompress();
            InvokeCB(onDecopressCB, BundleDecompressResult.Succeed, outPath);
        }

        static void OnDecompressFailed(BundleDecompressResult error)
        {
            ResetDecompress();
            InvokeCB(onDecopressCB, error, outPath);
        }

        static void InvokeCB(Delegate del, BundleDecompressResult result, string outPath)
        {
            try
            {
                del.DynamicInvoke(result, outPath);
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void Tick()
        {
            if (!isDecompressing)
                return;

            if (handleZipFailed)
                OnDecompressFailed(error);
            else if (handleZipFinish)
                OnDecompress();
        }
    }
}
