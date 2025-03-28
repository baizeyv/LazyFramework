using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lazy.Res;
using Lazy.Res.HotUpdate;
using Lazy.Runtime.Utility;
using Lazy.Utility;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using HotUpdateManager = Lazy.Res.HotUpdateManager;

namespace Lazy.Editor.Build
{
    public class LazyBuildTool : ScriptableObject
    {
        public static List<string> AbOptions = new();

        public static List<bool> AbSelectedOptions = new();

        public static string BuildPath
        {
            get =>
                EditorPrefs.GetString(EditorConstant.BuildPathKey, Application.dataPath + "/Build");
            set => EditorPrefs.SetString(EditorConstant.BuildPathKey, value);
        }

        public static string ToVersion
        {
            get => EditorPrefs.GetString(EditorConstant.ToVersionKey, "1.0.0");
            set => EditorPrefs.SetString(EditorConstant.ToVersionKey, value);
        }

        public static string CodeVersion
        {
            get => EditorPrefs.GetString(EditorConstant.CodeVersionKey, "1");
            set => EditorPrefs.SetString(EditorConstant.CodeVersionKey, value);
        }

        public static string AssetRemoteAddress
        {
            get =>
                EditorPrefs.GetString(
                    EditorConstant.AssetRemoteAddressKey,
                    "http://127.0.0.1:7373/Remote"
                );
            set => EditorPrefs.SetString(EditorConstant.AssetRemoteAddressKey, value);
        }

        public static bool EnableHotUpdate
        {
            get => EditorPrefs.GetBool(EditorConstant.EnableHotUpdateKey, false);
            set => EditorPrefs.SetBool(EditorConstant.EnableHotUpdateKey, value);
        }

        public static bool EnablePackage
        {
            get => EditorPrefs.GetBool(EditorConstant.EnablePackageKey, false);
            set => EditorPrefs.SetBool(EditorConstant.EnablePackageKey, value);
        }

        public static bool EnableOptionalPackage
        {
            get => EditorPrefs.GetBool(EditorConstant.EnableOptionalPackageKey, false);
            set => EditorPrefs.SetBool(EditorConstant.EnableOptionalPackageKey, value);
        }

        public static string OptionalPackage
        {
            get => EditorPrefs.GetString(EditorConstant.OptionalPackageKey, "0_1_2_3");
            set => EditorPrefs.SetString(EditorConstant.OptionalPackageKey, value);
        }

        public static bool ExportAndroidProject
        {
            get => EditorPrefs.GetBool(EditorConstant.ExportAndroidProject, true);
            set => EditorPrefs.SetBool(EditorConstant.ExportAndroidProject, value);
        }

        public static string ExportAndroidPath
        {
            get =>
                EditorPrefs.GetString(
                    EditorConstant.ExportAndroidPath,
                    $"{Application.dataPath}/../../ex"
                );
            set => EditorPrefs.SetString(EditorConstant.ExportAndroidPath, value);
        }

        public static bool ExportCurrentPlatform = true;

        public static BuildTarget BuildTarget = BuildTarget.NoTarget;

        public static int Index = 0;

        public static BuildTarget[] Options = Enum.GetValues(typeof(BuildTarget))
            .Cast<BuildTarget>()
            .Select(option => (BuildTarget)Enum.Parse(typeof(BuildTarget), option.ToString()))
            .ToArray();

        public static string[] OptionNames = Array.ConvertAll(Options, x => x.ToString());

        public static void Build()
        {
            // TODO: 分成保存本地的和不保存本地的, 此处的保存本地的意思的是否打入包中
            var appName = Application.productName;
            var buildPath = BuildPath;
            var enumValues = Enum.GetValues(typeof(BuildTarget));
            var index = Array.FindIndex(
                (BuildTarget[])enumValues,
                target =>
                    target.ToString() == EditorPrefs.GetString(EditorConstant.ExportPlatformKey, "")
            );
            var buildTarget = EditorPrefs.GetBool(EditorConstant.ExportCurrentPlatformKey, true)
                ? EditorUserBuildSettings.activeBuildTarget
                : Options[index];
            var toVersion = ToVersion;
            var codeVersion = CodeVersion;
            var optionalPackage = OptionalPackage;
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.WSAPlayer:
                    appName += ".exe";
                    break;
                case BuildTarget.Android:
                    appName += ".apk";
                    break;
            }

