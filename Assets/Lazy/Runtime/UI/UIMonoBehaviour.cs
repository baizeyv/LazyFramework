using Lazy.Utility;
using UnityEngine;

namespace Lazy.UI
{
    public class UIMonoBehaviour : MonoBehaviour
    {
        /// <summary>
        /// * 在UI组件中可用于状态切换后的界面刷新,覆写OnShow,调用Show即可
        /// ! 不能直接在Panel或Dialog脚本中调用,应该通过UIManager来调用
        /// </summary>
        public virtual void Show()
        {
            if (this is IPanel)
                (this as IPanel)?.Transform.gameObject.SetVisible(true);
            OnShow();
        }

        protected virtual void OnShow() { }

        /// <summary>
        /// ! 不能直接在Panel或Dialog脚本中调用,应该通过UIManager来调用
        /// </summary>
        public virtual void Hide()
        {
            if (this is IPanel)
            {
                // # 在界面中调用
                UIManager.Instance.TryDisable(this as IPanel);
                OnHide();
            }
            else
            {
                // # UI组件中调用
                UIManager.Instance.Hide();
            }
        }

        protected virtual void OnHide() { }

        /// <summary>
        /// ! 不能直接在Panel或Dialog脚本中调用,应该通过UIManager来调用
        /// </summary>
        public virtual void Close(bool destroy = true)
        {
            if (this is IPanel)
                OnClose();
            else
                UIManager.Instance.Close();
        }

        protected virtual void OnClose() { }

        /// <summary>
        /// ! 不能直接在Panel或Dialog脚本中调用,应该通过UIManager来调用
        /// </summary>
        public virtual void Back()
        {
            if (this is IPanel)
                OnBack();
            else
                UIManager.Instance.Back();
        }

        protected virtual void OnBack() { }
    }
}
