using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lazy.Manage;
using Lazy.Res;
using Lazy.Runtime.Utility;
using Lazy.Singleton;
using Lazy.Utility;
using UnityEngine;

namespace Lazy.UI
{
    // TODO: 弹窗打开队列
    [MonoSingletonPath("Lazy/UI")]
    public class UIManager : MonoSingleton<UIManager>, IManager
    {
        #region DataStruct

        // ! 是界面就放入panelStack, 是弹窗就放入dialogQueue

        /// <summary>
        /// * 界面栈
        /// </summary>
        private readonly HashStack<IPanel> _panelStack = new();

        /// <summary>
        /// * 弹窗队列
        /// </summary>
        private readonly HashQueue<IDialog> _dialogQueue = new();

        /// <summary>
        /// * 界面表
        /// </summary>
        private readonly PanelTable _table = new();

        /// <summary>
        /// * 正在播放动画的界面
        /// </summary>
        private readonly HashSet<IPanel> _playingTweenPanels = new();

        /// <summary>
        /// * 正在播放动画的弹窗
        /// </summary>
        private readonly HashSet<IDialog> _playingTweenDialogs = new();

        /// <summary>
        /// * 等待隐藏的界面列表
        /// </summary>
        private readonly List<IPanel> _pendingDisable = new();

        /// <summary>
        /// * UI根节点
        /// </summary>
        private UIRoot _root;

        #endregion

        private WaitForEndOfFrame _waitForEndOfFrame = new();

        private UIManager() { }

        #region API

        /// <summary>
        /// * 同步打开指定界面
        /// </summary>
        /// <param name="data"></param>
        /// <param name="prefabName"></param>
        /// <param name="layer"></param>
        /// <param name="openType"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T OpenPanelSync<T>(
            IPanelData data = null,
            string prefabName = null,
            UILayer layer = UILayer.PanelLow,
            PanelOpenType openType = PanelOpenType.Single
        )
            where T : UIPanel
        {
            var key = PanelKey.Obtain();
            key.OpenType = openType;
            key.Layer = layer;
            key.Data = data;
            key.PanelType = typeof(T);
            key.GameObjectName = prefabName;
            var panel = OpenSync(key) as T;
            key.Free();
            if (panel is IDialog)
            {
                // # 入队
                _dialogQueue.Enqueue(panel as IDialog);
                _dialogQueue.Sort(x => x.Order);
            }
            else
            {
                // # 入栈
                _panelStack.Push(panel);
                _panelStack.Sort(x => x.Order);
            }

            return panel;
        }

        /// <summary>
        /// * 异步打开指定界面 (不常用)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="prefabName"></param>
        /// <param name="layer"></param>
        /// <typeparam name="T"></typeparam>
        public void OpenPanelAsync<T>(
            IPanelData data = null,
            string prefabName = null,
            UILayer layer = UILayer.PanelLow
        )
            where T : UIPanel
        {
            CoroutineCenter.StartCoroutine(OpenPanelCoroutine<T>(data, prefabName, layer));
        }

        /// <summary>
        /// * 展示指定界面
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void ShowPanel<T>()
            where T : UIPanel
        {
            var key = PanelKey.Obtain();
            key.PanelType = typeof(T);
            Show(key);
            key.Free();
        }

        /// <summary>
        /// * 隐藏界面 (不可指定隐藏)
        /// </summary>
        public void Hide()
        {
            if (_dialogQueue.Count > 0)
            {
                // # 存在弹窗
                if (_dialogQueue.TryDequeue(out var dialog))
                    dialog.Hide();
            }
            else if (_panelStack.Count > 0)
            {
                // # 不存在弹窗,但存在界面
                if (_panelStack.TryPop(out var panel))
                    panel.Hide();
            }
            else
            {
                Log.Log.W(this).Tag(this).Msg("当前不存在界面以及弹窗,却正在尝试隐藏!").Do();
            }
        }

        /// <summary>
        /// * 关闭界面 (不可指定关闭)
        /// </summary>
        public void Close()
        {
            if (_dialogQueue.Count > 0)
            {
                // # 存在弹窗
                if (_dialogQueue.TryDequeue(out var dialog))
                {
                    dialog.Close(dialog.CloseDestroy);
                    _table.Remove(dialog);
                    dialog.Info.Free();
                    dialog.Info = null;
                }
            }
            else if (_panelStack.Count > 0)
            {
                // # 不存在弹窗,但存在界面
                if (_panelStack.TryPop(out var panel))
                {
                    panel.Close(panel.CloseDestroy);
                    _table.Remove(panel);
                    panel.Info.Free();
                    panel.Info = null;
                }
            }
            else
            {
                Log.Log.W(this).Tag(this).Msg("当前不存在界面以及弹窗,却正在尝试关闭!").Do();
            }
        }

