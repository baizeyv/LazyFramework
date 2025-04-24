using System;
using System.Collections.Generic;
using Lazy;
using UnityEngine;

namespace Lazy
{
    [Serializable]
    public class ConsoleWindow : IDebuggerWindow
    {
        /// <summary>
        /// * 日志队列
        /// </summary>
        private readonly Queue<LogWrapper> _logs = new();

        /// <summary>
        /// * 日志滚动框滚动位置
        /// </summary>
        private Vector2 _logScrollPosition = Vector2.zero;

        /// <summary>
        /// * 日志堆栈滚动框滚动位置
        /// </summary>
        private Vector2 _stackScrollPosition = Vector2.zero;

        /// <summary>
        /// * Info类型的日志数量
        /// </summary>
        private int _infoCount = 0;

        /// <summary>
        /// * Warning类型的日志数量
        /// </summary>
        private int _warningCount = 0;

        /// <summary>
        /// * Error类型的日志数量
        /// </summary>
        private int _errorCount = 0;

        /// <summary>
        /// * Fatal类型的日志数量
        /// </summary>
        private int _fatalCount = 0;

        /// <summary>
        /// * 当前选中的日志包装
        /// </summary>
        private LogWrapper _selectedLog;

        /// <summary>
        /// * 上一次锁定日志滚动框滚动开关
        /// </summary>
        private bool _lastLockScrollFilter = true;

        /// <summary>
        /// * Info日志过滤开关
        /// </summary>
        private bool _lastInfoFilter = true;

        /// <summary>
        /// * Warning日志过滤开关
        /// </summary>
        private bool _lastWarningFilter = true;

        /// <summary>
        /// * Error日志过滤开关
        /// </summary>
        private bool _lastErrorFilter = true;

        /// <summary>
        /// * Fatal日志过滤开关
        /// </summary>
        private bool _lastFatalFilter = true;

        [SerializeField]
        [Tooltip("锁定日志滚动框")]
        private bool lockScroll = true;

        [SerializeField]
        [Tooltip("日志最大数量")]
        private int maxLog = 100;

        [SerializeField]
        [Tooltip("Info日志过滤开关")]
        private bool infoFilter = true;

        [SerializeField]
        [Tooltip("Warning日志过滤开关")]
        private bool warningFilter = true;

        [SerializeField]
        [Tooltip("Error日志过滤开关")]
        private bool errorFilter = true;

        [SerializeField]
        [Tooltip("Fatal日志过滤开关")]
        private bool fatalFilter = true;

        [SerializeField]
        [Tooltip("Info日志颜色")]
        private Color32 infoColor = Color.white;

        [SerializeField]
        [Tooltip("Warning日志颜色")]
        private Color32 warningColor = Color.yellow;

        [SerializeField]
        [Tooltip("Error日志颜色")]
        private Color32 errorColor = Color.red;

        [SerializeField]
        [Tooltip("Fatal日志颜色")]
        private Color32 fatalColor = new Color(0.7f, 0.2f, 0.2f);

        /// <summary>
        /// * Warning日志数量
        /// </summary>
        public int WarningCount => _warningCount;

        /// <summary>
        /// * Error日志数量
        /// </summary>
        public int ErrorCount => _errorCount;

        /// <summary>
        /// * Fatal日志数量
        /// </summary>
        public int FatalCount => _fatalCount;

        private void OnLogMessageReceived(string logMessage, string stackTrack, LogType logType)
        {
            if (logType == LogType.Assert)
                logType = LogType.Error;

            _logs.Enqueue(LogWrapper.Obtain(logType, logMessage, stackTrack));
            while (_logs.Count > maxLog)
                _logs.Dequeue().Free();
        }

        private void Clear()
        {
            _logs.Clear();
        }

        /// <summary>
        /// * 刷新每种日志的数量
        /// </summary>
        public void RefreshCount()
        {
            _infoCount = 0;
            _warningCount = 0;
            _errorCount = 0;
            _fatalCount = 0;
            foreach (var log in _logs)
                switch (log.LOGType)
                {
                    case LogType.Log:
                        _infoCount++;
                        break;
                    case LogType.Warning:
                        _warningCount++;
                        break;
                    case LogType.Error:
                        _errorCount++;
                        break;
                    case LogType.Exception:
                        _fatalCount++;
                        break;
                }
        }

        /// <summary>
        /// * 获取日志文本颜色
        /// </summary>
        /// <param name="logType"></param>
        /// <returns></returns>
        internal Color32 GetLogStringColor(LogType logType)
        {
            Color32 color = logType switch
            {
                LogType.Log => infoColor,
                LogType.Warning => warningColor,
                LogType.Error => errorColor,
                LogType.Exception => fatalColor,
                _ => Color.white,
            };

            return color;
        }

