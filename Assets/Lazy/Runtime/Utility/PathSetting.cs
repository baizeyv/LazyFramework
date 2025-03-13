using UnityEngine;

namespace Lazy
{
    public static class PathSetting
    {
        /// <summary>
        /// * AssetBundle打包后所在的StreamingAssets下的文件夹的名称
        /// </summary>
        public const string AssetBundlesName = "AssetBundles";

        /// <summary>
        /// * AssetBundle包所在的文件夹路径
        /// </summary>
        public const string AssetBundlesPath = "Assets/AssetBundles/";

        /// <summary>
        /// * Resources 根路径
        /// </summary>
        public const string ResourcesPath = "Resources/";

        public static string StreamingAssetsPath =>
            $"{Application.streamingAssetsPath}/{AssetBundlesName}/{GetPlatformName()}/";

        public static string AssetBundlesOutPath =>
            $"{Application.dataPath}/StreamingAssets/{AssetBundlesName}/{GetPlatformName()}";

        public static string AssetBundlesFolder => $"{Application.dataPath}/{AssetBundlesName}";

        public static string ImagePath => $"{Application.persistentDataPath}/images/";

        public static string GetPlatformName()
        {
#if UNITY_STANDALONE_WIN
            var ret = "Windows";
#elif UNITY_STANDALONE_OSX
            var ret = "macOS";
#elif UNITY_STANDALONE_LINUX
            var ret = "Linux";
#elif UNITY_IPHONE || UNITY_IOS
            var ret = "iOS";
#elif UNITY_ANDROID
            var ret = "Android";
#elif UNITY_WEBGL
            var ret = "WebGL";
#else
            var ret = "Unknown";
#endif
            return ret;
        }
    }
}