            PlayerSettings.bundleVersion = toVersion;
            PlayerSettings.iOS.buildNumber = codeVersion;

            if (EnablePackage)
            {
                // # 启用分包
                var toPath =
                    FileUtility.TruncatePath(Application.dataPath, 1) + "/Temp_OptionalPackage";
                FileUtility.SafeDeleteDir(toPath);
                var mappings = JsonConvert.DeserializeObject<Dictionary<string, AssetMapping>>(
                    Resources.Load<TextAsset>(nameof(AssetBundleMapping)).ToString()
                );
                var packagePath =
                    buildPath + HotUpdateManager.RemoteDirName + HotUpdateManager.PackageDirName;
                FileUtility.SafeDeleteDir(packagePath);
                // # 分别打包Package
                var packages = optionalPackage.Split(HotUpdateManager.Separator);
                foreach (var package in packages)
                {
                    if (string.IsNullOrEmpty(package))
                        continue;
                    CopyDeleteUnnecessaryAssetBundle(
                        PathSetting.StreamingAssetsPath,
                        mappings,
                        toPath,
                        packagePath,
                        package
                    );

                    ZipUtility.IZipCallback zipCallback = new ZipUtility.ZipResult();
                    string[] paths = { packagePath };
                    var zipName = packagePath + HotUpdateManager.Separator + package + ".zip";
                    ZipUtility.Zip(paths, zipName, null, zipCallback);

                    FileUtility.SafeDeleteDir(packagePath);
                    Log.Log.MsgI("分包输出目录：" + zipName + " ，手动上传至CDN资源服务器。");
                }

                AssetDatabase.Refresh();

                var locationPathName =
                    buildPath + "/" + buildTarget + "_Optional_" + toVersion + "/" + appName;
                if (ExportAndroidProject)
                    locationPathName = ExportAndroidPath;
                FileUtility.CheckFileAndCreateDirWhenNeeded(locationPathName);
                BuildReport buildReport;
                if (buildTarget == BuildTarget.Android)
                {
                    // # 配置 Android 设置
                    PlayerSettings.SetApplicationIdentifier(
                        NamedBuildTarget.Android,
                        "com.godlike.lazy"
                    );
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = ExportAndroidProject;
                    PlayerSettings.Android.bundleVersionCode = int.Parse(codeVersion);
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
                    PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
                    buildReport = BuildPipeline.BuildPlayer(
                        GetBuildScenes(),
                        locationPathName,
                        buildTarget,
                        ExportAndroidProject
                            ? BuildOptions.AcceptExternalModificationsToPlayer
                            : BuildOptions.None
                    );
                }
                else
                {
                    buildReport = BuildPipeline.BuildPlayer(
                        GetBuildScenes(),
                        locationPathName,
                        buildTarget,
                        BuildOptions.None
                    );
                }

                if (buildReport.summary.result != BuildResult.Succeeded)
                    Log.Log.MsgE(
                        $"导出失败了，检查一下 Unity 内置的 Build Settings 导出的路径是否存在，Unity 没有给我清理缓存！: {buildReport.summary.result}"
                    );
                if (Directory.Exists(toPath))
                {
                    FileUtility.SafeCopyDirectory(toPath, Application.streamingAssetsPath, true);
                    FileUtility.SafeDeleteDir(toPath);
                }

                Log.Log.MsgI("游戏分包打包成功! " + locationPathName);
            }
            else
            {
                // # 全量包
                var scenePaths = GetBuildScenes();
                if (buildTarget == BuildTarget.Android)
                {
                    // # 设置输出路径 (将生成 Android 工程文件)
                    // var outputPath = "Builds/AndroidProject";
                    var outputPath = string.Empty;
                    if (ExportAndroidProject)
                        outputPath = ExportAndroidPath;
                    else
                        outputPath = ExportAndroidProject
                            ? ExportAndroidPath
                            : buildPath + "/" + buildTarget + "_Full_" + toVersion + "/" + appName;
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = ExportAndroidProject;
                    FileUtility.SafeClearDir(outputPath);
                    // # 配置 Android 设置
                    PlayerSettings.SetApplicationIdentifier(
                        NamedBuildTarget.Android,
                        "com.godlike.lazy"
                    );
                    PlayerSettings.Android.bundleVersionCode = int.Parse(codeVersion);
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
                    PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
                    // # 构建选项: 生成工程而非APK,允许二次开发
                    var options = new BuildPlayerOptions()
                    {
                        scenes = scenePaths,
                        locationPathName = outputPath,
                        target = buildTarget,
                        options = ExportAndroidProject
                            ? BuildOptions.AcceptExternalModificationsToPlayer
                            : BuildOptions.None,
                    };
                    var buildReport = BuildPipeline.BuildPlayer(options);
                    if (buildReport.summary.result != BuildResult.Succeeded)
                        Log.Log.MsgE("Build Failed!");
                    else
                        Log.Log.MsgI("全量包导出成功");
                }
                else
                {
                    var locationPathName =
                        buildPath
                        + "/"
                        + buildTarget.ToString()
                        + "_Full_"
                        + toVersion
                        + "/"
                        + appName;
                    FileUtility.CheckFileAndCreateDirWhenNeeded(locationPathName);
                    var buildReport = BuildPipeline.BuildPlayer(
                        GetBuildScenes(),
                        locationPathName,
                        buildTarget,
                        BuildOptions.None
                    );
                    if (buildReport.summary.result != BuildResult.Succeeded)
                        Log.Log.MsgE(
                            $"导出失败了，检查一下 Unity 内置的 Build Settings 导出的路径是否存在，Unity 没有给我清理缓存！: {buildReport.summary.result}"
                        );

                    Log.Log.MsgI("游戏全量包打包成功! " + locationPathName);
                }
            }

