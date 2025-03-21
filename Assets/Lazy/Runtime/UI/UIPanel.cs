using System;
using DG.Tweening;
using Lazy.UI.Common;
using Lazy.Utility;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lazy.UI
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    [RequireComponent(typeof(Empty4Raycast))] // # 防止界面点击穿透
    [RequireComponent(typeof(DOTweenSequence))]
    public abstract class UIPanel : UIMonoBehaviour, IPanel
    {
        [Tooltip("隐藏动画是否倒放入场动画")]
        [SerializeField]
        private bool hideAnimationIsRewind;

        [Tooltip("关闭时是否Destroy")]
        [SerializeField]
        private bool closeDestroy = true;

        protected IPanelData Data;

        protected DOTweenSequence ShowTweenSequence;

        protected DOTweenSequence HideTweenSequence;

        /// <summary>
        /// * 退场动画完成回调
        /// </summary>
        protected event Action HideCallback;

        protected Canvas Canvas;

        public virtual void Setup(IPanelData panelData)
        {
            Canvas = GetComponent<Canvas>();
            Data = panelData;
            State = PanelState.Idle;
            if (hideAnimationIsRewind)
            {
                ShowTweenSequence = HideTweenSequence = GetComponent<DOTweenSequence>();
            }
            else
            {
                var list = GetComponents<DOTweenSequence>();
                ShowTweenSequence = list[0];
                HideTweenSequence = list[1];
            }

            ShowTweenSequence.SetNoPlayOnAwake();
            HideTweenSequence.SetNoPlayOnAwake();

            OnSetup(Data);
            Open(Data);
        }

        public void Open(IPanelData panelData = null)
        {
            OnOpen(panelData);
            Show();
        }

        public override void Close(bool destroy = true)
        {
            Info.Data = Data;
            Hide();
            HideCallback += () =>
            {
                base.Close(destroy);
                if (destroy)
                    Destroy(gameObject);
            };
        }

        public override void Show()
        {
            State = PanelState.ShowAnimation;
            var showTween = ShowTweenSequence.DOPlay();
            showTween.OnComplete(() =>
            {
                UIManager.Instance.RemovePlayingTweenPanel(this);
                UIManager.Instance.BlockRaycastState(false);
                OnShowTweenEnd();
            });
            UIManager.Instance.BlockRaycastState(true);
            UIManager.Instance.AddPlayingTweenPanel(this);
            base.Show();
        }

        public override void Hide()
        {
            HideCallback = null;
            State = PanelState.HideAnimation;
            var hideTween = hideAnimationIsRewind
                ? HideTweenSequence.DORewind()
                : HideTweenSequence.DOPlay();
            UIManager.Instance.BlockRaycastState(true);
            UIManager.Instance.AddPlayingTweenPanel(this);
            OnEndTweenBegin();
            hideTween.OnComplete(() =>
            {
                UIManager.Instance.RemovePlayingTweenPanel(this);
                UIManager.Instance.BlockRaycastState(false);
                base.Hide();
                HideCallback.Fire();
                // # 清空事件
                HideCallback = null;
            });
        }

        protected virtual void OnSetup(IPanelData panelData = null) { }

        protected virtual void OnOpen(IPanelData panelData = null) { }

        protected override void OnShow() { }

        protected override void OnHide() { }

        protected override void OnClose() { }

        protected override void OnBack() { }

        protected virtual void OnEndTweenBegin() { }

        protected virtual void OnShowTweenEnd() { }

        public PanelState State { get; set; }
        public PanelInfo Info { get; set; }
        public Transform Transform => transform;

        public int Order
        {
            get
            {
                var canvas = Canvas ?? GetComponent<Canvas>();
                if (!Canvas.overrideSorting)
                    return 0;
                return canvas.sortingOrder;
            }
        }

        public bool CloseDestroy => closeDestroy;

#if UNITY_EDITOR
        protected void OnValidate()
        {
            var seqArray = GetComponents<DOTweenSequence>();
            if (hideAnimationIsRewind)
            {
                if (seqArray.Length <= 1)
                    return;
                for (var i = 1; i < seqArray.Length; i++)
                {
                    var i1 = i;
                    EditorApplication.delayCall += () => DestroyImmediate(seqArray[i1]);
                }
            }
            else
            {
                switch (seqArray.Length)
                {
                    case 2:
                        return;
                    case > 2:
                    {
                        for (var i = 2; i < seqArray.Length; i++)
                        {
                            var i1 = i;
                            EditorApplication.delayCall += () => DestroyImmediate(seqArray[i1]);
                        }

                        break;
                    }
                }

                EditorApplication.delayCall += () =>
                {
                    while (seqArray.Length < 2)
                    {
                        gameObject.AddComponent<DOTweenSequence>();
                        seqArray = GetComponents<DOTweenSequence>();
                    }
                };
            }
        }
#endif
    }
}
