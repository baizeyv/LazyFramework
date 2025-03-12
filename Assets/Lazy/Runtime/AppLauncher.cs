using System;
using System.Collections;
using Lazy.Manage;
using Lazy.Res;
using Lazy.Res.Manager;
using Lazy.Singleton;
using Lazy.Utility;

namespace Lazy
{
    [MonoSingletonPath("AppLauncher")]
    public class AppLauncher : MonoSingleton<AppLauncher>
    {
        /// <summary>
        /// * 启动游戏
        /// </summary>
        public event Action OnStartGame;

        private IEnumerator Start()
        {
            ManagerCenter.Setup(this);

            App.Asset = ManagerCenter.Create(() => AssetManager.Instance);
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

        private void OnDestroy()
        {
            OnStartGame = null;
        }
    }
}
