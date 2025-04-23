using System;
using DG.Tweening;
using Lazy;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lazy
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    [RequireComponent(typeof(CanvasRenderer))]
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

        protected IPanelData PanelData;

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
            PanelData = panelData;
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

            OnSetup();
            Open(PanelData);
        }

        public void Open(IPanelData panelData = null)
        {
            PanelData = panelData;
            OnOpen();
            Show();
        }

        public override void Close(bool destroy = true)
        {
            Info.Data = PanelData;
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
            gameObject.SetVisible(true);
            State = PanelState.ShowAnimation;
            var showTween = ShowTweenSequence.DOPlay();
            showTween.OnComplete(() =>
            {
                if (this is IDialog)
                    UIManager.Instance.RemovePlayingTweenDialog(this as IDialog);
                else
                    UIManager.Instance.RemovePlayingTweenPanel(this);
                UIManager.Instance.BlockRaycastState(false);
                OnShowTweenEnd();
            });
            UIManager.Instance.BlockRaycastState(true);
            if (this is IDialog)
                UIManager.Instance.AddPlayingTweenDialog(this as IDialog);
            else
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
            if (this is IDialog)
                UIManager.Instance.AddPlayingTweenDialog(this as IDialog);
            else
                UIManager.Instance.AddPlayingTweenPanel(this);
            OnEndTweenBegin();
            hideTween.OnComplete(() =>
            {
                if (this is IDialog)
                    UIManager.Instance.RemovePlayingTweenDialog(this as IDialog);
                else
                    UIManager.Instance.RemovePlayingTweenPanel(this);
                UIManager.Instance.BlockRaycastState(false);
                base.Hide();
                HideCallback.Fire();
                // # 清空事件
                HideCallback = null;
                if (this is IDialog)
                {
                    ((IDialog)this).Callback.Fire();
                    ((IDialog)this).Callback = null;
                }
            });
        }

        protected virtual void OnSetup() { }

        protected virtual void OnOpen() { }

        protected override void OnShow() { }

        protected override void OnHide() { }

        protected override void OnClose() { }

        protected override void OnBack() { }

        protected virtual void OnEndTweenBegin() { }

        protected virtual void OnShowTweenEnd() { }

        protected sealed override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected sealed override void OnBeforeDestroy()
        {
            OnUIDestroy();
            ClearUIComponents();
        }

        protected virtual void OnUIDestroy() { }

        protected virtual void ClearUIComponents() { }

        public PanelState State { get; set; }
        public PanelInfo Info { get; set; }
        public Transform Transform => transform;

        public int Order
        {
            get
            {
                var canvas = Canvas ?? GetComponent<Canvas>();
                if (!canvas.overrideSorting)
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
                            EditorApplication.delayCall += () =>
                            {
                                if (seqArray[i1] != null)
                                    DestroyImmediate(seqArray[i1], true);
                            };
                        }

                        break;
                    }
                }

                EditorApplication.delayCall += () =>
                {
                    while (seqArray.Length < 2)
                        try
                        {
                            gameObject.AddComponent<DOTweenSequence>();
                            seqArray = GetComponents<DOTweenSequence>();
                        }
                        catch (Exception)
                        {
                            break;
                        }
                };
            }
        }
#endif
    }
}
