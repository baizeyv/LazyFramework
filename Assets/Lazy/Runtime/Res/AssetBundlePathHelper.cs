using System.IO;
using System.Text.RegularExpressions;
using Lazy;
using Lazy.Res.HotUpdate;

namespace Lazy.Res
{
    public static class AssetBundlePathHelper
    {
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
            string assetBundlePath;
            switch (type)
            {
                case SourceType.StreamingAssets:
                    assetBundlePath = PathSetting.StreamingAssetsPath;
                    break;
                case SourceType.HotUpdatePath:
                    assetBundlePath = PathSetting.HotUpdatePath;
                    break;
                case SourceType.PackagePath:
                    assetBundlePath = PathSetting.PackagePath;
                    break;
                case SourceType.RemoteAddress:
                    if (string.IsNullOrEmpty(AppConfig.LocalVersion.AssetRemoteAddress))
                        Log.Log.MsgE("加载远程包需要配置远程地址：AssetRemoteAddress");
                    assetBundlePath = PathSetting.RemoteAddress;
                    break;
                default:
                    Log.Log.MsgE("AssetBundle的源类型不能为空 TODO!");
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
                && assetMapping is { Updated: true }
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