        public void Back()
        {
            // !!!!!!!!!!!!!!!!!!!
            // TODO: !!!!!!!!!!!!!!!!!!!!!
            if (_dialogQueue.Count > 0)
            {
                // # 存在弹窗
                if (_dialogQueue.TryDequeue(out var dialog))
                {
                    // TODO:
                    dialog.Close();
                    _table.Remove(dialog);
                    dialog.Info.Free();
                    dialog.Info = null;
                }
            }
            else if (_panelStack.Count > 0)
            {
                // # 不存在弹窗,但存在界面
                // TODO: 回到上一个Hide的界面
                if (_panelStack.TryPop(out var panel))
                {
                    // TODO:
                    panel.Close();
                    _table.Remove(panel);
                    panel.Info.Free();
                    panel.Info = null;
                }
            }
            else
            {
                Log.Log.W(this).Tag(this).Msg("当前不存在界面以及弹窗,却正在尝试返回!").Do();
            }
        }

        #endregion

        internal void TryDisable(IPanel panel)
        {
            if (panel is IDialog)
            {
                // # 弹窗直接Disable
                panel.Transform.gameObject.SetVisible(false);
            }
            else
            {
                if (_playingTweenPanels.Count > 0)
                {
                    _pendingDisable.Add(panel);
                    return;
                }

                panel.Transform.gameObject.SetVisible(false);
                foreach (var item in _pendingDisable)
                    item.Transform.gameObject.SetVisible(false);
                _pendingDisable.Clear();
            }
        }

        /// <summary>
        /// * 添加正在播放动画的界面
        /// </summary>
        /// <param name="panel"></param>
        internal void AddPlayingTweenPanel(IPanel panel)
        {
            _playingTweenPanels.Add(panel);
        }

        /// <summary>
        /// * 添加正在播放动画的弹窗
        /// </summary>
        /// <param name="dialog"></param>
        internal void AddPlayingTweenDialog(IDialog dialog)
        {
            _playingTweenDialogs.Add(dialog);
        }

        /// <summary>
        /// * 移除正在播放动画的界面
        /// </summary>
        /// <param name="panel"></param>
        internal void RemovePlayingTweenPanel(IPanel panel)
        {
            _playingTweenPanels.Remove(panel);
        }

        /// <summary>
        /// * 移除正在播放动画的弹窗
        /// </summary>
        /// <param name="dialog"></param>
        internal void RemovePlayingTweenDialog(IDialog dialog)
        {
            _playingTweenDialogs.Remove(dialog);
        }

        internal void BlockRaycastState(bool raycastTarget)
        {
            if (!raycastTarget && (_playingTweenPanels.Count > 0 || _playingTweenDialogs.Count > 0))
            {
                // # 有正在播放动画的则必须不可点击 (即入场和退场时不可点击)
                if (!_root.raycastBlocker.raycastTarget)
                    _root.raycastBlocker.raycastTarget = true;
                return;
            }

            _root.raycastBlocker.raycastTarget = raycastTarget;
        }

        private IEnumerator OpenPanelCoroutine<T>(
            IPanelData data = null,
            string prefabName = null,
            UILayer layer = UILayer.PanelLow,
            PanelOpenType openType = PanelOpenType.Single
        )
            where T : UIPanel
        {
            var key = PanelKey.Obtain();
            key.OpenType = openType;
            key.Layer = layer;
            key.Data = data;
            key.PanelType = typeof(T);
            key.GameObjectName = prefabName;
            var loaded = false;
            OpenAsync(
                key,
                panel =>
                {
                    loaded = true;
                    key.Free();
                    if (panel is IDialog)
                    {
                        // # 入队
                        _dialogQueue.Enqueue(panel as IDialog);
                        _dialogQueue.Sort(x => x.Order);
                    }
                    else
                    {
                        // # 入栈
                        _panelStack.Push(panel);
                        _panelStack.Sort(x => x.Order);
                    }
                }
            );
            while (!loaded)
                yield return _waitForEndOfFrame;
        }

        private void OpenAsync(PanelKey key, Action<IPanel> onLoad)
        {
            if (key.OpenType == PanelOpenType.Single)
            {
                // # 只能打开一次
                var panel = _table.Search(key).FirstOrDefault();
                if (panel == null)
                {
                    CreateAsync(key, onLoad.Fire);
                }
                else
                {
                    if (panel.Info != null && panel.Info.Layer != key.Layer)
                        _root.SetLayerOfPanel(key.Layer, panel);
                    panel.Open(key.Data);
                    onLoad.Fire(panel);
                }
            }
            else
            {
                // # 可以打开多次
                CreateAsync(key, onLoad.Fire);
            }
        }

