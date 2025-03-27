using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lazy.Editor.Build;
using Lazy.Res;
using Lazy.Utility;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Lazy.Editor.AssetEditor
{
    public class AssetBundleBuildTool : ScriptableObject
    {
        private static Dictionary<string, AssetMapping> _assetMapping;

        private static Dictionary<string, string[]> _resourceMapping;

        /// <summary>
        /// * AssetBundle名与资产文件名不同时查找使用
        /// </summary>
        private static Dictionary<string, string> _diffAssetPathMapping;

        /// <summary>
        /// * 打包后AssetBundle名加上MD5
        /// ! 微信小游戏使用
        /// </summary>
        private const bool AppendHashToAssetBundleName = false;

        public static void BuildAllAssetBundles()
        {
            AssetDatabase.RemoveUnusedAssetBundleNames();

            // 获取“StreamingAssets”文件夹路径（不一定这个文件夹，可自定义）
            var assetBundleOutputPathDir = PathSetting.AssetBundlesOutPath;
            // ! AB包名为空时自动设置
            GenerateAssetNames();
            GenerateResourceNames();
            AssetDatabase.Refresh();
            Debug.Log("自动设置AssetBundleName（AB名为空时）");

            FileUtility.CheckOrCreateDir(assetBundleOutputPathDir);
            AssetDatabase.Refresh();

            Caching.ClearCache();
            // ! 打包生成AB包 (目标平台自动根据当前平台设置，WebGL不可使用BuildAssetBundleOptions.None压缩)
            BuildPipeline.BuildAssetBundles(
                assetBundleOutputPathDir,
                BuildAssetBundleOptions.ChunkBasedCompression,
                EditorUserBuildSettings.activeBuildTarget
            );
            AssetDatabase.Refresh();
            Debug.Log(
                "打包AssetBundle："
                    + PathSetting.AssetBundlesOutPath
                    + "  当前打包平台："
                    + EditorUserBuildSettings.activeBuildTarget
            );

            // # 清理多余文件夹和ab
            DeleteRemovedAssetBundles();

            // 复制AssetBundle到Stream打包目录
            var outPath = PathSetting.StreamingAssetsPath;
            FileUtility.SafeClearDir(outPath);
            FileUtility.CheckOrCreateDir(outPath);
            FileUtility.SafeCopyDirectory(assetBundleOutputPathDir, outPath, true);
            AssetDatabase.Refresh();

            // # 等待AB打包完成，再写入数据
            GenerateAssetNames(true);
            GenerateResourceNames(true);
            Debug.Log(
                $"写入资产数据 生成：{nameof(AssetBundleMapping)}.json，生成：{nameof(ResourceMapping)}.json"
            );
            FileUtility.SafeClearDir(outPath);
            FileUtility.CheckOrCreateDir(outPath);
            CopyLocalAssetBundle(assetBundleOutputPathDir, outPath);
            AssetDatabase.Refresh();

            Debug.Log("资产打包成功!");
        }

        /// <summary>
        /// * 将需要打到包中的AssetBundle复制到Application.StreamingAssetsPath中
        /// </summary>
        private static void CopyLocalAssetBundle(string sourcePath, string targetPath)
        {
            // TODO: 只复制需要打到本地包中的AssetBundle及Manifest
            if (LazyBuildTool.EnableHotUpdate)
            {
                // # 需要热更的情况只把需要打到包中的AssetBundle复制过去
                var localBundles = EditorPrefs.GetString(EditorConstant.LocalAssetBundlesKey, "");
                if (string.IsNullOrEmpty(localBundles)) // # 不需要将任何AssetBundle打入包中
                    return;
                var locals = localBundles.Split(';');
                Stack<string> stack = new();
                stack.Push(sourcePath);
                while (stack.Count > 0)
                {
                    var currentPath = stack.Pop();
                    // # 检查目录
                    var directories = Directory.GetDirectories(currentPath);
                    foreach (var directory in directories)
                        stack.Push(directory);
                    // # 检查文件
                    var files = Directory.GetFiles(currentPath);
                    foreach (var file in files)
                    {
                        var extension = Path.GetExtension(file).ToLower();
                        if (
                            extension != ".meta"
                            && extension != ".manifest"
                            && extension != ".ds_store"
                        )
                        {
                            var filename = Path.GetFileNameWithoutExtension(file);
                            if (
                                locals.Contains(filename)
                                || filename.Equals("Windows")
                                || filename.Equals("macOS")
                                || filename.Equals("Linux")
                                || filename.Equals("Android")
                                || filename.Equals("iOS")
                                || filename.Equals("WebGL")
                                || filename.Equals("Unknown")
                            )
                            {
                                FileUtility.SafeCopyFile(file, targetPath + "/" + filename);
                                FileUtility.SafeCopyFile(
                                    file + ".manifest",
                                    targetPath + "/" + filename + ".manifest"
                                );
                            }
                        }
                    }
                }
            }
            else
            {
                // # 不需要热更的情况就全量复制AssetBundle
                FileUtility.SafeCopyDirectory(sourcePath, targetPath, true);
            }
        }

        /// <summary>
        /// * 生成AssetBundle资源索引
        /// </summary>
        /// <param name="isWrite"></param>
        private static void GenerateAssetNames(bool isWrite = false)
        {
            if (isWrite)
                _diffAssetPathMapping = null;
            else
                _diffAssetPathMapping = new Dictionary<string, string>();

            FileUtility.CheckOrCreateDir(PathSetting.AssetBundlesFolder);
            if (Directory.Exists(PathSetting.AssetBundlesFolder))
            {
                // # 该文件夹已存在

                // # 获取其所有子文件夹路径
                var subFolderPaths = Directory.GetDirectories(
                    PathSetting.AssetBundlesFolder,
                    "*",
                    SearchOption.AllDirectories
                );
                // # 获取该文件夹下的所有文件的路径
                var subFilePaths = Directory.GetFiles(
                    PathSetting.AssetBundlesFolder,
                    "*",
                    SearchOption.AllDirectories
                );

                // #合并文件夹和文件的路径，可以根据需要调整顺序
                var allPaths = subFilePaths.Concat(subFolderPaths).ToArray();

                var tmpNames = new List<string>();

                _assetMapping = new Dictionary<string, AssetMapping>();

                // # 遍历所有路径
                foreach (var filePath in allPaths)
                {
                    if (
                        Path.GetExtension(filePath).Equals(".meta")
                        || Path.GetExtension(filePath).Equals(".DS_Store")
                    )
                        // # 排除meta文件和OSX系统拉的shit
                        continue;
                    var path = FileUtility.FormatToUnityPath(filePath);

                    // # 获取不带扩展名的文件名
                    var filenameWithoutExtension = Path.GetFileNameWithoutExtension(path);

                    var assetPath = AssetBundlePathHelper.GetAssetPath(path);

                    if (File.Exists(path))
                    {
                        // # 文件

                        var abName = SetAssetBundleName(assetPath);

                        if (!isWrite)
                            continue;

                        if (tmpNames.Contains(filenameWithoutExtension.ToLower()))
                        {
                            // # 生成唯一ID
                            var id = Guid.NewGuid().ToString();
                            filenameWithoutExtension += id;
                            Debug.Log(
                                "资源名称重复（大小写不敏感）："
                                    + filePath
                                    + "，增加唯一识别ID后为："
                                    + filenameWithoutExtension
                            );
                        }

                        tmpNames.Add(filenameWithoutExtension.ToLower());

                        // # 只留下一个 assetPath
                        var assetPathsForAbName = new List<string> { assetPath.ToLower() };

                        _assetMapping.Add(
                            filenameWithoutExtension,
                            new AssetMapping(
                                abName.ToLower(),
                                assetPathsForAbName.ToArray(),
                                LazyBuildTool.ToVersion,
                                FileUtility.GetFileSize(
                                    AssetBundlePathHelper.GetAssetBundleFullName(abName.ToLower())
                                ),
                                FileUtility.CreateMD5ForFile(
                                    AssetBundlePathHelper.GetAssetBundleFullName(abName.ToLower())
                                ),
                                GetPackage(path),
                                false
                            )
                        );
                    }
                    else if (Directory.Exists(path))
                    {
                        // # 是文件夹

                        if (!isWrite)
                            continue;

                        // # 文件夹资产信息，使用资产名名代替
                        var assetNameDir = Directory
                            .GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                            .Where(p =>
                                !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
                                && !p.EndsWith(".DS_Store", StringComparison.OrdinalIgnoreCase)
                            )
                            .Select(Path.GetFileNameWithoutExtension)
                            .ToArray();

                        filenameWithoutExtension += AssetManager.DirectorySuffix;

                        if (tmpNames.Contains(filenameWithoutExtension))
                        {
                            var id = Guid.NewGuid().ToString();
                            filenameWithoutExtension += id;
                            Debug.Log(
                                "文件夹名称重复（大小写不敏感）："
                                    + path
                                    + "，增加唯一识别ID后为："
                                    + filenameWithoutExtension
                            );
                        }

                        tmpNames.Add(filenameWithoutExtension);

                        _assetMapping.Add(
                            filenameWithoutExtension,
                            new AssetMapping(
                                "",
                                assetNameDir,
                                LazyBuildTool.ToVersion,
                                0,
                                "",
                                "",
                                false
                            )
                        );
                    }
                }

                if (isWrite)
                {
                    // # 把总的manifest加上
                    if (tmpNames.Contains(PathSetting.GetPlatformName()))
                        Debug.LogError(
                            "总AssetBundleManifest和其他资产名重复，请检查资产："
                                + PathSetting.GetPlatformName()
                        );
                    else
                        _assetMapping.Add(
                            PathSetting.GetPlatformName(),
                            new AssetMapping(
                                PathSetting.GetPlatformName(),
                                new string[] { },
                                LazyBuildTool.ToVersion,
                                FileUtility.GetFileSize(
                                    AssetBundlePathHelper.GetAssetBundleFullName(
                                        PathSetting.GetPlatformName()
                                    )
                                ),
                                FileUtility.CreateMD5ForFile(
                                    AssetBundlePathHelper.GetAssetBundleFullName(
                                        PathSetting.GetPlatformName()
                                    )
                                ),
                                "",
                                false
                            )
                        );
                    WriteAssetNames();
                }
            }
        }

        /// <summary>
        /// * 生成Resources资源索引
        /// </summary>
        /// <param name="isWrite"></param>
        private static void GenerateResourceNames(bool isWrite = false)
        {
            if (!isWrite)
                return;
            // # 获取所有 Resources 文件夹
            var dirs = Directory.GetDirectories(
                Application.dataPath,
                "Resources",
                SearchOption.AllDirectories
            );

            var tmpNames = new List<string>();

            _resourceMapping = new Dictionary<string, string[]>();

            foreach (var dir in dirs)
            {
                // # 获取文件夹的路径
                var folderPaths = Directory.GetDirectories(dir, "*", SearchOption.AllDirectories);
                // # 获取文件的路径
                var filePaths = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                // 合并文件夹和文件的路径，可以根据需要调整顺序
                var allPaths = filePaths.Concat(folderPaths).ToArray();

                foreach (var item in allPaths)
                {
                    var filePath = FileUtility.FormatToUnityPath(item);
                    if (filePath.EndsWith(".meta") || filePath.EndsWith(".DS_Store"))
                        continue;
                    // # 获取不带扩展名的文件名
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

                    if (File.Exists(filePath))
                    {
                        var noSuffix = Path.ChangeExtension(filePath, null);
                        var resourcesPath = AssetBundlePathHelper.GetResourcesPath(noSuffix);
                        var realPath = resourcesPath.Replace(PathSetting.ResourcesPath, "");

                        if (tmpNames.Contains(fileNameWithoutExtension))
                        {
                            var id = Guid.NewGuid().ToString(); // # 生成一个唯一的ID
                            fileNameWithoutExtension += id;
                            Debug.Log(
                                "资源名称重复（大小写不敏感）："
                                    + filePath
                                    + "，增加唯一识别ID后为："
                                    + fileNameWithoutExtension
                            );
                        }

                        tmpNames.Add(fileNameWithoutExtension);

                        _resourceMapping.Add(fileNameWithoutExtension, new[] { realPath });
                    }
                    else if (Directory.Exists(filePath))
                    {
                        // # 文件夹资产信息，使用资产名代替
                        var assetNameDir = Directory
                            .GetFiles(filePath, "*", SearchOption.TopDirectoryOnly)
                            .Where(path =>
                                !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
                                && !path.EndsWith(".DS_Store", StringComparison.OrdinalIgnoreCase)
                            )
                            .Select(Path.GetFileNameWithoutExtension)
                            .ToArray();
                        fileNameWithoutExtension += AssetManager.DirectorySuffix;

                        if (tmpNames.Contains(fileNameWithoutExtension))
                        {
                            var id = Guid.NewGuid().ToString(); // # 生成一个唯一的ID
                            fileNameWithoutExtension += id;
                            Debug.Log(
                                "文件夹名称重复（大小写不敏感）："
                                    + filePath
                                    + "，增加唯一识别ID后为："
                                    + fileNameWithoutExtension
                            );
                        }

                        tmpNames.Add(fileNameWithoutExtension);
                        _resourceMapping.Add(fileNameWithoutExtension, assetNameDir);
                    }
                }
            }

            WriteResourceNames();
        }

        private static void WriteResourceNames()
        {
            // var resourceMapPath =
            //     FileUtility.FormatToUnityPath(FileUtility.TruncatePath(GetScriptPath(), 5))
            //     + "/AssetMap/Resources/"
            //     + nameof(ResourceMapping)
            //     + ".json";
            // FileUtility.SafeDeleteFile(resourceMapPath);
            // FileUtility.SafeDeleteFile(resourceMapPath + ".meta");
            // FileUtility.CheckFileAndCreateDirWhenNeeded(resourceMapPath);
            // AssetDatabase.Refresh();

            var path =
                Application.dataPath
                + "/Lazy/AssetMap/Resources/"
                + nameof(ResourceMapping)
                + ".json";
            FileUtility.CheckFileAndCreateDirWhenNeeded(path);
            var settings = new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Include,
            };
            var jsonContent = JsonConvert.SerializeObject(_resourceMapping, settings);
            FileUtility.SafeWriteAllText(path, jsonContent);
            AssetDatabase.Refresh();
        }

        private static void WriteAssetNames()
        {
            // var assetMapPath =
            //     FileUtility.FormatToUnityPath(FileUtility.TruncatePath(GetScriptPath(), 5))
            //     + "/AssetMap/Resources/"
            //     + nameof(AssetBundleMapping)
            //     + ".json";
            // FileUtility.SafeDeleteFile(assetMapPath);
            // FileUtility.SafeDeleteFile(assetMapPath + ".meta");
            // FileUtility.CheckFileAndCreateDirWhenNeeded(assetMapPath);
            // AssetDatabase.Refresh();

            var assetBundleMapPath =
                Application.dataPath
                + "/Lazy/AssetMap/Resources/"
                + nameof(AssetBundleMapping)
                + ".json";
            FileUtility.CheckFileAndCreateDirWhenNeeded(assetBundleMapPath);

            var settings = new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Include,
            };
            var jsonContent = JsonConvert.SerializeObject(_assetMapping, settings);
            FileUtility.SafeWriteAllText(assetBundleMapPath, jsonContent);

            AssetDatabase.Refresh();
        }

        private static string SetAssetBundleName(string path)
        {
            var ai = AssetImporter.GetAtPath(path);
            string bundleName;
#pragma warning disable CS0162
            if (AppendHashToAssetBundleName)
                bundleName =
                    Path.ChangeExtension(path, null).Replace(PathSetting.AssetBundlesPath, "")
                    + "_"
                    + FileUtility.CreateMD5ForFile(path);
            else
                bundleName = Path.ChangeExtension(path, null)
                    .Replace(PathSetting.AssetBundlesPath, "");
#pragma warning restore CS0162

            if (ai.assetBundleName.Equals(bundleName))
                return ai.assetBundleName;
            if (string.IsNullOrEmpty(ai.assetBundleName))
            {
                ai.assetBundleName = bundleName;
                EditorUtility.SetDirty(ai);
            }
            else if (_diffAssetPathMapping != null)
            {
                // # 资产名和assetBundle名不同
                _diffAssetPathMapping["/" + ai.assetBundleName] = "/" + bundleName.ToLower();
            }

            return ai.assetBundleName;
        }

        private static void DeleteRemovedAssetBundles()
        {
            FileUtility.CheckOrCreateDir(PathSetting.AssetBundlesFolder);
            var assetPaths = new List<string>();
            var assetBundlesPath = PathSetting.AssetBundlesFolder;
            RecordAssetsAndDirectories(assetBundlesPath, assetBundlesPath, assetPaths, true);
            assetPaths.Add("/" + PathSetting.GetPlatformName().ToLower());

            FileUtility.CheckOrCreateDir(PathSetting.AssetBundlesOutPath);
            var abPaths = new List<string>();
            var abBundlesPath = PathSetting.AssetBundlesOutPath;
            RecordAssetsAndDirectories(abBundlesPath, abBundlesPath, abPaths);

            foreach (var ab in abPaths)
                if (
                    !assetPaths.Contains(ab)
                    && !AssetPathsContainsDiscrepantAssetBundle(assetPaths, ab)
                )
                {
                    var abPath = PathSetting.AssetBundlesOutPath + ab;
                    if (File.Exists(abPath))
                    {
                        // It's a file, delete the file
                        if (FileUtility.SafeDeleteFile(abPath))
                            FileUtility.SafeDeleteFile(abPath + ".meta");

                        if (FileUtility.SafeDeleteFile(abPath + ".manifest"))
                            FileUtility.SafeDeleteFile(abPath + ".manifest" + ".meta");
                        Debug.Log("删除多余AB文件：" + abPath);
                    }
                    else if (Directory.Exists(abPath))
                    {
                        // It's a folder, delete the folder
                        if (FileUtility.SafeDeleteDir(abPath))
                        {
                            Debug.Log("删除多余AB文件夹：" + abPath);
                            // If the folder is deleted successfully, handle .meta file
                            var metaFilePath = abPath + ".meta";
                            if (File.Exists(metaFilePath))
                                FileUtility.SafeDeleteFile(metaFilePath);
                        }
                    }
                    else
                    {
                        Debug.Log("AB文件路径不存在，已经被删除了：" + abPath);
                    }
                }

            AssetDatabase.Refresh();
        }

        private static bool AssetPathsContainsDiscrepantAssetBundle(
            List<string> assetPaths,
            string ab
        )
        {
            return _diffAssetPathMapping.TryGetValue(ab, out var disPath)
                && assetPaths.Contains(disPath);
        }

        private static void RecordAssetsAndDirectories(
            string basePath,
            string rootPath,
            List<string> assetPaths,
            bool removeExtension = false
        )
        {
            var stack = new Stack<string>();
            stack.Push(rootPath);

            while (stack.Count > 0)
            {
                var currentPath = stack.Pop();
                var relativePath = currentPath.Replace(basePath, "");

                // Check for directories
                var directories = Directory.GetDirectories(currentPath);
                foreach (var directory in directories)
                {
                    stack.Push(directory);
                    assetPaths.Add(
                        FileUtility.FormatToUnityPath(directory.Replace(basePath, "").ToLower())
                    );
                }

                // Check for files
                var files = Directory.GetFiles(currentPath);
                foreach (var file in files)
                {
                    var extension = Path.GetExtension(file).ToLower();
                    if (
                        extension != ".meta"
                        && extension != ".manifest"
                        && extension != ".ds_store"
                    )
                    {
                        // It's a file under AssetBundles, record as "Audio/click11"
                        if (removeExtension)
                        {
#pragma warning disable CS0162
                            if (AppendHashToAssetBundleName)
                                assetPaths.Add(
                                    FileUtility.FormatToUnityPath(
                                        Path.ChangeExtension(
                                                relativePath + "/" + Path.GetFileName(file),
                                                null
                                            )
                                            .ToLower()
                                    )
                                        + "_"
                                        + FileUtility.CreateMD5ForFile(file)
                                );
                            else
                                assetPaths.Add(
                                    FileUtility.FormatToUnityPath(
                                        Path.ChangeExtension(
                                                relativePath + "/" + Path.GetFileName(file),
                                                null
                                            )
                                            .ToLower()
                                    )
                                );
#pragma warning restore CS0162
                        }
                        else
                        {
                            assetPaths.Add(
                                FileUtility
                                    .FormatToUnityPath(relativePath + "/" + Path.GetFileName(file))
                                    .ToLower()
                            );
                        }
                    }
                }
            }
        }

        private static string GetPackage(string path)
        {
            // # 使用正则表达式切割地址
            var packages = Regex.Split(path, @"[\\/]");
            foreach (var pkg in packages)
            {
                // # 判断地址中是否包含"Package_"
                var index = pkg.IndexOf(HotUpdateManager.PackageSplitter, StringComparison.Ordinal);
                if (index != -1)
                {
                    // # 此时是包含了的情况
                    var part = pkg.Substring(index + HotUpdateManager.PackageSplitter.Length);
                    return part;
                }
            }

            return "";
        }

        private static string GetScriptPath()
        {
            var monoScript = MonoScript.FromScriptableObject(
                CreateInstance<AssetBundleBuildTool>()
            );
            // 获取脚本在 Assets 中的相对路径
            var scriptRelativePath = AssetDatabase.GetAssetPath(monoScript);
            // 获取绝对路径并规范化
            var scriptPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", scriptRelativePath)
            );
            return scriptPath;
        }
    }
}
