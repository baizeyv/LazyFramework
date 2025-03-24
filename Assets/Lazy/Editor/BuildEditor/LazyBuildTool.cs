using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lazy.Res;
using Lazy.Res.HotUpdate;
using Lazy.Utility;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Lazy.Editor.Build
{
    public class LazyBuildTool : ScriptableObject
    {
        private static string _buildPath = Application.dataPath + "/Build";

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
                    "http://127.0.0.1:7373/"
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

        public static bool ExportCurrentPlatform = true;

        public static BuildTarget BuildTarget = BuildTarget.NoTarget;

        public static int Index = 0;

        public static BuildTarget[] Options = Enum.GetValues(typeof(BuildTarget))
            .Cast<BuildTarget>()
            .Select(option => (BuildTarget)Enum.Parse(typeof(BuildTarget), option.ToString()))
            .ToArray();

        public static string[] OptionNames = Array.ConvertAll(Options, x => x.ToString());

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
            FileUtility.CheckOrCreateDir(appVersionResourcesPath);
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
            var buildPath = EditorPrefs.GetString(EditorConstant.BuildPathKey, "");
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