        /// <summary>
        /// * 获取日志文本
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        private string GetLogString(LogWrapper log)
        {
            var color = GetLogStringColor(log.LOGType);
            return TextUtility.Format(
                "<color=#{0}{1}{2}{3}>[{4}][{5}] {6}</color>",
                color.r.ToString("x2"),
                color.g.ToString("x2"),
                color.b.ToString("x2"),
                color.a.ToString("x2"),
                log.LOGTime.ToString("HH:mm:ss.fff"),
                log.LOGFrameCount.ToString(),
                log.LOGMessage
            );
        }

        public void Initialize(params object[] args)
        {
            Application.logMessageReceived += OnLogMessageReceived;
            _lastLockScrollFilter = lockScroll = true;
            _lastInfoFilter = infoFilter = true;
            _lastWarningFilter = warningFilter = true;
            _lastErrorFilter = errorFilter = true;
            _lastFatalFilter = fatalFilter = true;
        }

        public void Shutdown()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            Clear();
        }

        public void OnEnter() { }

        public void OnLeave() { }

        public void OnProcess(float elapsedSeconds, float realElapsedSeconds)
        {
            _lastLockScrollFilter = lockScroll;
            _lastInfoFilter = infoFilter;
            _lastWarningFilter = warningFilter;
            _lastErrorFilter = errorFilter;
            _lastFatalFilter = fatalFilter;
        }

        public void OnDraw()
        {
            RefreshCount();
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Clear All", GUILayout.Width(70f)))
                    Clear();
                lockScroll = GUILayout.Toggle(lockScroll, "Lock Scroll", GUILayout.Width(85f));
                GUILayout.FlexibleSpace();
                infoFilter = GUILayout.Toggle(
                    infoFilter,
                    TextUtility.Format("Info ({0})", _infoCount.ToString()),
                    GUILayout.Width(65f)
                );
                warningFilter = GUILayout.Toggle(
                    warningFilter,
                    TextUtility.Format("Warning ({0})", _warningCount.ToString()),
                    GUILayout.Width(95f)
                );
                errorFilter = GUILayout.Toggle(
                    errorFilter,
                    TextUtility.Format("Error ({0})", _errorCount.ToString()),
                    GUILayout.Width(85f)
                );
                fatalFilter = GUILayout.Toggle(
                    fatalFilter,
                    TextUtility.Format("Fatal ({0})", _fatalCount.ToString()),
                    GUILayout.Width(85f)
                );
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical("box");
            {
                if (lockScroll)
                    // # 日志滚动框锁定滚动
                    _logScrollPosition.y = float.MaxValue;

                _logScrollPosition = GUILayout.BeginScrollView(_logScrollPosition);
                {
                    var selected = false;
                    foreach (var log in _logs)
                    {
                        switch (log.LOGType)
                        {
                            case LogType.Log:
                                if (!infoFilter)
                                    continue;
                                break;
                            case LogType.Warning:
                                if (!warningFilter)
                                    continue;
                                break;
                            case LogType.Error:
                                if (!errorFilter)
                                    continue;
                                break;
                            case LogType.Exception:
                                if (!fatalFilter)
                                    continue;
                                break;
                        }

                        if (GUILayout.Toggle(_selectedLog == log, GetLogString(log)))
                        {
                            selected = true;
                            if (_selectedLog != log)
                            {
                                _selectedLog = log;
                                _stackScrollPosition = Vector2.zero;
                            }
                        }
                    }

                    if (!selected)
                        _selectedLog = null;
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            {
                _stackScrollPosition = GUILayout.BeginScrollView(
                    _stackScrollPosition,
                    GUILayout.Height(100f)
                );
                {
                    if (_selectedLog != null)
                    {
                        var color = GetLogStringColor(_selectedLog.LOGType);
                        if (
                            GUILayout.Button(
                                TextUtility.Format(
                                    "<color=#{0}{1}{2}{3}><b>{4}</b></color>{6}{6}{5}",
                                    color.r.ToString("x2"),
                                    color.g.ToString("x2"),
                                    color.b.ToString("x2"),
                                    color.a.ToString("x2"),
                                    _selectedLog.LOGMessage,
                                    _selectedLog.LOGStackTrack,
                                    Environment.NewLine
                                ),
                                "label"
                            )
                        )
                            Lazy.Debugger.CopyToClipboard(
                                TextUtility.Format(
                                    "{0}{2}{2}{1}",
                                    _selectedLog.LOGMessage,
                                    _selectedLog.LOGStackTrack,
                                    Environment.NewLine
                                )
                            );
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }
    }
}
