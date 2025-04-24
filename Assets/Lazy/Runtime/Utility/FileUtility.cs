using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using FileInfo = System.IO.FileInfo;

namespace Lazy
{
    public static class FileUtility
    {
        public static bool SafeCopyFile(string fromFile, string toFile)
        {
            try
            {
                if (string.IsNullOrEmpty(fromFile))
                    return false;

                if (!File.Exists(fromFile))
                    return false;

                CheckFileAndCreateDirWhenNeeded(toFile);
                SafeDeleteFile(toFile);
                File.Copy(fromFile, toFile, true);
                return true;
            }
            catch (Exception ex)
            {
                Log.MsgD(
                    $"SafeCopyFile failed! formFile = {fromFile}, toFile = {toFile}, with err = {ex.Message}"
                );
                return false;
            }
        }

        public static string SafeReadAllText(string file)
        {
            try
            {
                if (string.IsNullOrEmpty(file) || !File.Exists(file))
                    return null;
                File.SetAttributes(file, FileAttributes.Normal);
                return File.ReadAllText(file);
            }
            catch (Exception e)
            {
                Log.MsgE($"ReadAllText Failed: {file}, error: {e.Message}");
                return null;
            }
        }

        public static bool SafeDeleteFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return true;

                if (!File.Exists(filePath))
                    return true;

                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SafeDeleteFile failed! path = {filePath} with err: {ex.Message}");
                return false;
            }
        }

        public static bool SafeClearDir(string folderPath, string[] excludeName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(folderPath))
                    return true;

                if (Directory.Exists(folderPath))
                    DeleteDirectory(folderPath, excludeName);

                Directory.CreateDirectory(folderPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.MsgE($"SafeClearDir failed! path = {folderPath} with err = {ex.Message}");
                return false;
            }
        }

        public static bool SafeCopyDirectory(
            string sourceDirName,
            string destDirName,
            bool copySubDirs,
            string[] excludeName = null
        )
        {
            try
            {
                var dir = new DirectoryInfo(sourceDirName);
                if (dir.Exists == false)
                    throw new DirectoryNotFoundException(
                        "Source directory does not exist or could not be found: " + sourceDirName
                    );

                var dirs = dir.GetDirectories();
                if (Directory.Exists(destDirName) == false)
                    Directory.CreateDirectory(destDirName);

                var files = dir.GetFiles();
                foreach (var file in files)
                {
                    var copy = true;
                    if (excludeName != null)
                        foreach (var s in excludeName)
                            if (file.Name.EndsWith(s))
                                copy = false;

                    if (copy)
                    {
                        var temppath = Path.Combine(destDirName, file.Name);
                        file.CopyTo(temppath, true);
                    }
                }

                if (copySubDirs == true)
                    foreach (var subdir in dirs)
                    {
                        var temppath = Path.Combine(destDirName, subdir.Name);
                        SafeCopyDirectory(subdir.FullName, temppath, copySubDirs);
                    }

                return true;
            }
            catch (Exception ex)
            {
                Log.MsgE(
                    string.Format(
                        "SafeCopyDirectory failed! sourceDirName = {0}, destDirName = {1}, with err = {2}",
                        sourceDirName,
                        destDirName,
                        ex.Message
                    )
                );
                return false;
            }
        }

        public static bool SafeDeleteDir(string folderPath, string[] excludeName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(folderPath))
                    return true;

                if (Directory.Exists(folderPath))
                    DeleteDirectory(folderPath, excludeName);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SafeDeleteDir failed! path = {folderPath} with err: {ex.Message}");
                return false;
            }
        }

        private static void DeleteDirectory(string dirPath, string[] excludeName = null)
        {
            if (!Directory.Exists(dirPath))
                return;

            var files = Directory.GetFiles(dirPath);
            var dirs = Directory.GetDirectories(dirPath);

            foreach (var file in files)
            {
                var delete = true;
                if (excludeName != null)
                    foreach (var s in excludeName)
                        if (file.EndsWith(s))
                            delete = false;

                if (!delete)
                    continue;
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (var dir in dirs)
                DeleteDirectory(dir, excludeName);

            var filesAfter = Directory.GetFiles(dirPath);
            var dirsAfter = Directory.GetDirectories(dirPath);
            if (filesAfter.Length == 0 && dirsAfter.Length == 0)
                Directory.Delete(dirPath, false);
        }

        public static void CheckFileAndCreateDirWhenNeeded(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var fileInfo = new FileInfo(filePath);
            var dirInfo = fileInfo.Directory;
            if (dirInfo is { Exists: false })
                Directory.CreateDirectory(dirInfo.FullName);
        }

        public static bool SafeWriteAllText(string outFile, string text)
        {
            try
            {
                if (string.IsNullOrEmpty(outFile))
                    return false;

                CheckFileAndCreateDirWhenNeeded(outFile);
                if (File.Exists(outFile))
                    File.SetAttributes(outFile, FileAttributes.Normal);

                File.WriteAllText(outFile, text);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    string.Format(
                        "SafeWriteAllText failed! path = {0} with err = {1}",
                        outFile,
                        ex.Message
                    )
                );
                return false;
            }
        }

        public static string FormatToUnityPath(string path)
        {
            return path.Replace('\\', '/');
        }

        /// <summary>
        /// * 从路径的末尾向前截取指定级别的目录
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="levels"></param>
        /// <returns></returns>
        public static string TruncatePath(string fullPath, int levels)
        {
            for (var i = 0; i < levels; i++)
            {
                fullPath = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrEmpty(fullPath))
                    break;
            }

            return fullPath;
        }

        /// <summary>
        /// * 检测目录,若目录不存在则创建该目录
        /// </summary>
        /// <param name="folderPath"></param>
        public static void CheckOrCreateDir(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return;

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
        }

        public static string CreateMD5ForFile(string filename)
        {
            try
            {
                using (var file = new FileStream(filename, FileMode.Open))
                {
                    using (var md5 = MD5.Create())
                    {
                        var retVal = md5.ComputeHash(file);
                        var sb = new StringBuilder();
                        foreach (var str in retVal)
                            sb.Append(str.ToString("x2"));

                        return sb.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return "";
            }
        }

        public static long GetFileSize(string filePath)
        {
            long sum = 0;
            if (!File.Exists(filePath))
                return 0;

            var files = new FileInfo(filePath);
            sum += files.Length;
            return sum;
        }
    }
}
