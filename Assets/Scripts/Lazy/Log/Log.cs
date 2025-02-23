using System.Text;
using Lazy.Pool;
using UnityEngine;

namespace Lazy.Log
{
    public static class Log
    {
        private static readonly Color Rosewater = new(244f / 255f, 219f / 255f, 214f / 255f, 1);
        private static readonly Color Flamingo = new(240f / 255f, 198f / 255f, 198f / 255f, 1);
        private static readonly Color Pink = new(245f / 255f, 189f / 255f, 230f / 255f, 1);
        private static readonly Color Mauve = new(198f / 255f, 160f / 255f, 246f / 255f, 1);
        private static readonly Color Red = new(237f / 255f, 135f / 255f, 150f / 255f, 1);
        private static readonly Color Maroon = new(238f / 255f, 153f / 255f, 160f / 255f, 1);
        private static readonly Color Peach = new(245f / 255f, 169f / 255f, 127f / 255f, 1);
        private static readonly Color Yellow = new(238f / 255f, 212f / 255f, 159f / 255f, 1);
        private static readonly Color Green = new(166f / 255f, 218f / 255f, 149f / 255f, 1);
        private static readonly Color Teal = new(139f / 255f, 213f / 255f, 202f / 255f, 1);
        private static readonly Color Sky = new(145f / 255f, 215f / 255f, 227f / 255f, 1);
        private static readonly Color Sapphire = new(125f / 255f, 196f / 255f, 228f / 255f, 1);
        private static readonly Color Blue = new(138f / 255f, 173f / 255f, 244f / 255f, 1);
        private static readonly Color Lavender = new(183f / 255f, 189f / 255f, 248f / 255f, 1);
        private static readonly Color Text = new(202f / 255f, 211f / 255f, 245f / 255f, 1);

        private static LogLevel _logLevel = LogLevel.Error;

        public static void Enable()
        {
            Debug.unityLogger.logEnabled = true;
            Debug.unityLogger.filterLogType =
                LogType.Error | LogType.Assert | LogType.Warning | LogType.Log | LogType.Exception;
        }

        public static void Disable()
        {
            Debug.unityLogger.logEnabled = false;
            Debug.unityLogger.filterLogType = LogType.Error | LogType.Exception;
        }

        public static void SetLogLevel(LogLevel level)
        {
            _logLevel = level;
        }

        public static Logger D(Object context = null)
        {
            var logger = SafeObjectPool<LoggerType>.Instance.Obtain();
            return logger.Debug(context);
        }

        public static Logger E(Object context = null)
        {
            var logger = SafeObjectPool<LoggerType>.Instance.Obtain();
            return logger.Error(context);
        }

        public static Logger W(Object context = null)
        {
            var logger = SafeObjectPool<LoggerType>.Instance.Obtain();
            return logger.Warning(context);
        }

        public static Logger I(Object context = null)
        {
            var logger = SafeObjectPool<LoggerType>.Instance.Obtain();
            return logger.Info(context);
        }

        public static void MsgI(object message, Object context = null)
        {
            I(context).Msg(message);
        }

        public static void MsgD(object message, Object context = null)
        {
            D(context).Msg(message);
        }

        public static void MsgE(object message, Object context = null)
        {
            E(context).Msg(message);
        }

        public static void MsgW(object message, Object context = null)
        {
            W(context).Msg(message);
        }

        public static void VarI(Object context = null, params object[] content)
        {
            var logger = I(context);
            for (int i = 0; i < content.Length; i+=2)
            {
                logger.Var(content[i].ToString(), content[i + 1]);
            }
        }

        internal class LoggerType : IPoolable
        {
            internal OutputType _outputType;

            public Logger Error(Object context = null)
            {
                _outputType = OutputType.Error;
                var logger = SafeObjectPool<Logger>.Instance.Obtain();
                logger.Setup(this, context);
                return logger;
            }

            public Logger Warning(Object context = null)
            {
                _outputType = OutputType.Warning;
                var logger = SafeObjectPool<Logger>.Instance.Obtain();
                logger.Setup(this, context);
                return logger;
            }

            public Logger Debug(Object context = null)
            {
                _outputType = OutputType.Debug;
                var logger = SafeObjectPool<Logger>.Instance.Obtain();
                logger.Setup(this, context);
                return logger;
            }

            public Logger Info(Object context = null)
            {
                _outputType = OutputType.Info;
                var logger = SafeObjectPool<Logger>.Instance.Obtain();
                logger.Setup(this, context);
                return logger;
            }