            CopyRemoteBundleToBuildPath();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// * 第一次构建游戏时将不需要存在本地的放到导出目录中
        /// </summary>
        private static void CopyRemoteBundleToBuildPath()
        {
            var allLocalBundles = EditorPrefs.GetString(EditorConstant.LocalAssetBundlesKey, "");
            // # 项目 Resources 中的 AssetBundleMapping.json (最完整的AssetBundleMapping.json)
            var resAssetBundleMappings = JsonConvert.DeserializeObject<
                Dictionary<string, AssetMapping>
            >(Resources.Load<TextAsset>(nameof(AssetBundleMapping)).text, Constant.JsonSetting);
            // # 热更新资源文件夹路径 For Example:foo/bar/HotUpdate_1.0.1
            var hotUpdatePath =
                BuildPath
                + HotUpdateManager.RemoteDirName
                + HotUpdateManager.HotUpdateDirName
                + HotUpdateManager.Separator
                + ToVersion;
            if (string.IsNullOrEmpty(allLocalBundles))
            {
                // # 没有存在本地的,全部复制到导出目录
                CopyHotUpdateAssetBundle(
                    PathSetting.AssetBundlesOutPath,
                    resAssetBundleMappings,
                    hotUpdatePath
                );
                return;
            }

            var localBundleArray = allLocalBundles.Split(';');
            var generateAssetBundleMappings = new Dictionary<string, AssetMapping>();
            foreach (
                var resAssetMapping in from resAssetMapping in resAssetBundleMappings
                let bundleName = resAssetMapping.Value.AssetBundleName
                where !localBundleArray.Contains(bundleName)
                select resAssetMapping
            )
                generateAssetBundleMappings.TryAdd(resAssetMapping.Key, resAssetMapping.Value);

            // # 若文件夹不存在则创建文件夹
            FileUtility.CheckOrCreateDir(hotUpdatePath);
            // # 清空文件夹
            FileUtility.SafeClearDir(hotUpdatePath);
            CopyHotUpdateAssetBundle(
                PathSetting.AssetBundlesOutPath,
                generateAssetBundleMappings,
                hotUpdatePath
            );
            // # 导出的热更资源 HotUpdate_AssetBundleMapping.json 的路径
            var hotUpdateMapPath =
                BuildPath
                + HotUpdateManager.RemoteDirName
                + "/HotUpdate"
                + HotUpdateManager.Separator
                + nameof(AssetBundleMapping)
                + ".json";
            FileUtility.SafeCopyFile(
                Application.dataPath
                    + "/Lazy/AssetMap/Resources/"
                    + nameof(AssetBundleMapping)
                    + ".json",
                hotUpdateMapPath
            );
        }

