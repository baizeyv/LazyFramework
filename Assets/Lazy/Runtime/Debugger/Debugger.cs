using System;
using System.Collections.Generic;
using System.Linq;
using Lazy;
using Lazy.Debugger.Misc;
using Lazy.Debugger.Module;
using Lazy.Runtime.Utility;
using Lazy.Singleton;
using UnityEngine;

namespace Lazy.Debugger
{
    [ManagerUpdate]
    [ManagerGUI]
    [MonoSingletonPath("Lazy/Debugger")]
    public class Debugger : MonoSingleton<Debugger>, IManager
    {
        private static TextEditor _textEditor;

        [SerializeField]
        private GUISkin customSkin;

        [SerializeField]
        private DebuggerShowType showType = DebuggerShowType.Icon;

        /// <summary>
        /// * 自定义窗口缩放比例
        /// </summary>
        public static float CustomWindowScale = Screen.width / 1080f * 2f;

        /// <summary>
        /// * Small Debug Icon大小
        /// </summary>
        private Rect customIconRect = new(10, 100, 60, 120);

        /// <summary>
        /// * Large Debug 窗口大小
        /// </summary>
        private Rect customWindowRect = new(8, 100, 525, 830);

        /// <summary>
        /// * Large Debug 界面一行最多几个按钮
        /// </summary>
        public int maxButtonsPerRow = 5;

        /// <summary>
        /// * Information模式下当前工具栏的index
        /// </summary>
        private int _toolbarIndex;

        /// <summary>
        /// * Information模式下当前选择的工具栏index
        /// </summary>
        private int _selectIndex;

        /// <summary>
        /// * Information模式下当前选择的窗口
        /// </summary>
        private IDebuggerWindow _selectedWindow;

        [SerializeField]
        private FPSCounter fpsCounter;

        [SerializeField]
        private ConsoleWindow consoleWindow = new();

        [SerializeField]
        private EnvironmentWindow environmentWindow = new();

        [SerializeField]
        private SystemWindow systemWindow = new();

        [SerializeField]
        private ScreenWindow screenWindow = new();

        [SerializeField]
        private ProfilerWindow profilerWindow = new();

        private CheatWindowBase _cheatWindow;

        private Dictionary<int, WindowWrapper> _windows = new();

        /// <summary>
        /// * 上一次的热区控制
        /// </summary>
        private int _lastHotControl;

        private Debugger() { }

        /// <summary>
        /// * 添加窗口
        /// </summary>
        /// <param name="windowName"></param>
        /// <param name="window"></param>
        public void AddWindow(string windowName, IDebuggerWindow window)
        {
            var id = _windows.Values.Count;
            _windows.Add(id, new WindowWrapper { WindowName = windowName, Window = window });
        }

        /// <summary>
        /// * 设置作弊窗口
        /// </summary>
        /// <param name="window"></param>
        /// <param name="args"></param>
        public void SetCheatWindow(CheatWindowBase window, params object[] args)
        {
            _cheatWindow = window;
            _cheatWindow.Initialize(args);
        }

        public override void OnSingletonInitialize()
        {
            fpsCounter = new FPSCounter(0.5f);
            AppLauncher.Instance.OnPauseGame += _ =>
            {
                fpsCounter?.Reset();
            };
            AppLauncher.Instance.OnStartGame += () =>
            {
                Log.Log.SepD(this);
            };
            consoleWindow.Initialize();
            _textEditor = new TextEditor();

            _windows.Add(0, new WindowWrapper { WindowName = "Console", Window = consoleWindow });
            _windows.Add(
                1,
                new WindowWrapper { WindowName = "Environment", Window = environmentWindow }
            );
            _windows.Add(2, new WindowWrapper { WindowName = "System", Window = systemWindow });
            _windows.Add(3, new WindowWrapper { WindowName = "Screen", Window = screenWindow });
            _windows.Add(4, new WindowWrapper { WindowName = "Profiler", Window = profilerWindow });
        }

        public void OnUpdate()
        {
            _selectedWindow?.OnProcess(Time.deltaTime, Time.unscaledTime);
            fpsCounter.Update(Time.unscaledDeltaTime);

            if (showType == DebuggerShowType.Icon)
            {
                _lastHotControl = 1;
                return;
            }

            if (showType == DebuggerShowType.Cheat)
                _cheatWindow?.OnProcess(Time.deltaTime, Time.unscaledTime);

            var curHotControl = GUIUtility.hotControl;
            if (curHotControl != _lastHotControl)
            {
                if (curHotControl != 0)
                    UIRoot.Instance.debuggerRaycastBlocker.raycastTarget = true;
                else
                    UIRoot.Instance.debuggerRaycastBlocker.raycastTarget = false;
            }
        }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroyRelease() { }

