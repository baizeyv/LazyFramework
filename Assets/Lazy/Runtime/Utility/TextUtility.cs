using System;
using System.Text;

namespace Lazy.Runtime.Utility
{
    public static class TextUtility
    {
        private const int StringBuilderCapacity = 1024;

        [ThreadStatic]
        private static StringBuilder _cachedStringBuilder;

        public static string Format(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format))
                throw new ArgumentException("Format is invalid");
            if (args == null)
                throw new ArgumentException("Arguments is invalid");
            CheckCachedStringBuilder();
            _cachedStringBuilder.Length = 0;
            _cachedStringBuilder.AppendFormat(format, args);
            return _cachedStringBuilder.ToString();
        }

        private static void CheckCachedStringBuilder()
        {
            if (_cachedStringBuilder != null)
                return;
            _cachedStringBuilder = new StringBuilder(StringBuilderCapacity);
        }
    }
}