        /// <summary>
        /// * 构建热更新资源
        /// </summary>
        public static void BuildHotUpdate()
        {
            var buildPath = BuildPath;
            var toVersion = ToVersion;
            // # 导出的 AppVersion.json 的路径
            var appVersionPath =
                buildPath + HotUpdateManager.RemoteDirName + "/" + nameof(AppVersion) + ".json";
            // # 导出的 AssetBundleMapping.json 的路径
            var assetBundleMappingPath =
                buildPath
                + HotUpdateManager.RemoteDirName
                + "/"
                + nameof(AssetBundleMapping)
                + ".json";
            // # 导出的热更资源 HotUpdate_AssetBundleMapping.json 的路径
            var hotUpdateMapPath =
                buildPath
                + HotUpdateManager.RemoteDirName
                + "/HotUpdate"
                + HotUpdateManager.Separator
                + nameof(AssetBundleMapping)
                + ".json";
            if (!File.Exists(appVersionPath) || !File.Exists(assetBundleMappingPath))
            {
                Log.Log.MsgE("请先构建一个游戏版本，再构建热更新文件！~");
                return;
            }

            JsonSerializerSettings settings =
                new() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate };
            // # 项目 Resources 中的 AssetBundleMapping.json (最完整的AssetBundleMapping.json)
            var resAssetBundleMappings = JsonConvert.DeserializeObject<
                Dictionary<string, AssetMapping>
            >(Resources.Load<TextAsset>(nameof(AssetBundleMapping)).text, settings);
            // # 构建项目导出的 AssetBundleMapping.json (原始AssetBundleMapping.json,在第一次构建热更新时使用)
            var assetBundleMappings = JsonConvert.DeserializeObject<
                Dictionary<string, AssetMapping>
            >(FileUtility.SafeReadAllText(assetBundleMappingPath), settings);
            if (File.Exists(hotUpdateMapPath))
                // # 热更AssetBundleMapping.json存在, 不使用原始的AssetBundleMapping.json,转而使用已热更过的AssetBundleMapping.json
                assetBundleMappings = JsonConvert.DeserializeObject<
                    Dictionary<string, AssetMapping>
                >(File.ReadAllText(hotUpdateMapPath), settings);

            // # 所有基于 assetBundleMappings 有修改的或新增的资源字典
            var generateAssetBundleMappings = new Dictionary<string, AssetMapping>();
            foreach (var resAssetMapping in resAssetBundleMappings)
            {
                // # 遍历 Resources 中的 AssetBundleMapping.json
                assetBundleMappings.TryGetValue(resAssetMapping.Key, out var assetMapping);
                if (assetMapping == null || resAssetMapping.Value.MD5 != assetMapping.MD5)
                    // # 新增的以及修改的资源,放入新生成的
                    generateAssetBundleMappings.TryAdd(resAssetMapping.Key, resAssetMapping.Value);
            }

            // # 热更新资源文件夹路径 For Example:foo/bar/HotUpdate_1.0.1
            var hotUpdatePath =
                buildPath
                + HotUpdateManager.RemoteDirName
                + HotUpdateManager.HotUpdateDirName
                + HotUpdateManager.Separator
                + toVersion;
            // # 若文件夹不存在则创建文件夹
            FileUtility.CheckOrCreateDir(hotUpdatePath);
            // # 清空文件夹
            FileUtility.SafeClearDir(hotUpdatePath);
            CopyHotUpdateAssetBundle(
                PathSetting.AssetBundlesOutPath,
                generateAssetBundleMappings,
                hotUpdatePath
            );

            // # 导出的 AppVersion
            var remoteAppVersion = JsonConvert.DeserializeObject<AppVersion>(
                FileUtility.SafeReadAllText(appVersionPath),
                settings
            );
            remoteAppVersion.Version = toVersion;
            // # 将热更版本列表进行更新
            if (!remoteAppVersion.HotUpdateVersions.Contains(toVersion))
                remoteAppVersion.HotUpdateVersions.Add(toVersion);
            FileUtility.SafeWriteAllText(
                appVersionPath,
                JsonConvert.SerializeObject(remoteAppVersion)
            );

            FileUtility.SafeCopyFile(
                Application.dataPath
                    + "/Lazy/AssetMap/Resources/"
                    + nameof(AssetBundleMapping)
                    + ".json",
                hotUpdateMapPath
            );

