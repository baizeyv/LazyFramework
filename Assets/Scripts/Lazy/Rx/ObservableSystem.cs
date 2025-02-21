using System;
using UnityEngine;

namespace Lazy.Rx
{
    public static class ObservableSystem
    {
        private static Action<Exception> unhandledException = DefaultUnhandledExceptionHandler;

        public static Action<Exception> GetUnhandledExceptionHandler()
        {
            return unhandledException;
        }

        /// <summary>
        /// * 默认的未处理的异常的处理器
        /// </summary>
        /// <param name="exception"></param>
        static void DefaultUnhandledExceptionHandler(Exception exception)
        {
            Debug.Log($"RX UnhandleException: {exception}");
        }
    }
}