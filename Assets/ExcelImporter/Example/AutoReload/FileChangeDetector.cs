using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace AutoReloadDemo
{
    public class FileChangeDetector
    {
        public byte[] bytes { get; private set; }
        private string filePath;

        private string lastFileHash;

        public FileChangeDetector(string filePath)
        {
            this.filePath = filePath;
            Detect();
        }

        public bool Detect()
        {
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(filePath);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("正在使用") || e.Message.ToLower().Contains("sharing"))
                {
                    string tempPath = Path.GetTempFileName();
                    File.Copy(filePath, tempPath, true);
                    bytes = File.ReadAllBytes(tempPath);
                    File.Delete(tempPath);
                }
                else
                {
                    Debug.LogError(
                        $"FileChangeDetector.Detect error:{e} stack:{e.StackTrace}\nfilePath:{filePath}"
                    );
                    return false;
                }
            }

            if (bytes != null)
            {
                this.bytes = bytes;
            }
            else
            {
                Debug.LogError("FileChangeDetector.Detect 未知原因读取失败");
                return false;
            }

            string fileHash = GetHash(bytes);
            if (fileHash != lastFileHash)
            {
                lastFileHash = fileHash;
                return true;
            }
            else
            {
                return false;
            }
        }

        static string GetHash(byte[] bytes)
        {
            //计算哈希值，非MD5
            System.Security.Cryptography.SHA256Managed sha256 =
                new System.Security.Cryptography.SHA256Managed();
            byte[] hash = sha256.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += string.Format("{0:x2}", x);
            }
            return hashString;
        }
    }
}
