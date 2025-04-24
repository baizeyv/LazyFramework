using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lazy;
using Lazy.Res;
using Lazy.Runtime.Utility;
using Lazy.Singleton;
using UnityEngine;

namespace Lazy
{
    [MonoSingletonPath("Lazy/UI")]
    public class UIManager : MonoSingleton<UIManager>, IManager
    {
        #region DataStruct

        // ! 是界面就放入panelStack, 是弹窗就放入dialogQueue

        /// <summary>
        /// * 已打开界面栈
        /// </summary>
        private readonly HashStack<IPanel> _panelStack = new();

        /// <summary>
        /// * 弹窗等待队列
        /// </summary>
        private readonly Queue<Action> _dialogPendingQueue = new();

        /// <summary>
        /// * 返回的界面栈
        /// </summary>
        private readonly Stack<IPanel> _backStack = new();

        /// <summary>
        /// * 当前显示的dialog
        /// </summary>
        private IDialog _currentDialog;

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
        public void OpenSync<T>(
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
            if (typeof(IDialog).IsAssignableFrom(typeof(T)))
            {
                if (_dialogPendingQueue.Count <= 0 && _currentDialog == null)
                    OpenDialogSync(key);
                else
                    _dialogPendingQueue.Enqueue(() =>
                    {
                        OpenDialogSync(key);
                    });
            }
            else
            {
                var panel = OpenPanelSync(key) as T;
                key.Free();
                // # 入栈
                _panelStack.Push(panel);
                _panelStack.Sort(x => x.Order);
            }
        }

        /// <summary>
        /// * 异步打开指定界面 (不常用)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="prefabName"></param>
        /// <param name="layer"></param>
        /// <param name="openType"></param>
        /// <typeparam name="T"></typeparam>
        public void OpenAsync<T>(
            IPanelData data = null,
            string prefabName = null,
            UILayer layer = UILayer.PanelLow,
            PanelOpenType openType = PanelOpenType.Single
        )
            where T : UIPanel
        {
            if (typeof(IDialog).IsAssignableFrom(typeof(T)))
            {
                Log.Log.MsgE("Dialog 不支持异步打开!");
                return;
            }

            CoroutineCenter.StartCoroutine(
                OpenPanelCoroutine<T>(data, prefabName, layer, openType)
            );
        }

        /// <summary>
        /// * 展示指定界面
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Show<T>()
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
            // # 存在弹窗
            if (_currentDialog != null)
            {
                _currentDialog.Hide();
                _currentDialog.Callback = TryOpenNextDialog;
                _currentDialog = null;
            }
            else if (_panelStack.Count > 0)
            {
                // # 不存在弹窗,但存在界面
                if (_panelStack.TryPop(out var panel))
                {
                    panel.Hide();
                    _backStack.Push(panel);
                }
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
            if (_currentDialog != null)
            {
                // # 存在弹窗
                _currentDialog.Close(_currentDialog.CloseDestroy);
                _currentDialog.Callback = TryOpenNextDialog;
                if (_currentDialog.CloseDestroy)
                {
                    _table.Remove(_currentDialog);
                    _currentDialog.Info.Free();
                    _currentDialog.Info = null;
                }

                _currentDialog = null;
            }
            else if (_panelStack.Count > 0)
            {
                // # 不存在弹窗,但存在界面
                if (_panelStack.TryPop(out var panel))
                {
                    panel.Close(panel.CloseDestroy);
                    if (panel.CloseDestroy)
                    {
                        _table.Remove(panel);
                        panel.Info.Free();
                        panel.Info = null;
                    }
                }

                _backStack.Clear();
            }
            else
            {
                Log.Log.W(this).Tag(this).Msg("当前不存在界面以及弹窗,却正在尝试关闭!").Do();
            }
        }

