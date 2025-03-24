using System;
using Lazy.Res;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lazy
{
    public sealed class UIRoot : MonoBehaviour
    {
        /// <summary>
        /// * UI 层级的专用相机
        /// </summary>
        public Camera uiCamera;

        /// <summary>
        /// * Event System
        /// </summary>
        public EventSystem eventSystem;

        /// <summary>
        /// * 根画布
        /// </summary>
        public Canvas canvas;

        /// <summary>
        /// * Canvas Scaler
        /// </summary>
        public CanvasScaler canvasScaler;

        /// <summary>
        /// * 背景层 (沿用根Root层级界面使用)
        /// </summary>
        public RectTransform backgroundLayer;

        /// <summary>
        /// * 低界面层 (沿用根Root层级界面使用)
        /// </summary>
        public RectTransform panelLowLayer;

        /// <summary>
        /// * 高界面层 (沿用根Root层级界面使用)
        /// </summary>
        public RectTransform panelHighLayer;

        /// <summary>
        /// * 弹窗层 (沿用根Root层级界面使用)
        /// </summary>
        public RectTransform dialogLayer;

        /// <summary>
        /// * 引导层 (沿用根Root层级界面使用)
        /// </summary>
        public RectTransform guideLayer;

        /// <summary>
        /// * Toast层 (沿用根Root层级界面使用)
        /// </summary>
        public RectTransform toastLayer;

        /// <summary>
        /// * 存在覆盖根canvas order in layer的界面使用的层级
        /// </summary>
        public RectTransform canvasLayer;

        /// <summary>
        /// * 射线检测阻止器
        /// </summary>
        public Empty4Raycast raycastBlocker;

        /// <summary>
        /// * Debugger射线检测阻止器
        /// </summary>
        public Empty4Raycast debuggerRaycastBlocker;

        private static UIRoot _instance;

        public static UIRoot Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                var prefab = AssetManager.Instance.LoadSync<GameObject>("UIRoot");
                var root = Instantiate(prefab);
                root.name = "Lazy-UIRoot";
                _instance = root.GetComponent<UIRoot>();
                DontDestroyOnLoad(root);
                return _instance;
            }
        }

        public void SetLayerOfPanel(UILayer layer, IPanel panel)
        {
            var panelCanvas = panel.Transform.GetComponent<Canvas>();
            if (panelCanvas.overrideSorting)
            {
                if (panel is IDialog)
                    // # 弹窗
                    panel.Transform.SetParent(dialogLayer);
                else
                    // # 覆盖了父Canvas的层级
                    panel.Transform.SetParent(canvasLayer);
            }
            else
            {
                switch (layer)
                {
                    case UILayer.Background:
                        panel.Transform.SetParent(backgroundLayer);
                        break;
                    case UILayer.PanelLow:
                        panel.Transform.SetParent(panelLowLayer);
                        break;
                    case UILayer.PanelHigh:
                        panel.Transform.SetParent(panelHighLayer);
                        break;
                    case UILayer.Guide:
                        panel.Transform.SetParent(guideLayer);
                        break;
                    case UILayer.Toast:
                        panel.Transform.SetParent(toastLayer);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
                }
            }

            if (panel.Info != null && panel.Info.Layer != layer)
                panel.Info.Layer = layer;
            // # 作为最上边的界面或弹窗
            panel.Transform.SetAsLastSibling();
        }
    }
}
