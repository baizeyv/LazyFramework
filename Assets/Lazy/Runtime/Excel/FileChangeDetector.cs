#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Lazy.Excel
{
    public class FileChangeDetector
    {
        private string _filePath;

        public byte[] Bytes { get; private set; }

        private string _lastFileHash;

        public FileChangeDetector(string filePath)
        {
            _filePath = filePath;
            Detect(); // # 这次检测主要是给lastFileHash赋值
        }

        public bool Detect()
        {
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(_filePath);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("正在使用") || e.Message.ToLower().Contains("sharing"))
                {
                    var tempPath = Path.GetTempFileName();
                    File.Copy(_filePath, tempPath, true);
                    bytes = File.ReadAllBytes(tempPath);
                    File.Delete(tempPath);
                }
                else
                {
                    Log.Log.MsgE(
                        $"FileChangeDetector.Detect error:{e} stack:{e.StackTrace}\nfilePath:{_filePath}"
                    );
                    return false;
                }
            }

            Bytes = bytes;

            var fileHash = GetHash(bytes);
            if (fileHash != _lastFileHash)
            {
                _lastFileHash = fileHash;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static string GetHash(byte[] bytes)
        {
            //计算哈希值，非MD5
            var sha256 = new System.Security.Cryptography.SHA256Managed();
            var hash = sha256.ComputeHash(bytes);
            return hash.Aggregate(string.Empty, (current, x) => current + $"{x:x2}");
        }
    }
}
#endif