        /// <summary>
        /// * 界面返回
        /// </summary>
        public void Back()
        {
            if (_currentDialog != null)
            {
                // # 存在弹窗
                _currentDialog.Close(_currentDialog.CloseDestroy);
                _currentDialog.Back();
                _currentDialog.Callback = TryOpenNextDialog;
                if (_currentDialog.CloseDestroy)
                {
                    _table.Remove(_currentDialog);
                    _currentDialog.Info.Free();
                    _currentDialog.Info = null;
                }

                _currentDialog = null;
            }
            else if (_panelStack.Count > 0)
            {
                if (_panelStack.TryPop(out var panel))
                {
                    panel.Close(panel.CloseDestroy);
                    panel.Back();
                    if (panel.CloseDestroy)
                    {
                        _table.Remove(panel);
                        panel.Info.Free();
                        panel.Info = null;
                    }

                    // # 立即打开要返回的界面
                    if (_backStack.TryPop(out var backPanel))
                    {
                        _panelStack.Push(panel);
                        _panelStack.Sort(x => x.Order);
                        backPanel.Show();
                    }
                }
            }
            else
            {
                Log.Log.W(this).Tag(this).Msg("当前不存在界面以及弹窗,却正在尝试返回!").Do();
            }
        }

        #endregion

        private void TryOpenNextDialog()
        {
            if (_dialogPendingQueue.Count <= 0)
                return;
            if (_dialogPendingQueue.TryDequeue(out var task))
                task.Fire();
        }

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
            OpenPanelAsync(
                key,
                panel =>
                {
                    loaded = true;
                    key.Free();
                    // # 入栈
                    _panelStack.Push(panel);
                    _panelStack.Sort(x => x.Order);
                }
            );
            while (!loaded)
                yield return _waitForEndOfFrame;
        }

        private void OpenPanelAsync(PanelKey key, Action<IPanel> onLoad)
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

        /// <summary>
        /// * 同步打开弹窗
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private IDialog OpenDialogSync(PanelKey key)
        {
            if (key.OpenType == PanelOpenType.Single)
            {
                // # 只能打开一次
                var dialog = _table.Search(key).FirstOrDefault() as IDialog;
                if (dialog == null)
                    // # 创建新的dialog
                    dialog = CreateDialogSync(key);
                else
                    dialog.Open(key.Data);

                if (dialog.Info != null && dialog.Info.Layer != key.Layer)
                    _root.SetLayerOfPanel(key.Layer, dialog);
                key.Free();
                _currentDialog = dialog;
                return dialog;
            }
            else
            {
                // # 可以打开多次
                var dialog =
                    _table.Search(key).FirstOrDefault(x => x.State == PanelState.Closed) as IDialog;
                if (dialog == null)
                    dialog = CreateDialogSync(key);
                else
                    dialog.Open(key.Data);

                if (dialog.Info != null && dialog.Info.Layer != key.Layer)
                    _root.SetLayerOfPanel(key.Layer, dialog);
                key.Free();
                _currentDialog = dialog;
                return dialog;
            }
        }

        private IPanel OpenPanelSync(PanelKey key)
        {
            if (key.OpenType == PanelOpenType.Single)
            {
                // # 只能打开一次
                var panel = _table.Search(key).FirstOrDefault();
                if (panel == null)
                    panel = CreatePanelSync(key);
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
                    panel = CreatePanelSync(key);
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

        private IDialog CreateDialogSync(PanelKey key)
        {
            var dialog = LoadPanelSync(key) as IDialog;
            _root.SetLayerOfPanel(key.Layer, dialog);
            SetDefaultSizeOfPanel(dialog);
            dialog.Transform.gameObject.name = string.IsNullOrEmpty(key.GameObjectName)
                ? key.PanelType.Name
                : key.GameObjectName;
            dialog.Info = PanelInfo.Obtain(key.Layer, key.Data, key.PanelType);
            _table.Add(dialog);
            dialog.Setup(key.Data);
            return dialog;
        }

        private IPanel CreatePanelSync(PanelKey key)
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
                if (_currentDialog == null && _dialogPendingQueue.Count <= 0)
                {
                    // # 立即打开弹窗
                    panel.Show();
                    _currentDialog = panel as IDialog;
                }
                else
                {
                    // # 入队
                    _dialogPendingQueue.Enqueue(() =>
                    {
                        panel.Show();
                        _currentDialog = panel as IDialog;
                    });
                }
            }
            else
            {
                // # 入栈
                _panelStack.Push(panel);
                _panelStack.Sort(x => x.Order);
                panel?.Show();
            }
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
                _dialogPendingQueue.Clear();
                _playingTweenPanels.Clear();
                _playingTweenDialogs.Clear();
                _backStack.Clear();
            };
        }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroyRelease() { }

        public void OnGui() { }
    }
}
