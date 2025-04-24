using Lazy;

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

        private static HotUpdateManager _hotUpdateManager;

        public static HotUpdateManager HotUpdate
        {
            get
            {
                return _hotUpdateManager ??= ManagerCenter.Create(() => HotUpdateManager.Instance);
            }
            set => _hotUpdateManager ??= value;
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

        private static PoolManager _poolManager;

        /// <summary>
        /// * 池管理器
        /// </summary>
        public static PoolManager Pool
        {
            get { return _poolManager ??= ManagerCenter.Create(() => PoolManager.Instance); }
            set => _poolManager ??= value;
        }

        private static TimerManager _timerManager;

        /// <summary>
        /// * 事件管理器
        /// </summary>
        public static TimerManager Timer
        {
            get { return _timerManager ??= ManagerCenter.Create(() => TimerManager.Instance); }
            set => _timerManager ??= value;
        }

        private static AudioManager _audioManager;

        /// <summary>
        /// * 音频管理器
        /// </summary>
        public static AudioManager Audio
        {
            get { return _audioManager ??= ManagerCenter.CreateMono(() => AudioManager.Instance); }
            set => _audioManager ??= value;
        }

        private static StorageManager _storageManager;

        public static StorageManager Storage
        {
            get { return _storageManager ??= ManagerCenter.Create(() => StorageManager.Instance); }
            set => _storageManager ??= value;
        }

        private static FsmManager _fsmManager;

        public static FsmManager Fsm
        {
            get { return _fsmManager ??= ManagerCenter.Create(() => FsmManager.Instance); }
            set => _fsmManager ??= value;
        }

        private static UIManager _uiManager;

        public static UIManager UI
        {
            get { return _uiManager ??= ManagerCenter.CreateMono(() => UIManager.Instance); }
            set => _uiManager ??= value;
        }

        private static RedDotManager _redDotManager;

        public static RedDotManager RedDot
        {
            get { return _redDotManager ??= ManagerCenter.Create(() => RedDotManager.Instance); }
            set => _redDotManager ??= value;
        }

        private static Debugger _debugger;

        public static Debugger Debugger
        {
            get { return _debugger ??= ManagerCenter.CreateMono(() => Debugger.Instance); }
            set => _debugger ??= value;
        }
    }
}