        public void OnGui()
        {
            var cachedGuiSkin = GUI.skin;
            var cachedMatrix = GUI.matrix;

            GUI.skin = customSkin;
            GUI.matrix = Matrix4x4.Scale(new Vector3(CustomWindowScale, CustomWindowScale, 1));
            switch (showType)
            {
                case DebuggerShowType.Icon:
                    customIconRect = GUILayout.Window(
                        0,
                        customIconRect,
                        DrawDebuggerIcon,
                        "<b>DEBUGGER</b>"
                    );
                    break;
                case DebuggerShowType.Information:
                    customWindowRect = GUILayout.Window(
                        0,
                        customWindowRect,
                        DrawDebuggerInformation,
                        "<b>LAZY INFORMATION DEBUGGER</b>"
                    );
                    break;
                case DebuggerShowType.Cheat:
                    customWindowRect = GUILayout.Window(
                        0,
                        customWindowRect,
                        DrawDebuggerCheat,
                        "<b>LAZY CHEAT DEBUGGER</b>"
                    );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            GUI.matrix = cachedMatrix;
            GUI.skin = cachedGuiSkin;
        }

        private void DrawDebuggerIcon(int windowId)
        {
            GUILayout.Space(5);
            Color32 color = Color.white;

            consoleWindow.RefreshCount();
            if (consoleWindow.FatalCount > 0)
                color = consoleWindow.GetLogStringColor(LogType.Exception);
            else if (consoleWindow.ErrorCount > 0)
                color = consoleWindow.GetLogStringColor(LogType.Error);
            else if (consoleWindow.WarningCount > 0)
                color = consoleWindow.GetLogStringColor(LogType.Warning);
            else
                color = consoleWindow.GetLogStringColor(LogType.Log);

            var fpsText = TextUtility.Format(
                "<color=#{0}{1}{2}{3}><b>FPS: {4}</b></color>",
                color.r.ToString("x2"),
                color.g.ToString("x2"),
                color.b.ToString("x2"),
                color.a.ToString("x2"),
                fpsCounter.CurrentFPS.ToString("F2")
            );

            if (GUILayout.Button(fpsText, GUILayout.Width(100f), GUILayout.Height(40f)))
            {
                showType = DebuggerShowType.Information;
                _selectedWindow = consoleWindow;
                _selectedWindow.OnEnter();
                UIRoot.Instance.debuggerRaycastBlocker.raycastTarget = true;
            }

            if (
                GUILayout.Button(
                    "<color=#ecb0c1><b>CHEAT</b></color>",
                    GUILayout.Width(100f),
                    GUILayout.Height(40f)
                )
            )
            {
                UIRoot.Instance.debuggerRaycastBlocker.raycastTarget = true;
                if (_cheatWindow != null)
                {
                    showType = DebuggerShowType.Cheat;
                    _cheatWindow.OnEnter();
                }
            }

            GUI.DragWindow();
        }

        private void DrawDebuggerInformation(int windowId)
        {
            var names = _windows
                .Select(item => TextUtility.Format("<b>{0}</b>", item.Value.WindowName))
                .ToList();
            names.Add("<color=#c8161d><b>Close ✖</b></color>");
            GUILayout.BeginVertical();
            {
                for (var i = 0; i < names.Count; i += maxButtonsPerRow)
                {
                    GUILayout.BeginHorizontal();
                    {
                        for (var j = i; j < i + maxButtonsPerRow && j < names.Count; j++)
                            if (GUILayout.Toggle(_toolbarIndex == j, names[j], "Button"))
                                _toolbarIndex = j;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            if (_toolbarIndex >= _windows.Values.Count)
            {
                // # 回到Icon模式
                showType = DebuggerShowType.Icon;
                _selectedWindow.OnLeave();
                _selectedWindow = null;
                _selectIndex = 0;
                _toolbarIndex = 0;
                return;
            }

            if (_selectedWindow == null)
                return;

            if (_selectIndex != _toolbarIndex)
            {
                _selectedWindow.OnLeave();
                _selectIndex = _toolbarIndex;
                if (_windows.TryGetValue(_selectIndex, out var window))
                    _selectedWindow = window.Window;
                _selectedWindow?.OnEnter();
            }

            _selectedWindow?.OnDraw();

            GUI.DragWindow();
        }

        private void DrawDebuggerCheat(int windowId)
        {
            _cheatWindow?.OnDraw();
            GUI.DragWindow();
        }

        public void SetShowType(DebuggerShowType type)
        {
            showType = type;
        }

        public static void CopyToClipboard(string text)
        {
            if (_textEditor == null)
                return;
            _textEditor.text = text;
            _textEditor.OnFocus();
            _textEditor.Copy();
            _textEditor.text = string.Empty;
        }
    }
}
