using Lazy.Download;
using Lazy.Manage;
using Lazy.Platform;
using Lazy.Res;

namespace Lazy
{
    public static class App
    {
        private static PlatformManager _platformManager;

        /// <summary>
        /// * 平台管理器
        /// </summary>
        public static PlatformManager Platform
        {
            get
            {
                return _platformManager ??= ManagerCenter.Create(() => PlatformManager.Instance);
            }
            set => _platformManager ??= value;
        }

        private static AssetManager _assetManager;

        /// <summary>
        /// * 资产管理器
        /// </summary>
        public static AssetManager Asset
        {
            get { return _assetManager ??= ManagerCenter.Create(() => AssetManager.Instance); }
            set => _assetManager ??= value;
        }

        private static DownloadManager _downloadManager;

        /// <summary>
        /// * 下载管理器
        /// </summary>
        public static DownloadManager Download
        {
            get
            {
                return _downloadManager ??= ManagerCenter.Create(() => DownloadManager.Instance);
            }
            set => _downloadManager ??= value;
        }
    }
}