            public void Reset()
            {
                _outputType = OutputType.Info;
            }
        }

        public class Logger : IPoolable
        {
            private Object _context;

            private LoggerType _loggerType;

            private StringBuilder _sb = new();

            private StringBuilder _preSb = new();

            private Color _varColor;

            private Color _msgColor;

            private bool _filter;

            internal void Setup(LoggerType loggerType, Object context = null)
            {
                _loggerType = loggerType;
                _context = context;
                _filter = Filter(loggerType._outputType);
                _varColor = GetVarColor(loggerType._outputType);
                _msgColor = GetMsgColor(loggerType._outputType);
                _preSb.Append(GetPrefix(loggerType._outputType));
            }

            public Logger Tag(string tagName)
            {
                _preSb.Append($"[<color=#{ColorUtility.ToHtmlStringRGB(Pink)}><i><u>{tagName}</u></i></color>] ");
                return this;
            }

            public Logger Tag(object tagObj)
            {
                var tagName = tagObj.GetType().Name;
                return Tag(tagName);
            }

            public Logger Var(string name, object value)
            {
                name = name.Replace(" ", "_");
                _sb.Append(
                    $"<color=#{ColorUtility.ToHtmlStringRGB(_varColor)}><b>{name}</b></color>=<color=#{ColorUtility.ToHtmlStringRGB(Green)}><i>{value}</i></color> ");
                return this;
            }

            public Logger Msg(object msg)
            {
                _sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(_msgColor)}><u>{msg}</u></color> ");
                return this;
            }

            public Logger Cr()
            {
                _sb.AppendLine();
                return this;
            }

            public Logger Sep()
            {
                _sb.AppendLine();
                _sb.AppendLine(
                    $"<color=#{ColorUtility.ToHtmlStringRGB(Teal)}>----------------------------------------------</color>");
                return this;
            }

            public void Do()
            {
                if (_filter)
                {
                    var content = _preSb.Append(_sb);
                    if (_context == null)
                    {
                        Debug.Log(content.ToString());
                    }
                    else
                    {
                        Debug.Log(content.ToString(), _context);
                    }
                }

                SafeObjectPool<LoggerType>.Instance.Free(_loggerType);
                SafeObjectPool<Logger>.Instance.Free(this);
            }

            private bool Filter(OutputType outputType)
            {
                switch (outputType)
                {
                    case OutputType.Error:
                        return _logLevel >= LogLevel.Error;
                    case OutputType.Debug:
                        return _logLevel >= LogLevel.Debug;
                    case OutputType.Warning:
                        return _logLevel >= LogLevel.Warning;
                    case OutputType.Info:
                        return _logLevel >= LogLevel.Info;
                }

                return false;
            }

            private static Color GetVarColor(OutputType outputType)
            {
                switch (outputType)
                {
                    case OutputType.Error:
                        return Maroon;
                    case OutputType.Debug:
                        return Sky;
                    case OutputType.Warning:
                        return Flamingo;
                    case OutputType.Info:
                        return Rosewater;
                }

                return Text;
            }

            private static Color GetMsgColor(OutputType outputType)
            {
                switch (outputType)
                {
                    case OutputType.Error:
                        return Mauve;
                    case OutputType.Debug:
                        return Sapphire;
                    case OutputType.Warning:
                        return Peach;
                    case OutputType.Info:
                        return Lavender;
                }

                return Text;
            }

            private static string GetPrefix(OutputType outputType)
            {
                switch (outputType)
                {
                    case OutputType.Error:
                        return $"<color=#{ColorUtility.ToHtmlStringRGB(Red)}><b>ERR</b></color> \t";
                    case OutputType.Debug:
                        return $"<color=#{ColorUtility.ToHtmlStringRGB(Blue)}><b>DBG</b></color> \t";
                    case OutputType.Warning:
                        return $"<color=#{ColorUtility.ToHtmlStringRGB(Yellow)}><b>WRN</b></color> \t";
                    case OutputType.Info:
                        return $"<color=#{ColorUtility.ToHtmlStringRGB(Text)}><b>INF</b></color> \t";
                }

                return "NONE";
            }

            public void Reset()
            {
                _context = null;
                _loggerType = null;
                _sb.Clear();
                _preSb.Clear();
            }
        }

        internal enum OutputType
        {
            Info,
            Debug,
            Warning,
            Error,
        }

        public enum LogLevel
        {
            None = -1,
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }
    }
}