            Log.Log.MsgD("构建热更新包版本成功！版本：" + toVersion);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// * 复制热更的AssetBundle到目标目录
        /// </summary>
        /// <param name="assetBundlesOutput">BuildPipline打包的AssetBundle目录</param>
        /// <param name="mappings">有新增或更新的AssetMapping字典</param>
        /// <param name="toPath">要复制到的目录</param>
        private static void CopyHotUpdateAssetBundle(
            string assetBundlesOutput,
            Dictionary<string, AssetMapping> mappings,
            string toPath
        )
        {
            Dictionary<string, AssetMapping> tempMappings = new();
            foreach (var mapping in mappings)
                tempMappings.TryAdd(mapping.Value.AssetBundleName, mapping.Value);

            Stack<string> stack = new();
            stack.Push(assetBundlesOutput);
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
                        var filePath = FileUtility.FormatToUnityPath(file);
                        var filePathManifest = FileUtility.FormatToUnityPath(file) + ".manifest";
                        var abName = filePath.Replace(assetBundlesOutput + "/", "");
                        if (tempMappings.TryGetValue(abName, out _))
                        {
                            FileUtility.SafeCopyFile(
                                filePath,
                                FileUtility.FormatToUnityPath(
                                    toPath + "/" + GetAssetBundlesPath(filePath)
                                )
                            );
                            FileUtility.SafeCopyFile(
                                filePathManifest,
                                FileUtility.FormatToUnityPath(
                                    toPath + "/" + GetAssetBundlesPath(filePathManifest)
                                )
                            );
                        }
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        private static void CopyDeleteUnnecessaryAssetBundle(
            string assetBundlesOutPath,
            Dictionary<string, AssetMapping> mappings,
            string toPath,
            string toPath2,
            string package
        )
        {
            var tmpMappings = new Dictionary<string, AssetMapping>();
            foreach (var mapping in mappings)
                tmpMappings.TryAdd(mapping.Value.AssetBundleName, mapping.Value);

            Stack<string> stack = new();
            stack.Push(assetBundlesOutPath);

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
                        var filePath = FileUtility.FormatToUnityPath(file);
                        var filePathManifest = FileUtility.FormatToUnityPath(file) + ".manifest";
                        var abName = filePath.Replace(assetBundlesOutPath + "/", "");
                        if (tmpMappings.TryGetValue(abName, out var assetMapping))
                            if (
                                abName == assetMapping.AssetBundleName
                                && package.Equals(assetMapping.Package)
                            )
                            {
                                FileUtility.SafeCopyFile(
                                    filePath,
                                    FileUtility.FormatToUnityPath(
                                        toPath + "/" + GetAssetBundlesPath(filePath)
                                    )
                                );
                                FileUtility.SafeCopyFile(
                                    filePath,
                                    FileUtility.FormatToUnityPath(
                                        toPath2 + "/" + GetAssetBundlesPath(filePath)
                                    )
                                );
                                FileUtility.SafeDeleteFile(filePath);
                                FileUtility.SafeDeleteFile(filePath + ".meta");

                                FileUtility.SafeCopyFile(
                                    filePathManifest,
                                    FileUtility.FormatToUnityPath(
                                        toPath + "/" + GetAssetBundlesPath(filePathManifest)
                                    )
                                );
                                FileUtility.SafeCopyFile(
                                    filePathManifest,
                                    FileUtility.FormatToUnityPath(
                                        toPath2 + "/" + GetAssetBundlesPath(filePathManifest)
                                    )
                                );
                                FileUtility.SafeDeleteFile(filePathManifest);
                                FileUtility.SafeDeleteFile(filePathManifest + ".meta");
                            }
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        private static string GetAssetBundlesPath(string fullPath)
        {
            var rgx = new Regex(@"AssetBundles[\\/].+$");
            var matches = rgx.Match(fullPath);

            var assetPath = "";
            if (matches.Success)
                assetPath = matches.Value;

            assetPath = FileUtility.FormatToUnityPath(assetPath);
            return assetPath;
        }

        /// <summary>
        /// * 写入 App Version
        /// </summary>
        public static void WriteAppVersion()
        {
            var optionalPackage = EditorPrefs.GetString(EditorConstant.OptionalPackageKey, "");
            var toVersion = EditorPrefs.GetString(EditorConstant.ToVersionKey, "");
            var assetRemoteAddress =
                EditorPrefs.GetString(EditorConstant.AssetRemoteAddressKey, "")
                + "/"
                + PathSetting.GetPlatformName();
            var enableHotUpdate = EditorPrefs.GetBool(EditorConstant.EnableHotUpdateKey, false);
            var enablePackage = EditorPrefs.GetBool(EditorConstant.EnablePackageKey, false);
            var buildPath = EditorPrefs.GetString(EditorConstant.BuildPathKey, "");
            var appVersionPath =
                FileUtility.FormatToUnityPath(FileUtility.TruncatePath(GetScriptPath(), 3))
                + "/AssetMap/Resources/"
                + nameof(AppVersion)
                + ".json";
            FileUtility.SafeDeleteFile(appVersionPath);
            FileUtility.SafeDeleteFile(appVersionPath + ".meta");
            FileUtility.CheckOrCreateDir(appVersionPath);
            AssetDatabase.Refresh();

            List<string> packageList;
            if (!string.IsNullOrEmpty(optionalPackage))
                packageList = new List<string>(optionalPackage.Split(HotUpdateManager.Separator));
            else
                packageList = new List<string>();

            var appVersion = new AppVersion(
                toVersion,
                assetRemoteAddress,
                enableHotUpdate,
                new List<string>(),
                enablePackage,
                packageList
            );
            // # 写入到文件
            var appVersionResourcesPath =
                Application.dataPath + "/Lazy/AssetMap/Resources/" + nameof(AppVersion) + ".json";
            // # 序列化对象
            JsonSerializerSettings settings =
                new() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate };
            var json = JsonConvert.SerializeObject(appVersion, settings);
            FileUtility.SafeDeleteFile(appVersionResourcesPath);
            FileUtility.SafeDeleteFile(appVersionResourcesPath + ".meta");
            AssetDatabase.Refresh();
            FileUtility.CheckFileAndCreateDirWhenNeeded(appVersionResourcesPath);
            FileUtility.SafeWriteAllText(appVersionResourcesPath, json);
            // # 复制到导出目录
            FileUtility.CheckOrCreateDir(buildPath + HotUpdateManager.RemoteDirName);
            FileUtility.SafeCopyFile(
                appVersionResourcesPath,
                buildPath + HotUpdateManager.RemoteDirName + "/" + nameof(AppVersion) + ".json"
            );
            Log.Log.MsgD($"写入游戏版本: {appVersion.Version}");
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// * 写入资产版本
        /// </summary>
        public static void WriteAssetVersion()
        {
            var buildPath = BuildPath;
            var assetBundleMappingPath =
                Application.dataPath
                + "/Lazy/AssetMap/Resources/"
                + nameof(AssetBundleMapping)
                + ".json";
            FileUtility.SafeCopyFile(
                assetBundleMappingPath,
                buildPath
                    + HotUpdateManager.RemoteDirName
                    + "/"
                    + nameof(AssetBundleMapping)
                    + ".json"
            );
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// * 设置下拉框所有的 AssetBundle 名称
        /// </summary>
        /// <returns></returns>
        public static void GetAllAssetBundle()
        {
            var assetBundleMappingPath =
                Application.dataPath
                + "/Lazy/AssetMap/Resources/"
                + nameof(AssetBundleMapping)
                + ".json";
            var json = FileUtility.SafeReadAllText(assetBundleMappingPath);
            var mappings = JsonConvert.DeserializeObject<Dictionary<string, AssetMapping>>(
                json,
                Constant.JsonSetting
            );
            HashSet<string> bundles = new();
            foreach (
                var mapping in mappings
                    .Values.Where(mapping => !string.IsNullOrEmpty(mapping.AssetBundleName))
                    .Where(mapping =>
                        !mapping.AssetBundleName.Equals("Windows")
                        && !mapping.AssetBundleName.Equals("macOS")
                        && !mapping.AssetBundleName.Equals("Linux")
                        && !mapping.AssetBundleName.Equals("Android")
                        && !mapping.AssetBundleName.Equals("iOS")
                        && !mapping.AssetBundleName.Equals("WebGL")
                        && !mapping.AssetBundleName.Equals("Unknown")
                    )
            )
                bundles.Add(mapping.AssetBundleName);

            AbOptions = bundles.ToList();
        }

        private static string[] GetBuildScenes()
        {
            return (
                from e in EditorBuildSettings.scenes
                where e != null && e.enabled
                select e.path
            ).ToArray();
        }

        /// <summary>
        /// * 获取脚本路径
        /// </summary>
        /// <returns></returns>
        private static string GetScriptPath()
        {
            var monoScript = MonoScript.FromScriptableObject(CreateInstance<LazyBuildTool>());

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
