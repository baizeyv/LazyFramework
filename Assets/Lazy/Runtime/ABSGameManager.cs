using UnityEngine;

namespace Lazy
{
    public abstract class ABSGameManager : MonoBehaviour
    {
        private void Start()
        {
            AppLauncher.Instance.OnStartGame += OnStart;
            AppLauncher.Instance.OnFocusGame += OnFocus;
            AppLauncher.Instance.OnPauseGame += OnPause;
            AppLauncher.Instance.OnQuitGame += OnQuit;
        }

        protected virtual void OnStart() { }

        protected virtual void OnQuit() { }

        protected virtual void OnFocus(bool focus) { }

        protected virtual void OnPause(bool pause) { }
    }
}
