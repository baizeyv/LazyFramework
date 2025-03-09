using System;

namespace Lazy.Utility
{
    public static class URLUtility
    {
        /// <summary>
        /// * 是否是合法的HTTP URI
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static bool IsLegalHttpUri(string uri)
        {
            return !string.IsNullOrEmpty(uri) && (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsLegalUri(string uri)
        {
            return !string.IsNullOrEmpty(uri) && uri.Contains("://");
        }
    }
}