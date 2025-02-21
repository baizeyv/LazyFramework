using System;
using Lazy.Utility;
using UnityEngine;

namespace Lazy.UI
{
    [RequireComponent(typeof(DOTweenSequence))]
    public class UCPanel<T, U> : MonoBehaviour, IPanel where T : Enum where U : class, IPanelData, new()
    {
        /// <summary>
        /// * 界面数据
        /// </summary>
        protected U Data;

        /// <summary>
        /// * 显示状态 (结合状态机进行使用)
        /// </summary>
        protected T DisplayState;

        public void Setup(IPanelData panelData)
        {
            Data = panelData as U ?? new U();
            OnSetup(panelData);
        }

        public void Open(IPanelData panelData)
        {
            Data = panelData as U ?? new U();
            OnOpen(panelData);
        }

        public void Open()
        {
            OnOpen(null);
        }

        public void Close()
        {
            OnClose();
        }

        public void Show()
        {
            OnShow();
        }

        public void Hide()
        {
            OnHide();
        }

        protected virtual void OnSetup(IPanelData panelData)
        {
        }

        protected virtual void OnOpen(IPanelData panelData)
        {
        }

        protected virtual void OnClose()
        {
        }

        protected virtual void OnShow()
        {
        }

        protected virtual void OnHide()
        {
        }

        protected virtual void OnTweenBegin(PanelTweenType tweenType)
        {
        }

        protected virtual void OnTweenEnd(PanelTweenType tweenType)
        {
        }
    }

    public enum PanelTweenType
    {
        In,
        Out
    }
}