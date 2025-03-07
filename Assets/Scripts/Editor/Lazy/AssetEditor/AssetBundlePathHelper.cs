using System.Text.RegularExpressions;
using Lazy.Utility;
using UnityEditor;
using UnityEngine;

namespace Lazy.Editor.AssetEditor
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

        public static string GetAssetBundlePath(SourceType type = SourceType.STREAMING_ASSETS)
        {
            // TODO:
            string assetBundlePath;
            switch (type)
            {
                case SourceType.STREAMING_ASSETS:
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
            SourceType type = SourceType.STREAMING_ASSETS
        )
        {
            var assetBundlePath = GetAssetBundlePath(type);
            if (string.IsNullOrEmpty(assetBundlePath))
                return null;
            return assetBundlePath + assetBundleFileName;
        }
    }

    /// <summary>
    /// 源类型的枚举。
    /// </summary>
    public enum SourceType
    {
        NONE,
        STREAMING_ASSETS,
        HOT_UPDATE_PATH,
        PACKAGE_PATH,
        REMOTE_ADDRESS,
    }
}
