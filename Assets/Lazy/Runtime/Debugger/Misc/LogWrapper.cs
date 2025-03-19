using System;
using Lazy.Ref;
using Lazy.Timer;
using UnityEngine;

namespace Lazy.Debugger.Misc
{
    /// <summary>
    /// * Unity日志记录包装器
    /// </summary>
    public sealed class LogWrapper : IReference
    {
        /// <summary>
        /// * 日志时间
        /// </summary>
        public DateTimeOffset LOGTime { get; private set; }

        /// <summary>
        /// * 日志帧计数器
        /// </summary>
        public int LOGFrameCount { get; private set; }

        /// <summary>
        /// * 日志类型
        /// </summary>
        public LogType LOGType { get; private set; }

        /// <summary>
        /// * 日志信息
        /// </summary>
        public string LOGMessage { get; private set; }

        /// <summary>
        /// * 日志堆栈信息
        /// </summary>
        public string LOGStackTrack { get; private set; }

        public LogWrapper()
        {
            LOGTime = default;
            LOGFrameCount = 0;
            LOGType = LogType.Error;
            LOGMessage = string.Empty;
            LOGStackTrack = string.Empty;
        }

        public static LogWrapper Obtain(LogType logType, string logMessage, string stackTrack)
        {
            var wrapper = ReferencePool.Instance.Obtain<LogWrapper>();
            TimerManager.Instance.GetLocalTime(out _, out var time);
            wrapper.LOGTime = time;
            wrapper.LOGFrameCount = Time.frameCount;
            wrapper.LOGType = logType;
            wrapper.LOGMessage = logMessage;
            wrapper.LOGStackTrack = stackTrack;
            return wrapper;
        }

        public void Free()
        {
            ReferencePool.Instance.Free(this);
        }

        public void Clear()
        {
            LOGTime = default;
            LOGFrameCount = 0;
            LOGType = LogType.Error;
            LOGMessage = string.Empty;
            LOGStackTrack = string.Empty;
        }
    }
}
