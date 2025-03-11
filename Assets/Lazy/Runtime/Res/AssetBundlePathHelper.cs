using System.IO;
using System.Text.RegularExpressions;
using Lazy.Res.HotUpdate;
using Lazy.Utility;
using UnityEditor;
using UnityEngine;

namespace Lazy.Res
{
    public static class AssetBundlePathHelper
    {
        public static string GetPlatformName(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.WSAPlayer:
                    return "WSAPlayer";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.WebGL:
                    return "WebGL";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
#if !UNITY_2019_2_OR_NEWER
                case BuildTarget.StandaloneLinux:
#endif
                case BuildTarget.StandaloneLinux64:
#if !UNITY_2019_2_OR_NEWER
                case BuildTarget.StandaloneLinuxUniversal:
#endif
                    return "Linux";
                case BuildTarget.StandaloneOSX:
                    return "OSX";
                default:
                    return "Unknown";
            }
        }

        public static string GetAssetPath(string fullPath)
        {
            var rgx = new Regex(@"Assets[\\/].+$");
            var matches = rgx.Match(fullPath);

            var assetPath = "";
            if (matches.Success)
                assetPath = matches.Value;

            assetPath = FileUtility.FormatToUnityPath(assetPath);
            return assetPath;
        }

        public static string GetResourcesPath(string fullPath)
        {
            var rgx = new Regex(@"Resources[\\/].+$");
            var matches = rgx.Match(fullPath);

            var assetPath = "";
            if (matches.Success)
                assetPath = matches.Value;

            assetPath = FileUtility.FormatToUnityPath(assetPath);
            return assetPath;
        }

        public static string GetAssetBundlePath(SourceType type = SourceType.StreamingAssets)
        {
            // TODO:
            string assetBundlePath;
            switch (type)
            {
                case SourceType.StreamingAssets:
                    assetBundlePath = PathSetting.StreamingAssetsPath;
                    break;
                default:
                    Debug.LogError("AssetBundle的源类型不能为空 TODO!");
                    return null;
            }

            return assetBundlePath;
        }

        public static string GetAssetBundleFullName(
            string assetBundleFileName = null,
            SourceType type = SourceType.StreamingAssets
        )
        {
            var assetBundlePath = GetAssetBundlePath(type);
            if (string.IsNullOrEmpty(assetBundlePath))
                return null;
            return assetBundlePath + assetBundleFileName;
        }

        public static string GetAssetBundlePathByAssetBundleName(string assetBundleName)
        {
            string fullPath;
            if (
                AppConfig.LocalVersion.EnableHotUpdate
                && File.Exists(GetAssetBundleFullName(assetBundleName, SourceType.HotUpdatePath))
            )
                fullPath = GetAssetBundleFullName(assetBundleName, SourceType.HotUpdatePath);
            else if (
                AppConfig.LocalVersion.EnablePackage
                && File.Exists(GetAssetBundleFullName(assetBundleName, SourceType.PackagePath))
            )
                fullPath = GetAssetBundleFullName(assetBundleName, SourceType.PackagePath);
            else if (AssetManager.ForceRemoteAssetBundle)
                fullPath = GetAssetBundleFullName(assetBundleName, SourceType.RemoteAddress);
            else
                fullPath = GetAssetBundleFullName(assetBundleName);
            return fullPath;
        }

        /// <summary>
        /// * 获取没有包名的路径
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetAssetBundlePathWithoutBundleName(string assetName)
        {
            string fullPath;
            if (
                AppConfig.LocalVersion.EnableHotUpdate
                && AssetBundleMapping.Mappings.TryGetValue(assetName, out var assetMapping)
                && assetMapping != null
                && assetMapping.Updated
            )
                fullPath = GetAssetBundleFullName(null, SourceType.HotUpdatePath);
            else if (
                AppConfig.LocalVersion.EnablePackage
                && AssetBundleMapping.Mappings.TryGetValue(assetName, out var assetMappingPackage)
                && assetMappingPackage != null
                && !string.IsNullOrEmpty(assetMappingPackage.Package)
            )
                fullPath = GetAssetBundleFullName(null, SourceType.PackagePath);
            else if (AssetManager.ForceRemoteAssetBundle)
                fullPath = GetAssetBundleFullName(null, SourceType.RemoteAddress);
            else
                fullPath = GetAssetBundleFullName();

            return fullPath;
        }

        public static string GetRemoteAssetBundleCompletePath()
        {
            return !string.IsNullOrEmpty(AppConfig.LocalVersion.AssetRemoteAddress)
                ? GetAssetBundleFullName(null, SourceType.RemoteAddress)
                : null;
        }
    }

    /// <summary>
    /// 源类型的枚举。
    /// </summary>
    public enum SourceType
    {
        None,
        StreamingAssets,
        HotUpdatePath,
        PackagePath,
        RemoteAddress,
    }
}