        private IPanel OpenSync(PanelKey key)
        {
            if (key.OpenType == PanelOpenType.Single)
            {
                // # 只能打开一次
                var panel = _table.Search(key).FirstOrDefault();
                if (panel == null)
                    panel = CreateSync(key);
                else
                    panel.Open(key.Data);

                if (panel.Info != null && panel.Info.Layer != key.Layer)
                    _root.SetLayerOfPanel(key.Layer, panel);
                return panel;
            }
            else
            {
                // # 可以打开多次
                var panel = _table
                    .Search(key)
                    .FirstOrDefault(item => item.State == PanelState.Closed);
                if (panel == null)
                    panel = CreateSync(key);
                else
                    panel.Open(key.Data);
                if (panel.Info != null && panel.Info.Layer != key.Layer)
                    _root.SetLayerOfPanel(key.Layer, panel);
                return panel;
            }
        }

        private void CreateAsync(PanelKey key, Action<IPanel> onPanelCreate)
        {
            LoadPanelAsync(
                key,
                panel =>
                {
                    _root.SetLayerOfPanel(key.Layer, panel);
                    SetDefaultSizeOfPanel(panel);
                    panel.Transform.gameObject.name = string.IsNullOrEmpty(key.GameObjectName)
                        ? key.PanelType.Name
                        : key.GameObjectName;
                    panel.Info = PanelInfo.Obtain(key.Layer, key.Data, key.PanelType);
                    _table.Add(panel);
                    panel.Setup(key.Data);
                    onPanelCreate.Fire(panel);
                }
            );
        }

        private IPanel CreateSync(PanelKey key)
        {
            var panel = LoadPanelSync(key);
            _root.SetLayerOfPanel(key.Layer, panel);
            SetDefaultSizeOfPanel(panel);
            panel.Transform.gameObject.name = string.IsNullOrEmpty(key.GameObjectName)
                ? key.PanelType.Name
                : key.GameObjectName;
            panel.Info = PanelInfo.Obtain(key.Layer, key.Data, key.PanelType);
            _table.Add(panel);
            panel.Setup(key.Data);
            return panel;
        }

        private void LoadPanelAsync(PanelKey key, Action<IPanel> onPanelLoad)
        {
            LoadPanelPrefabAsync(
                key,
                prefab =>
                {
                    var panel = Instantiate(prefab);
                    var script = panel.GetComponent<UIPanel>();
                    onPanelLoad.Fire(script);
                }
            );
        }

        private IPanel LoadPanelSync(PanelKey key)
        {
            var prefab = LoadPanelPrefabSync(key);
            var panel = Instantiate(prefab);
            var script = panel.GetComponent<UIPanel>();
            return script;
        }

        private void LoadPanelPrefabAsync(PanelKey key, Action<GameObject> onPanelLoad)
        {
            AssetManager.Instance.LoadAsync<GameObject>(
                key.PanelType != null
                    ?
                    // # 有类型
                    key.PanelType.Name
                    : key.GameObjectName,
                x => onPanelLoad(x)
            );
        }

        private GameObject LoadPanelPrefabSync(PanelKey key)
        {
            return AssetManager.Instance.LoadSync<GameObject>(
                key.PanelType != null
                    ?
                    // # 有类型
                    key.PanelType.Name
                    : key.GameObjectName
            );
        }

        private void Show(PanelKey key)
        {
            var panel = _table.Search(key).FirstOrDefault();
            if (panel is IDialog)
            {
                // # 入队
                _dialogQueue.Enqueue(panel as IDialog);
                _dialogQueue.Sort(x => x.Order);
            }
            else
            {
                // # 入栈
                _panelStack.Push(panel);
                _panelStack.Sort(x => x.Order);
            }

            panel?.Show();
        }

        private void SetDefaultSizeOfPanel(IPanel panel)
        {
            var panelTrans = panel.Transform as RectTransform;
            if (panelTrans == null)
                return;
            panelTrans.offsetMin = Vector2.zero;
            panelTrans.offsetMax = Vector2.zero;
            panelTrans.anchoredPosition3D = Vector3.zero;
            panelTrans.anchorMin = Vector2.zero;
            panelTrans.anchorMax = Vector2.one;
            panelTrans.localScale = Vector3.one;
        }

        public override void OnSingletonInitialize()
        {
            _root = UIRoot.Instance;
            AppLauncher.Instance.OnQuitGame += () =>
            {
                _table.Clear();
                _panelStack.Clear();
                _dialogQueue.Clear();
                _playingTweenPanels.Clear();
                _playingTweenDialogs.Clear();
            };
        }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroyRelease() { }

        public void OnGui() { }
    }
}
