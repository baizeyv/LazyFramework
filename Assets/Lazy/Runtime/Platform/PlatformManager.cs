using Lazy;
using UnityEngine;

namespace Lazy
{
    public class PlatformManager : Singleton<PlatformManager>, IManager
    {
        /// <summary>
        /// * 安卓当前的 Activity
        /// </summary>
        private static AndroidJavaObject _currentActivity;

        /// <summary>
        /// * 安卓JavaClass UnityPlayer
        /// </summary>
        private static AndroidJavaClass _unityPlayer;

        private PlatformManager() { }

        public override void OnSingletonInitialize()
        {
            Log.I()
                .Msg($"Platform Unity Initialized: <color=#00ff00>{Debug.isDebugBuild}</color>")
                .Tag("PLATFORM")
                .Do();
#if UNITY_ANDROID
            if (_currentActivity == null)
            {
#if !UNITY_EDITOR
                _unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                _currentActivity = _unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
#endif
            }
#endif
        }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroyRelease() { }

        public void OnGui() { }
    }
}
