using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace Lazy
{
    public static class ZipUtility
    {
        private const int BufferSize = 2048;

        public interface IZipCallback
        {
            /// <summary>
            /// 预压缩
            /// </summary>
            /// <param name="entry"></param>
            /// <returns>true表示继续执行</returns>
            bool OnPreZip(ZipEntry entry);

            void OnPostZip(ZipEntry entry);

            void OnFinished(string result);
        }

        public class ZipResult : IZipCallback
        {
            public bool OnPreZip(ZipEntry entry)
            {
                if (!entry.IsFile)
                    return true;
                var extension = Path.GetExtension(entry.Name).ToLower();
                return !extension.Equals(".meta") && !extension.Equals(".ds_store");
            }

            public void OnPostZip(ZipEntry entry) { }

            public void OnFinished(string result)
            {
                Log.MsgD($"Zip Finished: {result}");
            }
        }

        /// <summary>
        /// * 解压缩Zip文件
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="destinationDirectory"></param>
        /// <param name="password"></param>
        /// <param name="coverFile"></param>
        /// <returns></returns>
        public static bool UnZipFile(
            string sourceFile,
            string destinationDirectory = null,
            string password = null,
            bool coverFile = false
        )
        {
            var result = false;
            if (!File.Exists(sourceFile))
            {
                Log.MsgE($"要解压的文件不存在: {sourceFile}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(destinationDirectory))
                destinationDirectory = Path.GetDirectoryName(sourceFile);

            FileUtility.CheckOrCreateDir(destinationDirectory);
            try
            {
                using (
                    var zipStream = new ZipInputStream(
                        File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read)
                    )
                )
                {
                    zipStream.Password = password;
                    var zipEntry = zipStream.GetNextEntry();
                    while (zipEntry != null)
                    {
                        if (zipEntry.IsDirectory)
                        {
                            // # 若是文件夹则创建
                            var path = Path.Combine(
                                destinationDirectory,
                                Path.GetDirectoryName(zipEntry.Name)
                            );
                            FileUtility.CheckOrCreateDir(path);
                        }
                        else
                        {
                            var fileName = Path.GetFileName(zipEntry.Name);
                            if (!string.IsNullOrEmpty(fileName) && fileName.Trim().Length > 0)
                            {
                                var path = Path.Combine(destinationDirectory, zipEntry.Name);
                                if (File.Exists(path))
                                {
                                    if (coverFile)
                                    {
                                        FileUtility.SafeDeleteFile(path);
                                    }
                                    else
                                    {
                                        zipEntry = zipStream.GetNextEntry();
                                        continue;
                                    }
                                }

                                if (!File.Exists(path))
                                {
                                    var fileItem = new FileInfo(path);
                                    using (var writeStream = fileItem.Create())
                                    {
                                        var buffer = new byte[BufferSize];
                                        var readLength = 0;
                                        do
                                        {
                                            readLength = zipStream.Read(buffer, 0, BufferSize);
                                            writeStream.Write(buffer, 0, readLength);
                                        } while (readLength == BufferSize);

                                        writeStream.Flush();
                                        writeStream.Close();
                                    }
                                }
                            }
                        }

                        // # 获取下一个文件
                        zipEntry = zipStream.GetNextEntry();
                    }

                    zipStream.Close();
                }

                result = true;
            }
            catch (Exception e)
            {
                GC.Collect();
                Log.MsgE($"文件解压错误: {e.Message}");
                return result;
            }

            Log.MsgD($"解压完成: {sourceFile}");
            GC.Collect();
            return true;
        }

        public static IEnumerator UnZipFileCoroutine(
            string sourceFile,
            string destinationDirectory = null,
            string password = null,
            bool coverFile = false
        )
        {
            if (!File.Exists(sourceFile))
            {
                Log.MsgE($"要解压的文件不存在: {sourceFile}");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(destinationDirectory))
                destinationDirectory = Path.GetDirectoryName(sourceFile);

            FileUtility.CheckOrCreateDir(destinationDirectory);
            using (
                var zipStream = new ZipInputStream(
                    File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read)
                )
            )
            {
                zipStream.Password = password;
                var zipEntry = zipStream.GetNextEntry();
                while (zipEntry != null)
                {
                    if (zipEntry.IsDirectory)
                    {
                        // # 若是文件夹则创建
                        var path = Path.Combine(
                            destinationDirectory,
                            Path.GetDirectoryName(zipEntry.Name)
                        );
                        FileUtility.CheckOrCreateDir(path);
                    }
                    else
                    {
                        var fileName = Path.GetFileName(zipEntry.Name);
                        if (!string.IsNullOrEmpty(fileName) && fileName.Trim().Length > 0)
                        {
                            var path = Path.Combine(destinationDirectory, zipEntry.Name);
                            if (File.Exists(path))
                            {
                                if (coverFile)
                                {
                                    FileUtility.SafeDeleteFile(path);
                                }
                                else
                                {
                                    zipEntry = zipStream.GetNextEntry();
                                    continue;
                                }
                            }

                            if (!File.Exists(path))
                            {
                                try
                                {
                                    var fileItem = new FileInfo(path);
                                    using (var writeStream = fileItem.Create())
                                    {
                                        var buffer = new byte[BufferSize];
                                        var readLength = 0;
                                        do
                                        {
                                            readLength = zipStream.Read(buffer, 0, BufferSize);
                                            writeStream.Write(buffer, 0, readLength);
                                        } while (readLength == BufferSize);

                                        writeStream.Flush();
                                        writeStream.Close();
                                    }
                                }
                                catch (Exception e)
                                {
                                    Log.MsgE($"解压错误: {e.Message}");
                                }

                                // # 解压一个等待一帧
                                yield return null;
                            }
                        }
                    }

                    // # 获取下一个文件
                    zipEntry = zipStream.GetNextEntry();
                }

                zipStream.Close();
            }

            Log.MsgD($"解压完成: {sourceFile}");
            GC.Collect();
        }

        /// <summary>
        /// * 异步解压文件
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="destinationDirectory"></param>
        /// <param name="password"></param>
        /// <param name="coverFile"></param>
        public static async Task UnZipFileAsync(
            string sourceFile,
            string destinationDirectory = null,
            string password = null,
            bool coverFile = false
        )
        {
            await Task.Run(() =>
            {
                UnZipFile(sourceFile, destinationDirectory, password, coverFile);
            });
        }

        /// <summary>
        /// * 压缩
        /// </summary>
        /// <param name="pFileOrDirArray">需要压缩的文件和文件夹</param>
        /// <param name="pZipFilePath">输出的zip文件完整路径</param>
        /// <param name="pPassword">密码</param>
        /// <param name="callback">回调</param>
        /// <param name="pZipLevel">压缩等级</param>
        /// <returns></returns>
        public static bool Zip(
            string[] pFileOrDirArray,
            string pZipFilePath,
            string pPassword = null,
            IZipCallback callback = null,
            int pZipLevel = 6
        )
        {
            if (pFileOrDirArray == null)
            {
                callback?.OnFinished("输入路径为空");
                return false;
            }

            if (string.IsNullOrEmpty(pZipFilePath))
            {
                callback?.OnFinished("输出路径为空");
                return false;
            }

            var zipOutputStream = new ZipOutputStream(File.Create(pZipFilePath));
            zipOutputStream.SetLevel(pZipLevel);
            zipOutputStream.Password = pPassword;

            foreach (var fileorDirectory in pFileOrDirArray)
            {
                var result = false;
                if (Directory.Exists(fileorDirectory))
                    result = ZipDirectory(fileorDirectory, string.Empty, zipOutputStream, callback);
                else if (File.Exists(fileorDirectory))
                    result = ZipFile(fileorDirectory, string.Empty, zipOutputStream, callback);

                if (!result)
                {
                    GC.Collect();
                    callback?.OnFinished($"压缩失败: {fileorDirectory}");
                    return false;
                }
            }

            zipOutputStream.Finish();
            zipOutputStream.Close();
            zipOutputStream = null;
            GC.Collect();
            callback?.OnFinished($"压缩完成: {string.Join(", ", pFileOrDirArray)}");
            return true;
        }

        /// <summary>
        /// * 压缩指定文件
        /// </summary>
        /// <param name="pFileName">需要压缩的文件名</param>
        /// <param name="pParentPath">相对路径</param>
        /// <param name="pZipOutputStream">压缩输出流</param>
        /// <param name="callback">回调</param>
        /// <returns></returns>
        private static bool ZipFile(
            string pFileName,
            string pParentPath,
            ZipOutputStream pZipOutputStream,
            IZipCallback callback = null
        )
        {
            ZipEntry entry = null;
            FileStream fileStream = null;
            try
            {
                var path = pParentPath + Path.GetFileName(pFileName);
                entry = new ZipEntry(path) { DateTime = DateTime.Now };
                if (callback != null && !callback.OnPreZip(entry))
                    return true; // # 过滤

                fileStream = File.OpenRead(pFileName);
                var buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, buffer.Length);
                fileStream.Close();
                entry.Size = buffer.Length;

                pZipOutputStream.PutNextEntry(entry);
                pZipOutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                Log.MsgE($"压缩失败: {e.Message}");
                return false;
            }
            finally
            {
                if (null != fileStream)
                {
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }

            callback.OnPostZip(entry);
            return true;
        }

        /// <summary>
        /// * 压缩文件夹
        /// </summary>
        /// <param name="pDirPath">文件夹路径</param>
        /// <param name="pParentPath">相对路径</param>
        /// <param name="pZipOutputStream">压缩输出流</param>
        /// <param name="callback">回调</param>
        /// <returns></returns>
        private static bool ZipDirectory(
            string pDirPath,
            string pParentPath,
            ZipOutputStream pZipOutputStream,
            IZipCallback callback = null
        )
        {
            ZipEntry entry = null;
            var path = Path.Combine(pParentPath, GetDirName(pDirPath));
            try
            {
                entry = new ZipEntry(path) { DateTime = DateTime.Now, Size = 0 };
                if (callback != null && !callback.OnPreZip(entry))
                    return true; // # 过滤
                pZipOutputStream.PutNextEntry(entry);
                pZipOutputStream.Flush();
                var files = Directory.GetFiles(pDirPath);
                foreach (var file in files)
                    ZipFile(
                        file,
                        Path.Combine(pParentPath, GetDirName(pDirPath)),
                        pZipOutputStream,
                        callback
                    );
            }
            catch (Exception e)
            {
                Log.MsgE($"压缩失败, {e.Message}");
                return false;
            }

            var directories = Directory.GetDirectories(pDirPath);
            foreach (var dir in directories)
                if (
                    !ZipDirectory(
                        dir,
                        Path.Combine(pParentPath, GetDirName(pDirPath)),
                        pZipOutputStream,
                        callback
                    )
                )
                    return false;
            callback?.OnPostZip(entry);
            return true;
        }

        private static string GetDirName(string pPath)
        {
            if (!Directory.Exists(pPath))
                return string.Empty;

            pPath = pPath.Replace("\\", "/");
            var ss = pPath.Split('/');
            if (string.IsNullOrEmpty(ss[^1]))
                return ss[^2] + "/";
            return ss[^1] + "/";
        }
    }
}
