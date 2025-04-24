using System;
using System.Collections;
using Lazy;

namespace Lazy
{
    [MonoSingletonPath("Lazy/AppLauncher")]
    public class AppLauncher : MonoSingleton<AppLauncher>
    {
        /// <summary>
        /// * 启动游戏
        /// </summary>
        public event Action OnStartGame;

        /// <summary>
        /// * 退出游戏
        /// </summary>
        public event Action OnQuitGame;

        /// <summary>
        /// * 暂停游戏
        /// </summary>
        public event Action<bool> OnPauseGame;

        /// <summary>
        /// * 聚焦游戏
        /// </summary>
        public event Action<bool> OnFocusGame;

        private IEnumerator Start()
        {
            ManagerCenter.Setup(this);

            // ! 这里的顺序不能改变
#if DEBUG
            App.Debugger = ManagerCenter.CreateMono(() => Lazy.Debugger.Instance);
#endif
            App.Storage = ManagerCenter.Create(() => StorageManager.Instance);
            App.Platform = ManagerCenter.Create(() => PlatformManager.Instance);
            App.HotUpdate = ManagerCenter.Create(() => HotUpdateManager.Instance);
            App.Asset = ManagerCenter.Create(() => AssetManager.Instance);
            App.Download = ManagerCenter.Create(() => DownloadManager.Instance);
            App.Timer = ManagerCenter.Create(() => TimerManager.Instance);
            App.Pool = ManagerCenter.Create(() => PoolManager.Instance);
            App.Audio = ManagerCenter.CreateMono(() => AudioManager.Instance);
            App.UI = ManagerCenter.CreateMono(() => UIManager.Instance);
            App.Fsm = ManagerCenter.Create(() => FsmManager.Instance);
            App.RedDot = ManagerCenter.Create(() => RedDotManager.Instance);
#if UNITY_WEBGL
            yield return AssetBundleManager.Instance.LoadAssetBundleManifest();
#endif
            StartGame();
            yield break;
        }

        private void StartGame()
        {
            OnStartGame?.Invoke();
        }

        private void Update()
        {
            ManagerCenter.Update();
        }

        private void LateUpdate()
        {
            ManagerCenter.LateUpdate();
        }

        private void FixedUpdate()
        {
            ManagerCenter.FixedUpdate();
        }

        private void OnGUI()
        {
            ManagerCenter.GUI();
        }

        private void OnDestroy()
        {
            OnStartGame = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            OnFocusGame?.Invoke(hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            OnPauseGame?.Invoke(pauseStatus);
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            OnQuitGame?.Invoke();
            ManagerCenter.Destroy();
        }
    }
}
